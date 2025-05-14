// src/components/DashboardPage.tsx
import React from "react";
import { getCurrentUser } from "../utils/auth";

const DashboardPage = () => {
    const currentUser = getCurrentUser();

    if (!currentUser) {
        return <div>Usuário não autenticado.</div>;
    }

    return (
        <div>
            <h1>Bem-vindo, {currentUser.unique_name}</h1>
            <p>Você está autenticado com o papel: {currentUser.role}</p>
        </div>
    );
};

export default DashboardPage;
