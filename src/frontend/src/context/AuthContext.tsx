// src/contexts/AuthContext.tsx
import React, {
  createContext,
  useState,
  useEffect,
  useContext,
  ReactNode,
  useCallback,
} from 'react';
import axiosConfig from '../services/axiosConfig';
import { User } from '../types/User';
import { eventEmitter, AUTH_EVENTS } from '../services/eventEmitter'; // Importe o event emitter

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: { email: string; password: string }) => Promise<boolean>;
  logout: () => Promise<void>; // Este é o logout iniciado pelo usuário
  checkAuthStatus: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const clearAuthState = useCallback(() => {
    console.log("AuthContext: Clearing auth state.");
    setUser(null);
    setIsAuthenticated(false);
    setIsLoading(false); // Certifique-se de que o loading também é resetado
  }, []);

  const checkAuthStatus = useCallback(async () => {
    // ... (implementação do checkAuthStatus como antes)
    // No catch ou finally de checkAuthStatus, se não autenticado, chame clearAuthState()
    // em vez de duplicar setUser(null), setIsAuthenticated(false)
    setIsLoading(true);
    try {
      const response = await axiosConfig.get<User>('/user/me');
      if (response.data) {
        setUser(response.data);
        setIsAuthenticated(true);
      } else {
        clearAuthState();
      }
    } catch (error) {
      clearAuthState();
    } finally {
      setIsLoading(false);
    }
  }, [clearAuthState]);

  useEffect(() => {
    checkAuthStatus();
  }, [checkAuthStatus]);

  // Inscrever-se no evento de logout forçado
  useEffect(() => {
    const handleForceLogout = () => {
      console.log("AuthContext: FORCE_LOGOUT event received. Clearing local state.");
      clearAuthState();
      // O redirecionamento será feito pelo interceptor
    };

    const unsubscribe = eventEmitter.subscribe(AUTH_EVENTS.FORCE_LOGOUT, handleForceLogout);

    // Limpar a inscrição quando o componente for desmontado
    return () => {
      unsubscribe();
    };
  }, [clearAuthState]); // Adiciona clearAuthState como dependência

  const login = async (credentials: { email: string; password: string }): Promise<boolean> => {
    // ... (implementação do login como antes)
    // No try:
    // await axiosConfig.post('/auth/login', credentials);
    // await checkAuthStatus(); // Isso vai popular o user e isAuthenticated
    // return isAuthenticated; // Ou retorne true se checkAuthStatus popular o user

    // No catch:
    // clearAuthState();
    // return false;
    setIsLoading(true);
    try {
      await axiosConfig.post('/auth/login', credentials);
      await checkAuthStatus(); // Revalida e atualiza o estado
      // checkAuthStatus definirá isAuthenticated, então podemos confiar nisso para o retorno
      // Para garantir que o valor retornado reflita o estado após checkAuthStatus:
      const success = !!(await axiosConfig.get<User>('/user/me').catch(() => null))?.data; // Verifica novamente
      setIsLoading(false);
      return success;
    } catch (error) {
        // (F) Este CATCH é para erros vindos de (B) ou de (C) se checkAuthStatus lançar erro não tratado internamente
        console.error('Login failed in AuthContext:', error);
        clearAuthState(); 
        setIsLoading(false); // (G) isLoading do AuthContext
        return false; // (H) Indica falha no login
    }
  };

  const logout = async () => { // Logout iniciado pelo usuário
    setIsLoading(true);
    try {
      await axiosConfig.post('/auth/logout');
    } catch (error) {
      console.error('Backend logout failed:', error);
    } finally {
      clearAuthState(); // Limpa o estado local independentemente do backend
      // O redirecionamento é responsabilidade do componente que chama logout
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated,
        isLoading,
        login,
        logout,
        checkAuthStatus,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  // ... (como antes)
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider. Make sure your component is a child of AuthProvider.');
  }
  return context;
};