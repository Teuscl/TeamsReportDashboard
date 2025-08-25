import React, { useState, useCallback, useEffect, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { DataTable } from '@/components/CustomTable/DataTable';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { getAnalysisJobs, startAnalysisJob, reprocessAnalysisJob } from '@/services/analysisService';
import { AnalysisJob } from '@/types/AnalysisJob';
import { toast } from 'sonner';
import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Badge } from '@/components/ui/badge';
import { FileUp, Loader2, RefreshCw, RotateCw } from 'lucide-react';
import { useJobPolling } from './useJobPolling';
import { Progress } from "@/components/ui/progress";
import { Input } from '@/components/ui/input'; // ✅ Componente de Input importado

const ImportsPage: React.FC = () => {
    const [jobs, setJobs] = useState<AnalysisJob[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [fileToUpload, setFileToUpload] = useState<File | null>(null);
    const [jobName, setJobName] = useState<string>(''); // ✅ Novo estado para o nome do job
    const [isUploading, setIsUploading] = useState(false);
    const [uploadProgress, setUploadProgress] = useState<number>(0);
    const [reprocessingId, setReprocessingId] = useState<string | null>(null);

    const fetchJobs = useCallback(async () => {
        if (jobs.length === 0) setIsLoading(true);
        try {
            const data = await getAnalysisJobs();
            setJobs(data.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()));
        } catch (error) {
            toast.error("Falha ao buscar o histórico de importações.");
        } finally {
            setIsLoading(false);
        }
    }, [jobs.length]);

    useEffect(() => {
        fetchJobs();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    
    useJobPolling(jobs, setJobs);

    const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (event.target.files && event.target.files[0]) {
            const file = event.target.files[0];
            if (file.type === 'application/zip' || file.name.endsWith('.zip')) {
                setFileToUpload(file);
            } else {
                toast.error("Por favor, selecione um arquivo .zip");
                event.target.value = '';
                setFileToUpload(null);
            }
        }
    };

   const handleUpload = async () => {
    // ====================== DIAGNÓSTICO FINAL ======================
    console.log("Botão 'Iniciar Análise' foi clicado.");
    console.log("Valor ATUAL do estado 'jobName':", `'${jobName}'`);
    console.log("Valor ATUAL do estado 'fileToUpload':", fileToUpload);
    // ==============================================================

    if (!fileToUpload || !jobName.trim()) {
        toast.warning("Por favor, preencha o nome do job e selecione um arquivo.");
        return;
    }
    setIsUploading(true);
    setUploadProgress(0);
    try {
        await startAnalysisJob(fileToUpload, jobName.trim(), (progress) => setUploadProgress(progress));
        toast.success("Upload concluído! Análise iniciada.");
        setFileToUpload(null);
        setJobName('');
        fetchJobs(); 
    } catch (error: any) {
        // ✨ Pequena melhoria no erro para exibir a mensagem do backend
        const message = error?.response?.data?.errors?.name?.[0] || error?.response?.data?.detail || "Erro desconhecido ao iniciar importação.";
        toast.error(`Falha na importação: ${message}`);
    } finally {
        setIsUploading(false);
    }
};
    const handleReprocess = async (jobId: string) => {
        setReprocessingId(jobId);
        toast.info("Iniciando reprocessamento do job...");
        try {
            const response = await reprocessAnalysisJob(jobId);
            toast.success(response.message || "Job reenviado para processamento!");
            fetchJobs(); 
        } catch (error: any) {
            const message = error?.response?.data?.message || "Ocorreu um erro desconhecido.";
            toast.error(`Falha no reprocessamento: ${message}`);
        } finally {
            setReprocessingId(null);
        }
    };

    const columns: ColumnDef<AnalysisJob>[] = useMemo(() => [
        
        { accessorKey: 'id', header: 'ID do Job', enableSorting: false },
        {
            accessorKey: 'name',
            header: 'Nome do Importação',
            enableSorting :false
        },
        { 
            accessorKey: 'status', header: 'Status',
            cell: ({ row }) => {
                const { status, errorMessage } = row.original;
                let statusText: string = status;
                let variant: "default" | "secondary" | "destructive" = 'secondary';

                if (status === 'Completed') {
                    if (errorMessage) {
                        variant = 'destructive';
                        statusText = 'Process. Falhou';
                    } else {
                        variant = 'default';
                    }
                } else if (status === 'Failed') {
                    variant = 'destructive';
                }

                return (
                    <Badge variant={variant} className="flex items-center gap-1.5 w-32 justify-center">
                        {status === 'Pending' && <Loader2 className="h-3 w-3 animate-spin" />}
                        {statusText}
                    </Badge>
                );
            } 
        },
        { accessorKey: 'createdAt', header: 'Data de Envio', cell: ({ row }) => format(parseISO(row.original.createdAt), "dd/MM/yyyy - HH:mm", { locale: ptBR }) },
        { accessorKey: 'errorMessage', header: 'Detalhes', enableSorting: false, cell: ({ row }) => <span className="text-muted-foreground text-xs max-w-xs block truncate" title={row.original.errorMessage ?? 'Sucesso'}>{row.original.errorMessage ?? ''}</span> },
        {
            id: 'actions',
            header: () => <div className="text-right">Ações</div>,
            cell: ({ row }) => {
                const job = row.original;
                const canReprocess = job.status === 'Failed' || (job.status === 'Completed' && !!job.errorMessage);
                if (!canReprocess) return null;

                const isThisJobReprocessing = reprocessingId === job.id;
                return (
                    <div className="text-right">
                        <Button variant="ghost" size="sm" onClick={() => handleReprocess(job.id)} disabled={isThisJobReprocessing} title="Tentar reprocessar os relatórios">
                            {isThisJobReprocessing ? <Loader2 className="h-4 w-4 animate-spin" /> : <RotateCw className="h-4 w-4" />}
                            <span className="sr-only">Reprocessar</span>
                        </Button>
                    </div>
                );
            }
        }
    ], [reprocessingId]);

    return (
        <div className='container mx-auto py-10 px-4 md:px-0'>
            <h1 className="text-2xl md:text-3xl font-bold mb-6">Importação e Acompanhamento</h1>
            
            <Card className="mb-8">
                <CardHeader>
                    <CardTitle>Nova Importação</CardTitle>
                    <CardDescription>Envie um arquivo .zip contendo os chats do Teams para análise da IA.</CardDescription>
                </CardHeader>
                <CardContent>
                    {/* ✅ Formulário atualizado com campo de nome */}
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
                            <label htmlFor="file-upload" className="flex items-center justify-center w-full h-12 px-4 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md shadow-sm cursor-pointer hover:bg-gray-50 dark:bg-zinc-800 dark:text-gray-200 dark:border-zinc-700">
                                {fileToUpload ? fileToUpload.name : "Clique para selecionar o arquivo .zip"}
                            </label>
                            <input id="file-upload" name="file-upload" type="file" className="sr-only" onChange={handleFileChange} accept=".zip,application/zip" />
                        </div>
                        <Button onClick={handleUpload} disabled={!fileToUpload || !jobName.trim() || isUploading}>
                            {isUploading ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <FileUp className="mr-2 h-4 w-4" />}
                            {isUploading ? `Enviando... ${uploadProgress}%` : "Iniciar Análise"}
                        </Button>
                    </div>
                    {isUploading && <div className="mt-4"><Progress value={uploadProgress} className="w-full" /></div>}
                </CardContent>
            </Card>

            <div className="flex justify-between items-center mb-4">
                <h2 className="text-xl font-semibold">Histórico de Importações</h2>
                <Button variant="ghost" size="sm" onClick={() => fetchJobs()} disabled={isLoading}>
                    <RefreshCw className={`mr-2 h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
                    Atualizar
                </Button>
            </div>

            <DataTable columns={columns} data={jobs} filterColumnId="name" filterPlaceholder="Filtrar por nome do Job..." />
        </div>
    );
};

export default ImportsPage;