from fastapi import FastAPI, UploadFile, File, HTTPException, Path
import uvicorn
import zipfile
import io
import os
from types import SimpleNamespace

# Importa as funções de lógica do outro arquivo
from analysis_logic import (
    process_msg_files_to_dataframe,
    start_openai_batch_job,
    get_batch_job_status_and_results
)

app = FastAPI(
    title="API de Análise de Chats do Help Desk",
    description="Faça upload de um arquivo .zip contendo arquivos .msg para iniciar uma análise assíncrona.",
    version="1.1.0"
)

@app.post("/analyze/start", status_code=202, summary="Inicia a Análise de um Arquivo .zip")
async def start_analysis(file: UploadFile = File(..., description="Um único arquivo .zip contendo os chats no formato .msg")):
    """
    Recebe um arquivo .zip, descompacta os arquivos .msg em memória, inicia um trabalho
    em lote na OpenAI e retorna imediatamente o ID do trabalho para consulta futura.
    """
    # 1. VERIFICA SE O ARQUIVO É .ZIP (LÓGICA CORRETA)
    if not file.filename.lower().endswith(".zip"):
        raise HTTPException(
            status_code=400,
            detail="Formato de arquivo inválido. Apenas .zip é permitido."
        )

    msg_files_from_zip = []
    try:
        # 2. LÊ E DESCOMPACTA O ZIP EM MEMÓRIA
        zip_content = await file.read()
        with zipfile.ZipFile(io.BytesIO(zip_content), 'r') as zf:
            for file_name in zf.namelist():
                if file_name.startswith('__MACOSX/') or not file_name.lower().endswith('.msg'):
                    continue
                with zf.open(file_name) as msg_file:
                    virtual_upload_file = SimpleNamespace(
                        file=io.BytesIO(msg_file.read()),
                        filename=os.path.basename(file_name)
                    )
                    msg_files_from_zip.append(virtual_upload_file)
    except zipfile.BadZipFile:
        raise HTTPException(status_code=400, detail="O arquivo enviado não é um .zip válido.")
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro ao processar o arquivo zip: {str(e)}")

    if not msg_files_from_zip:
        raise HTTPException(status_code=400, detail="Nenhum arquivo .msg foi encontrado dentro do .zip.")

    try:
        # 3. CHAMA A LÓGICA DE PROCESSAMENTO (que não mudou)
        grouped_conversations_df = process_msg_files_to_dataframe(msg_files_from_zip)
        if grouped_conversations_df.empty:
            raise HTTPException(status_code=400, detail="Nenhuma conversa válida foi encontrada para processamento.")

        batch_id = start_openai_batch_job(grouped_conversations_df)
        return {"message": f"{len(msg_files_from_zip)} arquivos .msg processados. Análise iniciada.", "batch_id": batch_id}
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Erro interno durante a análise: {str(e)}")


@app.get("/analyze/results/{batch_id}", summary="Consulta o Status e Resultado da Análise")
async def get_analysis_results(batch_id: str = Path(..., description="O ID do trabalho em lote retornado por /analyze/start", example="batch_abc123")):
    """
    Verifica o status e obtém o resultado de um trabalho de análise. Deve ser chamado
    periodicamente até que o status seja 'completed' ou 'failed'.
    """
    try:
        # A função em analysis_logic.py foi corrigida para ser síncrona, então removemos o await
        result = get_batch_job_status_and_results(batch_id)
        return result
    except Exception as e:
        raise HTTPException(status_code=404, detail=f"Não foi possível processar o trabalho com ID {batch_id}: {str(e)}")

# Comando para rodar o servidor: uvicorn main:app --reload --port 8001
if __name__ == "__main__":
    uvicorn.run("main:app", host="0.0.0.0", port=8001, reload=True)