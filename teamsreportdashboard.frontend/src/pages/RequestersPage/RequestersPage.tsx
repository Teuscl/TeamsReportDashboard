import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { ColumnDef, SortingState, SortingFn } from '@tanstack/react-table';
import { MoreHorizontal, Upload } from "lucide-react";

// Componentes
import { DataTable } from '@/components/CustomTable/DataTable';
import { Button } from "@/components/ui/button";
import { 
  DropdownMenu, 
  DropdownMenuContent, 
  DropdownMenuItem, 
  DropdownMenuLabel, 
  DropdownMenuTrigger 
} from "@/components/ui/dropdown-menu";
import { Checkbox } from '@/components/ui/checkbox';
import { RequesterFormModal } from '@/components/RequesterFormModal';
import { RequesterUploadModal } from '@/components/RequesterUploadModal';

// Serviços e Tipos
import { getRequesters, deleteRequester } from '@/services/requesterService';
import { RequesterDto } from '@/types/Requester';
import { toast } from 'sonner';

type ModalMode = 'create' | 'edit' | null;

const RequestersPage: React.FC = () => {
  const [requesters, setRequesters] = useState<RequesterDto[]>([]);
  const [dataLoading, setDataLoading] = useState(true);
  const [modalMode, setModalMode] = useState<ModalMode>(null);
  const [requesterToEdit, setRequesterToEdit] = useState<RequesterDto | null>(null);
  const [isFormModalOpen, setIsFormModalOpen] = useState(false);
  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);

  const fetchRequesters = useCallback(async () => {
    setDataLoading(true);
    try {
      const data = await getRequesters();
      setRequesters(data);
    } catch (error) {
      console.error('Erro ao buscar solicitantes:', error);
      toast.error("Erro ao buscar solicitantes.");
    } finally {
      setDataLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchRequesters();
  }, [fetchRequesters]);

  const handleDelete = useCallback(async (requester: RequesterDto) => {
    const performDelete = async () => {
      try {
        await deleteRequester(requester.id);
        toast.success(`Solicitante "${requester.name}" excluído com sucesso.`);
        fetchRequesters();
      } catch (error: any) {
        const errorMessage = error?.response?.data?.errors?.[0] || 'Ocorreu um erro inesperado.';
        toast.error("Falha na Exclusão", { description: errorMessage });
      }
    };
    
    toast.warning(`Tem certeza que deseja excluir "${requester.name}"?`, {
      description: "Esta ação não pode ser desfeita.",
      duration: Infinity,
      action: { label: "Confirmar Exclusão", onClick: () => performDelete() },
      cancel: { label: "Cancelar", onClick: () => toast.dismiss() },
      classNames: { actionButton: 'bg-destructive text-destructive-foreground hover:bg-destructive/90' },
    });
  }, [fetchRequesters]);
  
  const closeFormModal = useCallback(() => {
    setIsFormModalOpen(false);
    setModalMode(null);
    setRequesterToEdit(null);
  }, []);
  
  const handleSaveSuccess = useCallback(() => {
    closeFormModal();
    fetchRequesters();
  }, [fetchRequesters, closeFormModal]);

  const handleUploadSuccess = useCallback(() => {
    setIsUploadModalOpen(false);
    fetchRequesters();
  }, [fetchRequesters]);

  const handleOpenCreateModal = useCallback(() => {
    setRequesterToEdit(null);
    setModalMode('create');
    setIsFormModalOpen(true);
  }, []);

  const handleOpenEditModal = useCallback((requester: RequesterDto) => {
    setRequesterToEdit({ ...requester });
    setModalMode('edit');
    setIsFormModalOpen(true);
  }, []);

  const initialSortConfig: SortingState = useMemo(() => [{ id: 'name', desc: false }], []);

  const localeSort: SortingFn<RequesterDto> = useCallback((rowA, rowB, columnId) => {
    const valA = rowA.getValue(columnId) as string;
    const valB = rowB.getValue(columnId) as string;
    if (!valA && !valB) return 0;
    if (!valA) return 1;
    if (!valB) return -1;
    return valA.localeCompare(valB, 'pt-BR', { sensitivity: 'base', numeric: true });
  }, []);

  const columns: ColumnDef<RequesterDto>[] = useMemo(() => [
    {
      id: "select",
      header: ({ table }) => <Checkbox checked={table.getIsAllPageRowsSelected() || (table.getIsSomePageRowsSelected() && "indeterminate")} onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)} aria-label="Selecionar todos" />,
      cell: ({ row }) => <Checkbox checked={row.getIsSelected()} onCheckedChange={(value) => row.toggleSelected(!!value)} aria-label="Selecionar linha" />,
    },
    { accessorKey: 'name', header: 'Nome', enableSorting: true, sortingFn: localeSort },
    { accessorKey: 'email', header: 'Email' },
    { 
      accessorKey: 'departmentName', 
      header: 'Departamento', 
      cell: ({ row }) => row.original.departmentName || <span className="text-muted-foreground">Sem Depto.</span>
    },
    {
      id: "actions",
      cell: ({ row }) => {
        const requester = row.original;
        return (
          <div className="text-right">
            {/* ========================================================================
                👇 A CORREÇÃO DEFINITIVA DO BUG ESTÁ AQUI 👇
               ======================================================================== */}
            <DropdownMenu modal={false}>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="h-8 w-8 p-0"><span className="sr-only">Abrir menu</span><MoreHorizontal className="h-4 w-4" /></Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuLabel>Ações</DropdownMenuLabel>
                <DropdownMenuItem onClick={() => handleOpenEditModal(requester)}>Editar</DropdownMenuItem>
                <DropdownMenuItem className="text-destructive focus:text-destructive" onClick={() => handleDelete(requester)}>Excluir</DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        );
      }
    },
  ], [localeSort, handleOpenEditModal, handleDelete]);

  if (dataLoading) {
    return (
      <div className="container mx-auto py-10 text-center">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
        <p>Carregando solicitantes...</p>
      </div>
    );
  }
  
  return (
    <div className='container mx-auto py-10 px-4 md:px-0'>
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
        <h1 className="text-2xl md:text-3xl font-bold">Gerenciamento de Solicitantes</h1>
        <div className="flex gap-2">
            <Button onClick={() => setIsUploadModalOpen(true)} variant="outline"><Upload className="mr-2 h-4 w-4" />Importar CSV</Button>
            <Button onClick={handleOpenCreateModal}>Criar Solicitante</Button>
        </div>
      </div>

      <DataTable
        columns={columns}
        data={requesters}
        filterColumnId="name"
        filterPlaceholder="Filtrar por nome..."
        initialSorting={initialSortConfig}
      />

      {isFormModalOpen && (
        <RequesterFormModal
          isOpen={isFormModalOpen}
          onClose={closeFormModal}
          onSaveSuccess={handleSaveSuccess}
          mode={modalMode!}
          requesterToEdit={requesterToEdit}
        />
      )}

      {isUploadModalOpen && (
        <RequesterUploadModal 
            isOpen={isUploadModalOpen}
            onClose={() => setIsUploadModalOpen(false)}
            onUploadSuccess={handleUploadSuccess}
        />
      )}
    </div>
  );
};

export default RequestersPage;