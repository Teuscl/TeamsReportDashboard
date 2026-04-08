// Arquivo: src/types/AnalysisJob.ts

export interface AnalysisJob {
  id: string;
  name: string;
  // ✨ ADICIONADO O STATUS 'Processing' PARA COERÊNCIA COM O BACKEND
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed';
  createdAt: string; // Formato ISO string
  completedAt?: string | null;
  errorMessage?: string | null;
}