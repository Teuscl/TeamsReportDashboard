// src/components/DashboardPage.tsx

import React from "react";

const DashboardPage = ({ currentUser }: { currentUser: any }) => {
    if (!currentUser) {
        return <div>Usuário não autenticado.</div>;
    }

    return (
        <div>
            <h1>Bem-vindo, {currentUser.name}</h1>
            <p>Você está autenticado com o papel: {currentUser.role}</p>
        </div>
    );
};

export default DashboardPage;
