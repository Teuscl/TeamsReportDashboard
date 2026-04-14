import React, { useState, useEffect, useCallback } from "react";
import { toast } from "sonner";
import { format, parseISO } from "date-fns";
import { ptBR } from "date-fns/locale";
import { Save, History, Loader2, AlertTriangle } from "lucide-react";

import { getPrompt, updatePrompt } from "@/services/promptService";
import { PromptHistoryEntry } from "@/types/Prompt";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";

const PromptPage: React.FC = () => {
  const [content, setContent] = useState("");
  const [savedContent, setSavedContent] = useState("");
  const [history, setHistory] = useState<PromptHistoryEntry[]>([]);
  const [lastUpdatedAt, setLastUpdatedAt] = useState<string | null>(null);
  const [lastUpdatedBy, setLastUpdatedBy] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  const isDirty = content !== savedContent;

  const loadPrompt = useCallback(async () => {
    try {
      setIsLoading(true);
      const data = await getPrompt();
      setContent(data.content);
      setSavedContent(data.content);
      setHistory(data.history);
      setLastUpdatedAt(data.lastUpdatedAt);
      setLastUpdatedBy(data.lastUpdatedBy);
    } catch {
      toast.error("Falha ao carregar o prompt de análise.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadPrompt();
  }, [loadPrompt]);

  const handleSave = async () => {
    if (!isDirty) return;

    setIsSaving(true);
    try {
      await updatePrompt(content);
      setSavedContent(content);
      toast.success("Prompt atualizado com sucesso.");
      await loadPrompt();
    } catch (err: unknown) {
      const message =
        (err as { response?: { data?: { message?: string; errors?: string[] } } })
          ?.response?.data?.message ??
        (err as { response?: { data?: { errors?: string[] } } })?.response?.data
          ?.errors?.[0] ??
        "Falha ao salvar o prompt.";
      toast.error(message);
    } finally {
      setIsSaving(false);
    }
  };

  const formatDate = (iso: string) =>
    format(parseISO(iso), "dd/MM/yyyy 'às' HH:mm", { locale: ptBR });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6 p-1 max-w-4xl mx-auto w-full">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Prompt de Análise</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Instrução enviada ao modelo de IA para cada análise de atendimento.
          </p>
        </div>
        {isDirty && (
          <Badge variant="secondary" className="flex items-center gap-1">
            <AlertTriangle className="h-3 w-3" />
            Alterações não salvas
          </Badge>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Conteúdo atual</CardTitle>
          <CardDescription>
            {lastUpdatedAt && lastUpdatedBy
              ? `Última edição por ${lastUpdatedBy} em ${formatDate(lastUpdatedAt)}`
              : lastUpdatedAt
              ? `Última edição em ${formatDate(lastUpdatedAt)}`
              : "Nenhuma edição registrada via plataforma (versão inicial do arquivo)."}
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <Textarea
            value={content}
            onChange={(e) => setContent(e.target.value)}
            className="min-h-[420px] font-mono text-sm resize-y"
            placeholder="Digite o prompt de análise aqui..."
          />
          <div className="flex items-center justify-between">
            <span className="text-xs text-muted-foreground">
              {content.length.toLocaleString("pt-BR")} / 50.000 caracteres
            </span>
            <Button
              onClick={handleSave}
              disabled={!isDirty || isSaving}
            >
              {isSaving ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Save className="mr-2 h-4 w-4" />
              )}
              Salvar alterações
            </Button>
          </div>
        </CardContent>
      </Card>

      {history.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <History className="h-5 w-5" />
              Histórico de versões
            </CardTitle>
            <CardDescription>
              Últimas {history.length} versão(ões) salvas. Cada salvamento cria
              um registro permanente para auditoria.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <ul className="flex flex-col gap-3">
              {history.map((entry, index) => (
                <li
                  key={entry.id}
                  className="flex flex-col gap-1 border-l-2 pl-4 border-muted-foreground/30"
                >
                  <div className="flex items-center gap-2 text-xs text-muted-foreground">
                    <span>{formatDate(entry.createdAt)}</span>
                    {entry.createdByEmail && (
                      <>
                        <span>·</span>
                        <span>{entry.createdByEmail}</span>
                      </>
                    )}
                    {index === 0 && (
                      <Badge variant="default" className="text-xs py-0">
                        atual
                      </Badge>
                    )}
                  </div>
                  <p className="text-sm text-foreground/80 line-clamp-2">
                    {entry.contentPreview}
                  </p>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default PromptPage;
