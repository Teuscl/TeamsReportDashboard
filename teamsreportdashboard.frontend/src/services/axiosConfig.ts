// src/services/axiosConfig.ts
import axios from 'axios';

const axiosConfig = axios.create({
    baseURL: 'https://localhost:7258/', // Substitua pela URL do seu backend
    headers: {
        'Content-Type': 'application/json',
    },
    withCredentials: true, // Permite o envio de cookies com as requisições
});

axiosConfig.interceptors.request.use(
    response =>response,
    async error => {
        const originalRequest = error.config;

        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            try {
                const refreshToken = await axiosConfig.post("auth/refresh");
                
                //Recebe o novo token de acesso e refresh 
                const newToken = refreshToken.data.token;
                axiosConfig.defaults.headers.common['Authorization'] = `Bearer ${newToken}`;

                // Atualiza o cabeçadalho da requisição original com o novo token
                originalRequest.headers['Authorization'] = `Bearer ${newToken}`;
                return axiosConfig(originalRequest);
            } catch (refreshError) {
                localStorage.removeItem("token");
                window.location.href = "/login"; // Redireciona para a página de login
                return Promise.reject(refreshError);
            }
        }
        return Promise.reject(error);
    }
);

export default axiosConfig;
