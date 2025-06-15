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




//TERMINAR DE IMPLEMENTAR A PÁGINA DE DEPARTAMENTOS
//RESTA TERMINAR O UPDATE E O DELETE
//CRIAR A PAGINA DE FUNCIONARIOS AMANHA, VALIDAR LOGICA DE INSERCAO DE ATENDIMENTO

const DepartmentsPage: React.FC = () => {
  const [departments, setDepartments] = useState<Department[]>([]);
  const [dataLoading, setDataLoading] = useState(true);

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

  const handleDelete = async (id: number) => {
    const confirmed = window.confirm("Tem certeza que deseja excluir este departamento? Todos os solicitantes associados terão seu departamento definido como nulo.");
    if (!confirmed) return;

    try {
      await deleteDepartment(id);
      fetchDepartments();
      toast.success("Departamento removido com sucesso.");
    } catch (error: any) {
      toast.error(`Erro ao excluir departamento: ${error?.response?.data?.message || 'Erro desconhecido.'}`);
    }
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
                <DropdownMenuItem onClick={() => alert(`Editar ${department.name}`)}>Editar</DropdownMenuItem>
                <DropdownMenuItem className="text-red-600 focus:text-red-700" onClick={() => handleDelete(department.id)}>
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
        <Button onClick={() => alert('Abrir modal de criação')}>
          Criar Departamento
        </Button>
      </div>
      <DataTable
        columns={columns}
        data={departments}
        filterColumnId="name"
        filterPlaceholder="Filtrar por nome do departamento..."        
      />
    </div>
  );
};

export default DepartmentsPage;