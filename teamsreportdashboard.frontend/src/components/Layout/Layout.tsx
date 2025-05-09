import {Outlet} from "react-router-dom";
import "./layout.css";
import classNames from "classnames";
import Sidebar from "../Sidebar/Sidebar";;
import JwtUser from "../types/JwtUser.tsx";
import {jwtDecode} from "jwt-decode"; // Importando a Sidebar

type LayoutProps = {
    port: string;
    isSidebarCollapsed: boolean;
    screenWidth: number;
    setIsSidebarCollapsed: (isSidebarCollapsed: boolean) => void;
    loggedUser: JwtUser | null;
};

const Layout = ({port, isSidebarCollapsed, screenWidth, setIsSidebarCollapsed, loggedUser}: LayoutProps) => {
    const classes = classNames({
        body: true,
        "body-trimmed": !isSidebarCollapsed && screenWidth > 768, // Aplica o estilo quando a sidebar não está colapsada e a tela é maior que 768px
    });

    const getCurrentUser = () => {
        const token = localStorage.getItem("token");
        if (!token) return null;

        try {
            return jwtDecode(token) as JwtUser; // Retorna o usuário logado decodificado
        } catch {
            return null;
        }
    };

    if (!loggedUser)
        loggedUser = getCurrentUser();

    return (
        <div className="layout-container">
            <Sidebar
                isSidebarCollapsed={isSidebarCollapsed}
                changeIsSidebarCollapsed={setIsSidebarCollapsed}
                port={port}
                loggedUser={loggedUser}
            />

            {/* O conteúdo principal da página será renderizado aqui */}
            <div className={classes}>
                {/* O Outlet renderiza o conteúdo das rotas dentro deste Layout */}
                <Outlet/>
            </div>
        </div>
    );
};

export default Layout;