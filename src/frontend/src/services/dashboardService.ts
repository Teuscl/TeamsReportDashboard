import axiosConfig from './axiosConfig';

export interface ChartData {
  name: string;
  total: number;
}

export interface DashboardData {
  totalAtendimentosMes: number;
  tempoMedioPrimeiraResposta: string;
  totalDepartamentos: number;
  totalSolicitantes: number;
  atendimentosPorMes: ChartData[];
  problemasPorCategoria: ChartData[];
  atendimentosPorTecnico: ChartData[]; // Renomeado
  atendimentosPorDepartamento: ChartData[]; // Novo
}

export const getDashboardData = async (): Promise<DashboardData> => {
  const response = await axiosConfig.get('/dashboard');
  return response.data;
};