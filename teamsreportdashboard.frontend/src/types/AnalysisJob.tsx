export interface AnalysisJob {
  id: string;
  name: string; // ✅ Novo campo adicionado
  status: 'Pending' | 'Completed' | 'Failed';
  createdAt: string;
  completedAt?: string;
  errorMessage?: string;
}