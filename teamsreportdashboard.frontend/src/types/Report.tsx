// src/types/Report.ts (NOVO ARQUIVO)

// Interface para o objeto Report como recebido da API e usado na tabela/estado
export interface Report {
  id: number;
  requesterId: number; // Chave estrangeira para o Solicitante
  requesterName: string;
  requesterEmail: string;
  technicianName?: string | null;
  requestDate: string; // String no formato ISO 8601
  reportedProblem: string;
  category: string;
  firstResponseTime: string; // Formato "HH:MM:SS"
  averageHandlingTime: string; // Formato "HH:MM:SS"
  createdAt?: string;
  updatedAt?: string;
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
  requesterId: number;
  requesterName?: string;
  requesterEmail?: string;
  technicianName?: string | null;
  requestDate?: string;
  reportedProblem?: string;
  category?: string;
  firstResponseTime?: string;
  averageHandlingTime?: string;
}