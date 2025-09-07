import React, { useState, useCallback, useEffect, useMemo, useRef } from "react";
import { ColumnDef } from "@tanstack/react-table";
import { DataTable } from "@/components/CustomTable/DataTable";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  getAnalysisJobs,
  startAnalysisJob,
  reprocessAnalysisJob,
} from "@/services/analysisService";
import { AnalysisJob } from "@/types/AnalysisJob";
import { toast } from "sonner";
import { format, parseISO } from "date-fns";
import { ptBR } from "date-fns/locale";
import { Badge } from "@/components/ui/badge";
import { FileUp, Loader2, RefreshCw, RotateCw } from "lucide-react";
import { useJobPolling } from "./useJobPolling";
import { Progress } from "@/components/ui/progress";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
} from "@/components/ui/dialog";

// mapa de status traduzido e estilizado
const statusMap: Record<
  string,
  { label: string; variant: "default" | "secondary" | "destructive"; showSpinner?: boolean }
> = {
  Pending: { label: "Na fila", variant: "secondary", showSpinner: true },
  Processing: { label: "Processando", variant: "secondary", showSpinner: true },
  Completed: { label: "Concluído", variant: "default" },
  Failed: { label: "Falhou", variant: "destructive" },
};

const ImportsPage: React.FC = () => {
  const [jobs, setJobs] = useState<AnalysisJob[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [fileToUpload, setFileToUpload] = useState<File | null>(null);
  const [jobName, setJobName] = useState<string>("");
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<number>(0);
  const [reprocessingId, setReprocessingId] = useState<string | null>(null);

  const [selectedError, setSelectedError] = useState<string | null>(null);
  const [confirmReprocessJob, setConfirmReprocessJob] = useState<AnalysisJob | null>(null);

  const fileInputRef = useRef<HTMLInputElement | null>(null);

  // busca jobs
  const fetchJobs = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await getAnalysisJobs();
      setJobs(
        data.sort(
          (a, b) =>
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        )
      );
    } catch {
      toast.error("Falha ao buscar o histórico de importações.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchJobs();
  }, [fetchJobs]);

  useJobPolling(jobs, setJobs);

  const handleFileChange = useCallback(
    (event: React.ChangeEvent<HTMLInputElement>) => {
      const f = event.target.files?.[0];
      if (!f) return;

      if (f.type === "application/zip" || f.name.toLowerCase().endsWith(".zip")) {
        setFileToUpload(f);
      } else {
        toast.error("Por favor, selecione um arquivo .zip");
        event.target.value = "";
        setFileToUpload(null);
      }
    },
    []
  );

  const handleUpload = useCallback(async () => {
    if (!fileToUpload || !jobName.trim()) {
      toast.warning("Por favor, preencha o nome do job e selecione um arquivo.");
      return;
    }
    // limite opcional de tamanho
    if (fileToUpload.size > 200 * 1024 * 1024) {
      toast.error("Arquivo muito grande (limite de 200MB).");
      return;
    }

    setIsUploading(true);
    setUploadProgress(0);
    try {
      await startAnalysisJob(fileToUpload, jobName.trim(), (p) =>
        setUploadProgress(Math.round(p))
      );
      toast.success("Upload concluído! Análise iniciada.");
      setFileToUpload(null);
      setJobName("");
      if (fileInputRef.current) fileInputRef.current.value = "";
      await fetchJobs();
    } catch (error: any) {
      const message =
        error?.response?.data?.errors?.name?.[0] ??
        error?.response?.data?.detail ??
        error?.response?.data?.message ??
        "Erro desconhecido ao iniciar importação.";
      toast.error(`Falha na importação: ${message}`);
    } finally {
      setIsUploading(false);
    }
  }, [fileToUpload, jobName, fetchJobs]);

  const handleReprocess = useCallback(
    async (job: AnalysisJob) => {
      setReprocessingId(job.id);
      toast.info("Iniciando reprocessamento do job...");
      try {
        const response = await reprocessAnalysisJob(job.id);
        toast.success(response?.message || "Job reenviado para processamento!");
        await fetchJobs();
      } catch (error: any) {
        const message =
          error?.response?.data?.detail ??
          error?.response?.data?.message ??
          "Ocorreu um erro desconhecido.";
        toast.error(`Falha no reprocessamento: ${message}`);
      } finally {
        setReprocessingId(null);
        setConfirmReprocessJob(null);
      }
    },
    [fetchJobs]
  );

  const renderStatus = (status: string, errorMessage?: string) => {
    let meta = statusMap[status] ?? {
      label: status,
      variant: "secondary" as const,
    };
    if (status === "Completed" && errorMessage) {
      meta = { label: "Concluído com erros", variant: "destructive" };
    }
    return (
      <Badge
        variant={meta.variant}
        className="flex items-center gap-1.5 w-40 justify-center"
      >
        {meta.showSpinner && <Loader2 className="h-3 w-3 animate-spin" />}
        {meta.label}
      </Badge>
    );
  };

  const columns: ColumnDef<AnalysisJob>[] = useMemo(
    () => [
      { accessorKey: "id", header: "ID do Job", enableSorting: false },
      { accessorKey: "name", header: "Nome da Importação", enableSorting: false },
      {
        accessorKey: "status",
        header: "Status",
        cell: ({ row }) =>
          renderStatus(row.original.status, row.original.errorMessage),
      },
      {
        accessorKey: "createdAt",
        header: "Data de Envio",
        cell: ({ row }) =>
          format(parseISO(row.original.createdAt), "dd/MM/yyyy - HH:mm", {
            locale: ptBR,
          }),
      },
      {
        accessorKey: "errorMessage",
        header: "Detalhes",
        enableSorting: false,
        cell: ({ row }) => (
          row.original.errorMessage ? (
            <Button
              variant="link"
              className="text-xs text-red-600 truncate max-w-xs"
              onClick={() => setSelectedError(row.original.errorMessage!)}
            >
              Ver erro
            </Button>
          ) : (
            <span className="text-muted-foreground text-xs">Sucesso</span>
          )
        ),
      },
      {
        id: "actions",
        header: () => <div className="text-right">Ações</div>,
        cell: ({ row }) => {
          const job = row.original;
          const canReprocess =
            job.status === "Failed" ||
            (job.status === "Completed" );
          if (!canReprocess) return null;

          const isThisJobReprocessing = reprocessingId === job.id;
          return (
            <div className="text-right">
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setConfirmReprocessJob(job)}
                disabled={isThisJobReprocessing}
                aria-busy={isThisJobReprocessing}
                title="Tentar reprocessar os relatórios"
              >
                {isThisJobReprocessing ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <RotateCw className="h-4 w-4" />
                )}
                <span className="sr-only">Reprocessar</span>
              </Button>
            </div>
          );
        },
      },
    ],
    [reprocessingId]
  );

  return (
    <div className="container mx-auto py-10 px-4 md:px-0">
      <h1 className="text-2xl md:text-3xl font-bold mb-6">
        Importação e Acompanhamento
      </h1>

      {/* Form de Upload */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle>Nova Importação</CardTitle>
          <CardDescription>
            Envie um arquivo .zip contendo os chats do Teams para análise da IA.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col sm:flex-row items-center gap-4">
            <div className="flex-1 w-full">
              <Input
                type="text"
                placeholder="Nome do Job (Ex: Atendimentos 05/25)"
                value={jobName}
                onChange={(e) => setJobName(e.target.value)}
                disabled={isUploading}
              />
            </div>
            <div className="flex-1 w-full">
              <label
                htmlFor="file-upload"
                className="flex items-center justify-center w-full h-12 px-4 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md shadow-sm cursor-pointer hover:bg-gray-50 dark:bg-zinc-800 dark:text-gray-200 dark:border-zinc-700"
              >
                {fileToUpload ? fileToUpload.name : "Clique para selecionar o arquivo .zip"}
              </label>
              <input
                ref={fileInputRef}
                id="file-upload"
                name="file-upload"
                type="file"
                className="sr-only"
                onChange={handleFileChange}
                accept=".zip,application/zip"
              />
            </div>
            <Button
              onClick={handleUpload}
              disabled={!fileToUpload || !jobName.trim() || isUploading}
            >
              {isUploading ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <FileUp className="mr-2 h-4 w-4" />
              )}
              {isUploading ? `Enviando... ${uploadProgress}%` : "Iniciar Análise"}
            </Button>
          </div>
          {isUploading && (
            <div className="mt-4">
              <Progress value={uploadProgress} className="w-full" />
            </div>
          )}
        </CardContent>
      </Card>

      {/* Histórico */}
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-xl font-semibold">Histórico de Importações</h2>
        <Button
          variant="ghost"
          size="sm"
          onClick={fetchJobs}
          disabled={isLoading}
        >
          <RefreshCw
            className={`mr-2 h-4 w-4 ${isLoading ? "animate-spin" : ""}`}
          />
          Atualizar
        </Button>
      </div>

      <DataTable
        columns={columns}
        data={jobs}
        filterColumnId="name"
        filterPlaceholder="Filtrar por nome do Job..."
      />

      {/* Dialog Detalhes do Erro */}
      <Dialog open={!!selectedError} onOpenChange={() => setSelectedError(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Detalhes do Erro</DialogTitle>
            <DialogDescription>
              Mensagem retornada pelo processamento.
            </DialogDescription>
          </DialogHeader>
          <div className="bg-muted p-4 rounded-md text-sm whitespace-pre-wrap break-words max-h-64 overflow-auto">
            {selectedError}
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setSelectedError(null)}>
              Fechar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Dialog Confirmação Reprocessamento */}
      <Dialog
        open={!!confirmReprocessJob}
        onOpenChange={() => setConfirmReprocessJob(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Confirmar Reprocessamento</DialogTitle>
            <DialogDescription>
              Deseja realmente reprocessar o job{" "}
              <span className="font-semibold">
                {confirmReprocessJob?.name}
              </span>
              ?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmReprocessJob(null)}>
              Cancelar
            </Button>
            <Button
              onClick={() =>
                confirmReprocessJob && handleReprocess(confirmReprocessJob)
              }
              disabled={!!reprocessingId}
            >
              {reprocessingId === confirmReprocessJob?.id ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <RotateCw className="mr-2 h-4 w-4" />
              )}
              Confirmar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default ImportsPage;
