// src/services/axiosConfig.ts
import axios from 'axios';
import { eventEmitter, AUTH_EVENTS } from './eventEmitter'; 

const axiosConfig = axios.create({
    baseURL: 'https://localhost:7258/', 
    headers: {
        'Content-Type': 'application/json',
    },
    withCredentials: true,
});

let isRefreshing = false;
let failedQueue: Array<{ resolve: (value?: any) => void, reject: (reason?: any) => void }> = [];

const processQueue = (error: any | null, token: string | null = null) => {
    failedQueue.forEach(prom => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token); // Para HttpOnly, o token não é passado, o navegador lida
        }
    });
    failedQueue = [];
};

axiosConfig.interceptors.response.use(
    response => response,
    async error => {
        const originalRequest = error.config;

        // Verifica se é um erro 401 e se não é uma tentativa de retry que já falhou
        if (error.response?.status === 401 && !originalRequest._retry) {
            if (isRefreshing) {
                // Se já estiver atualizando, adiciona a requisição à fila
                return new Promise((resolve, reject) => {
                    failedQueue.push({ resolve, reject });
                })
                .then(() => axiosConfig(originalRequest)) // Tenta a requisição original novamente
                .catch(err => Promise.reject(err));
            }

            originalRequest._retry = true; // Marca para evitar loop infinito de retries
            isRefreshing = true;

            try {
                console.log("Attempting to refresh token...");
                await axiosConfig.post("/auth/refresh"); // Tenta renovar o token
                console.log("Token refreshed successfully.");
                processQueue(null); // Processa a fila de requisições pendentes com sucesso
                return axiosConfig(originalRequest); // Reenvia a requisição original
            } catch (refreshError) {
                console.error("Token refresh failed:", refreshError);
                processQueue(refreshError, null); // Processa a fila com erro

                // Dispara o evento para o AuthContext limpar o estado local
                console.log("Dispatching FORCE_LOGOUT event.");
                eventEmitter.dispatch(AUTH_EVENTS.FORCE_LOGOUT);

                // Redireciona para a página de login
                // Verifique se já não está na página de login para evitar loop de redirect
                if (window.location.pathname !== "/") {
                    window.location.href = "/";
                }
                
                return Promise.reject(refreshError); // Rejeita a promessa da requisição original
            } finally {
                isRefreshing = false;
            }
        }
        return Promise.reject(error);
    }
);

export default axiosConfig;