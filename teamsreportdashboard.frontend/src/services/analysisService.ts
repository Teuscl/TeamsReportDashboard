// src/services/analysisService.ts
import axios from '@/services/axiosConfig';
import { AnalysisJob } from '@/types/AnalysisJob';

/**
 * Inicia o job de análise enviando um arquivo .zip e um nome para o job.
 */
export const startAnalysisJob = async (
  file: File,
  jobName: string,
  onProgress: (progress: number) => void
): Promise<{ jobId: string }> => {
  const formData = new FormData();
  console.log(jobName)
  formData.append("file", file);
  formData.append("name", jobName.trim());

  // Debug para garantir que o campo está indo
  for (let [key, value] of formData.entries()) {
    console.log("FormData =>", key, value);
  }

  const response = await axios.post("/analysis/start", formData, {
    // ❌ NÃO setar Content-Type manualmente, axios faz isso sozinho
    onUploadProgress: (progressEvent) => {
      const { loaded, total } = progressEvent;
      if (total && typeof total === "number") {
        const percentCompleted = Math.round((loaded * 100) / total);
        onProgress(percentCompleted);
      }
    },
  });

  return response.data;
};

/** Lista de jobs */
export const getAnalysisJobs = async (): Promise<AnalysisJob[]> => {
  const response = await axios.get('/analysis');
  return response.data;
};

/** Status de um job */
export const getJobStatus = async (jobId: string): Promise<AnalysisJob> => {
  const response = await axios.get(`/analysis/status/${jobId}`);
  return response.data;
};

/** Reprocessar job */
export const reprocessAnalysisJob = async (jobId: string): Promise<{ message: string }> => {
  const response = await axios.post(`/analysis/reprocess/${jobId}`);
  return response.data;
};
0                           