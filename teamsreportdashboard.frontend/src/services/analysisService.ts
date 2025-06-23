import axios from '@/services/axiosConfig'; // Usando sua instância configurada do Axios
import { AnalysisJob } from '@/types/AnalysisJob';

/**
 * Inicia o job de análise enviando um arquivo .zip.
 * @param file O arquivo .zip a ser enviado.
 * @param onProgress Uma função de callback que recebe a porcentagem (0-100) do progresso do upload.
 * @returns Uma promessa que resolve com o ID do job.
 */
export const startAnalysisJob = async (
  file: File,
  onProgress: (progress: number) => void
): Promise<{ jobId: string }> => {
  const formData = new FormData();
  formData.append('file', file);

  // ROTA CORRIGIDA: Removido o '/api'
  const response = await axios.post('/analysis/start', formData, {
    headers: {
      'Content-Type': 'multipart/form-data',
    },
    onUploadProgress: (progressEvent) => {
      const { loaded, total } = progressEvent;
      if (total && typeof total === 'number') {
        const percentCompleted = Math.round((loaded * 100) / total);
        onProgress(percentCompleted);
      }
    },
  });

  return response.data;
};

/**
 * Busca o histórico de todos os jobs de análise.
 * @returns Uma promessa que resolve com uma lista de jobs.
 */
export const getAnalysisJobs = async (): Promise<AnalysisJob[]> => {
  // ROTA CORRIGIDA: Removido o '/api'
  const response = await axios.get('/analysis'); 
  return response.data;
};

/**
 * Busca o status e os detalhes de um job de análise específico.
 * @param jobId O ID (GUID) do job a ser consultado.
 * @returns Uma promessa que resolve com os detalhes do job.
 */
export const getJobStatus = async (jobId: string): Promise<AnalysisJob> => {
  // ROTA CORRIGIDA: Removido o '/api'
  const response = await axios.get(`/analysis/status/${jobId}`);
  return response.data;
};