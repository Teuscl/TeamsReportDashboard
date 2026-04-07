import extract_msg
import os
import pandas as pd
from bs4 import BeautifulSoup
import re
from openai import OpenAI
from dotenv import load_dotenv
import json
import tempfile
from typing import List

# Carrega a chave da API do arquivo .env
load_dotenv()
client = OpenAI()

# Constante para o e-mail do Help Desk
HELP_DESK_NORMALIZED_EMAIL = 'helpdesk@pecege.com'


# ==============================================================================
# SEÇÃO 1: FUNÇÕES DE HELPERS PARA LIMPEZA E NORMALIZAÇÃO DE DADOS
# ==============================================================================

def extract_and_clean_email(text_field: str) -> str or None:
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

    if HELP_DESK_NORMALIZED_EMAIL in valid_emails_found:
        other_emails = [e for e in valid_emails_found if e != HELP_DESK_NORMALIZED_EMAIL]
        return other_emails[0] if other_emails else HELP_DESK_NORMALIZED_EMAIL
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

    if from_email == HELP_DESK_NORMALIZED_EMAIL and pd.notna(to_email):
        return to_email
    elif pd.notna(from_email) and from_email != HELP_DESK_NORMALIZED_EMAIL:
        return from_email
    
    return f"unknown_chat_{row_name}" # Fallback simplificado


# ==============================================================================
# SEÇÃO 2: FUNÇÕES PRINCIPAIS DO FLUXO DE ANÁLISE
# ==============================================================================

def process_msg_files_to_dataframe(uploaded_files: List) -> pd.DataFrame:
    """Recebe arquivos .msg, processa e retorna um DataFrame com conversas agrupadas."""
    messages_data = []
    for uploaded_file in uploaded_files:
        try:
            uploaded_file.file.seek(0)
            msg = extract_msg.Message(uploaded_file.file)

            msg_message_html = msg.htmlBodyPrepared
            if msg_message_html:
                formatted_message_body = BeautifulSoup(msg_message_html, 'lxml').get_text(separator=' ', strip=True)
            else:
                formatted_message_body = msg.body.strip() if msg.body else ""

            messages_data.append({
                'Original_From': msg.sender, 'Original_To': msg.to,
                'Date': msg.date, 'Message_Body': formatted_message_body,
                'Source_File': uploaded_file.filename
            })
        except Exception as e:
            print(f"Erro ao processar o arquivo {uploaded_file.filename}: {e}")
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
    df_valid_client_chats = df_sorted[df_sorted['ChatID'].str.contains('@', na=False) & (df_sorted['ChatID'] != HELP_DESK_NORMALIZED_EMAIL)]

    if df_valid_client_chats.empty: return pd.DataFrame()

    grouped_conversations = df_valid_client_chats.groupby('ChatID')['FormattedMessage'].apply(lambda x: '\n\n'.join(x)).reset_index()
    grouped_conversations.rename(columns={'FormattedMessage': 'ConversationHistory'}, inplace=True)
    
    return grouped_conversations

def start_openai_batch_job(conversations_df: pd.DataFrame) -> str:
    """Recebe o DataFrame de conversas, cria um trabalho em lote na OpenAI e retorna o ID."""
    prompt_base ="""
# CONTEXTO
Você é um analista inteligente de conversas de atendimento técnico. Receberá abaixo uma transcrição de conversa entre um colaborador e o time de Help Desk. Sua tarefa é analisar e identificar **atendimentos reais de suporte**.

# TAREFA PRINCIPAL
### Instruções:
1. **Identifique e separe atendimentos distintos**, baseando-se em: mudança de data; intervalos longos entre mensagens (mais de 60 minutos); novas mensagens que iniciam outro problema (ex: “bom dia, estou com outro problema…”).
2. **Ignore conversas que não representem uma solicitação de suporte técnico**, como: agradecimentos; confirmações de que o problema já foi resolvido; mensagens informativas; elogios sem pedido de ajuda.
3. Considere como atendimento **válido** apenas quando a primeira mensagem da conversa expressar: um pedido de ajuda; um relato de problema; um sintoma ou erro.

# REGRAS ESTRITAS DE EXTRAÇÃO (Para cada atendimento)
4.  Se nenhum atendimento válido for encontrado, retorne uma lista vazia: `{"atendimentos": []}`.
### Regras Estritas para Extração de Dados (Para cada atendimento):
- `quem_solicitou_atendimento`: (String, Obrigatório, Máx 55 chars) Nome completo da pessoa que pediu suporte.
- `email_solicitante`: (String, Obrigatório, E-mail Válido, Máx 100 chars) E-mail da pessoa que pediu suporte.
- `quem_respondeu`: (String, Obrigatório, Máx 50 chars) **O nome de um único atendente, definido pela seguinte lógica:**
    - **Lógica de Seleção**: Analise a conversa cronologicamente e identifique **o PRIMEIRO atendente** que se apresentar (ex: "Mateus aqui", "aqui é o Barbi").
    - **Lista Canônica de Nomes**: O nome identificado deve pertencer **exclusivamente** à lista:
      `["Mateus", "André", "Jofre", "Sandro", "Alexandre", "Barbi", "Clara"]`.
    - **Normalização**: Converta apelidos e variações para o nome canônico da lista (ex: "xande" → "Alexandre"; "MATEUS" → "Mateus").
    - **Retorno**: O valor final deve ser **exatamente um dos nomes da lista acima, sem nenhuma variação** (sem apelidos, grafias alternativas, espaços extras ou diferenças de caixa).
    - **Caso Padrão (Fallback)**: Se **nenhum** atendente da lista se identificar em toda a conversa, e somente nesse caso, use **exatamente** a string `"Helpdesk Pecege"`.
    - **Proibição de Variações**: Não utilize grafias diferentes como `"Help Desk Pecege"`, `"helpdesk"`, `"HelpDesk Pecegé"`, etc. O retorno aceito é **apenas** `"Helpdesk Pecege"`.
- `data_solicitacao`: (String, Obrigatório) Data da primeira mensagem do atendimento. **Use o formato estrito `AAAA-MM-DD`**.
- `hora_primeira_mensagem`: (String, Obrigatório) Hora da primeira mensagem do atendimento. **Use o formato estrito `HH:MM:SS` (24 horas)**.
- `tempo_primeira_resposta`: (String, Obrigatório) Tempo entre a primeira mensagem do solicitante e a primeira resposta do Help Desk. **Use o formato estrito `HH:MM:SS`**. Exemplo: para 2 minutos e 15 segundos, retorne `00:02:15`.
- `tempo_total_atendimento`: (Número Inteiro, Obrigatório) Tempo total entre a primeira e a última mensagem do atendimento, **arredondado para o minuto mais próximo**. Se o total for 47 minutos, retorne o número `47`.
- `problema_relatado`: (String, Obrigatório, Máx 255 chars) Resumo breve e objetivo do problema relatado. **Não pode ser uma string vazia**. Se não estiver claro, use "Problema não especificado pelo usuário".
- `categoria`: Uma das seguintes, respeitando ortografia e acentos:
  - Onedrive
  - Hardware
  - Windows
  - Office 365 (Excel, Outlook, PowerPoint, Word)
  - Outros Sistemas(Opus, Humaniza, Soul, LMS, MOVE, Solution, etc)
  - Sharepoint
  - Teams
  - Software
  - Navegadores
  - Impressora (Qualquer assunto relacionado a impressoras)
  - Problema de conexão (cabeada)
  - Problema de conexão (WiFi)
  - Outros

Exemplo de estrutura de resposta:
```json
{
  "atendimentos": [
    {
      "quem_solicitou_atendimento": "Fulano de Tal",
      "email_solicitante": "fulano@pecege.com",
      "quem_respondeu": "Mateus",
      "data_solicitacao": "2025-06-25",
      "hora_primeira_mensagem": "15:40:10",
      "tempo_primeira_resposta": "00:01:35",
      "tempo_total_atendimento": 47,
      "problema_relatado": "Usuário informa que a impressora do terceiro andar não está funcionando e apresenta erro.",
      "categoria": "Problema de Impressora"
    }
  ]
}

5. Se nenhuma solicitação de atendimento técnico for encontrada, **retorne um objeto JSON com uma lista vazia**: `{"atendimentos": []}`
6. Caso o tempo_total_atendimento fique zerado, revise por gentileza e verifique se não houve erro de formatação, análise ou se a conversa não foi interrompida abruptamente.

```json
[]
"""
    tasks = []
    for index, row in conversations_df.iterrows():
        task = {
            "custom_id": f"task-{row['ChatID']}-{index}", "method": "POST", "url": "/v1/chat/completions",
            "body": {
                "model": "gpt-4o-mini", "temperature": 0.1, "response_format": {"type": "json_object"},
                "messages": [
                    {"role": "system", "content": prompt_base},
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
            batch_file = client.files.create(file=file_to_upload, purpose="batch")
    except Exception as e:
        raise RuntimeError(f"Erro ao enviar o arquivo .jsonl para a OpenAI: {str(e)}")
    finally:
        os.remove(tmp_file_path) 

    batch_job = client.batches.create(input_file_id=batch_file.id, endpoint="/v1/chat/completions", completion_window="24h")
    return batch_job.id

def get_batch_job_status_and_results(batch_id: str) -> dict:
    """Verifica o status de um trabalho em lote. Se concluído, baixa e retorna os resultados."""
    batch_job = client.batches.retrieve(batch_id)
    
    response = {
        "batch_id": batch_job.id, "status": batch_job.status, "created_at": batch_job.created_at,
        "completed_at": batch_job.completed_at, "failed_at": batch_job.failed_at,
        "request_counts": batch_job.request_counts, "results": None, "errors": None
    }

    if batch_job.status == "completed":
        if batch_job.output_file_id:
            result_content = client.files.content(batch_job.output_file_id).content
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
            error_content = client.files.content(batch_job.error_file_id).content
            response["errors"] = error_content.decode('utf-8')
    
    return response