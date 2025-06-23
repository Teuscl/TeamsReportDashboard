// src/App.tsx

import { BrowserRouter, Routes, Route, Outlet } from "react-router-dom";

// --- PÁGINAS ---
import LoginPage from "./pages/LoginPage/LoginPage";
import DashboardPage from "./pages/DashboardPage/DashboardPage";
import UsersPage from "./pages/UsersPage/UsersPage";
import ProfilePage from "./pages/ProfilePage/ProfilePage";
import ReportsPage from "./pages/ReportsPage/ReportsPage";
import ChangeMyPasswordPage from "./pages/ChangeMyPasswordPage/ChangeMyPasswordPage";
import DepartmentsPage from "./pages/DepartmentsPage/DepartmentsPage";
import RequestersPage from "./pages/RequestersPage/RequestersPage";
// ... outras páginas públicas se houver

// --- CONTEXTO E LAYOUT ---
import { AuthProvider } from "./context/AuthContext";
import { SidebarProvider } from "./components/ui/sidebar";
import Layout from "@/components/Layout/Layout"; // 👈 1. CORREÇÃO: Importe o SEU componente de Layout

// --- ROTAS PROTEGIDAS ---
import ProtectedRoute from "@/Routes/ProtectedRoute";
import RoleProtectedRoute from "@/Routes/RoleProtectedRoutes";
import { RoleEnum } from "@/utils/role";
import ImportsPage from "./pages/ImportsPage/ImportsPage";


function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* --- Rotas Públicas --- */}
          <Route path="/" element={<LoginPage />} />
          {/* ...outras rotas públicas... */}

          {/* --- Área Protegida por Login --- */}
          <Route element={<ProtectedRoute />}>
            <Route element={
              <SidebarProvider>
                <Layout> {/* Usando o componente de Layout importado corretamente */}
                  <Outlet/>
                </Layout>
              </SidebarProvider>
            }>

              {/* Rotas para TODOS os usuários logados */}
              <Route path="/dashboard" element={<DashboardPage />} />
              <Route path="/reports" element={<ReportsPage />} />
              <Route path="/profile" element={<ProfilePage />} />
              <Route path="/change-my-password" element={<ChangeMyPasswordPage />} />

              {/* Rotas para Admin e Master */}
              <Route element={<RoleProtectedRoute allowedRoles={[RoleEnum.Admin, RoleEnum.Master]} />}>
                <Route path="/departments" element={<DepartmentsPage />} />
                <Route path="/requesters" element={<RequestersPage />} />
                <Route path="/imports" element={<ImportsPage />} />
              </Route>

              {/* Rota apenas para Master */}
              <Route element={<RoleProtectedRoute allowedRoles={[RoleEnum.Master]} />}>
                <Route path="/users" element={<UsersPage />} />
              </Route>
              
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;