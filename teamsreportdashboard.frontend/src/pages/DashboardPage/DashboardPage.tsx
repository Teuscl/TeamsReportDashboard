// src/pages/DashboardPage/DashboardPage.tsx
import React from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer } from 'recharts';
import { ClipboardList, Clock, Users, HardDrive } from 'lucide-react';

// --- Dados de Exemplo (Substitua por dados da sua API) ---

// 1. Métricas (KPIs)
const totalAtendimentosMesAtual = 178;
const tempoMedioPrimeiraResposta = "00:12:45";

// 2. Dados para os Gráficos
const atendimentosPorMes = [
  { mes: 'Jan', total: 120 },
  { mes: 'Fev', total: 150 },
  { mes: 'Mar', total: 135 },
  { mes: 'Abr', total: 190 },
  { mes: 'Mai', total: 210 },
  { mes: 'Jun', total: 178 },
  // ... continue para o ano todo
];

const problemasPorCategoria = [
  { categoria: 'Hardware', total: 45 },
  { categoria: 'Software', total: 82 },
  { categoria: 'Rede', total: 21 },
  { categoria: 'Impressora', total: 15 },
  { categoria: 'Outros', total: 15 },
];

const atendimentosPorEquipe = [
  { equipe: 'Suporte N1', total: 125 },
  { equipe: 'Suporte N2', total: 40 },
  { equipe: 'Infraestrutura', total: 13 },
];

// --- Componente da Página ---
const DashboardPage: React.FC = () => {
  return (
    <div className="flex-1 space-y-4 p-4 pt-6 md:p-8">
      <h2 className="text-3xl font-bold tracking-tight">Dashboard de Atendimentos</h2>
      
      {/* Grid para os Cards de KPI */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
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
        {/* Você pode adicionar mais 2 cards de métricas aqui se desejar */}
      </div>

      {/* Card com os Gráficos em Abas */}
      <Card className="col-span-1 lg:col-span-2">
        <CardHeader>
          <CardTitle>Análise de Atendimentos</CardTitle>
          <CardDescription>
            Visualize os atendimentos por diferentes perspectivas.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-2 pl-2">
          <Tabs defaultValue="porMes" className="space-y-4">
            <TabsList>
              <TabsTrigger value="porMes">Total por Mês</TabsTrigger>
              <TabsTrigger value="porCategoria">Problemas por Categoria</TabsTrigger>
              <TabsTrigger value="porEquipe">Atendimentos por Equipe</TabsTrigger>
            </TabsList>

            {/* Conteúdo da Aba "Por Mês" */}
            <TabsContent value="porMes" className="w-full">
              <ResponsiveContainer width="100%" height={350}>
                <BarChart data={atendimentosPorMes}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} />
                  <XAxis dataKey="mes" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <Tooltip cursor={{ fill: 'hsl(var(--muted))' }} contentStyle={{ backgroundColor: 'hsl(var(--background))', border: '1px solid hsl(var(--border))' }} />
                  <Legend />
                  <Bar dataKey="total" name="Total de Atendimentos" fill="hsl(var(--primary))" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </TabsContent>

            {/* Conteúdo da Aba "Por Categoria" */}
            <TabsContent value="porCategoria" className="w-full">
              <ResponsiveContainer width="100%" height={350}>
                <BarChart data={problemasPorCategoria} layout="vertical"> {/* Gráfico de barras na vertical */}
                  <CartesianGrid strokeDasharray="3 3" horizontal={false} />
                  <XAxis type="number" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis type="category" dataKey="categoria" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} width={100} />
                  <Tooltip cursor={{ fill: 'hsl(var(--muted))' }} contentStyle={{ backgroundColor: 'hsl(var(--background))', border: '1px solid hsl(var(--border))' }} />
                  <Legend />
                  <Bar dataKey="total" name="Quantidade de Problemas" fill="hsl(var(--primary))" radius={[0, 4, 4, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </TabsContent>

            {/* Conteúdo da Aba "Por Equipe" */}
            <TabsContent value="porEquipe" className="w-full">
              <ResponsiveContainer width="100%" height={350}>
                <BarChart data={atendimentosPorEquipe}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} />
                  <XAxis dataKey="equipe" stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <YAxis stroke="#888888" fontSize={12} tickLine={false} axisLine={false} />
                  <Tooltip cursor={{ fill: 'hsl(var(--muted))' }} contentStyle={{ backgroundColor: 'hsl(var(--background))', border: '1px solid hsl(var(--border))' }} />
                  <Legend />
                  <Bar dataKey="total" name="Total de Atendimentos" fill="hsl(var(--primary))" radius={[4, 4, 0, 0]} />
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