// src/App.tsx

import React, { useState } from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import LoginPage from "./pages/LoginPage/LoginPage";
import DashboardPage from "./pages/DashboardPage"; // Página de exemplo para redirecionamento
import { Container } from "react-bootstrap";

function App() {
    const [currentUser, setCurrentUser] = useState<any>(null);

    const handleLogin = (user: any) => {
        setCurrentUser(user);
    };

    return (
        <Router>
            <Container>
                <Routes>
                    <Route path="/" element={<LoginPage onLogin={handleLogin} />} />
                    <Route path="/dashboard" element={<DashboardPage currentUser={currentUser} />} />
                </Routes>
            </Container>
        </Router>
    );
}

export default App;
