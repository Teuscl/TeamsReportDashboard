// src/App.tsx
import { BrowserRouter, Routes, Route } from "react-router-dom";
// Importe suas páginas e componentes de rota
import LoginPage from "./pages/LoginPage/LoginPage";
import DashboardPage from "./pages/DashboardPage/DashboardPage";
import UsersPage from "./pages/UsersPage/UsersPage"; // Exemplo
import ProfilePage from "./pages/ProfilePage/ProfilePage"; // Exemplo


import Layout from "@/components/Layout/Layout"; // Seu componente de Layout visual
import { SidebarProvider } from "./components/ui/sidebar"; // Se usado com Layout
import { AuthProvider } from "./context/AuthContext";
import ProtectedRoute from "./Routes/ProtectedRoute";

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
          <Routes>
            <Route
              path="/"
              element={
                <LoginPage />              
              }
              />
            <Route element={<ProtectedRoute />}>
              <Route
              path="/dashboard"
              element={
                <SidebarProvider>
                    <Layout>
                      <DashboardPage />
                    </Layout>
                  </SidebarProvider>
              }
              />

            </Route>          
            
            <Route
              path="/users"
              element={
                <SidebarProvider>
                    <Layout>
                      <UsersPage />
                    </Layout>
                  </SidebarProvider>
              }
              />
            <Route
              path="/profile"
              element={
                <SidebarProvider>
                    <Layout>
                      <ProfilePage />
                    </Layout>
                  </SidebarProvider>                  
              }
              />          
          </Routes>
        </BrowserRouter>
    </AuthProvider>
    
  );
}

export default App;