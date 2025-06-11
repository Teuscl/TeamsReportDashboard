// src/pages/DashboardPage/DashboardPage.tsx
import React from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { ClipboardList, Clock } from 'lucide-react';

// --- DADOS DE EXEMPLO (como antes) ---
const totalAtendimentosMesAtual = 178;
const tempoMedioPrimeiraResposta = "00:12:45";
const atendimentosPorMes = [
  { mes: 'Jan', total: 120 }, { mes: 'Fev', total: 150 }, { mes: 'Mar', total: 135 },
  { mes: 'Abr', total: 190 }, { mes: 'Mai', total: 210 }, { mes: 'Jun', total: 178 },
  { mes: 'Jul', total: 220 }, { mes: 'Ago', total: 240 }, { mes: 'Set', total: 200 },
  { mes: 'Out', total: 280 }, { mes: 'Nov', total: 300 }, { mes: 'Dez', total: 230 },
];
const problemasPorCategoria = [
  { categoria: 'Hardware', total: 45 }, { categoria: 'Software', total: 82 },
  { categoria: 'Rede', total: 21 }, { categoria: 'Impressora', total: 15 }, { categoria: 'Outros', total: 15 },
];
const atendimentosPorEquipe = [
  { equipe: 'Suporte N1', total: 125 }, { equipe: 'Suporte N2', total: 40 }, { equipe: 'Infraestrutura', total: 13 },
];

// --- COMPONENTE DE TOOLTIP CUSTOMIZADO (como antes) ---
const CustomTooltip = ({ active, payload, label }: any) => {
  if (active && payload && payload.length) {
    return (
      <div className="rounded-lg border bg-background p-2 shadow-sm">
        <div className="flex flex-col">
          <span className="text-[0.70rem] uppercase text-muted-foreground">{label}</span>
          <span className="font-bold text-foreground">{`${payload[0].name}: ${payload[0].value}`}</span>
        </div>
      </div>
    );
  }
  return null;
};


const DashboardPage: React.FC = () => {
  return (
    <div className="flex-1 space-y-4 p-4 pt-6 md:p-8">
      <h2 className="text-3xl font-bold tracking-tight">Dashboard de Atendimentos</h2>
      
      {/* Cards de KPI (sem alterações) */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* ... Seus cards de métricas ... */}
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Atendimentos no Mês</CardTitle>
            <ClipboardList className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totalAtendimentosMesAtual}</div>
            <p className="text-xs text-muted-foreground">Total de atendimentos no mês atual</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">TME 1ª Resposta</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{tempoMedioPrimeiraResposta}</div>
            <p className="text-xs text-muted-foreground">Tempo médio para o primeiro contato</p>
          </CardContent>
        </Card>
      </div>

      {/* Card com os Gráficos em Abas */}
      <Card>
        <CardHeader>
          <CardTitle>Análise de Atendimentos</CardTitle>
          <CardDescription>Visualize os atendimentos por diferentes perspectivas.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <Tabs defaultValue="porMes" className="w-full">
            <TabsList className="grid w-full grid-cols-3">
              <TabsTrigger value="porMes">Total por Mês</TabsTrigger>
              <TabsTrigger value="porCategoria">Problemas por Categoria</TabsTrigger>
              <TabsTrigger value="porEquipe">Atendimentos por Equipe</TabsTrigger>
            </TabsList>

            {/* Aba "Por Mês" */}
            <TabsContent value="porMes" className="pt-4">
              <ResponsiveContainer width="100%" height={350}>
                <BarChart data={atendimentosPorMes}>
                  <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
                  <XAxis dataKey="mes" fontSize={12} tickLine={false} axisLine={false} stroke="hsl(var(--muted-foreground))" />
                  <YAxis fontSize={12} tickLine={false} axisLine={false} stroke="hsl(var(--muted-foreground))" />
                  <Tooltip cursor={{ className: "fill-muted" }} content={<CustomTooltip />} />
                  <Legend wrapperStyle={{fontSize: "0.8rem"}}/>
                  <Bar dataKey="total" name="Total de Atendimentos" radius={[4, 4, 0, 0]} className="fill-primary" />
                </BarChart>
              </ResponsiveContainer>
            </TabsContent>

            {/* Aba "Por Categoria" */}
            <TabsContent value="porCategoria" className="pt-4">
              <ResponsiveContainer width="100%" height={350}>
                <BarChart data={problemasPorCategoria} layout="vertical">
                  <CartesianGrid strokeDasharray="3 3" horizontal={false} className="stroke-muted"/>
                  <XAxis type="number" fontSize={12} tickLine={false} axisLine={false} stroke="hsl(var(--muted-foreground))"/>
                  <YAxis type="category" dataKey="categoria" fontSize={12} tickLine={false} axisLine={false} width={100} stroke="hsl(var(--muted-foreground))"/>
                  <Tooltip cursor={{ className: "fill-muted" }} content={<CustomTooltip />} />
                  <Legend wrapperStyle={{fontSize: "0.8rem"}}/>
                  <Bar dataKey="total" name="Quantidade de Problemas" radius={[0, 4, 4, 0]} className="fill-primary" />
                </BarChart>
              </ResponsiveContainer>
            </TabsContent>

            {/* Aba "Por Equipe" */}
            <TabsContent value="porEquipe" className="pt-4">
              <ResponsiveContainer width="100%" height={350}>
                <BarChart data={atendimentosPorEquipe}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} className="stroke-muted"/>
                  <XAxis dataKey="equipe" fontSize={12} tickLine={false} axisLine={false} stroke="hsl(var(--muted-foreground))"/>
                  <YAxis fontSize={12} tickLine={false} axisLine={false} stroke="hsl(var(--muted-foreground))"/>
                  <Tooltip cursor={{ className: "fill-muted" }} content={<CustomTooltip />} />
                  <Legend wrapperStyle={{fontSize: "0.8rem"}}/>
                  <Bar dataKey="total" name="Total de Atendimentos" radius={[4, 4, 0, 0]} className="fill-primary" />
                </BarChart>
              </ResponsiveContainer>
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>
    </div>
  );
};

export default DashboardPage;