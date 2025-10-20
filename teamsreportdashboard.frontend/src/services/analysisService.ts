// Caminho: src/services/analysisService.ts

import { AnalysisJob} from "@/types/AnalysisJob";
import api from "./axiosConfig";

// READ (GET ALL) - Para carga inicial e refresh manual
export const getAnalysisJobs = async (): Promise<AnalysisJob[]> => {
  const response = await api.get("/analysis");
  return response.data;
};

// READ (GET ONE) - Usado pelo hook de polling
export const getJobStatus = async (jobId: string): Promise<AnalysisJob> => {
  const response = await api.get(`/analysis/${jobId}`);
  return response.data;
};

// CREATE (POST)
export const startAnalysisJob = async (file: File, name: string, onProgress: (p: number) => void): Promise<{ jobId: string }> => {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('name', name.trim());

  const response = await api.post("/analysis/start", formData, {
    onUploadProgress: (e) => {
      if (e.total) onProgress(Math.round((e.loaded * 100) / e.total));
    },
  });
  return response.data;
};

// UPDATE (PUT)
export const updateAnalysisJob = async (jobId: string, data: { name: string }): Promise<void> => {
  await api.put(`/analysis/${jobId}`, data);
};

// DELETE
export const deleteAnalysisJob = async (jobId: string): Promise<void> => {
  await api.delete(`/analysis/${jobId}`);
};

// REPROCESS (POST CUSTOM ACTION)
export const reprocessAnalysisJob = async (jobId: string): Promise<{ message: string }> => {
  const response = await api.post(`/analysis/reprocess/${jobId}`);
  return response.data;
};