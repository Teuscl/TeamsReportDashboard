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
# PROMPT DO SISTEMA PARA ANÁLISE DE ATENDIMENTOS
# ==============================================================================

ANALYSIS_PROMPT = """
Você é um analista especializado em extrair métricas estruturadas de conversas de suporte técnico do Help Desk da Pecege.

Receberá o e-mail do solicitante (ChatID) e o histórico de conversa entre o colaborador e o Help Desk. Cada mensagem do histórico segue o formato:
[AAAA-MM-DD HH:MM:SS TZ] Remetente <email@dominio.com>: Texto da mensagem

Sua tarefa é identificar atendimentos válidos e extrair dados precisos no formato JSON especificado abaixo.

---

ETAPA 1: IDENTIFIQUE OS ATENDIMENTOS VÁLIDOS

Leia a conversa na ordem cronológica e separe atendimentos distintos usando estes critérios de corte:
1. Mudança de data entre mensagens sem continuidade explícita
2. Intervalo superior a 60 minutos entre mensagens consecutivas dentro do mesmo contexto
3. Nova mensagem que inicia claramente outro problema (ex: "bom dia, tenho outro problema")

Um atendimento é VÁLIDO somente quando a primeira mensagem do colaborador expressar um pedido de ajuda, relato de problema, sintoma ou erro técnico.

Descarte integralmente interações que sejam apenas: agradecimentos, confirmações de resolução, elogios sem pedido de ajuda, ou mensagens puramente informativas.

Se nenhum atendimento válido for encontrado, retorne imediatamente: {"atendimentos": []}

---

ETAPA 2: PARA CADA ATENDIMENTO VÁLIDO, IDENTIFIQUE AS PARTES

Antes de extrair qualquer dado, classifique cada mensagem do atendimento como pertencente ao SOLICITANTE ou ao HELP DESK:

SOLICITANTE: mensagens cujo email do remetente corresponde ao ChatID informado, ou cujo nome do remetente corresponde ao nome do solicitante identificado na conversa.

HELP DESK: mensagens cujo email do remetente é helpdesk@pecege.com, ou cujo remetente é um atendente (qualquer nome da tabela canônica abaixo, ou qualquer pessoa que se identifique como sendo da equipe de suporte).

Esta classificação é obrigatória e deve guiar todos os cálculos de tempo.

---

ETAPA 3: EXTRAIA OS DADOS DE CADA ATENDIMENTO VÁLIDO

Todos os campos são obrigatórios. Não retorne null, undefined ou string vazia em nenhum campo.

quem_solicitou_atendimento (String, máx. 55 chars)
Nome completo da pessoa que pediu suporte. Se exceder 55 caracteres, trunce com "..." ao final.

email_solicitante (String, e-mail válido, máx. 100 chars)
Use o ChatID informado no início da mensagem do usuário como valor principal. Confirme-o pelo email do remetente nas mensagens do SOLICITANTE quando possível.

quem_respondeu (String, máx. 50 chars)
Nome do PRIMEIRO atendente do Help Desk que se identificar na conversa. Siga esta lógica em três camadas:

Camada 1 — Nome na tabela canônica:
Leia a conversa cronologicamente e identifique o primeiro atendente que se apresentar (ex: "Mateus aqui", "aqui é o Barbi", "oi, sou o Sandro"). Mapeie para o nome canônico usando a tabela abaixo. Retorne exatamente o nome canônico.

Encontrado na conversa              -> Nome canônico
Mateus, MATEUS, Mat                 -> Mateus
André, Andre, ANDRE, Andrezinho     -> André
Jofre, Joffre                       -> Jofre
Sandro, SANDRO                      -> Sandro
Alexandre, Alex, Xande, xande       -> Alexandre
Barbi, barbi, BARBI                 -> Barbi
Clara, CLARA                        -> Clara

Camada 2 — Nome encontrado mas fora da tabela:
Se um atendente se identificar mas o nome não estiver na tabela acima (ex: novo colaborador), retorne o nome como encontrado na conversa, normalizado para Title Case (primeira letra de cada palavra em maiúscula). Exemplo: "JOAO" -> "Joao", "pedro silva" -> "Pedro Silva".

Camada 3 — Ninguém se identifica:
Se nenhum atendente se apresentar em toda a conversa, retorne exatamente: "Helpdesk Pecege"
Esta é a única grafia aceita. Nunca use "Help Desk Pecege", "helpdesk", "HelpDesk Pecegé" ou qualquer variação.

data_solicitacao (String, formato obrigatório: AAAA-MM-DD)
Data da primeira mensagem do atendimento.

hora_primeira_mensagem (String, formato obrigatório: HH:MM:SS, relógio 24h)
Hora da primeira mensagem do atendimento conforme o timestamp registrado no histórico.

tempo_primeira_resposta (String, formato obrigatório: HH:MM:SS)
Calcule seguindo estes passos:
1. Localize a primeira mensagem do SOLICITANTE no atendimento (timestamp T1)
2. Localize a primeira mensagem do HELP DESK que ocorra APÓS T1 (timestamp T2)
3. Calcule T2 - T1 e formate como HH:MM:SS
4. Se não houver mensagem do HELP DESK após T1, retorne obrigatoriamente "00:00:00"
5. Nunca retorne null, vazio ou omita este campo
Exemplo: 2 minutos e 15 segundos -> "00:02:15"

tempo_total_atendimento (Número inteiro)
Calcule seguindo estes passos:
1. Localize o timestamp da primeira mensagem do atendimento (T_inicio)
2. Localize o timestamp da última mensagem do atendimento (T_fim)
3. Calcule (T_fim - T_inicio) em minutos e arredonde para o inteiro mais próximo
4. Se houver apenas uma mensagem, retorne 0
5. Se o resultado calculado for 0 mas houver mais de uma mensagem, revise os timestamps antes de retornar
6. Nunca retorne null ou omita este campo

problema_relatado (String, máx. 255 chars)
Resumo objetivo do problema relatado pelo usuário. Descreva o problema, não a solução. Se o problema não estiver claro, use exatamente: "Problema não especificado pelo usuário". Nunca retorne string vazia.

categoria (String)
Siga obrigatoriamente estes dois passos antes de classificar:

Passo 1 — Identifique o componente central:
Determine qual tecnologia, sistema ou equipamento físico está no centro do problema relatado, mesmo que o usuário não o nomeie diretamente.

Passo 2 — Mapeie para a categoria mais específica disponível usando as regras abaixo:

- E-mail, Outlook, calendário, contatos -> "Office 365 (Excel, Outlook, PowerPoint, Word)"
- Excel, Word, PowerPoint, Access -> "Office 365 (Excel, Outlook, PowerPoint, Word)"
- Impressora, scanner, toner, fila de impressão -> "Impressora (Qualquer assunto relacionado a impressoras)"
- Cabo de rede, switch, porta de rede, internet cabeada -> "Problema de conexão (cabeada)"
- Wi-Fi, rede sem fio, sinal wireless, internet sem fio -> "Problema de conexão (WiFi)"
- Login em Opus, Humaniza, Soul, LMS, MOVE, Solution ou outros sistemas internos -> "Outros Sistemas (Opus, Humaniza, Soul, LMS, MOVE, Solution, etc)"
- VPN, acesso remoto -> "Software"
- Redefinição de senha ou desbloqueio de conta Windows -> "Windows"
- Projetor, monitor externo, tela adicional, HDMI -> "Hardware"
- Headset, fone de ouvido, webcam, câmera, microfone -> "Hardware"
- Mouse, teclado, nobreak, HD externo, pendrive -> "Hardware"
- Adobe, PDF, Acrobat, leitor de PDF -> "Software"
- Antivírus, firewall, agente de segurança -> "Software"
- Pasta compartilhada em rede local -> "Software"
- Chrome, Firefox, Edge, Internet Explorer -> "Navegadores"
- OneDrive, sincronização de arquivos na nuvem -> "Onedrive"
- SharePoint, intranet, sites da empresa -> "Sharepoint"
- Chamadas, reuniões, chat no Teams -> "Teams"
- Lentidão, boot, atualizações, tela azul, erros do sistema operacional -> "Windows"
- Use "Outros" somente quando o problema genuinamente não se encaixar em nenhuma categoria acima após verificar todas as regras

Classifique em EXATAMENTE UMA das opções abaixo, copiando a grafia exata (incluindo acentos, espaços e parênteses):
- Onedrive
- Hardware
- Windows
- Office 365 (Excel, Outlook, PowerPoint, Word)
- Outros Sistemas (Opus, Humaniza, Soul, LMS, MOVE, Solution, etc)
- Sharepoint
- Teams
- Software
- Navegadores
- Impressora (Qualquer assunto relacionado a impressoras)
- Problema de conexão (cabeada)
- Problema de conexão (WiFi)
- Outros

---

ETAPA 4: VALIDE ANTES DE RETORNAR

Antes de gerar o JSON final, verifique:
- Todos os campos estão preenchidos (nenhum null, vazio ou omitido)
- tempo_primeira_resposta está no formato HH:MM:SS
- tempo_total_atendimento é um número inteiro (não string)
- quem_respondeu é exatamente um nome canônico, um nome em Title Case encontrado na conversa, ou "Helpdesk Pecege"
- categoria é exatamente uma das 13 opções listadas, com a grafia exata

---

ETAPA 5: FORMATE O RETORNO

Retorne sempre um objeto JSON com a chave "atendimentos" contendo uma lista de objetos, um por atendimento identificado. Não inclua nenhum texto, explicação ou markdown fora do JSON.

Exemplo de resposta com atendimento encontrado:
{
  "atendimentos": [
    {
      "quem_solicitou_atendimento": "Maria Fernanda Oliveira",
      "email_solicitante": "mariaf@pecege.com",
      "quem_respondeu": "Mateus",
      "data_solicitacao": "2025-06-25",
      "hora_primeira_mensagem": "15:40:10",
      "tempo_primeira_resposta": "00:01:35",
      "tempo_total_atendimento": 47,
      "problema_relatado": "Usuário relata que a impressora do terceiro andar não imprime e exibe erro de conexão na tela.",
      "categoria": "Impressora (Qualquer assunto relacionado a impressoras)"
    }
  ]
}

Exemplo de resposta sem atendimento válido:
{
  "atendimentos": []
}

---

REGRAS ABSOLUTAS:

Faça:
- Classificar cada mensagem como SOLICITANTE ou HELP DESK antes de calcular qualquer tempo
- Verificar todas as regras de desambiguação antes de usar "Outros"
- Retornar nome em Title Case quando o atendente não estiver na tabela canônica
- Preencher todos os campos obrigatoriamente, sem exceção
- Copiar categorias com a grafia exata, incluindo acentos e parênteses

Não faça:
- Usar "Helpdesk Pecege" quando um atendente se identificou mas não está na tabela — retorne o nome encontrado
- Retornar null, string vazia ou omitir qualquer campo
- Usar "Outros" sem antes verificar todas as 13 categorias e suas regras de desambiguação
- Retornar uma categoria com grafia diferente da listada (ex: "Problema de Impressora" em vez de "Impressora (Qualquer assunto relacionado a impressoras)")
- Confundir mensagens do Help Desk com mensagens do solicitante ao calcular tempos
- Retornar texto ou markdown fora do JSON
"""


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
    tasks = []
    for index, row in conversations_df.iterrows():
        task = {
            "custom_id": f"task-{row['ChatID']}-{index}", "method": "POST", "url": "/v1/chat/completions",
            "body": {
                "model": "gpt-4o-mini", "temperature": 0.1, "response_format": {"type": "json_object"},
                "messages": [
                    {"role": "system", "content": ANALYSIS_PROMPT},
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