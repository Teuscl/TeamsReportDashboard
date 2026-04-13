"""
Testes de integração para os endpoints FastAPI (main.py).
Usa httpx.AsyncClient + ASGITransport para testar sem subir servidor real.
A OpenAI é mockada para não fazer chamadas de rede.
"""
import io
import json
import os
import sys
import zipfile
from unittest.mock import AsyncMock, MagicMock, patch

import pytest
import pytest_asyncio
from httpx import ASGITransport, AsyncClient

sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

from main import app

# ==============================================================================
# Helpers
# ==============================================================================

def _make_zip(include_msg: bool = True, include_junk: bool = False) -> bytes:
    """Cria um arquivo .zip em memória com conteúdo sintético."""
    buf = io.BytesIO()
    with zipfile.ZipFile(buf, "w") as zf:
        if include_msg:
            # .msg mínimo: conteúdo vazio é suficiente para testar o fluxo de extração
            zf.writestr("Item (1).msg", b"")
        if include_junk:
            zf.writestr("README.txt", b"ignored")
            zf.writestr("__MACOSX/._Item (1).msg", b"ignored")
    buf.seek(0)
    return buf.read()


def _fake_df():
    """DataFrame mínimo que simula saída do process_msg_files_to_dataframe."""
    import pandas as pd
    return pd.DataFrame({"ChatID": ["usuario@pecege.com"], "ConversationHistory": ["mensagem de teste"]})


def _empty_df():
    import pandas as pd
    return pd.DataFrame()


# ==============================================================================
# Fixtures
# ==============================================================================

@pytest.fixture
def anyio_backend():
    return "asyncio"


@pytest_asyncio.fixture
async def client():
    transport = ASGITransport(app=app)
    async with AsyncClient(transport=transport, base_url="http://test") as c:
        yield c


# ==============================================================================
# POST /analyze/start
# ==============================================================================

class TestStartAnalysisEndpoint:
    @pytest.mark.asyncio
    async def test_arquivo_nao_zip_retorna_400(self, client):
        response = await client.post(
            "/analyze/start",
            files={"file": ("relatorio.pdf", b"conteudo qualquer", "application/pdf")},
        )
        assert response.status_code == 400
        assert "zip" in response.json()["detail"].lower()

    @pytest.mark.asyncio
    async def test_zip_corrompido_retorna_400(self, client):
        response = await client.post(
            "/analyze/start",
            files={"file": ("dados.zip", b"isto nao eh um zip", "application/zip")},
        )
        assert response.status_code == 400
        assert "zip válido" in response.json()["detail"].lower()

    @pytest.mark.asyncio
    async def test_zip_sem_msg_retorna_400(self, client):
        zip_bytes = _make_zip(include_msg=False, include_junk=True)
        response = await client.post(
            "/analyze/start",
            files={"file": ("dados.zip", zip_bytes, "application/zip")},
        )
        assert response.status_code == 400
        assert ".msg" in response.json()["detail"]

    @pytest.mark.asyncio
    async def test_zip_valido_sem_conversas_retorna_400(self, client):
        zip_bytes = _make_zip(include_msg=True)
        with (
            patch("main.run_in_threadpool", new=AsyncMock(return_value=_empty_df())),
        ):
            response = await client.post(
                "/analyze/start",
                files={"file": ("dados.zip", zip_bytes, "application/zip")},
            )
        assert response.status_code == 400
        assert "conversa" in response.json()["detail"].lower()

    @pytest.mark.asyncio
    async def test_zip_valido_retorna_202_com_batch_id(self, client):
        zip_bytes = _make_zip(include_msg=True, include_junk=True)
        with (
            patch("main.run_in_threadpool", new=AsyncMock(return_value=_fake_df())),
            patch("main.start_openai_batch_job", new=AsyncMock(return_value="batch_abc123")),
        ):
            response = await client.post(
                "/analyze/start",
                files={"file": ("dados.zip", zip_bytes, "application/zip")},
            )
        assert response.status_code == 202
        body = response.json()
        assert body["batch_id"] == "batch_abc123"
        assert "processados" in body["message"]

    @pytest.mark.asyncio
    async def test_arquivos_macosx_sao_ignorados(self, client):
        """Garante que entradas __MACOSX/ não são contadas como .msg."""
        buf = io.BytesIO()
        with zipfile.ZipFile(buf, "w") as zf:
            zf.writestr("__MACOSX/._Item.msg", b"")   # deve ser ignorado
            zf.writestr("Item (1).msg", b"")           # único válido
        buf.seek(0)

        with (
            patch("main.run_in_threadpool", new=AsyncMock(return_value=_fake_df())),
            patch("main.start_openai_batch_job", new=AsyncMock(return_value="batch_xyz")),
        ):
            response = await client.post(
                "/analyze/start",
                files={"file": ("dados.zip", buf.getvalue(), "application/zip")},
            )
        assert response.status_code == 202
        # Apenas 1 .msg válido foi encontrado
        assert "1 arquivos" in response.json()["message"]

    @pytest.mark.asyncio
    async def test_tmp_dir_e_removido_em_caso_de_sucesso(self, client):
        """Verifica que o diretório temporário é limpo após o processamento."""
        import tempfile
        created_dirs = []
        original_mkdtemp = tempfile.mkdtemp

        def spy_mkdtemp(**kwargs):
            d = original_mkdtemp(**kwargs)
            created_dirs.append(d)
            return d

        zip_bytes = _make_zip(include_msg=True)
        with (
            patch("main.tempfile.mkdtemp", side_effect=spy_mkdtemp),
            patch("main.run_in_threadpool", new=AsyncMock(return_value=_fake_df())),
            patch("main.start_openai_batch_job", new=AsyncMock(return_value="batch_ok")),
        ):
            await client.post(
                "/analyze/start",
                files={"file": ("dados.zip", zip_bytes, "application/zip")},
            )

        for d in created_dirs:
            assert not os.path.exists(d), f"tmp_dir '{d}' não foi removido após o request!"

    @pytest.mark.asyncio
    async def test_tmp_dir_e_removido_em_caso_de_falha(self, client):
        """Verifica que o diretório temporário é limpo mesmo quando o processamento falha."""
        import tempfile
        created_dirs = []
        original_mkdtemp = tempfile.mkdtemp

        def spy_mkdtemp(**kwargs):
            d = original_mkdtemp(**kwargs)
            created_dirs.append(d)
            return d

        zip_bytes = _make_zip(include_msg=True)
        with (
            patch("main.tempfile.mkdtemp", side_effect=spy_mkdtemp),
            patch("main.run_in_threadpool", new=AsyncMock(side_effect=RuntimeError("falha simulada"))),
        ):
            response = await client.post(
                "/analyze/start",
                files={"file": ("dados.zip", zip_bytes, "application/zip")},
            )

        assert response.status_code == 500
        for d in created_dirs:
            assert not os.path.exists(d), f"tmp_dir '{d}' não foi removido após falha!"


# ==============================================================================
# GET /analyze/results/{batch_id}
# ==============================================================================

class TestGetAnalysisResultsEndpoint:
    def _mock_batch_job(self, status="in_progress", output_file_id=None, error_file_id=None):
        job = MagicMock()
        job.id = "batch_abc123"
        job.status = status
        job.created_at = 1700000000
        job.completed_at = 1700001000 if status == "completed" else None
        job.failed_at = None
        job.request_counts = MagicMock(total=1, completed=1, failed=0)
        job.output_file_id = output_file_id
        job.error_file_id = error_file_id
        return job

    @pytest.mark.asyncio
    async def test_batch_em_andamento(self, client):
        job = self._mock_batch_job(status="in_progress")
        with patch("analysis_logic.client.batches.retrieve", new=AsyncMock(return_value=job)):
            response = await client.get("/analyze/results/batch_abc123")
        assert response.status_code == 200
        body = response.json()
        assert body["status"] == "in_progress"
        assert body["results"] is None

    @pytest.mark.asyncio
    async def test_batch_concluido_sem_output_file(self, client):
        job = self._mock_batch_job(status="completed", output_file_id=None)
        with patch("analysis_logic.client.batches.retrieve", new=AsyncMock(return_value=job)):
            response = await client.get("/analyze/results/batch_abc123")
        assert response.status_code == 200
        body = response.json()
        assert body["status"] == "completed"
        assert body["results"] is None

    @pytest.mark.asyncio
    async def test_batch_concluido_com_resultado(self, client):
        job = self._mock_batch_job(status="completed", output_file_id="file-output-123")

        analysis_result = {"atendimentos": [{"quem_solicitou_atendimento": "Teste"}]}
        jsonl_line = json.dumps({
            "response": {
                "body": {
                    "choices": [{"message": {"content": json.dumps(analysis_result)}}]
                }
            }
        })
        file_content = MagicMock()
        file_content.content = jsonl_line.encode("utf-8")

        with (
            patch("analysis_logic.client.batches.retrieve", new=AsyncMock(return_value=job)),
            patch("analysis_logic.client.files.content", new=AsyncMock(return_value=file_content)),
        ):
            response = await client.get("/analyze/results/batch_abc123")

        assert response.status_code == 200
        body = response.json()
        assert body["status"] == "completed"
        assert len(body["results"]) == 1
        assert body["results"][0]["atendimentos"][0]["quem_solicitou_atendimento"] == "Teste"

    @pytest.mark.asyncio
    async def test_batch_concluido_linha_invalida_nao_crasha(self, client):
        job = self._mock_batch_job(status="completed", output_file_id="file-output-123")

        file_content = MagicMock()
        file_content.content = b"linha json invalida"

        with (
            patch("analysis_logic.client.batches.retrieve", new=AsyncMock(return_value=job)),
            patch("analysis_logic.client.files.content", new=AsyncMock(return_value=file_content)),
        ):
            response = await client.get("/analyze/results/batch_abc123")

        assert response.status_code == 200
        body = response.json()
        assert body["results"][0]["error"] == "Falha ao parsear JSON"

    @pytest.mark.asyncio
    async def test_batch_id_invalido_retorna_404(self, client):
        with patch(
            "analysis_logic.client.batches.retrieve",
            new=AsyncMock(side_effect=Exception("batch not found")),
        ):
            response = await client.get("/analyze/results/batch_invalido")
        assert response.status_code == 404
