// src/types/Report.ts (NOVO ARQUIVO)

// Interface para o objeto Report como recebido da API e usado na tabela/estado
export interface Report {
  id: number;
  requesterName: string;
  requesterEmail: string;
  technicianName?: string | null; // Pode ser nulo
  requestDate: string; // Manter como string ISO 8601 para transferência, converter para Date na UI se necessário
  reportedProblem: string;
  category: string; // Opcional, pode ser usado para categorizar problemas
  firstResponseTime: string; // Formato "HH:MM:SS" ou string ISO 8601 Duration "PTnHnM S"
  averageHandlingTime: string; // Mesmo formato que firstResponseTime
  createdAt?: string; // Adicionado de EntityBase (opcional no frontend se não usado)
  updatedAt?: string;
   // Adicionado de EntityBase (opcional no frontend se não usado)
}

// Payload para criar um Report (espelha CreateReportDto)
export interface CreateReportPayload {
  requesterName: string;
  requesterEmail: string;
  technicianName?: string | null;
  requestDate: string; // Enviar como string ISO 8601
  reportedProblem: string;
  category: string;
  firstResponseTime: string; // Enviar como "HH:MM:SS"
  averageHandlingTime: string; // Enviar como "HH:MM:SS"
}

// Payload para atualizar um Report (espelha UpdateReportDto)
// Todos os campos são opcionais para PATCH
export interface UpdateReportPayload {
  requesterName?: string;
  requesterEmail?: string;
  technicianName?: string | null;
  requestDate?: string;
  reportedProblem?: string;
  category?: string;
  firstResponseTime?: string;
  averageHandlingTime?: string;
}