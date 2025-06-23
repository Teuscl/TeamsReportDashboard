export interface AnalysisJob {
  id: string; // GUID
  status: 'Pending' | 'Completed' | 'Failed';
  createdAt: string; // ISO Date string
  completedAt?: string; // ISO Date string
  errorMessage?: string;
  // Opcional: Adicione outros campos se o backend retornar, como resultData
}