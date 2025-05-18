import { BrowserRouter, Routes, Route } from "react-router-dom";
import UsersPage from "./pages/UsersPage/UsersPage";
import Layout from "@/components/Layout/Layout";
import { SidebarProvider } from "./components/ui/sidebar";
import LoginPage from "./pages/LoginPage/LoginPage";
import ProfilePage from "./pages/ProfilePage/ProfilePage";
import { ProtectedRoute } from "./components/Routes/ProtectedRoute";
import { PublicRoute } from "./components/Routes/PublicRoute";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route
          path="/"
          element={
            <PublicRoute>
              <LoginPage />
            </PublicRoute>
          } 
          />
        <Route
          path="/users"
          element={
            <ProtectedRoute allowedRoles={["Master"]}>
              <SidebarProvider>
              <Layout>
                <UsersPage/>
              </Layout>
              </SidebarProvider>
            </ProtectedRoute>           
          }
        />
        <Route
          path="/profile"
          element={
            <SidebarProvider>
              <Layout>
                <ProfilePage/>
              </Layout>
            </SidebarProvider>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
