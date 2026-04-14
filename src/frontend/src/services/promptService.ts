import api from "./axiosConfig";
import { PromptResponse } from "@/types/Prompt";

export const getPrompt = async (): Promise<PromptResponse> => {
  const response = await api.get("/prompt");
  return response.data;
};

export const updatePrompt = async (content: string): Promise<void> => {
  await api.put("/prompt", { content });
};
