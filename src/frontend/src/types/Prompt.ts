export interface PromptHistoryEntry {
  id: string;
  contentPreview: string;
  createdAt: string;
  createdByEmail: string | null;
}

export interface PromptVersionDetail {
  id: string;
  content: string;
  createdAt: string;
  createdByEmail: string | null;
}

export interface PromptResponse {
  content: string;
  lastUpdatedAt: string | null;
  lastUpdatedBy: string | null;
  history: PromptHistoryEntry[];
}
