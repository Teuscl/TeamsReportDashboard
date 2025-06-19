import React, { useEffect, useState, useCallback } from 'react';
import { ColumnDef, SortingState, SortingFn } from '@tanstack/react-table';
import { DataTable } from '@/components/CustomTable/DataTable';
import { MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { getRequesters, deleteRequester } from '@/services/requesterService';
import { RequesterDto } from '@/types/Requester';
import { toast } from 'sonner';
// Import do modal corrigido (será criado no próximo passo)
import { RequesterFormModal } from '@/components/RequesterFormModal';
import { Checkbox } from '@/components/ui/checkbox';

const RequestersPage: React.FC = () => {
 const [requesters, setRequesters] = useState<RequesterDto[]>([]);
 const [dataLoading, setDataLoading] = useState(true);
 const [modalMode, setModalMode] = useState<'create' | 'edit' | null>(null);
 const [requesterToEdit, setRequesterToEdit] = useState<RequesterDto | null>(null);
 const [isModalOpen, setIsModalOpen] = useState(false);

 const fetchRequesters = useCallback(async () => {
   setDataLoading(true);
   try {
     const data = await getRequesters();
     setRequesters(data);
   } catch (error) {
     toast.error("Erro ao buscar solicitantes.");
   } finally {
     setDataLoading(false);
   }
 }, []);

 useEffect(() => {
   fetchRequesters();
 }, [fetchRequesters]);

 const handleDelete = (requester: RequesterDto) => {

  
    toast(`Tem certeza que deseja excluir "${requester.name}"?`, {
      description: "Esta ação não pode ser desfeita. Relatórios associados a este solicitante podem impedir a exclusão.",
      duration: Infinity,
      position: "top-center",
      className: "flex flex-col justify-center items-center",  
      action: {
        label: "Confirmar Exclusão",
        onClick: async () => {
          try {
            await deleteRequester(requester.id);
            toast.success("Solicitante removido com sucesso.");
            fetchRequesters();
          } catch (error: any) {
            toast.error(error?.response?.data?.message || 'Erro ao excluir solicitante.');
          }
        },
      },
      
      cancel: {
        label: "Cancelar",
        onClick: () => {}, 
      },
      classNames: { actionButton: 'bg-destructive text-destructive-foreground' },
    });
  };

 const handleOpenCreateModal = () => {
   setRequesterToEdit(null);
   setModalMode('create');
   setIsModalOpen(true);
 };

 const handleOpenEditModal = (requester: RequesterDto) => {
   setRequesterToEdit(requester);
   setModalMode('edit');
   setIsModalOpen(true);
 };

 const closeModal = () => {
   setIsModalOpen(false);
   setModalMode(null);
   setRequesterToEdit(null);
 };

 const handleSaveSuccess = () => {
   closeModal();
   fetchRequesters();
 };
 const initialSortConfig: SortingState = [
     {
       id: 'name', // A chave da coluna que você quer ordenar
       desc: false,        // 'true' para ordenar da mais recente para a mais antiga
     }
   ];


  const localeSort: SortingFn<RequesterDto> = (rowA, rowB, columnId) => {
  const valA = rowA.getValue(columnId) as string;
  const valB = rowB.getValue(columnId) as string;
  
  // Usamos localeCompare com o local 'pt-BR'
  // A opção sensitivity: 'base' trata 'e' e 'é' como o mesmo caractere base para ordenação.
  return valA.localeCompare(valB, 'pt-BR', { sensitivity: 'base' });
  ;

 const columns: ColumnDef<RequesterDto>[] = [
   {
      id: "select",
      header: ({ table }) => ( <Checkbox checked={table.getIsAllPageRowsSelected() || (table.getIsSomePageRowsSelected() && "indeterminate")} onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)} aria-label="Select all" /> ),
      cell: ({ row }) => ( <Checkbox checked={row.getIsSelected()} onCheckedChange={(value) => row.toggleSelected(!!value)} aria-label="Select row" /> ),
      enableSorting: false,
      enableHiding: false,
    },
   { accessorKey: 'name', header: 'Nome', enableSorting: true},
   { accessorKey: 'email', header: 'Email', enableSorting: false },
   {
     accessorKey: 'departmentName',
     header: 'Departamento',
     enableSorting: false,
     cell: ({ row }) => row.original.departmentName || <span className="text-muted-foreground">Sem Depto.</span>
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
               <Button variant="ghost" className="h-8 w-8 p-0"><span className="sr-only">Abrir menu</span><MoreHorizontal /></Button>
             </DropdownMenuTrigger>
             <DropdownMenuContent align="end">
               <DropdownMenuLabel>Ações</DropdownMenuLabel>
               <DropdownMenuItem onClick={() => handleOpenEditModal(requester)}>Editar</DropdownMenuItem>
               <DropdownMenuItem className="text-destructive" onClick={() => handleDelete(requester)}>Excluir</DropdownMenuItem>
             </DropdownMenuContent>
           </DropdownMenu>
         </div>
       );
     }
   },
 ];

 if (dataLoading) {
   return <div className="container mx-auto py-10 text-center">Carregando solicitantes...</div>;
 }

 return (
   <div className='container mx-auto py-10 px-4 md:px-0'>
     <div className="flex justify-between items-center mb-6">
       <h1 className="text-2xl md:text-3xl font-bold">Gerenciamento de Solicitantes</h1>
       <Button onClick={handleOpenCreateModal}>Criar Solicitante</Button>
     </div>
     <DataTable
       columns={columns}
       data={requesters}
       filterColumnId="name"
       filterPlaceholder="Filtrar por nome..."
       initialSorting={initialSortConfig}
     />
     {isModalOpen && (
       <RequesterFormModal
         isOpen={isModalOpen}
         onClose={closeModal}
         onSaveSuccess={handleSaveSuccess}
         mode={modalMode || 'create'}
         requesterToEdit={requesterToEdit}
       />
     )}
   </div>
 );
};

export default RequestersPage;