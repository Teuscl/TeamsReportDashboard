import axiosConfig from './axiosConfig';

export const forgotPassword = async (email: string): Promise<any> => {
  const response = await axiosConfig.post('/auth/forgot-password', { email });
  return response.data; // Retorna a mensagem de sucesso do backend
};

export interface ResetPasswordPayload {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export const resetPassword = async (payload: ResetPasswordPayload): Promise<any> => {
  const response = await axiosConfig.post('/auth/reset-password-forgotten', payload);
  return response.data; // Retorna a mensagem de sucesso
};