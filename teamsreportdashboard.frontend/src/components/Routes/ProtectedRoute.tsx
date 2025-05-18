import { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { getCurrentUser } from "@/utils/auth";
import { toast } from "sonner";

interface ProtectedRouteProps {
  allowedRoles: string[];
  children: ReactNode;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  allowedRoles,
  children,
}) => {
  const user = getCurrentUser();

  if (!user) {
    toast.error("Você precisa estar logado.");
    return <Navigate to="/login" replace />;
  }

  if (!allowedRoles.includes(user.role)) {
    toast.error("Acesso negado: você não tem permissão.");
    return null; // 👈 Sem redirecionamento, apenas bloqueia a renderização
  }

  return <>{children}</>;
};
