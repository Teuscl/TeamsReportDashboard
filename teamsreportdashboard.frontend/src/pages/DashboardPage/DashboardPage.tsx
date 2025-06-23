import React, { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { ClipboardList, Clock, Building, Users, PlusCircle } from 'lucide-react';
import { getDashboardData, DashboardData, ChartData } from '@/services/dashboardService';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { DashboardDetailsModal } from '@/components/DashboardDetailModal';
import { useTheme } from "@/components/theme-provider";

// Componente de Tooltip customizado para os gráficos
const CustomTooltip = ({ active, payload, label }: any) => {
  if (active && payload && payload.length) {
    return (
      <div className="rounded-lg border bg-background p-2 shadow-sm">
        <div className="flex flex-col">
          <span className="text-[0.70rem] uppercase text-muted-foreground">{label}</span>
          <span className="font-bold text-foreground">{payload[0].value}</span>
        </div>
      </div>
    );
  }
  return null;
};

const DashboardPage: React.FC = () => {
  const { theme } = useTheme();
  const [data, setData] = useState<DashboardData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);
  const [modalContent, setModalContent] = useState<{ title: string; data: ChartData[] } | null>(null);

  // Define a cor do texto do gráfico com base no tema atual
  const chartTextColor = theme === 'dark' ? '#94a3b8' : '#334155'; // Cores (slate-400 / slate-700)

  useEffect(() => {
    const fetchData = async () => {
      setIsLoading(true);
      try {
        const result = await getDashboardData();
        setData(result);
      } catch (error) {
        console.error("Erro ao buscar dados do dashboard:", error);
        toast.error("Não foi possível carregar os dados do dashboard.");
      } finally {
        setIsLoading(false);
      }
    };
    fetchData();
  }, []);

  const handleOpenDetailsModal = (title: string, data: ChartData[]) => {
    setModalContent({ title, data });
    setIsDetailsModalOpen(true);
  };

  if (isLoading) {
    return <div className="flex-1 space-y-4 p-4 pt-6 md:p-8">Carregando dados do dashboard...</div>;
  }

  if (!data) {
    return <div className="flex-1 space-y-4 p-4 pt-6 md:p-8 text-red-500">Falha ao carregar dados. Tente novamente mais tarde.</div>;
  }

  return (
    <div className="flex-1 space-y-4 p-4 pt-6 md:p-8">
      <h2 className="text-3xl font-bold tracking-tight">Dashboard de Atendimentos</h2>
      
      {/* Cards de KPI */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Atendimentos no Mês</CardTitle>
            <ClipboardList className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{data.totalAtendimentosMes}</div>
            <p className="text-xs text-muted-foreground">Total de atendimentos no mês atual</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">TME 1ª Resposta</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{data.tempoMedioPrimeiraResposta}</div>
            <p className="text-xs text-muted-foreground">Tempo médio para o primeiro contato</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Departamentos</CardTitle>
            <Building className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{data.totalDepartamentos}</div>
            <p className="text-xs text-muted-foreground">Total de departamentos cadastrados</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Solicitantes</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{data.totalSolicitantes}</div>
            <p className="text-xs text-muted-foreground">Total de solicitantes cadastrados</p>
          </CardContent>
        </Card>
      </div>

      {/* Gráficos com botão "Ver todos" */}
      <Card>
        <CardHeader>
          <CardTitle>Análise de Atendimentos</CardTitle>
          <CardDescription>Visualize os atendimentos por diferentes perspectivas.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <Tabs defaultValue="porMes" className="w-full">
            <TabsList className="grid w-full grid-cols-2 md:grid-cols-4">
              <TabsTrigger value="porMes">Total por Mês</TabsTrigger>
              <TabsTrigger value="porCategoria">Por Categoria</TabsTrigger>
              <TabsTrigger value="porTecnico">Por Técnico</TabsTrigger>
              <TabsTrigger value="porDepartamento">Por Departamento</TabsTrigger>
            </TabsList>

            <TabsContent value="porMes" className="pt-4">
              <ResponsiveContainer width="100%" height={350} key={`porMes-${theme}`}>
                <BarChart data={data.atendimentosPorMes}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="name" fontSize={12} tickLine={false} axisLine={false} stroke={chartTextColor} />
                  <YAxis fontSize={12} tickLine={false} axisLine={false} stroke={chartTextColor} />
                  <Tooltip cursor={{ className: "fill-muted" }} content={<CustomTooltip />} />
                  <Legend wrapperStyle={{fontSize: "0.8rem", color: chartTextColor}}/>
                  <Bar dataKey="total" name="Atendimentos" radius={[4, 4, 0, 0]} className="fill-primary" />
                </BarChart>
              </ResponsiveContainer>
            </TabsContent>

            <TabsContent value="porCategoria" className="pt-4 space-y-2">
              <div className="flex items-center justify-end">
                <Button variant="link" className="h-auto p-0 text-xs" onClick={() => handleOpenDetailsModal('Todas as Categorias', data.problemasPorCategoria)}>
                  <PlusCircle className="mr-1 h-3 w-3" /> Ver todos
                </Button>
              </div>
              <ResponsiveContainer width="100%" height={350} key={`porCategoria-${theme}`}>
                <BarChart data={data.problemasPorCategoria.slice(0, 5)} layout="vertical">
                  <CartesianGrid strokeDasharray="3 3" horizontal={false} className="stroke-muted"/>
                  <XAxis type="number" fontSize={12} tickLine={false} axisLine={false} stroke={chartTextColor}/>
                  <YAxis type="category" dataKey="name" fontSize={12} tickLine={false} axisLine={false} width={120} stroke={chartTextColor} />
                  <Tooltip cursor={{ className: "fill-muted" }} content={<CustomTooltip />} />
                  <Legend wrapperStyle={{fontSize: "0.8rem", color: chartTextColor}}/>
                  <Bar dataKey="total" name="Problemas" radius={[0, 4, 4, 0]} className="fill-primary" />
                </BarChart>
              </ResponsiveContainer>
            </TabsContent>
            
            <TabsContent value="porTecnico" className="pt-4 space-y-2">
               <div className="flex items-center justify-end">
                <Button variant="link" className="h-auto p-0 text-xs" onClick={() => handleOpenDetailsModal('Atendimentos por Técnico', data.atendimentosPorTecnico)}>
                  <PlusCircle className="mr-1 h-3 w-3" /> Ver todos
                </Button>
              </div>
              <ResponsiveContainer width="100%" height={350} key={`porTecnico-${theme}`}>
                <BarChart data={data.atendimentosPorTecnico.slice(0, 5)}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} className="stroke-muted"/>
                  <XAxis dataKey="name" fontSize={12} tickLine={false} axisLine={false} stroke={chartTextColor}/>
                  <YAxis fontSize={12} tickLine={false} axisLine={false} stroke={chartTextColor}/>
                  <Tooltip cursor={{ className: "fill-muted" }} content={<CustomTooltip />} />
                  <Legend wrapperStyle={{fontSize: "0.8rem", color: chartTextColor}}/>
                  <Bar dataKey="total" name="Atendimentos" radius={[4, 4, 0, 0]} className="fill-primary" />
                </BarChart>
              </ResponsiveContainer>
            </TabsContent>

            <TabsContent value="porDepartamento" className="pt-4 space-y-2">
              <div className="flex items-center justify-end">
                <Button variant="link" className="h-auto p-0 text-xs" onClick={() => handleOpenDetailsModal('Atendimentos por Departamento', data.atendimentosPorDepartamento)}>
                  <PlusCircle className="mr-1 h-3 w-3" /> Ver todos
                </Button>
              </div>
              <ResponsiveContainer width="100%" height={350} key={`porDepartamento-${theme}`}>
                <BarChart data={data.atendimentosPorDepartamento.slice(0, 5)} layout="vertical">
                    <CartesianGrid strokeDasharray="3 3" horizontal={false} className="stroke-muted"/>
                    <XAxis type="number" fontSize={12} tickLine={false} axisLine={false} stroke={chartTextColor}/>
                    <YAxis type="category" dataKey="name" fontSize={12} tickLine={false} axisLine={false} width={120} stroke={chartTextColor} />
                    <Tooltip cursor={{ className: "fill-muted" }} content={<CustomTooltip />} />
                    <Legend wrapperStyle={{fontSize: "0.8rem", color: chartTextColor}}/>
                    <Bar dataKey="total" name="Atendimentos" radius={[0, 4, 4, 0]} className="fill-primary" />
                </BarChart>
              </ResponsiveContainer>
            </TabsContent>

          </Tabs>
        </CardContent>
      </Card>

      {modalContent && (
        <DashboardDetailsModal
          isOpen={isDetailsModalOpen}
          onClose={() => setIsDetailsModalOpen(false)}
          title={modalContent.title}
          data={modalContent.data}
        />
      )}
    </div>
  );
};

export default DashboardPage;
