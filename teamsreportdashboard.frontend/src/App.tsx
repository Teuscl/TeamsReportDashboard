import { BrowserRouter, Routes, Route } from "react-router-dom";
import UsersPage from "./pages/UsersPage/UsersPage";
import Layout from "./components/Layout/layout";
import { SidebarProvider } from "./components/ui/sidebar";
import LoginPage from "./pages/LoginPage/LoginPage";

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<LoginPage />} />
        <Route
          path="/users"
          element={
            <SidebarProvider>
              <Layout>
                <UsersPage/>
              </Layout>
            </SidebarProvider>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
