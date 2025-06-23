// src/pages/ReportsPage/ReportsPage.tsx (CÓDIGO CORRIGIDO)
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
  const navigate = useNavigate(); // 2. Inicialize o hook
  
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

  // Obter listas únicas para os filtros
  const uniqueTechnicians = useMemo(() => {
    const technicians = reports
      .map(report => report.technicianName)
      .filter((name): name is string => Boolean(name))
      .filter((value, index, self) => self.indexOf(value) === index);
    return technicians.sort();
  }, [reports]);

  const uniqueCategories = useMemo(() => {
    const categories = reports
      .map(report => report.category)
      .filter((category): category is string => Boolean(category))
      .filter((value, index, self) => self.indexOf(value) === index);
    return categories.sort();
  }, [reports]);

  // Filtrar os dados baseado nos filtros selecionados
  const filteredReports = useMemo(() => {
    return reports.filter(report => {
      if (selectedTechnician !== 'all' && report.technicianName !== selectedTechnician) {
        return false;
      }
      if (selectedCategory !== 'all' && report.category !== selectedCategory) {
        return false;
      }
      if (dateRange?.from || dateRange?.to) {
        try {
          const reportDate = parseISO(report.requestDate);
          if (dateRange.from && dateRange.to) {
            return isWithinInterval(reportDate, { start: dateRange.from, end: dateRange.to });
          } else if (dateRange.from) {
            return reportDate >= dateRange.from;
          } else if (dateRange.to) {
            return reportDate <= dateRange.to;
          }
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
      <div className="bg-white dark:bg-zinc-950 p-4 rounded-md shadow-lg w-[380px] border border-white-500">
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
                const message = error?.response?.data?.message || "Erro ao excluir relatório.";
                toast.error(`Erro ao excluir relatório: ${message}`);
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

  // Funções para limpar filtros
  const clearTechnicianFilter = () => setSelectedTechnician('all');
  const clearCategoryFilter = () => setSelectedCategory('all');
  const clearDateRangeFilter = () => setDateRange({ from: undefined, to: undefined });
  
  const clearAllFilters = () => {
    setSelectedTechnician('all');
    setSelectedCategory('all');
    setDateRange({ from: undefined, to: undefined });
  };

  const hasActiveFilters = selectedTechnician !== 'all' || selectedCategory !== 'all' || dateRange?.from || dateRange?.to;

  const initialSortConfig: SortingState = [
    {
      id: 'requestDate',
      desc: true,
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
    fetchReports();
  };
  
  // ALTERAÇÃO 1: Construção dinâmica das colunas
  const columns = useMemo<ColumnDef<Report>[]>(() => {
    const baseColumns: ColumnDef<Report>[] = [
      {
        id: "select",
        header: ({ table }) => ( <Checkbox checked={table.getIsAllPageRowsSelected() || (table.getIsSomePageRowsSelected() && "indeterminate")} onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)} aria-label="Select all" /> ),
        cell: ({ row }) => ( <Checkbox checked={row.getIsSelected()} onCheckedChange={(value) => row.toggleSelected(!!value)} aria-label="Select row" /> ),
        enableSorting: false,
        enableHiding: false,
      },
      { accessorKey: 'requestDate', header: 'Data Solicitação', cell: ({ row }) => { try { return format(new Date(row.original.requestDate), 'dd/MM/yyyy'); } catch { return row.original.requestDate; } } },
      { accessorKey: 'requesterName', header: 'Solicitante' },
      { accessorKey: 'requesterEmail', header: 'Email Solicitante', enableSorting: false },
      { accessorKey: 'technicianName', header: 'Técnico', enableSorting: false, cell: ({ row }) => row.original.technicianName || <span className="text-xs text-muted-foreground">N/A</span> },
      { accessorKey: 'reportedProblem', header: 'Problema Relatado', enableSorting: false, cell: ({row}) => ( <div className="max-w-xs whitespace-normal break-words" title={row.original.reportedProblem}> {row.original.reportedProblem} </div> ) },
      { accessorKey: 'category', header: 'Categoria', enableSorting: false },
      { accessorKey: 'firstResponseTime', header: 'Tpo. 1ª Resp.', enableSorting: false },
    ];

    // Se o usuário não for 'viewer', adiciona a coluna de ações
    if (currentUser && currentUser.role !== 2) {
      const actionsColumn: ColumnDef<Report> = {
        id: "actions",
        header: () => <div className="text-right">Ações</div>,
        enableSorting: false,
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
      };
      return [...baseColumns, actionsColumn];
    }

    return baseColumns;
  }, [currentUser]); // Recalcula as colunas se o usuário mudar

  if (authIsLoading) {
    return <div className="container mx-auto py-10 text-center">Carregando...</div>;
  }

  if (dataLoading && reports.length === 0) { 
    return <div className="container mx-auto py-10 text-center">Carregando relatórios...</div>;
  }

  return (
    <div className='container mx-auto py-10 px-4 md:px-0'>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl md:text-3xl font-bold">Gerenciamento de Relatórios</h1>
        <div className="flex gap-2">
    {/* 3. Adicione o novo botão de importação */}
      <Button variant="outline" onClick={() => navigate('/imports')}>
        <Upload className="mr-2 h-4 w-4" />
        Importar para análise
      </Button>

        {currentUser && currentUser.role !== 2 && (
          <Button onClick={handleOpenCreateModal}>
            Criar Relatório
          </Button>
        )}
        </div>
        
        

      </div>

      {/* Área de Filtros */}
      <div className="mb-6 p-4 border rounded-lg bg-muted/50">
        <div className="flex flex-wrap gap-4 items-end">
          {/* Filtro por Técnico */}
          <div className="min-w-[200px]">
            <label className="text-sm font-medium mb-2 block">Técnico</label>
            <Select value={selectedTechnician} onValueChange={setSelectedTechnician}>
              <SelectTrigger>
                <SelectValue placeholder="Todos os técnicos" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todos os técnicos</SelectItem>
                {uniqueTechnicians.map(technician => (
                  <SelectItem key={technician} value={technician}>
                    {technician}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Filtro por Categoria */}
          <div className="min-w-[200px]">
            <label className="text-sm font-medium mb-2 block">Categoria</label>
            <Select value={selectedCategory} onValueChange={setSelectedCategory}>
              <SelectTrigger>
                <SelectValue placeholder="Todas as categorias" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Todas as categorias</SelectItem>
                {uniqueCategories.map(category => (
                  <SelectItem key={category} value={category}>
                    {category}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Filtro por Range de Data */}
          <div className="min-w-[250px]">
            <label className="text-sm font-medium mb-2 block">Período</label>
            <Popover open={isDatePickerOpen} onOpenChange={setIsDatePickerOpen}>
              <PopoverTrigger asChild>
                <Button variant="outline" className="w-full justify-start text-left font-normal">
                  <Calendar className="mr-2 h-4 w-4" />
                  {dateRange?.from ? ( dateRange.to ? ( `${format(dateRange.from, "dd/MM/yyyy")} - ${format(dateRange.to, "dd/MM/yyyy")}` ) : ( `A partir de ${format(dateRange.from, "dd/MM/yyyy")}` ) ) : ( "Selecionar período" )}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0" align="start">
                <CalendarComponent
                  initialFocus
                  mode="range"
                  defaultMonth={dateRange?.from}
                  selected={dateRange}
                  onSelect={(range) => {
                    setDateRange(range);
                    if (range?.from && range?.to) {
                      setIsDatePickerOpen(false);
                    }
                  }}
                  numberOfMonths={2}
                  locale={ptBR}
                />
              </PopoverContent>
            </Popover>
          </div>

          {/* Botão Limpar Filtros */}
          {hasActiveFilters && (
            <Button variant="outline" onClick={clearAllFilters} className="flex items-center gap-2">
              <X className="h-4 w-4" />
              Limpar Filtros
            </Button>
          )}
        </div>

        {/* Badges dos Filtros Ativos */}
        {hasActiveFilters && (
          <div className="flex flex-wrap gap-2 mt-4">
            {selectedTechnician !== 'all' && (
              <Badge variant="secondary" className="flex items-center gap-1">
                Técnico: {selectedTechnician}
                <button onClick={clearTechnicianFilter} className="ml-1 hover:bg-muted-foreground/20 rounded-full p-0.5"><X className="h-3 w-3" /></button>
              </Badge>
            )}
            {selectedCategory !== 'all' && (
              <Badge variant="secondary" className="flex items-center gap-1">
                Categoria: {selectedCategory}
                <button onClick={clearCategoryFilter} className="ml-1 hover:bg-muted-foreground/20 rounded-full p-0.5"><X className="h-3 w-3" /></button>
              </Badge>
            )}
            {(dateRange?.from || dateRange?.to) && (
              <Badge variant="secondary" className="flex items-center gap-1">
                Período: {dateRange.from && format(dateRange.from, "dd/MM/yyyy")} {dateRange.from && dateRange.to && " - "} {dateRange.to && format(dateRange.to, "dd/MM/yyyy")}
                <button onClick={clearDateRangeFilter} className="ml-1 hover:bg-muted-foreground/20 rounded-full p-0.5"><X className="h-3 w-3" /></button>
              </Badge>
            )}
          </div>
        )}

        {/* Contador de Resultados */}
        <div className="mt-2 text-sm text-muted-foreground">
          Mostrando {filteredReports.length} de {reports.length} relatórios
        </div>
      </div>

      <DataTable
        columns={columns}
        data={filteredReports}
        filterColumnId="requesterName"
        filterPlaceholder="Filtrar por nome do solicitante..."
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