export interface Report {
  id: string;
  requesterId: string;
  requesterName: string;
  requesterEmail: string;
  technicianName?: string | null;
  requestDate: string;
  reportedProblem: string;
  category: string;
  firstResponseTime: string;
  averageHandlingTime: string;
  analyticalThinking?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateReportPayload {
  requesterName: string;
  requesterEmail: string;
  technicianName?: string | null;
  requestDate: string;
  reportedProblem: string;
  category: string;
  firstResponseTime: string;
  averageHandlingTime: string;
}

export interface UpdateReportPayload {
  requesterId: string;
  requesterName?: string;
  requesterEmail?: string;
  technicianName?: string | null;
  requestDate?: string;
  reportedProblem?: string;
  category?: string;
  firstResponseTime?: string;
  averageHandlingTime?: string;
}
