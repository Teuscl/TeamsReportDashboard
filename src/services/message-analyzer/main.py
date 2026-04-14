from fastapi import FastAPI, UploadFile, File, HTTPException, Path
from fastapi.concurrency import run_in_threadpool
import uvicorn
import zipfile
import os
import tempfile
import shutil
import aiofiles

MAX_UPLOAD_BYTES = 500 * 1024 * 1024  # 500 MB
MAX_MSG_FILES = 2_000

# Importa as funções de lógica do outro arquivo
from analysis_logic import (
    process_msg_files_to_dataframe,
    start_openai_batch_job,
    get_batch_job_status_and_results,
    get_analysis_prompt,
    PROMPT_FILE_PATH
)
from pydantic import BaseModel

class PromptUpdate(BaseModel):
    prompt: str

app = FastAPI(
    title="API de Análise de Chats do Help Desk",
    description="Faça upload de um arquivo .zip contendo arquivos .msg para iniciar uma análise assíncrona.",
    version="1.1.0"
)

@app.get("/analyze/prompt", summary="Obtém o prompt atual de análise")
async def get_prompt():
    """Retorna o conteúdo atual do arquivo prompt.txt."""
    return {"prompt": get_analysis_prompt()}

@app.post("/analyze/prompt", summary="Atualiza o prompt de análise")
async def update_prompt(data: PromptUpdate):
    """Atualiza o conteúdo do arquivo prompt.txt."""
    try:
        async with aiofiles.open(PROMPT_FILE_PATH, mode='w', encoding='utf-8') as f:
            await f.write(data.prompt)
        return {"message": "Prompt atualizado com sucesso."}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro ao atualizar o prompt: {str(e)}")

@app.post("/analyze/start", status_code=202, summary="Inicia a Análise de um Arquivo .zip")
async def start_analysis(file: UploadFile = File(..., description="Um único arquivo .zip contendo os chats no formato .msg")):
    """
    Recebe um arquivo .zip, descompacta os arquivos .msg em memória, inicia um trabalho
    em lote na OpenAI e retorna imediatamente o ID do trabalho para consulta futura.
    """
    # 1. VALIDA O ARQUIVO
    if not (file.filename or "").lower().endswith(".zip"):
        raise HTTPException(
            status_code=400,
            detail="Formato de arquivo inválido. Apenas .zip é permitido."
        )

    msg_files_from_zip = []
    tmp_dir = tempfile.mkdtemp(prefix="pecege_chats_")
    try:
        # 2. SALVA NO DISCO E DESCOMPACTA (Sem explodir a memória)
        zip_path = os.path.join(tmp_dir, "upload.zip")
        bytes_written = 0
        async with aiofiles.open(zip_path, 'wb') as out_file:
            while content := await file.read(1024 * 1024):  # chunks de 1MB
                bytes_written += len(content)
                if bytes_written > MAX_UPLOAD_BYTES:
                    raise HTTPException(
                        status_code=413,
                        detail=f"Arquivo muito grande. O limite é {MAX_UPLOAD_BYTES // (1024 * 1024)} MB."
                    )
                await out_file.write(content)

        with zipfile.ZipFile(zip_path, 'r') as zf:
            for file_name in zf.namelist():
                if file_name.startswith('__MACOSX/') or not file_name.lower().endswith('.msg'):
                    continue
                extracted_path = zf.extract(file_name, tmp_dir)
                msg_files_from_zip.append(extracted_path)

        if len(msg_files_from_zip) > MAX_MSG_FILES:
            raise HTTPException(
                status_code=400,
                detail=f"O arquivo contém mais de {MAX_MSG_FILES} arquivos .msg. Divida o envio em lotes menores."
            )
    except zipfile.BadZipFile:
        shutil.rmtree(tmp_dir, ignore_errors=True)
        raise HTTPException(status_code=400, detail="O arquivo enviado não é um .zip válido.")
    except Exception as e:
        shutil.rmtree(tmp_dir, ignore_errors=True)
        raise HTTPException(status_code=500, detail=f"Erro ao processar o arquivo zip: {str(e)}")

    if not msg_files_from_zip:
        shutil.rmtree(tmp_dir, ignore_errors=True)
        raise HTTPException(status_code=400, detail="Nenhum arquivo .msg foi encontrado dentro do .zip.")

    try:
        # 3. CHAMA A LÓGICA DE PROCESSAMENTO (Numa thread worker pra não travar a API)
        grouped_conversations_df = await run_in_threadpool(process_msg_files_to_dataframe, msg_files_from_zip)

        if grouped_conversations_df.empty:
            raise HTTPException(status_code=400, detail="Nenhuma conversa válida foi encontrada para processamento.")

        batch_id = await start_openai_batch_job(grouped_conversations_df)
        return {"message": f"{len(msg_files_from_zip)} arquivos .msg processados e análise assíncrona iniciada.", "batch_id": batch_id}
    except HTTPException:
        raise
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro interno durante a análise: {str(e)}")
    finally:
        shutil.rmtree(tmp_dir, ignore_errors=True)


@app.get("/analyze/results/{batch_id}", summary="Consulta o Status e Resultado da Análise")
async def get_analysis_results(batch_id: str = Path(..., description="O ID do trabalho em lote retornado por /analyze/start", examples=["batch_abc123"])):
    """
    Verifica o status e obtém o resultado de um trabalho de análise. Deve ser chamado
    periodicamente até que o status seja 'completed' ou 'failed'.
    """
    try:
        # Atualizado para buscar os status assincronamente da API OpenAI
        result = await get_batch_job_status_and_results(batch_id)
        return result
    except Exception as e:
        raise HTTPException(status_code=404, detail=f"Não foi possível processar o trabalho com ID {batch_id}: {str(e)}")

# Comando para rodar o servidor: uvicorn main:app --reload --port 8001
if __name__ == "__main__":
    uvicorn.run("main:app", host="0.0.0.0", port=8001, reload=True)