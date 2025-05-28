// src/services/reportService.ts (NOVO ARQUIVO)
import axiosConfig from './axiosConfig'; // Sua instância configurada do Axios
import { Report, CreateReportPayload, UpdateReportPayload } from '@/types/Report';

const API_URL = '/Report'; // Base URL do seu ReportController

export const getReports = async (): Promise<Report[]> => {
  const response = await axiosConfig.get<Report[]>(API_URL);
  return response.data;
};

export const getReportById = async (id: number): Promise<Report> => {
  const response = await axiosConfig.get<Report>(`${API_URL}/${id}`);
  return response.data;
};

export const createReport = async (payload: CreateReportPayload): Promise<Report> => {
  // O backend retorna Ok() no CreateReport, o que geralmente não tem corpo ou tem o objeto criado.
  // Se retornar o objeto criado, o tipo Report está correto. Se retorna só Ok(), ajuste.
  // Para este exemplo, assumimos que retorna o Report criado.
  // Se o backend retorna 201 Created com o objeto no corpo:
  const response = await axiosConfig.post<Report>(API_URL, payload);
  return response.data;
  // Se o backend retorna 200 OK sem corpo (como no seu controller atual):
  // await axiosConfig.post(API_URL, payload);
  // return { id: 0, ...payload, requestDate: payload.requestDate || new Date().toISOString() }; // Simulação, idealmente backend retorna o ID
};

export const updateReport = async (id: number, payload: UpdateReportPayload): Promise<void> => {
  // Seu backend retorna NoContent (204) para PATCH, então a Promise é void.
  await axiosConfig.patch(`${API_URL}/${id}`, payload);
};

export const deleteReport = async (id: number): Promise<void> => {
  // Seu backend retorna Ok com uma mensagem para DELETE, mas não precisamos do corpo aqui.
  await axiosConfig.delete(`${API_URL}/${id}`);
};