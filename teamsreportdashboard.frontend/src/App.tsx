// src/App.tsx
import { BrowserRouter, Routes, Route, Outlet, Navigate } from "react-router-dom"; // Adicionado Outlet e Navigate
// Importe suas páginas e componentes de rota
import LoginPage from "./pages/LoginPage/LoginPage";
import DashboardPage from "./pages/DashboardPage/DashboardPage";
import UsersPage from "./pages/UsersPage/UsersPage";
import ProfilePage from "./pages/ProfilePage/ProfilePage";
import ReportsPage from "./pages/ReportsPage/ReportsPage";
import ChangeMyPasswordPage from "./pages/ChangeMyPasswordPage/ChangeMyPasswordPage"; // 👈 Importe a nova página

import Layout from "@/components/Layout/Layout"; // Seu componente de Layout visual
import { SidebarProvider } from "./components/ui/sidebar"; // Se usado com Layout
import { AuthProvider } from "./context/AuthContext";
import ProtectedRoute from "./Routes/ProtectedRoute";
import PasswordChangedSuccessPage from "./pages/PasswordChangedSuccessPage/PasswordChangedSuccessPage";
import ForgotPasswordPage from "./pages/ForgotPasswordPage/ForgotPasswordPage";
import ResetPasswordPage from "./pages/ResetPasswordPage/ResetPasswordPage";
import DepartmentsPage from "./pages/DepartmentsPage/DepartmentsPage";
import RequestersPage from "./pages/RequestersPage/RequestersPage";
function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Rota Pública */}
          <Route path="/" element={<LoginPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route path="/auth/password-changed-successfully" element={<PasswordChangedSuccessPage />} /> 

          {/* Rotas Protegidas Genéricas (requerem apenas login) */}
          <Route element={<ProtectedRoute />}> {/* Protege todas as rotas aninhadas abaixo */}
            <Route element={ /* Elemento de Layout para estas rotas */
              <SidebarProvider>
                <Layout>
                  <Outlet /> {/* O Outlet renderizará o componente da rota filha */}
                </Layout>
              </SidebarProvider>
            }>
              {/* Rotas filhas que usam o Layout e são protegidas */}
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/reports" element={<ReportsPage />} />
              <Route path="/profile" element={<ProfilePage />} />
              <Route path="/change-my-password" element={<ChangeMyPasswordPage />} /> 
              <Route path="/users" element={<UsersPage />} />
              <Route path="/departments" element={<DepartmentsPage />} />
              <Route path="/requesters" element={<RequestersPage />} />
            </Route>
          </Route>
          
          

         

        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;