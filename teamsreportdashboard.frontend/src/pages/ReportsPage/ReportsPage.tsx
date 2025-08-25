// src/pages/ReportsPage/ReportsPage.tsx
import React, { useEffect, useState, useCallback, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
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
import {
  AlertDialog, AlertDialogAction, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Calendar as CalendarComponent } from "@/components/ui/calendar";
import { ReportFormModal } from '@/components/ReportFormModal';
import { getReports, deleteReport } from '@/services/reportService';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { Report } from '@/types/Report';
import { format, isWithinInterval, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Checkbox } from '@/components/ui/checkbox';
import { useNavigate } from 'react-router-dom';

const ReportsPage: React.FC = () => {
  const [reports, setReports] = useState<Report[]>([]);
  const { user: currentUser, isLoading: authIsLoading } = useAuth();
  const navigate = useNavigate();

  const [modalMode, setModalMode] = useState<'create' | 'edit' | null>(null);
  const [reportForModal, setReportForModal] = useState<Report | null>(null);
  const [dataLoading, setDataLoading] = useState(true);
  
  const [problemToShow, setProblemToShow] = useState<string | null>(null);

  // Estados para filtros
  const [selectedTechnician, setSelectedTechnician] = useState<string>('all');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [dateRange, setDateRange] = useState<DateRange | undefined>({ from: undefined, to: undefined });
  const [isDatePickerOpen, setIsDatePickerOpen] = useState(false);
  const [tempDateRange, setTempDateRange] = useState<DateRange | undefined>(dateRange);

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

  const uniqueTechnicians = useMemo(() => {
    const technicians = reports
      .map(report => report.technicianName)
      .filter((name): name is string => Boolean(name));
    return Array.from(new Set(technicians)).sort();
  }, [reports]);

  const uniqueCategories = useMemo(() => Array.from(new Set(reports.map(r => r.category).filter(Boolean))).sort(), [reports]);

  const filteredReports = useMemo(() => {
    return reports.filter(report => {
      if (selectedTechnician !== 'all' && report.technicianName !== selectedTechnician) {
        return false;
      }
      if (selectedCategory !== 'all' && report.category !== selectedCategory) {
        return false;
      }
      if (dateRange?.from && dateRange?.to) {
        try {
          const reportDate = parseISO(report.requestDate);
          const endDate = new Date(dateRange.to);
          endDate.setHours(23, 59, 59, 999);
          return isWithinInterval(reportDate, { start: dateRange.from, end: endDate });
        } catch (error) {
          console.warn('Erro ao filtrar por data:', error);
          return true;
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

  const handleClearFilters = () => {
    setSelectedTechnician('all');
    setSelectedCategory('all');
    setDateRange({ from: undefined, to: undefined });
    setTempDateRange({ from: undefined, to: undefined });
  };

  const handleApplyDateFilter = () => {
    setDateRange(tempDateRange);
    setIsDatePickerOpen(false);
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
                                <DropdownMenuItem onSelect={(e) => e.preventDefault()} onClick={() => setProblemToShow(report.reportedProblem)}>
                                    Ver Problema Relatado
                                </DropdownMenuItem>
                                <DropdownMenuItem onSelect={(e) => e.preventDefault()} onClick={() => handleOpenEditModal(report)}>
                                    Editar
                                </DropdownMenuItem>
                                <DropdownMenuSeparator />
                                <DropdownMenuItem onSelect={(e) => e.preventDefault()} onClick={() => handleDelete(report.id)} className="text-destructive focus:text-destructive">
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
  }, [currentUser]); // Adicionado currentUser como dependência

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
             <Popover 
                open={isDatePickerOpen} 
                onOpenChange={(isOpen) => {
                  setIsDatePickerOpen(isOpen);
                  if (isOpen) {
                    setTempDateRange(dateRange);
                  }
                }}
             >
               <PopoverTrigger asChild>
                 <Button variant="outline" className="w-full justify-start text-left font-normal">
                   <Calendar className="mr-2 h-4 w-4" />
                   {dateRange?.from ? (dateRange.to ? `${format(dateRange.from, "dd/MM/yyyy")} - ${format(dateRange.to, "dd/MM/yyyy")}` : `A partir de ${format(dateRange.from, "dd/MM/yyyy")}`) : "Selecionar período"}
                 </Button>
               </PopoverTrigger>
               <PopoverContent className="w-auto p-0" align="start">
                 <CalendarComponent
                   initialFocus
                   mode="range"
                   defaultMonth={tempDateRange?.from}
                   selected={tempDateRange}
                   onSelect={setTempDateRange}
                   numberOfMonths={2}
                   locale={ptBR}
                 />
                 <div className="p-3 border-t flex justify-end gap-2">
                    <Button variant="ghost" onClick={() => setIsDatePickerOpen(false)}>
                      Cancelar
                    </Button>
                    <Button onClick={handleApplyDateFilter}>
                      Aplicar
                    </Button>
                 </div>
               </PopoverContent>
             </Popover>
           </div>

          {(selectedTechnician !== 'all' || selectedCategory !== 'all' || dateRange?.from) && (
            <Button variant="ghost" onClick={handleClearFilters}>
              <X className="mr-2 h-4 w-4" />
              Limpar Filtros
            </Button>
          )}
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
      
      <AlertDialog
        open={!!problemToShow}
        onOpenChange={(isOpen) => {
          if (!isOpen) {
            setProblemToShow(null);
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Problema Relatado</AlertDialogTitle>
            <AlertDialogDescription
              className="text-base text-foreground max-h-[60vh] overflow-y-auto pt-4"
              style={{ whiteSpace: 'pre-wrap' }}
            >
              {problemToShow}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogAction onClick={() => setProblemToShow(null)}>
              Fechar
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

    </div>
  );
};

export default ReportsPage;