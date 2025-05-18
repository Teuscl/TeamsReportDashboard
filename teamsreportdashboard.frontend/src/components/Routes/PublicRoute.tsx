import { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { getCurrentUser } from "@/utils/auth";

interface PublicRouteProps {
  children: ReactNode;
  redirectTo?: string;
}

export const PublicRoute: React.FC<PublicRouteProps> = ({
  children,
  redirectTo = "/dashboard", // pode mudar se quiser
}) => {
  const user = getCurrentUser();

  if (user) {
    return <Navigate to={redirectTo} replace />;
  }

  return <>{children}</>;
};
