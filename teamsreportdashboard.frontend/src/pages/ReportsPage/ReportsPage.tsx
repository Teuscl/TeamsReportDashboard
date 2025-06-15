// src/pages/ReportsPage/ReportsPage.tsx (NOVO NOME DE ARQUIVO E COMPONENTE)
import React, { useEffect, useState, useCallback } from 'react';
import { ColumnDef, SortingState } from '@tanstack/react-table';
import { DataTable } from '../../components/CustomTable/DataTable'; // Seu DataTable customizado
import { MoreHorizontal } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { ReportFormModal } from '@/components/ReportFormModal'; // Modal unificado para Reports
import { getReports, deleteReport } from '@/services/reportService'; // Serviços para Report
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { Report } from '@/types/Report'; // Sua interface Report
import { format } from 'date-fns'; // Para formatar datas
// Se você tiver uma proteção de rota específica para quem pode ver esta página,
// o AuthContext pode ser usado para verificações adicionais,
// mas a proteção principal deve vir do componente de Rota em App.tsx.

const ReportsPage: React.FC = () => {
  const [reports, setReports] = useState<Report[]>([]);
  const { user: currentUser, isLoading: authIsLoading } = useAuth(); // Útil para permissões futuras
  
  const [modalMode, setModalMode] = useState<'create' | 'edit' | null>(null);
  const [reportForModal, setReportForModal] = useState<Report | null>(null);
  const [dataLoading, setDataLoading] = useState(true);

  const fetchReports = useCallback(async () => {
    setDataLoading(true);
    try {
      const data = await getReports();
      setReports(data);
    } catch (error) {
      console.error('Erro ao buscar relatórios:', error);
      toast.error("Erro ao buscar relatórios. Verifique o console.");
    } finally {
      setDataLoading(false);
    }
  }, []);

  useEffect(() => {
    // Se esta página requer autenticação, o ProtectedRoute em App.tsx já deve ter cuidado disso.
    // Se houver uma verificação de role específica, ela também deve estar na definição da rota.
    // Este useEffect busca os dados se a autenticação inicial já foi processada.
    if (authIsLoading) {
      return; 
    }
    fetchReports();
  }, [authIsLoading, fetchReports]); // fetchReports está em useCallback, então é seguro aqui.

  const handleDelete = async (id: number) => {
    const originalReports = [...reports]; // Cópia para possível reversão otimista
    const confirmed = window.confirm("Você tem certeza que deseja excluir este relatório?");
    if (!confirmed) return;

    // Atualização otimista (remove da UI antes da confirmação do backend)
    setReports(prev => prev.filter(report => report.id !== id));
    try {
      await deleteReport(id);
      toast.success("Relatório removido com sucesso.");
    } catch (error: any) {
      setReports(originalReports); // Reverte em caso de erro
      const message = error?.response?.data?.message || "Erro ao excluir relatório.";
      toast.error(`Erro ao excluir relatório: ${message}`);
    }
  };
  
  const initialSortConfig: SortingState = [
    {
      id: 'requestDate', // A chave da coluna que você quer ordenar
      desc: true,        // 'true' para ordenar da mais recente para a mais antiga
    }
  ];
  const handleOpenCreateModal = () => {
    setReportForModal(null);
    setModalMode('create');
  };

  const handleOpenEditModal = (reportToEdit: Report) => {
    setReportForModal(reportToEdit);
    setModalMode('edit');
  };

  const closeModal = () => {
    setModalMode(null);
    setReportForModal(null);
  };

  const handleSaveSuccess = () => {
    fetchReports(); // Recarrega a lista da tabela após criar ou editar
  };

   const columns: ColumnDef<Report>[] = [
    { 
      accessorKey: 'requesterName', 
      header: 'Solicitante' // Ordenável por padrão
    },
    { 
      accessorKey: 'requesterEmail', 
      header: 'Email Solicitante',
      enableSorting: false, // Desativado
    },
    { 
      accessorKey: 'technicianName', 
      header: 'Técnico', 
      enableSorting: false, // Desativado
      cell: ({ row }) => row.original.technicianName || <span className="text-xs text-muted-foreground">N/A</span> 
    },
    { 
      accessorKey: 'requestDate', 
      header: 'Data Solicitação', // Ordenável por padrão
      cell: ({ row }) => {
        try {
          return format(new Date(row.original.requestDate), 'dd/MM/yyyy');
        } catch {
          return row.original.requestDate;
        }
      }
    },
    { 
      accessorKey: 'reportedProblem', 
      header: 'Problema Relatado', 
      enableSorting: false, // Desativado
      cell: ({row}) => (
        <div className="max-w-xs whitespace-normal break-words" title={row.original.reportedProblem}>
          {row.original.reportedProblem}
        </div>
      )
    },
    { 
      accessorKey: 'category', 
      header: 'Categoria',
      enableSorting: false, // Desativado
    },
    { 
      accessorKey: 'firstResponseTime', 
      header: 'Tpo. 1ª Resp.',
      enableSorting: false, // Desativado
    },
    {
      id: "actions",
      header: () => <div className="text-right">Ações</div>,
      enableSorting: false, // Ações nunca devem ser ordenáveis
      cell: ({ row }) => {
        const reportRowData = row.original;
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
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => handleOpenEditModal(reportRowData)}>Editar</DropdownMenuItem>
                <DropdownMenuItem 
                  onClick={() => handleDelete(reportRowData.id)} 
                  className="text-destructive focus:text-destructive focus:bg-destructive/10"
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

  if (authIsLoading) { // Verifica se a autenticação ainda está carregando
    return <div className="container mx-auto py-10 text-center">Carregando...</div>;
  }

  // Se a página tiver alguma regra de acesso baseada em role, 
  // o componente de rota (ex: AdminRoute) deve cuidar do redirecionamento.
  // Aqui, apenas verificamos se há um usuário logado para o contexto, se necessário.
  // if (!currentUser) {
  //   return <div className="container mx-auto py-10 text-center">Você precisa estar logado para ver esta página.</div>;
  // }

  if (dataLoading && reports.length === 0) { 
      return <div className="container mx-auto py-10 text-center">Carregando relatórios...</div>;
  }

  return (
    <div className='container mx-auto py-10 px-4 md:px-0'>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl md:text-3xl font-bold">Gerenciamento de Relatórios</h1>
        <Button onClick={handleOpenCreateModal}>
          Criar Relatório
        </Button>
      </div>
      <DataTable
        columns={columns}
        data={reports}
        filterColumnId="requesterName" // 👈 Passe o ID da coluna para filtrar
        filterPlaceholder="Filtrar por nome do solicitante..." //👈 Passe o placeholder desejado
        initialSorting={initialSortConfig}
      />
      {modalMode && (
        <ReportFormModal
          mode={modalMode}
          reportToEdit={reportForModal}
          isOpen={!!modalMode}
          onClose={closeModal}
          onSaveSuccess={handleSaveSuccess}
        />
      )}
    </div>
  );
};

export default ReportsPage;