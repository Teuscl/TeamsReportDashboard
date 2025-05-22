// src/services/userService.ts
import axiosConfig from './axiosConfig';
import AxiosConfig from './axiosConfig';
import { User } from '@/types/User';

/**
 * Busca todos os usuários.
 */
export const getUsers = async (): Promise<User[]> => {
  const response = await AxiosConfig.get('/user');
  return response.data;
};


export const getMe = async (): Promise<User | null> => {
  console.log('%cuserService: getMe INVOCADO (VERSÃO REAL COM AXIOS - DEBUG INTENSO)', 'color: magenta; font-weight: bold;');

  try {
    console.log('%cuserService: getMe - DENTRO DO TRY, ANTES de "await axiosConfig.get"', 'color: magenta;');
    const response = await axiosConfig.get<User>('/user/me');
    console.log('%cuserService: getMe - APÓS "await axiosConfig.get", SUCESSO APARENTE. Response data:', 'color: green;', response.data);
    return response.data;
  } catch (error: any) {
    console.error('%cuserService: getMe - ERRO CAPTURADO NO CATCH DO getMe!', 'color: red; font-weight: bold;');
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

/**
 * Atualiza um usuário existente.
 * @param user Dados do usuário a serem atualizados.
 */
export const updateUser = async (user: User): Promise<User> => {
  await AxiosConfig.put('/user', user);
  return user;
};

/**
 * Exclui um usuário com base no ID.
 * @param id ID do usuário a ser excluído.
 */
export const deleteUser = async (id: number): Promise<void> => {
  await AxiosConfig.delete('/user', {
    params: { id },
  });
};
