import axiosConfig from './axiosConfig';
import { RequesterDto, CreateRequesterDto, UpdateRequesterDto, BulkInsertResultDto, BulkInsertFailure } from '@/types/Requester'; // Vamos criar este arquivo de tipos

export const getRequesters = async (): Promise<RequesterDto[]> => {
  const response = await axiosConfig.get('/requesters');
  return response.data;
};

export const createRequester = async (requester: CreateRequesterDto): Promise<RequesterDto> => {
  const response = await axiosConfig.post('/requesters', requester);
  return response.data;
};

export const updateRequester = async (id: number, requester: UpdateRequesterDto): Promise<void> => {
  await axiosConfig.put(`/requesters/${id}`, requester);
};

export const deleteRequester = async (id: number): Promise<void> => {
  await axiosConfig.delete(`/requesters/${id}`);
};

export const bulkInsertRequesters = async (file: File): Promise<BulkInsertResultDto> => {
  const formData = new FormData();
  formData.append('file', file);

  const response = await axiosConfig.post<BulkInsertResultDto>('/requesters/bulk-insert', formData, {
    headers: {
      // O browser define o Content-Type correto com o boundary automaticamente
      // quando usamos FormData, então não precisamos setar aqui.
    },
  });

  return response.data;
};