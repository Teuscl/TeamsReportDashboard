import React, { useEffect, useState, useCallback } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { DataTable } from '@/components/CustomTable/DataTable'; // Corrigido o caminho do import
import { MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuTrigger, // Removida uma vírgula extra aqui
} from "@/components/ui/dropdown-menu";
import { getDepartments, deleteDepartment } from '@/services/departmentService'; // Importando funções do serviço
import { Department } from '@/types/Department'; // Importando o tipo do arquivo de tipos
import { toast } from 'sonner';
import { Checkbox } from "@/components/ui/checkbox";
import { DepartmentFormModal } from '@/components/DepartmentFormModal';

const DepartmentsPage: React.FC = () => {
  const [departments, setDepartments] = useState<Department[]>([]);
  const [dataLoading, setDataLoading] = useState(true);
  const [modalMode, setModalMode] = useState<'create' | 'edit' | null>(null);
  const [departmentForModal, setDepartmentForModal] = useState<Department | null>(null);

  const fetchDepartments = useCallback(async () => {
    setDataLoading(true);
    try {
      const data = await getDepartments();
      setDepartments(data);
    } catch (error) {
      console.error('Erro ao buscar departamentos:', error);
      toast.error("Erro ao buscar departamentos.");
    } finally {
      setDataLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchDepartments();
  }, [fetchDepartments]);

   const handleDelete = (id: string, departmentName: string) => {
    toast("Você tem certeza?", {
      description: `Esta ação irá excluir permanentemente o departamento "${departmentName}". Os funcionários associados não serão excluídos, mas terão seu departamento definido como nulo.`,
      position: "top-center",
      className: "flex flex-row justify-center items-center",  
        
      // Botão de Ação Principal (Excluir)
      action: {
        label: "Confirmar Exclusão",        
        onClick: async () => {
          // A lógica de exclusão agora vive dentro do onClick do botão do toast
          try {
            await deleteDepartment(id);
            toast.success("Departamento removido com sucesso.");
            fetchDepartments(); // Recarrega a lista para refletir a exclusão
          } catch (error: any) {
            toast.error(`Erro ao excluir departamento: ${error?.response?.data?.message || 'Erro desconhecido.'}`);
          }
        },
      },
      // Botão de Cancelar
      cancel: {
        label: "Cancelar",
        onClick: () => {          
        },
      },     
      
      duration: Infinity, 
    });
  };

   const handleOpenCreateModal = () => {
    setDepartmentForModal(null);
    setModalMode('create');
  };

  const handleOpenEditModal = (departmentToEdit: Department) => {
    setDepartmentForModal(departmentToEdit);
    setModalMode('edit');
  };

  const closeModal = () => {
    setModalMode(null);
    setDepartmentForModal(null);
  };
   const handleSaveSuccess = () => {
    const successMessage = modalMode === 'create' 
      ? "Departamento criado com sucesso!" 
      : "Departamento atualizado com sucesso!";
    toast.success(successMessage);
    fetchDepartments();
    closeModal();
  };

 // Em: src/pages/DepartmentsPage/DepartmentsPage.tsx

  const columns: ColumnDef<Department>[] = [
    {
      id: "select",
      header: ({ table }) => ( <Checkbox checked={table.getIsAllPageRowsSelected() || (table.getIsSomePageRowsSelected() && "indeterminate")} onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)} aria-label="Select all" /> ),
      cell: ({ row }) => ( <Checkbox checked={row.getIsSelected()} onCheckedChange={(value) => row.toggleSelected(!!value)} aria-label="Select row" /> ),
      enableSorting: false,
      enableHiding: false,
    },
    {
      accessorKey: 'name',
      header: 'Nome',       
    },    
    {
      id: "actions",
      header: () => <div className="text-right">Ações</div>, // Define um cabeçalho de texto para alinhar
      
      enableSorting: false, // 👈 Ações nunca devem ser ordenáveis
      cell: ({ row }) => {
        const department = row.original;
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
                <DropdownMenuItem onClick={() => handleOpenEditModal(department)}>Editar</DropdownMenuItem>
                <DropdownMenuItem 
                  className="text-red-600 focus:text-red-700" 
                  onClick={() => handleDelete(department.id, department.name)} // Passe o id e o nome
                >
                  Excluir
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        );
      }
    },
  ];

  
  if (dataLoading) {
    return <div className="container mx-auto py-10 text-center">Carregando departamentos...</div>;
  }
  
  return (
    <div className='container mx-auto py-10 px-4 md:px-0'>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl md:text-3xl font-bold">Gerenciamento de Departamentos</h1>
        <Button onClick={handleOpenCreateModal}>
          Criar Departamento
        </Button>
      </div>
      <DataTable
        columns={columns}
        data={departments}
        filterColumnId="name"
        filterPlaceholder="Filtrar por nome do departamento..."        
      />
      {modalMode && (
        <DepartmentFormModal
          isOpen={!!modalMode}
          onClose={closeModal}
          onSaveSuccess={handleSaveSuccess}
          mode={modalMode}
          departmentToEdit={departmentForModal}
        />
      )}
    </div>
  );
};

export default DepartmentsPage;