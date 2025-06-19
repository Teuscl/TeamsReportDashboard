import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '@/context/AuthContext';
import { RoleEnum } from '@/utils/role';

interface RoleProtectedRouteProps {
  allowedRoles: RoleEnum[];
}

const RoleProtectedRoute: React.FC<RoleProtectedRouteProps> = ({ allowedRoles }) => {
  const { user, isLoading } = useAuth();

  // Enquanto o usuário está sendo carregado, não fazemos nada
  if (isLoading) {
    return <div>Carregando permissões...</div>;
  }

  // Se o usuário tem uma role que está na lista de permitidas, renderiza a página.
  // O <Outlet /> representa as rotas filhas (ex: <UsersPage />)
  if (user && allowedRoles.includes(user.role)) {
    return <Outlet />;
  }

  // Se o usuário não tiver a permissão, redireciona para o dashboard ou uma página "Não Autorizado"
  return <Navigate to="/dashboard" replace />;
};

export default RoleProtectedRoute;