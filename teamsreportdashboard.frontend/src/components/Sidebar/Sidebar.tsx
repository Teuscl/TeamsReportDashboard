import { Link } from "react-router-dom";
import "./sidebar.css";
import JwtUser from "../../types/JwtUser";
import classNames from "classnames";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faBars, faHome, faFileLines, faUserGear, faArrowRightFromBracket } from "@fortawesome/free-solid-svg-icons";
import { Fragment, useEffect, useState } from "react";

type SidebarProps = {
    isSidebarCollapsed: boolean;
    changeIsSidebarCollapsed: (isSidebarCollapsed: boolean) => void;
    port: string;
    loggedUser: JwtUser | null;
};

const Sidebar = ({
                     isSidebarCollapsed,
                     changeIsSidebarCollapsed,
                     port,
                     loggedUser,
                 }: SidebarProps) => {
    
    const [hasAdminRole, setHasAdminRole] = useState<boolean>(false);

    const sidebarItems = [
        { routerLink: "dashboard", icon: faHome, label: "Dashboard" },
        { routerLink: "atendimentos", icon: faFileLines, label: "Atendimentos" },
    ];

    const adminItems = [
        { routerLink: "usuarios", icon: faUserGear, label: "Usuários" },
    ];

    const sidebarClasses = classNames({
        sidenav: true,
        "sidenav-collapsed": isSidebarCollapsed,
    });

    const toggleCollapse = () => {
        changeIsSidebarCollapsed(!isSidebarCollapsed);
    };

    const handleLogout = () => {
        console.log("Logout clicado");
    };

    useEffect(() => {
        const checkAdminRole = async () => {
            try {
                await await.

    return (
        <div className={sidebarClasses}>
            <div className="logo-container">
                <button className="logo" onClick={toggleCollapse}>
                    <FontAwesomeIcon icon={faBars} />
                </button>
                {!isSidebarCollapsed && (
                    <Fragment>
                        <div className="logo-text">Menu</div>
                    </Fragment>
                )}
            </div>
            <div className="sidenav-nav">
                {sidebarItems.map(item => (
                    <li key={item.label} className="sidenav-nav-item">
                        <Link className="sidenav-nav-link" to={item.routerLink}>
                            <FontAwesomeIcon icon={item.icon} className="sidenav-link-icon" />
                            {!isSidebarCollapsed && <span className="sidenav-link-text">{item.label}</span>}
                        </Link>
                    </li>
                ))}

                <hr />
                <div className="sidenav-nav-item user-section">
                    <div className="user-info">
                        <div className="user-avatar">
                            <span>U</span> {/* Letra fixa para simular avatar */}
                        </div>
                        <span className="user-name">Usuário Demo</span>
                    </div>
                    <button className="logout-button" onClick={handleLogout}>
                        <FontAwesomeIcon icon={faArrowRightFromBracket} className="logout-icon" />
                    </button>
                </div>
            </div>
        </div>
    );
};

export default Sidebar;
