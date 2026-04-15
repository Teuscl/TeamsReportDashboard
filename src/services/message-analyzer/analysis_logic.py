import extract_msg
import hashlib
import os
import pandas as pd
from bs4 import BeautifulSoup
import re
from openai import AsyncOpenAI
import json
import tempfile
from typing import List

from config import settings

# Carrega o cliente Async (token pego via env var automaticamente)
client = AsyncOpenAI(api_key=settings.openai_api_key)

# ==============================================================================
# PROMPT DO SISTEMA PARA ANÁLISE DE ATENDIMENTOS
# ==============================================================================

PROMPT_FILE_PATH = os.path.join(os.path.dirname(__file__), "prompt.txt")
CONVERSATIONS_DIR = os.path.join(os.path.dirname(__file__), "conversations")

def get_analysis_prompt() -> str:
    """Lê o prompt de análise do arquivo prompt.txt."""
    try:
        if not os.path.exists(PROMPT_FILE_PATH):
            return "Prompt file not found."
        with open(PROMPT_FILE_PATH, "r", encoding="utf-8") as f:
            return f.read()
    except Exception as e:
        print(f"Erro ao ler o arquivo de prompt: {e}")
        return "Erro ao carregar o prompt."

# Mantemos a variável para compatibilidade, carregando-a inicialmente
ANALYSIS_PROMPT = get_analysis_prompt()

def save_conversations_to_excel(df: pd.DataFrame, batch_id: str) -> str:
    """Salva o DataFrame de conversas tratadas em um arquivo Excel na pasta conversations."""
    try:
        os.makedirs(CONVERSATIONS_DIR, exist_ok=True)
        filename = f"conversas_processadas_{batch_id}.xlsx"
        file_path = os.path.join(CONVERSATIONS_DIR, filename)
        
        # Exporta para Excel (sem o index do pandas)
        df.to_excel(file_path, index=False)
        return file_path
    except Exception as e:
        print(f"Erro ao salvar o arquivo Excel: {e}")
        return ""

# ==============================================================================
# SEÇÃO 1: FUNÇÕES DE HELPERS PARA LIMPEZA E NORMALIZAÇÃO DE DADOS
# ==============================================================================

def extract_and_clean_email(text_field: str) -> str | None:
    """Extrai e limpa um único e-mail de um campo de texto."""
    if pd.isna(text_field):
        return None

    text_field_str = str(text_field).replace('\x00', '').strip()
    parts = text_field_str.split(';')
    valid_emails_found = []

    for part in parts:
        part = part.strip()
        if not part or ('<none' in part.lower() or 'none>' in part.lower()) and '@' not in part:
            continue

        email_candidate_from_part = None
        match_angle = re.search(r'<([^>]+)>', part)

        if match_angle:
            candidate_in_angle = match_angle.group(1).strip()
            if '@' in candidate_in_angle and 'none' not in candidate_in_angle.lower():
                email_candidate_from_part = candidate_in_angle
        else:
            sub_parts = part.split()
            possible_emails_in_sub_part = [
                sp.strip(',.;:"\'()[]{}') for sp in sub_parts if '@' in sp and '.' in sp
            ]
            if possible_emails_in_sub_part:
                email_candidate_from_part = possible_emails_in_sub_part[-1]

        if email_candidate_from_part:
            cleaned_candidate = email_candidate_from_part.strip().lower()
            if re.fullmatch(r'([a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,})', cleaned_candidate):
                valid_emails_found.append(cleaned_candidate)

    if not valid_emails_found:
        return None

    if settings.help_desk_email in valid_emails_found:
        other_emails = [e for e in valid_emails_found if e != settings.help_desk_email]
        return other_emails[0] if other_emails else settings.help_desk_email
    elif valid_emails_found:
        return valid_emails_found[0]
    return None

def create_normalized_chat_id(row: pd.Series) -> str:
    """Cria um ID de chat normalizado para agrupar conversas."""
    from_email = row.get('From_Email')
    to_email = row.get('To_Email')
    row_name = row.name

    if isinstance(to_email, str) and 'none' in to_email.lower(): to_email = None
    if isinstance(from_email, str) and 'none' in from_email.lower(): from_email = None

    if from_email == settings.help_desk_email and pd.notna(to_email):
        return to_email
    elif pd.notna(from_email) and from_email != settings.help_desk_email:
        return from_email
    
    return f"unknown_chat_{row_name}" # Fallback simplificado


# ==============================================================================
# SEÇÃO 2: FUNÇÕES PRINCIPAIS DO FLUXO DE ANÁLISE
# ==============================================================================

def process_msg_files_to_dataframe(file_paths: List[str]) -> pd.DataFrame:
    """Recebe arquivos .msg, processa com segurança de memória e retorna um DataFrame."""
    messages_data = []
    for file_path in file_paths:
        try:
            with extract_msg.Message(file_path) as msg:
                msg_message_html = msg.htmlBodyPrepared
                if msg_message_html:
                    formatted_message_body = BeautifulSoup(msg_message_html, 'lxml').get_text(separator=' ', strip=True)
                else:
                    formatted_message_body = msg.body.strip() if msg.body else ""

                messages_data.append({
                    'Original_From': msg.sender, 'Original_To': msg.to,
                    'Date': msg.date, 'Message_Body': formatted_message_body,
                    'Source_File': os.path.basename(file_path)
                })
        except Exception as e:
            print(f"Erro ao processar o arquivo {file_path}: {e}")
            continue

    if not messages_data: return pd.DataFrame()

    df = pd.DataFrame(messages_data)
    df['Date'] = pd.to_datetime(df['Date'], errors='coerce', utc=True)
    df.dropna(subset=['Date'], inplace=True)
    df_sorted = df.sort_values(by='Date').reset_index(drop=True)
    
    for col in ['Original_From', 'Original_To', 'Message_Body']:
        if col in df_sorted.columns:
            df_sorted[col] = df_sorted[col].astype(str).str.replace('\x00', '', regex=False)
    
    df_sorted['From_Email'] = df_sorted['Original_From'].apply(extract_and_clean_email)
    df_sorted['To_Email'] = df_sorted['Original_To'].apply(extract_and_clean_email)
    df_sorted['ChatID'] = df_sorted.apply(create_normalized_chat_id, axis=1)

    def format_display_message(row):
        from_cleaned = str(row['Original_From']) if pd.notna(row['Original_From']) else 'Remetente Desconhecido'
        body_cleaned = str(row['Message_Body']) if pd.notna(row['Message_Body']) else ''
        return f"[{row['Date'].strftime('%Y-%m-%d %H:%M:%S %Z')}] {from_cleaned}: {body_cleaned}"

    df_sorted['FormattedMessage'] = df_sorted.apply(format_display_message, axis=1)
    df_valid_client_chats = df_sorted[df_sorted['ChatID'].str.contains('@', na=False) & (df_sorted['ChatID'] != settings.help_desk_email)]

    if df_valid_client_chats.empty: return pd.DataFrame()

    grouped_conversations = df_valid_client_chats.groupby('ChatID')['FormattedMessage'].apply(lambda x: '\n\n'.join(x)).reset_index()
    grouped_conversations.rename(columns={'FormattedMessage': 'ConversationHistory'}, inplace=True)
    
    return grouped_conversations

async def start_openai_batch_job(conversations_df: pd.DataFrame) -> str:
    """Recebe o DataFrame de conversas, cria um trabalho em lote na OpenAI e retorna o ID."""
    tasks = []
    # Recarrega o prompt para garantir que estamos usando a versão mais recente
    current_prompt = get_analysis_prompt()
    
    for index, row in conversations_df.iterrows():
        # custom_id deve ter no máximo 64 chars (limite da OpenAI Batch API).
        # Usamos um hash curto do email para garantir unicidade sem estourar o limite.
        chat_hash = hashlib.sha1(row['ChatID'].encode()).hexdigest()[:12]
        custom_id = f"task-{chat_hash}-{index}"
        task = {
            "custom_id": custom_id, "method": "POST", "url": "/v1/chat/completions",
            "body": {
                "model": "gpt-4o-mini", "temperature": 0.1, "response_format": {"type": "json_object"},
                "messages": [
                    {"role": "system", "content": current_prompt},
                    {"role": "user", "content": f"ChatID do solicitante: {row['ChatID']}\n\nHistórico da Conversa:\n{row['ConversationHistory']}"}
                ],
            }
        }
        tasks.append(task)
    
    with tempfile.NamedTemporaryFile(mode='w', delete=False, suffix=".jsonl", encoding='utf-8') as tmp_file:
        for task in tasks: tmp_file.write(json.dumps(task) + "\n")
        tmp_file_path = tmp_file.name

    try:
        with open(tmp_file_path, "rb") as file_to_upload:
            batch_file = await client.files.create(file=file_to_upload, purpose="batch")
    except Exception as e:
        raise RuntimeError(f"Erro ao enviar o arquivo .jsonl para a OpenAI: {str(e)}")
    finally:
        os.remove(tmp_file_path) 

    batch_job = await client.batches.create(input_file_id=batch_file.id, endpoint="/v1/chat/completions", completion_window="24h")
    
    # Exporta as conversas tratadas para Excel para conferência (ambiente de dev)
    save_conversations_to_excel(conversations_df, batch_job.id)
    
    return batch_job.id

async def get_batch_job_status_and_results(batch_id: str) -> dict:
    """Verifica o status de um trabalho em lote. Se concluído, baixa e retorna os resultados."""
    batch_job = await client.batches.retrieve(batch_id)
    
    response = {
        "batch_id": batch_job.id, "status": batch_job.status, "created_at": batch_job.created_at,
        "completed_at": batch_job.completed_at, "failed_at": batch_job.failed_at,
        "request_counts": batch_job.request_counts, "results": None, "errors": None
    }

    if batch_job.status == "completed":
        if batch_job.output_file_id:
            response_file = await client.files.content(batch_job.output_file_id)
            result_content = response_file.content
            lines = result_content.decode('utf-8').splitlines()
            results_data = []
            for line in lines:
                try:
                    full_json = json.loads(line)
                    analysis_json_str = full_json['response']['body']['choices'][0]['message']['content']
                    results_data.append(json.loads(analysis_json_str))
                except (json.JSONDecodeError, IndexError, KeyError) as e:
                    results_data.append({"error": "Falha ao parsear JSON", "raw_line": line})
            response["results"] = results_data
        
        if batch_job.error_file_id:
            error_file = await client.files.content(batch_job.error_file_id)
            error_content = error_file.content
            response["errors"] = error_content.decode('utf-8')
    
    return response