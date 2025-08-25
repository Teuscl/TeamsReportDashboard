// src/services/axiosConfig.ts (VERSÃO CORRIGIDA E RECOMENDADA)
import axios from 'axios';
import { eventEmitter, AUTH_EVENTS } from './eventEmitter'; 

const axiosConfig = axios.create({
    baseURL: 'https://localhost:7258/',     
    withCredentials: true,
});

let isRefreshing = false;
let failedQueue: Array<{ resolve: (value?: any) => void, reject: (reason?: any) => void }> = [];

const processQueue = (error: any | null, token: string | null = null) => {
    failedQueue.forEach(prom => {
        if (error) {
            prom.reject(error);
        } else {
            prom.resolve(token); // Para HttpOnly, token não é realmente usado aqui
        }
    });
    failedQueue = [];
};

axiosConfig.interceptors.response.use(
    response => response,
    async error => {
        const originalRequest = error.config;

        // URLs que NÃO devem acionar o refresh token em caso de 401
        const noRefreshEndpoints = ['/auth/login', '/auth/refresh']; 

        if (error.response?.status === 401 && !originalRequest._retry) {
            
            // Se a URL original é um dos endpoints que não devem dar refresh (EX: /auth/login),
            // apenas rejeite o erro diretamente. O AuthContext.login tratará este erro.
            if (originalRequest.url && noRefreshEndpoints.includes(originalRequest.url)) {
                console.log(`Interceptor: 401 on auth endpoint (${originalRequest.url}). Rejecting directly.`);
                return Promise.reject(error); // <-- ESSENCIAL PARA O FLUXO DE LOGIN FALHO
            }

            // Para outros 401s (ex: /user/me em uma sessão expirada, /reports, etc.)
            // tentar o refresh token:
            if (isRefreshing) {
                return new Promise((resolve, reject) => {
                    failedQueue.push({ resolve, reject });
                })
                .then(() => axiosConfig(originalRequest))
                .catch(err => Promise.reject(err));
            }

            originalRequest._retry = true;
            isRefreshing = true;

            try {
                console.log("Interceptor: Attempting to refresh token for", originalRequest.url);
                await axiosConfig.post("/auth/refresh"); 
                console.log("Interceptor: Token refreshed successfully.");
                processQueue(null); 
                // isRefreshing será false no finally, mas para evitar que a primeira request da fila
                // pense que ainda está refreshing, resetamos aqui antes de processar a originalRequest.
                // No entanto, o processQueue deve ser chamado antes do isRefreshing = false para que as
                // requests na fila possam ser refeitas. O finally é o lugar correto para isRefreshing = false.
                return axiosConfig(originalRequest);
            } catch (refreshError) {
                console.error("Interceptor: Token refresh failed for", originalRequest.url, ". Emitting FORCE_LOGOUT.", refreshError);
                processQueue(refreshError, null);
                eventEmitter.dispatch(AUTH_EVENTS.FORCE_LOGOUT);
                
                // IMPORTANTE: REMOVIDO o window.location.href = "/"; מכאן.
                // O AuthContext (via FORCE_LOGOUT) e o ProtectedRoute devem lidar com o redirecionamento.
                
                return Promise.reject(refreshError); // Rejeita o erro para a chamada original (ex: para checkAuthStatus)
            } finally {
                isRefreshing = false;
            }
        }
        return Promise.reject(error); // Rejeita outros erros não tratados acima (ex: 500, 403, etc.)
    }
);

export default axiosConfig;