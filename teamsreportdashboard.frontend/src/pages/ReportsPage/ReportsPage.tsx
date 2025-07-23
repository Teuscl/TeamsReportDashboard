// src/pages/ReportsPage/ReportsPage.tsx
import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { ColumnDef, SortingState } from '@tanstack/react-table';
import { DateRange } from 'react-day-picker';
import { DataTable } from '../../components/CustomTable/DataTable';
import { MoreHorizontal, Calendar, X, Upload } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select";
import {
  Popover, PopoverContent, PopoverTrigger,
} from "@/components/ui/popover";
import { Calendar as CalendarComponent } from "@/components/ui/calendar";
import { ReportFormModal } from '@/components/ReportFormModal';
import { getReports, deleteReport } from '@/services/reportService';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { Report } from '@/types/Report';
import { format, isWithinInterval, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Checkbox } from '@/components/ui/checkbox';
import { Badge } from '@/components/ui/badge';
import { useNavigate } from 'react-router-dom';

const ReportsPage: React.FC = () => {
  const [reports, setReports] = useState<Report[]>([]);
  const { user: currentUser, isLoading: authIsLoading } = useAuth();
  const navigate = useNavigate();

  const [modalMode, setModalMode] = useState<'create' | 'edit' | null>(null);
  const [reportForModal, setReportForModal] = useState<Report | null>(null);
  const [dataLoading, setDataLoading] = useState(true);

  // Estados para filtros
  const [selectedTechnician, setSelectedTechnician] = useState<string>('all');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [dateRange, setDateRange] = useState<DateRange | undefined>({ from: undefined, to: undefined });
  const [isDatePickerOpen, setIsDatePickerOpen] = useState(false);

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
    if (authIsLoading) {
      return;
    }
    fetchReports();
  }, [authIsLoading, fetchReports]);

  // Deriva listas únicas para os filtros
  const uniqueTechnicians = useMemo(() => {
    const technicians = reports
      .map(report => report.technicianName)
      .filter((name): name is string => Boolean(name));
    return Array.from(new Set(technicians)).sort();
  }, [reports]);

  const uniqueCategories = useMemo(() => Array.from(new Set(reports.map(r => r.category).filter(Boolean))).sort(), [reports]);

  // Filtra os dados da tabela
  const filteredReports = useMemo(() => {
    return reports.filter(report => {
      if (selectedTechnician !== 'all' && report.technicianName !== selectedTechnician) {
        return false;
      }
      if (selectedCategory !== 'all' && report.category !== selectedCategory) {
        return false;
      }
      if (dateRange?.from) {
        try {
          const reportDate = parseISO(report.requestDate);
          if (dateRange.from && dateRange.to) {
            return isWithinInterval(reportDate, { start: dateRange.from, end: dateRange.to });
          } else if (dateRange.from) {
            return reportDate >= dateRange.from;
          }
        } catch (error) {
          console.warn('Erro ao filtrar por data:', error);
        }
      }
      return true;
    });
  }, [reports, selectedTechnician, selectedCategory, dateRange]);

  const handleDelete = async (id: number) => {
    const originalReports = [...reports];
    toast.custom((t) => (
      <div className="bg-white dark:bg-zinc-950 p-4 rounded-md shadow-lg w-[380px] border border-input">
        <h3 className="text-lg font-semibold mb-2">Tem certeza que deseja excluir este relatório?</h3>
        <p className="text-sm text-muted-foreground mb-4">
          Esta ação não pode ser desfeita. Todos os dados associados a este relatório serão perdidos.
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onClick={() => toast.dismiss(t)}>
            Cancelar
          </Button>
          <Button
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            onClick={async () => {
              setReports(prev => prev.filter(report => report.id !== id));
              toast.dismiss(t);
              try {
                await deleteReport(id);
                toast.success("Relatório removido com sucesso.");
              } catch (error: any) {
                setReports(originalReports);
                const message = error?.response?.data?.message || "Erro ao excluir o relatório.";
                toast.error(`Falha ao excluir: ${message}`);
              }
            }}
          >
            Confirmar Exclusão
          </Button>
        </div>
      </div>
    ), {
      position: "top-center",
      duration: Infinity
    });
  };
  
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
    fetchReports();
    closeModal();
  };

  const columns = useMemo<ColumnDef<Report>[]>(() => {
    const baseColumns: ColumnDef<Report>[] = [
      {
        id: "select",
        header: ({ table }) => (<Checkbox checked={table.getIsAllPageRowsSelected() || (table.getIsSomePageRowsSelected() && "indeterminate")} onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)} aria-label="Selecionar todos" />),
        cell: ({ row }) => (<Checkbox checked={row.getIsSelected()} onCheckedChange={(value) => row.toggleSelected(!!value)} aria-label="Selecionar linha" />),
        enableSorting: false,
        enableHiding: false,
      },
      { accessorKey: 'requestDate', header: 'Data Solicitação', cell: ({ row }) => format(parseISO(row.original.requestDate), 'dd/MM/yyyy') },
      { accessorKey: 'requesterName', header: 'Solicitante' },
      { accessorKey: 'technicianName', header: 'Técnico', cell: ({ row }) => row.original.technicianName || <span className="text-xs text-muted-foreground">N/A</span> },
      { accessorKey: 'category', header: 'Categoria' },
      { accessorKey: 'firstResponseTime', header: 'Tpo. 1ª Resp.' },
    ];

    if (currentUser && currentUser.role !== 2) {
        const actionsColumn: ColumnDef<Report> = {
            id: "actions",
            header: () => <div className="text-right">Ações</div>,
            cell: ({ row }) => {
                const report = row.original;
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
                                <DropdownMenuItem onClick={() => handleOpenEditModal(report)}>Editar</DropdownMenuItem>
                                <DropdownMenuSeparator />
                                <DropdownMenuItem onClick={() => handleDelete(report.id)} className="text-destructive focus:text-destructive">
                                    Excluir
                                </DropdownMenuItem>
                            </DropdownMenuContent>
                        </DropdownMenu>
                    </div>
                );
            }
        };
        return [...baseColumns, actionsColumn];
    }

    return baseColumns;
  }, [currentUser]);

  if (authIsLoading || (dataLoading && reports.length === 0)) {
    return <div className="container mx-auto py-10 text-center">Carregando relatórios...</div>;
  }

  return (
    <div className='container mx-auto py-10 px-4 md:px-0'>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl md:text-3xl font-bold">Gerenciamento de Relatórios</h1>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => navigate('/imports')}>
            <Upload className="mr-2 h-4 w-4" />
            Importar para Análise
          </Button>
          {currentUser && currentUser.role !== 2 && (
            <Button onClick={handleOpenCreateModal}>
              Criar Relatório
            </Button>
          )}
        </div>
      </div>

      <div className="mb-6 p-4 border rounded-lg bg-muted/50">
        <div className="flex flex-wrap gap-4 items-end">
          <div className="min-w-[200px]">
            <label className="text-sm font-medium mb-2 block">Técnico</label>
            <Select value={selectedTechnician} onValueChange={setSelectedTechnician}>
              <SelectTrigger><SelectValue placeholder="Todos os técnicos" /></SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos os técnicos</SelectItem>
                {uniqueTechnicians.map(technician => (
                  <SelectItem key={technician} value={technician}>{technician}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="min-w-[200px]">
            <label className="text-sm font-medium mb-2 block">Categoria</label>
            <Select value={selectedCategory} onValueChange={setSelectedCategory}>
              <SelectTrigger><SelectValue placeholder="Todas as categorias" /></SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todas as categorias</SelectItem>
                {uniqueCategories.map(category => (
                  <SelectItem key={category} value={category}>{category}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="min-w-[250px]">
             <label className="text-sm font-medium mb-2 block">Período</label>
             <Popover open={isDatePickerOpen} onOpenChange={setIsDatePickerOpen}>
               <PopoverTrigger asChild>
                 <Button variant="outline" className="w-full justify-start text-left font-normal">
                   <Calendar className="mr-2 h-4 w-4" />
                   {dateRange?.from ? (dateRange.to ? `${format(dateRange.from, "dd/MM/yyyy")} - ${format(dateRange.to, "dd/MM/yyyy")}` : `A partir de ${format(dateRange.from, "dd/MM/yyyy")}`) : "Selecionar período"}
                 </Button>
               </PopoverTrigger>
               <PopoverContent className="w-auto p-0" align="start">
                 <CalendarComponent
                   initialFocus mode="range" defaultMonth={dateRange?.from} selected={dateRange}
                   onSelect={(range) => {
                     setDateRange(range);
                     if (range?.from && range?.to) {
                       setIsDatePickerOpen(false);
                     }
                   }}
                   numberOfMonths={2} locale={ptBR}
                 />
               </PopoverContent>
             </Popover>
           </div>
        </div>
      </div>

      <DataTable
        columns={columns}
        data={filteredReports}
        filterColumnId="requesterName"
        filterPlaceholder="Filtrar por nome do solicitante..."
      />

      {modalMode && (
        <ReportFormModal
          mode={modalMode}
          reportToEdit={reportForModal}
          isOpen={!!modalMode}
          onClose={closeModal}
          onSaveSuccess={handleSaveSuccess}
          uniqueCategories={uniqueCategories}
        />
      )}
    </div>
  );
};

export default ReportsPage;