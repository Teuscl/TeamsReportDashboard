// src/services/userService.ts
import JwtUser from '@/types/JwtUser';
import AxiosConfig from './axiosConfig';
import { User } from '@/types/User';

/**
 * Busca todos os usuários.
 */
export const getUsers = async (): Promise<User[]> => {
  const response = await AxiosConfig.get('/user');
  return response.data;
};
export const getMe = async (): Promise<JwtUser> => {
  const response = await AxiosConfig.get("/me"); // ou "users/me"
  return response.data;
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
