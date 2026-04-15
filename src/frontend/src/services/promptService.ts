import api from "./axiosConfig";
import { PromptResponse, PromptVersionDetail } from "@/types/Prompt";

export const getPrompt = async (): Promise<PromptResponse> => {
  const response = await api.get("/prompt");
  return response.data;
};

export const getPromptVersion = async (id: string): Promise<PromptVersionDetail> => {
  const response = await api.get(`/prompt/history/${id}`);
  return response.data;
};

export const updatePrompt = async (content: string): Promise<void> => {
  await api.put("/prompt", { content });
};
