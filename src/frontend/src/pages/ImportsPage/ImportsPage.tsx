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
  deleteAnalysisJob,
} from "@/services/analysisService";
import { AnalysisJob } from "@/types/AnalysisJob";
import { toast } from "sonner";
import { format, parseISO } from "date-fns";
import { ptBR } from "date-fns/locale";
import { Badge } from "@/components/ui/badge";
import { FileUp, Loader2, RefreshCw, RotateCw, MoreHorizontal, Edit, Trash2 } from "lucide-react";
import { useJobPolling } from "./useJobPolling";
import { Progress } from "@/components/ui/progress";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { EditJobModal } from "@/components/EditJobModal";

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
  const [confirmDeleteJob, setConfirmDeleteJob] = useState<AnalysisJob | null>(null);
  
  // Estados do modal de edição - simplificados
  const [jobToEdit, setJobToEdit] = useState<AnalysisJob | null>(null);

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

  const handleOpenEditModal = useCallback((job: AnalysisJob) => {
      setJobToEdit(job);
    }, []);

    // 2. FECHAR O MODAL: Apenas limpa o job selecionado.
  const handleCloseEditModal = () => {
    setJobToEdit(null);
  };

  // 3. QUANDO SALVAR COM SUCESSO: Apenas manda recarregar a lista.
  const handleEditSuccess = () => {
    fetchJobs();
  };

  const handleDelete = useCallback(async (job: AnalysisJob) => {
    try {
      await deleteAnalysisJob(job.id);
      toast.success("Job removido com sucesso!");
      setConfirmDeleteJob(null);
      await fetchJobs();
    } catch (error: any) {
      const message =
        error?.response?.data?.detail ??
        error?.response?.data?.message ??
        "Erro desconhecido ao excluir job.";
      toast.error(`Falha na exclusão: ${message}`);
    }
  }, [fetchJobs]);

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
      { 
        accessorKey: "id", 
        header: "ID do Job", 
        enableSorting: false,
        cell: ({ row }) => (
          <span className="font-mono text-xs">{row.original.id}</span>
        ),
      },
      { 
        accessorKey: "name", 
        header: "Nome da Importação", 
        enableSorting: false,
        cell: ({ row }) => (
          <span className="max-w-xs truncate block" title={row.original.name}>
            {row.original.name}
          </span>
        ),
      },
      {
        accessorKey: "status",
        header: "Status",
        cell: ({ row }) =>
          renderStatus(row.original.status, row.original.errorMessage || undefined),
      },
      {
        accessorKey: "createdAt",
        header: "Data de Envio",
        cell: ({ row }) => (
          <span className="whitespace-nowrap">
            {format(parseISO(row.original.createdAt), "dd/MM/yyyy - HH:mm", {
              locale: ptBR,
            })}
          </span>
        ),
      },
      {
        accessorKey: "errorMessage",
        header: "Detalhes",
        enableSorting: false,
        cell: ({ row }) => (
          row.original.errorMessage ? (
            <Button
              variant="link"
              className="text-xs text-red-600 truncate max-w-xs p-0 h-auto"
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
            (job.status === "Completed");
          
          const canEdit = job.status !== "Processing";
          const canDelete = job.status !== "Processing" ;
          const isThisJobReprocessing = reprocessingId === job.id;
           // Adiciona um estado para controlar o DropdownMenu
          const [isMenuOpen, setIsMenuOpen] = useState(false);

          // Função que organiza a abertura do modal
          const handleEditClick = () => {
            // 1. Fecha o menu
            setIsMenuOpen(false);
            // 2. Abre o modal
            handleOpenEditModal(job);
          };

          // Função para Reprocessar
          const handleReprocessClick = () => {
            setIsMenuOpen(false); // Fecha o menu
            setConfirmReprocessJob(job); // Abre o modal de confirmação
          };

          // Função para Excluir
          const handleDeleteClick = () => {
            setIsMenuOpen(false); // Fecha o menu
            setConfirmDeleteJob(job); // Abre o modal de confirmação
          };

          return (
            <div className="text-right">
              <DropdownMenu open={isMenuOpen} onOpenChange={setIsMenuOpen}>
                <DropdownMenuTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-8 w-8 p-0"
                    disabled={isThisJobReprocessing}
                  >
                    {isThisJobReprocessing ? (
                      <Loader2 className="h-4 w-4 animate-spin" />
                    ) : (
                      <MoreHorizontal className="h-4 w-4" />
                    )}
                    <span className="sr-only">Abrir menu</span>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="w-48">
                  <DropdownMenuItem
                    onClick={handleEditClick}
                    disabled={!canEdit}
                  >
                    <Edit className="mr-2 h-4 w-4" />
                    Editar nome
                  </DropdownMenuItem>
                  
                  {canReprocess && (
                    <>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem
                        onSelect={handleReprocessClick}
                        disabled={isThisJobReprocessing}
                      >
                        <RotateCw className="mr-2 h-4 w-4" />
                        Reprocessar
                      </DropdownMenuItem>
                    </>
                  )}
                  
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    onClick={handleDeleteClick}
                    disabled={!canDelete}
                    className="text-red-600 focus:text-red-600"
                  >
                    <Trash2 className="mr-2 h-4 w-4" />
                    Excluir
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
          );
        },
      },
    ],
    [reprocessingId, handleOpenEditModal]
  );

  return (
    <div className="container mx-auto py-6 px-4 space-y-6">
      <h1 className="text-2xl md:text-3xl font-bold">
        Importação e Acompanhamento
      </h1>

      {/* BOTÃO DE TESTE */}
    

      {/* Form de Upload */}
      <Card>
        <CardHeader>
          <CardTitle>Nova Importação</CardTitle>
          <CardDescription>
            Envie um arquivo .zip contendo os chats do Teams para análise da IA.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col gap-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Input
                type="text"
                placeholder="Nome do Job (Ex: Atendimentos 05/25)"
                value={jobName}
                onChange={(e) => setJobName(e.target.value)}
                disabled={isUploading}
              />
              <div>
                <label
                  htmlFor="file-upload"
                  className="flex items-center justify-center w-full h-10 px-4 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md shadow-sm cursor-pointer hover:bg-gray-50 dark:bg-zinc-800 dark:text-gray-200 dark:border-zinc-700 transition-colors"
                >
                  <span className="truncate">
                    {fileToUpload ? fileToUpload.name : "Clique para selecionar o arquivo .zip"}
                  </span>
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
            </div>
            
            <div className="flex justify-end">
              <Button
                onClick={handleUpload}
                disabled={!fileToUpload || !jobName.trim() || isUploading}
                className="w-full md:w-auto"
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
              <Progress value={uploadProgress} className="w-full" />
            )}
          </div>
        </CardContent>
      </Card>

      {/* Histórico */}
      <div>
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-4">
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

        <div className="overflow-x-auto">
          <DataTable
            columns={columns}
            data={jobs}
            filterColumnId="name"
            filterPlaceholder="Filtrar por nome do Job..."
          />
        </div>
      </div>

      {/* Dialog Detalhes do Erro */}
      <Dialog open={!!selectedError} onOpenChange={() => setSelectedError(null)}>
        <DialogContent className="max-w-lg">
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

      <EditJobModal
        isOpen={!!jobToEdit}
        job={jobToEdit}
        onClose={handleCloseEditModal}
        onSaveSuccess={handleEditSuccess}
      />
      
        

      {/* Dialog Confirmação Reprocessamento */}
      <Dialog
        open={!!confirmReprocessJob}
        onOpenChange={() => setConfirmReprocessJob(null)}
      >
        <DialogContent className="max-w-md">
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

      {/* Dialog Confirmação Exclusão */}
      <Dialog
        open={!!confirmDeleteJob}
        onOpenChange={() => setConfirmDeleteJob(null)}
      >
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Confirmar Exclusão</DialogTitle>
            <DialogDescription>
              Deseja realmente excluir o job{" "}
              <span className="font-semibold">
                {confirmDeleteJob?.name}
              </span>
              ? Esta ação não pode ser desfeita.
            </DialogDescription>
          </DialogHeader>
          
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmDeleteJob(null)}>
              Cancelar
            </Button>
            <Button
              variant="destructive"
              onClick={() => confirmDeleteJob && handleDelete(confirmDeleteJob)}             
            >
              <Trash2 className="mr-2 h-4 w-4" />
              Excluir Permanentemente
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default ImportsPage;