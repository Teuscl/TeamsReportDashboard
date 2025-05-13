import React, { useEffect, useState } from "react";
import { BrowserRouter as Router, Routes, Route, Navigate } from "react-router-dom";
import LoginPage from "./pages/LoginPage/LoginPage";
import DashboardPage from "./pages/DashboardPage";
import Layout from "./components/Layout/Layout";
import UsersPage from "./pages/UsersPage/UsersPage";

function App() {
    const port: string = "7258";
    const [isSidebarCollapsed, setIsSidebarCollapsed] = useState<boolean>(false);
    const [screenWidth, setScreenWidth] = useState<number>(window.innerWidth);

    useEffect(() => {
        const updateSize = () => {
            setScreenWidth(window.innerWidth);
            if (window.innerWidth < 768) {
                setIsSidebarCollapsed(true);
            }
        };
        window.addEventListener("resize", updateSize);
        updateSize();
        return () => window.removeEventListener("resize", updateSize);
    }, []);


    return (
        <Router>
            <Routes>
            <Route path="/" element={<LoginPage />} />
                <Route
                    element={
                        <Layout
                            port={port}
                            screenWidth={screenWidth}
                            setIsSidebarCollapsed={setIsSidebarCollapsed}
                            isSidebarCollapsed={isSidebarCollapsed}
                        />
                    }
                >
                    <Route path="/dashboard" element={<DashboardPage />} />
                    {/* Outras rotas protegidas */}
                    { <Route path="/users" element={<UsersPage />} />}
                    {/* <Route path="/atendimentos" element={<AtendimentosPage />} /> */}
                </Route>
            </Routes>
        </Router>
    );
}

export default App;
