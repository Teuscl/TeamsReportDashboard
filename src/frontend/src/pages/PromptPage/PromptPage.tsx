import React, { useState, useEffect, useCallback } from "react";
import { toast } from "sonner";
import { format, parseISO } from "date-fns";
import { ptBR } from "date-fns/locale";
import {
  Save,
  History,
  Loader2,
  AlertTriangle,
  RotateCcw,
  X,
  GitCompare,
  Clock,
} from "lucide-react";

import { getPrompt, getPromptVersion, updatePrompt } from "@/services/promptService";
import { PromptHistoryEntry, PromptVersionDetail } from "@/types/Prompt";
import { DiffViewer } from "@/components/DiffViewer/DiffViewer";
import { cn } from "@/lib/utils";

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

  const [selectedVersionId, setSelectedVersionId] = useState<string | null>(null);
  const [selectedVersionDetail, setSelectedVersionDetail] = useState<PromptVersionDetail | null>(null);
  const [isLoadingVersion, setIsLoadingVersion] = useState(false);

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

  const handleSelectVersion = async (entry: PromptHistoryEntry) => {
    // Toggle off if already selected
    if (entry.id === selectedVersionId) {
      setSelectedVersionId(null);
      setSelectedVersionDetail(null);
      return;
    }

    setSelectedVersionId(entry.id);
    setSelectedVersionDetail(null);
    setIsLoadingVersion(true);

    try {
      const detail = await getPromptVersion(entry.id);
      setSelectedVersionDetail(detail);
    } catch {
      toast.error("Falha ao carregar versão selecionada.");
      setSelectedVersionId(null);
    } finally {
      setIsLoadingVersion(false);
    }
  };

  const handleRestore = () => {
    if (!selectedVersionDetail) return;
    setContent(selectedVersionDetail.content);
    setSelectedVersionId(null);
    setSelectedVersionDetail(null);
    toast.info("Versão restaurada no editor. Salve para confirmar.");
  };

  const handleCloseComparison = () => {
    setSelectedVersionId(null);
    setSelectedVersionDetail(null);
  };

  const formatDate = (iso: string) =>
    format(parseISO(iso), "dd/MM/yyyy 'às' HH:mm", { locale: ptBR });

  const formatShortDate = (iso: string) =>
    format(parseISO(iso), "dd/MM/yy, HH:mm", { locale: ptBR });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-full">
        <Loader2 className="h-7 w-7 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const isComparingMode = selectedVersionId !== null;
  const selectedVersionIndex = history.findIndex((h) => h.id === selectedVersionId);
  const selectedVersionNum = selectedVersionIndex >= 0 ? history.length - selectedVersionIndex : null;

  return (
    <div className="flex flex-col flex-1 min-h-0 gap-4">
      {/* ── Page header ── */}
      <div className="flex items-start justify-between shrink-0 pt-2">
        <div>
          <h1 className="text-xl font-semibold tracking-tight">Prompt de Análise</h1>
          <p className="text-xs text-muted-foreground mt-0.5 flex items-center gap-1.5">
            <Clock className="h-3 w-3 shrink-0" />
            {lastUpdatedAt && lastUpdatedBy
              ? `Última edição por ${lastUpdatedBy} em ${formatDate(lastUpdatedAt)}`
              : lastUpdatedAt
              ? `Última edição em ${formatDate(lastUpdatedAt)}`
              : "Nenhuma edição registrada via plataforma."}
          </p>
        </div>

        <div className="flex items-center gap-3 shrink-0">
          {isDirty && (
            <Badge variant="secondary" className="flex items-center gap-1.5 text-xs">
              <AlertTriangle className="h-3 w-3" />
              Não salvo
            </Badge>
          )}
          <Button onClick={handleSave} disabled={!isDirty || isSaving} size="sm">
            {isSaving ? (
              <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
            ) : (
              <Save className="mr-1.5 h-3.5 w-3.5" />
            )}
            Salvar
          </Button>
        </div>
      </div>

      {/* ── Main split area ── */}
      <div className="flex flex-1 min-h-0 rounded-lg border border-border overflow-hidden">

        {/* ── Left: History sidebar ── */}
        <aside className="w-[264px] shrink-0 flex flex-col border-r border-border overflow-hidden bg-muted/10">
          {/* Sidebar header */}
          <div className="flex items-center gap-2 px-4 py-3 border-b border-border shrink-0">
            <History className="h-3.5 w-3.5 text-muted-foreground" />
            <span className="text-xs font-medium text-muted-foreground uppercase tracking-wider">
              Histórico
            </span>
            {history.length > 0 && (
              <span className="ml-auto text-[10px] tabular-nums text-muted-foreground/45">
                {history.length} versões
              </span>
            )}
          </div>

          {/* Version list / timeline */}
          {history.length === 0 ? (
            <div className="flex-1 flex items-center justify-center px-4">
              <p className="text-xs text-muted-foreground text-center leading-relaxed">
                Nenhuma versão registrada ainda.
              </p>
            </div>
          ) : (
            <div className="flex-1 overflow-y-auto">
              {/* Timeline wrapper */}
              <div className="relative">
                {/* Vertical connecting line */}
                {history.length > 1 && (
                  <div
                    className="absolute w-px bg-border/50"
                    style={{ left: 20, top: 26, bottom: 26 }}
                  />
                )}

                {history.map((entry, index) => {
                  const isCurrent = index === 0;
                  const isSelected = selectedVersionId === entry.id;
                  const versionNum = history.length - index;

                  return (
                    <button
                      key={entry.id}
                      onClick={() => !isCurrent && handleSelectVersion(entry)}
                      disabled={isCurrent}
                      className={cn(
                        "relative w-full flex items-start gap-3 px-3 py-2.5 text-left transition-colors duration-100",
                        isCurrent ? "cursor-default" : "hover:bg-accent/50",
                        isSelected && !isCurrent && "bg-primary/[0.06] dark:bg-primary/[0.09]"
                      )}
                    >
                      {/* Timeline node */}
                      <div
                        className={cn(
                          "shrink-0 z-10 mt-0.5 size-[14px] rounded-full border-2 flex items-center justify-center bg-background",
                          isCurrent
                            ? "border-primary"
                            : isSelected
                            ? "border-primary"
                            : "border-muted-foreground/35"
                        )}
                      >
                        {isCurrent && (
                          <div className="size-[6px] rounded-full bg-primary" />
                        )}
                      </div>

                      {/* Entry content */}
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-1.5 mb-0.5">
                          <span
                            className={cn(
                              "text-[11px] font-mono font-semibold",
                              isCurrent || isSelected
                                ? "text-foreground/80"
                                : "text-foreground/55"
                            )}
                          >
                            v{versionNum}
                          </span>
                          {isCurrent && (
                            <Badge
                              variant="default"
                              className="text-[9px] py-0 h-3.5 px-1.5 leading-none"
                            >
                              atual
                            </Badge>
                          )}
                          {isSelected && !isCurrent && (
                            <GitCompare className="h-3 w-3 text-primary ml-auto shrink-0" />
                          )}
                        </div>

                        <div className="text-[10.5px] text-muted-foreground">
                          {formatShortDate(entry.createdAt)}
                        </div>

                        {entry.createdByEmail && (
                          <div className="text-[10px] text-muted-foreground/50 truncate mt-0.5">
                            {entry.createdByEmail}
                          </div>
                        )}

                        {/* Content preview — only for non-current versions */}
                        {!isCurrent && (
                          <p className="text-[10.5px] text-foreground/35 mt-1.5 line-clamp-2 leading-relaxed font-mono">
                            {entry.contentPreview}
                          </p>
                        )}
                      </div>
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {/* Sidebar footer hint */}
          {history.length > 1 && !isComparingMode && (
            <div className="px-4 py-2.5 border-t border-border shrink-0">
              <p className="text-[10px] text-muted-foreground/50 leading-relaxed">
                Clique em uma versão para comparar com a atual.
              </p>
            </div>
          )}
        </aside>

        {/* ── Right: Editor or Diff ── */}
        <div className="flex-1 flex flex-col min-h-0 overflow-hidden">
          {isComparingMode ? (
            /* ── Comparison mode ── */
            <>
              {/* Comparison action bar */}
              <div className="flex items-center justify-between px-4 py-2 border-b border-border bg-muted/5 shrink-0">
                <div className="flex items-center gap-2 text-xs text-muted-foreground min-w-0">
                  <GitCompare className="h-3.5 w-3.5 shrink-0" />
                  <span className="truncate">
                    Comparando{" "}
                    <span className="font-mono font-semibold text-foreground/75">
                      v{selectedVersionNum}
                    </span>
                    {selectedVersionDetail && (
                      <span className="text-muted-foreground/50 ml-1">
                        ({formatShortDate(selectedVersionDetail.createdAt)})
                      </span>
                    )}
                    {" "}↔ versão atual
                  </span>
                </div>

                <div className="flex items-center gap-1.5 shrink-0 ml-3">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleRestore}
                    disabled={!selectedVersionDetail}
                    className="h-7 text-xs gap-1.5"
                  >
                    <RotateCcw className="h-3 w-3" />
                    Restaurar esta versão
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleCloseComparison}
                    className="h-7 text-xs gap-1"
                  >
                    <X className="h-3 w-3" />
                    Fechar
                  </Button>
                </div>
              </div>

              {/* Diff content area */}
              <div className="flex-1 min-h-0 overflow-hidden">
                {isLoadingVersion ? (
                  <div className="flex items-center justify-center h-full">
                    <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
                  </div>
                ) : selectedVersionDetail ? (
                  <DiffViewer
                    oldText={selectedVersionDetail.content}
                    newText={savedContent}
                    oldLabel={[
                      `v${selectedVersionNum}`,
                      formatShortDate(selectedVersionDetail.createdAt),
                      selectedVersionDetail.createdByEmail ?? "",
                    ]
                      .filter(Boolean)
                      .join(" · ")}
                    newLabel="Versão atual"
                  />
                ) : null}
              </div>
            </>
          ) : (
            /* ── Editor mode ── */
            <>
              <div className="flex-1 min-h-0 relative">
                <Textarea
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  className="absolute inset-0 resize-none rounded-none border-0 font-mono text-[13px] leading-relaxed focus-visible:ring-0 bg-transparent"
                  placeholder="Digite o prompt de análise aqui…"
                />
              </div>

              {/* Editor footer */}
              <div className="flex items-center justify-between px-4 py-2.5 border-t border-border bg-muted/5 shrink-0">
                <span className="text-[11px] text-muted-foreground tabular-nums">
                  {content.length.toLocaleString("pt-BR")} / 50.000 caracteres
                </span>
                {history.length > 1 && (
                  <span className="text-[11px] text-muted-foreground/45">
                    Selecione uma versão no histórico para comparar
                  </span>
                )}
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default PromptPage;
