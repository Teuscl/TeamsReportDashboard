from pydantic_settings import BaseSettings, SettingsConfigDict

class Settings(BaseSettings):
    """
    Configurações da aplicação Message Analyzer.
    As variáveis são lidas do ambiente do Sistema Operacional ou do arquivo .env.
    """
    # E-mail canônico que o Help Desk usa para responder.
    help_desk_email: str = "helpdesk@pecege.com"
    
    # É possível adicionar a variável de API Key aqui como validação,
    # embora a lib `openai` pegue o OPENAI_API_KEY do ambiente automaticamente se presente.
    openai_api_key: str | None = None

    model_config = SettingsConfigDict(
        env_file=".env", 
        env_file_encoding="utf-8", 
        extra="ignore"
    )

settings = Settings()
