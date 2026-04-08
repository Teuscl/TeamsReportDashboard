// src/components/ProtectedRoute.tsx (crie este arquivo se ainda não existir)
import React from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '@/context/AuthContext'; // Ajuste o caminho se necessário

const ProtectedRoute: React.FC = () => {
  const { isAuthenticated, isLoading } = useAuth();
  const location = useLocation(); // Para redirecionar de volta após o login

  if (isLoading) {
    // Você pode retornar um componente de Spinner/Loading aqui
    // para uma melhor experiência do usuário.
    return <div>Verificando autenticação...</div>;
  }

  if (!isAuthenticated) {
    // Redireciona para a página de login, mas também passa a localização atual
    // para que possamos redirecionar o usuário de volta para onde ele estava tentando ir
    // após o login bem-sucedido.
    return <Navigate to="/" state={{ from: location }} replace />;
  }

  // Se autenticado, renderiza o componente filho da rota (usando <Outlet /> para rotas aninhadas)
  return <Outlet />;
};

export default ProtectedRoute;