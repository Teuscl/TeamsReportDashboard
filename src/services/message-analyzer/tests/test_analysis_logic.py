"""
Testes unitários para analysis_logic.py.
Cobre: extract_and_clean_email, create_normalized_chat_id, process_msg_files_to_dataframe.
Não requer conexão com a OpenAI.
"""
import glob
import os
import pytest
import pandas as pd

# Adiciona o diretório pai ao path para importar os módulos
import sys
sys.path.insert(0, os.path.dirname(os.path.dirname(__file__)))

from analysis_logic import (
    extract_and_clean_email,
    create_normalized_chat_id,
    process_msg_files_to_dataframe,
)
from config import settings

REAL_MSG_DIR = os.path.join(
    os.path.dirname(os.path.dirname(__file__)),
    "TeamsMessages/TeamsMessages/helpdesk@pecege.com (Primary)/TeamsMessagesData",
)
REAL_MSG_FILES = glob.glob(os.path.join(REAL_MSG_DIR, "*.msg"))


# ==============================================================================
# extract_and_clean_email
# ==============================================================================

class TestExtractAndCleanEmail:
    def test_email_entre_angulos(self):
        assert extract_and_clean_email("João Silva <joao@pecege.com>") == "joao@pecege.com"

    def test_helpdesk_com_outro_email_retorna_outro(self):
        assert extract_and_clean_email("helpdesk@pecege.com; joao@pecege.com") == "joao@pecege.com"

    def test_apenas_helpdesk_retorna_helpdesk(self):
        assert extract_and_clean_email("helpdesk@pecege.com") == settings.help_desk_email

    def test_none_retorna_none(self):
        assert extract_and_clean_email(None) is None

    def test_sem_email_retorna_none(self):
        assert extract_and_clean_email("sem email nenhum aqui") is None

    def test_none_entre_angulos_retorna_none(self):
        assert extract_and_clean_email("João <none>") is None

    def test_email_em_texto_simples(self):
        assert extract_and_clean_email("contato@empresa.com.br") == "contato@empresa.com.br"

    def test_email_maiusculo_normalizado(self):
        assert extract_and_clean_email("USER@PECEGE.COM") == "user@pecege.com"

    def test_multiplos_emails_retorna_primeiro_nao_helpdesk(self):
        result = extract_and_clean_email("joao@pecege.com; helpdesk@pecege.com; maria@pecege.com")
        assert result == "joao@pecege.com"

    def test_string_vazia_retorna_none(self):
        assert extract_and_clean_email("") is None

    def test_email_com_nulo_unicode(self):
        # \x00 é removido do campo completo antes de processar → email resultante é válido
        assert extract_and_clean_email("joao\x00@pecege.com") == "joao@pecege.com"

    def test_formato_none_com_arroba_descartado(self):
        # "<none@>" ou variantes não devem passar
        assert extract_and_clean_email("<none@>") is None


# ==============================================================================
# create_normalized_chat_id
# ==============================================================================

class TestCreateNormalizedChatId:
    def _row(self, from_email, to_email, index=0):
        s = pd.Series({"From_Email": from_email, "To_Email": to_email}, name=index)
        return s

    def test_helpdesk_from_retorna_to(self):
        row = self._row(settings.help_desk_email, "usuario@pecege.com")
        assert create_normalized_chat_id(row) == "usuario@pecege.com"

    def test_usuario_from_retorna_from(self):
        row = self._row("usuario@pecege.com", settings.help_desk_email)
        assert create_normalized_chat_id(row) == "usuario@pecege.com"

    def test_from_none_to_valido_retorna_fallback(self):
        row = self._row(None, "alguem@pecege.com", index=42)
        # from é None e não é helpdesk → fallback
        result = create_normalized_chat_id(row)
        assert result == "unknown_chat_42"

    def test_from_none_to_none_retorna_fallback(self):
        row = self._row(None, None, index=7)
        assert create_normalized_chat_id(row) == "unknown_chat_7"

    def test_from_none_string_ignorado(self):
        row = self._row("none", "usuario@pecege.com")
        # "none" é filtrado como inválido
        result = create_normalized_chat_id(row)
        assert result == "unknown_chat_0"


# ==============================================================================
# process_msg_files_to_dataframe
# ==============================================================================

class TestProcessMsgFilesToDataframe:
    def test_lista_vazia_retorna_dataframe_vazio(self):
        df = process_msg_files_to_dataframe([])
        assert df.empty

    def test_arquivo_invalido_ignorado_sem_crashar(self):
        df = process_msg_files_to_dataframe(["/tmp/nao_existe.msg"])
        assert df.empty

    @pytest.mark.skipif(not REAL_MSG_FILES, reason="Arquivos .msg reais não encontrados")
    def test_processa_arquivos_reais_retorna_dataframe(self):
        sample = REAL_MSG_FILES[:5]
        df = process_msg_files_to_dataframe(sample)
        assert not df.empty
        assert "ChatID" in df.columns
        assert "ConversationHistory" in df.columns

    @pytest.mark.skipif(not REAL_MSG_FILES, reason="Arquivos .msg reais não encontrados")
    def test_chatid_contem_arroba(self):
        sample = REAL_MSG_FILES[:10]
        df = process_msg_files_to_dataframe(sample)
        if not df.empty:
            assert df["ChatID"].str.contains("@").all()

    @pytest.mark.skipif(not REAL_MSG_FILES, reason="Arquivos .msg reais não encontrados")
    def test_helpdesk_nao_aparece_como_chatid(self):
        sample = REAL_MSG_FILES[:10]
        df = process_msg_files_to_dataframe(sample)
        if not df.empty:
            assert (df["ChatID"] != settings.help_desk_email).all()

    @pytest.mark.skipif(not REAL_MSG_FILES, reason="Arquivos .msg reais não encontrados")
    def test_history_nao_vazio(self):
        sample = REAL_MSG_FILES[:5]
        df = process_msg_files_to_dataframe(sample)
        if not df.empty:
            assert df["ConversationHistory"].str.len().gt(0).all()

    @pytest.mark.skipif(not REAL_MSG_FILES, reason="Arquivos .msg reais não encontrados")
    def test_context_manager_libera_arquivos(self):
        """Verifica que os arquivos .msg são fechados após processamento (sem ResourceWarning)."""
        import warnings
        sample = REAL_MSG_FILES[:3]
        with warnings.catch_warnings(record=True) as w:
            warnings.simplefilter("always")
            process_msg_files_to_dataframe(sample)
        resource_warnings = [x for x in w if issubclass(x.category, ResourceWarning)]
        assert resource_warnings == [], f"ResourceWarnings detectados: {resource_warnings}"
