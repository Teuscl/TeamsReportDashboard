import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { ColumnDef, SortingState, SortingFn } from '@tanstack/react-table';
import { DataTable } from '@/components/CustomTable/DataTable';
import { MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import { 
  DropdownMenu, 
  DropdownMenuContent, 
  DropdownMenuItem, 
  DropdownMenuLabel, 
  DropdownMenuTrigger 
} from "@/components/ui/dropdown-menu";
import { getRequesters, deleteRequester } from '@/services/requesterService';
import { RequesterDto } from '@/types/Requester';
import { toast } from 'sonner';
import { RequesterFormModal } from '@/components/RequesterFormModal';
import { Checkbox } from '@/components/ui/checkbox';

type ModalMode = 'create' | 'edit' | null;

const RequestersPage: React.FC = () => {
  const [requesters, setRequesters] = useState<RequesterDto[]>([]);
  const [dataLoading, setDataLoading] = useState(true);
  const [modalMode, setModalMode] = useState<ModalMode>(null);
  const [requesterToEdit, setRequesterToEdit] = useState<RequesterDto | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);

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
                // Tenta executar a exclusão
                await deleteRequester(requester.id);

                // Se chegou aqui, a exclusão foi bem-sucedida
                toast.success(`Solicitante "${requester.name}" excluído com sucesso.`);
                fetchRequesters(); // Atualiza a lista

            } catch (error: any) {
                // Se deu erro, captura a exceção aqui
                console.error('Erro ao excluir solicitante:', error);

                // Extrai a mensagem de erro específica do backend
                const errorMessage = error?.response?.data?.errors?.[0] || 'Ocorreu um erro inesperado.';

                // Exibe um toast de erro customizado e mais detalhado
                toast.error("Falha na Exclusão", {
                    description: errorMessage,
                    duration: 8000, // Duração maior para o usuário ler
                    position: "top-center",
                });
            }
        };

        // Este é o toast de confirmação inicial
        toast.warning(`Tem certeza que deseja excluir "${requester.name}"?`, {
            description: "Esta ação não pode ser desfeita.",
            duration: Infinity,
            position: "top-center",
            action: {
                label: "Confirmar Exclusão",
                onClick: () => performDelete(), // Chama a função que contém o try/catch
            },
            cancel: {
                label: "Cancelar",
                onClick: () => toast.dismiss(),
            },
            classNames: { 
                actionButton: 'bg-destructive text-destructive-foreground hover:bg-destructive/90' 
            },
        });
    }, [fetchRequesters]);

  const handleOpenCreateModal = useCallback(() => {
    setRequesterToEdit(null);
    setModalMode('create');
    setIsModalOpen(true);
  }, []);

  const handleOpenEditModal = useCallback((requester: RequesterDto) => {
    setRequesterToEdit(requester);
    setModalMode('edit');
    setIsModalOpen(true);
  }, []);

  const closeModal = useCallback(() => {
    setIsModalOpen(false);
    setModalMode(null);
    setRequesterToEdit(null);
  }, []);

  const handleSaveSuccess = useCallback(() => {
    closeModal();
    fetchRequesters();
  }, [closeModal, fetchRequesters]);

  // Configuração inicial de ordenação
  const initialSortConfig: SortingState = useMemo(() => [
    {
      id: 'name',
      desc: false,
    }
  ], []);

  // Função de ordenação localizada
  const localeSort: SortingFn<RequesterDto> = useCallback((rowA, rowB, columnId) => {
    const valA = rowA.getValue(columnId) as string;
    const valB = rowB.getValue(columnId) as string;
    
    if (!valA && !valB) return 0;
    if (!valA) return 1;
    if (!valB) return -1;
    
    return valA.localeCompare(valB, 'pt-BR', { 
      sensitivity: 'base',
      numeric: true 
    });
  }, []);

  // Definição das colunas (memoizada para evitar re-renders desnecessários)
  const columns: ColumnDef<RequesterDto>[] = useMemo(() => [
    {
      id: "select",
      header: ({ table }) => (
        <Checkbox
          checked={
            table.getIsAllPageRowsSelected() ||
            (table.getIsSomePageRowsSelected() && "indeterminate")
          }
          onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)}
          aria-label="Selecionar todos"
        />
      ),
      cell: ({ row }) => (
        <Checkbox
          checked={row.getIsSelected()}
          onCheckedChange={(value) => row.toggleSelected(!!value)}
          aria-label="Selecionar linha"
        />
      ),
      enableSorting: false,
      enableHiding: false,
    },
    {
      accessorKey: 'name',
      header: 'Nome',
      enableSorting: true,
      sortingFn: localeSort,
    },
    {
      accessorKey: 'email',
      header: 'Email',
      enableSorting: false,
    },
    {
      accessorKey: 'departmentName',
      header: 'Departamento',
      enableSorting: false,
      cell: ({ row }) => (
        row.original.departmentName || 
        <span className="text-muted-foreground">Sem Depto.</span>
      )
    },
    {
      id: "actions",
      header: () => <div className="text-right">Ações</div>,
      enableSorting: false,
      size: 100,
      cell: ({ row }) => {
        const requester = row.original;
        return (
          <div className="text-right">
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="h-8 w-8 p-0">
                  <span className="sr-only">Abrir menu</span>
                  <MoreHorizontal className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuLabel>Ações</DropdownMenuLabel>
                <DropdownMenuItem onClick={() => handleOpenEditModal(requester)}>
                  Editar
                </DropdownMenuItem>
                <DropdownMenuItem 
                  className="text-destructive focus:text-destructive" 
                  onClick={() => handleDelete(requester)}
                >
                  Excluir
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        );
      }
    },
  ], [localeSort, handleOpenEditModal, handleDelete]);

  // Estado de loading
  if (dataLoading) {
    return (
      <div className="container mx-auto py-10 text-center">
        <div className="flex flex-col items-center gap-4">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
          <p>Carregando solicitantes...</p>
        </div>
      </div>
    );
  }

  // Estado vazio
  if (!requesters.length) {
    return (
      <div className='container mx-auto py-10 px-4 md:px-0'>
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl md:text-3xl font-bold">Gerenciamento de Solicitantes</h1>
          <Button onClick={handleOpenCreateModal}>Criar Solicitante</Button>
        </div>
        <div className="text-center py-10">
          <p className="text-muted-foreground mb-4">Nenhum solicitante encontrado.</p>
          <Button onClick={handleOpenCreateModal} variant="outline">
            Criar Primeiro Solicitante
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className='container mx-auto py-10 px-4 md:px-0'>
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
        <h1 className="text-2xl md:text-3xl font-bold">Gerenciamento de Solicitantes</h1>
        <Button onClick={handleOpenCreateModal}>
          Criar Solicitante
        </Button>
      </div>

      <DataTable
        columns={columns}
        data={requesters}
        filterColumnId="name"
        filterPlaceholder="Filtrar por nome..."
        initialSorting={initialSortConfig}
      />

      {isModalOpen && modalMode && (
        <RequesterFormModal
          isOpen={isModalOpen}
          onClose={closeModal}
          onSaveSuccess={handleSaveSuccess}
          mode={modalMode}
          requesterToEdit={requesterToEdit}
        />
      )}
    </div>
  );
};

export default RequestersPage;