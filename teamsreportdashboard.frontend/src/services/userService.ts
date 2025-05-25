// src/services/userService.ts
import { RoleEnum } from '@/utils/role';
import axiosConfig from './axiosConfig';
import AxiosConfig from './axiosConfig';
import { User } from '@/types/User';


// Payload para criar um usuário
export interface CreateUserPayload {
  name: string;
  email: string;
  password: string; // Senha é obrigatória na criação
  role: RoleEnum;
  isActive: boolean;
}

export interface UpdateUserPayload {
  id: number; 
  name: string;
  email: string;
  role: RoleEnum;
  isActive: boolean;
}

export const getUsers = async (): Promise<User[]> => {
  const response = await AxiosConfig.get('/user');
  return response.data;
};

export const getMe = async (): Promise<User | null> => {
  try {
    const response = await axiosConfig.get<User>('/user/me');
    return response.data;
  } catch (error: any) {
    console.error('%cuserService: getMe - Detalhes do Erro:', 'color: red;', error);
    if (error.isAxiosError && error.response) {
      console.error('%cuserService: getMe - Erro Axios com response:', 'color: red;', {
        status: error.response.status,
        data: error.response.data,
      });
    }
    // ... (outros logs de erro Axios se desejar) ...
    console.log('%cuserService: getMe - ANTES de "throw error" no catch do getMe.', 'color: magenta;');
    throw error; // <-- ESTA LINHA É VITAL E NÃO PODE ESTAR FALTANDO OU COMENTADA
  }
};

export const createUser = async (payload: CreateUserPayload): Promise<User> => {
  // Assumindo que a resposta da criação também é um objeto User compatível
  const response = await axiosConfig.post<User>('/user', payload);
  return response.data;
};

export const updateUser = async (payload: UpdateUserPayload): Promise<User> => {
  await AxiosConfig.put('/user', payload);
  return payload as User; // Retorna o payload atualizado como User
};

export const deleteUser = async (id: number): Promise<void> => {
  await AxiosConfig.delete('/user', {
    params: { id },
  });
};
