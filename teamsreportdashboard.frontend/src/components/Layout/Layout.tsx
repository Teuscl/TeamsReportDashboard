import { Outlet, Navigate } from "react-router-dom";
import "./layout.css";
import classNames from "classnames";
import Sidebar from "../Sidebar/Sidebar";
import { getCurrentUser } from "../../utils/auth";
import JwtUser from "../../types/JwtUser";

type LayoutProps = {
    port: string;
    isSidebarCollapsed: boolean;
    screenWidth: number;
    setIsSidebarCollapsed: (collapsed: boolean) => void;
};

const Layout = ({
    port,
    isSidebarCollapsed,
    screenWidth,
    setIsSidebarCollapsed,
}: LayoutProps) => {
    const loggedUser: JwtUser | null = getCurrentUser();

    if (!loggedUser) {
        return <Navigate to="/" replace />;
    }

    const layoutClass = classNames("body", {
        "body-trimmed": !isSidebarCollapsed && screenWidth > 768,
    });

    return (
        <div className="layout-container">
            <Sidebar
                isSidebarCollapsed={isSidebarCollapsed}
                changeIsSidebarCollapsed={setIsSidebarCollapsed}
                port={port}
                loggedUser={loggedUser}
            />
            <div className={layoutClass}>
                <Outlet />
            </div>
        </div>
    );
};

export default Layout;
