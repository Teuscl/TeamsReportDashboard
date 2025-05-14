import { Link } from "react-router-dom";
import "./sidebar.css";
import JwtUser from "../../types/JwtUser";
import classNames from "classnames";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import {
    faBars,
    faHome,
    faFileLines,
    faUserGear,
    faArrowRightFromBracket
} from "@fortawesome/free-solid-svg-icons";
import { Fragment } from "react";
import { logout } from "../../utils/auth"; // <-- novo helper de logout

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
    const sidebarItems = [
        { routerLink: "/dashboard", icon: faHome, label: "Dashboard" },
        { routerLink: "/atendimentos", icon: faFileLines, label: "Atendimentos" },
    ];

    const adminItems = [
        { routerLink: "/users", icon: faUserGear, label: "Usuários" },
    ];

    const sidebarClasses = classNames({
        sidenav: true,
        "sidenav-collapsed": isSidebarCollapsed,
    });

    const toggleCollapse = () => {
        changeIsSidebarCollapsed(!isSidebarCollapsed);
    };

    // normaliza o role (caso venha como string ou array)
    const roles = loggedUser?.role
        ? Array.isArray(loggedUser.role)
            ? loggedUser.role
            : [loggedUser.role]
        : [];

    const isAdmin = roles.includes("admin");

    const userName = loggedUser?.name || "Usuário";
    const avatarLetter = userName[0]?.toUpperCase() || "U";

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
                {[...sidebarItems, ...(isAdmin ? adminItems : [])].map(item => (
                    <li key={item.label} className="sidenav-nav-item">
                        <Link className="sidenav-nav-link" to={item.routerLink}>
                            <FontAwesomeIcon icon={item.icon} className="sidenav-link-icon" />
                            {!isSidebarCollapsed && (
                                <span className="sidenav-link-text">{item.label}</span>
                            )}
                        </Link>
                    </li>
                ))}

                <hr />
                <div className="sidenav-nav-item user-section">
                    <div className="user-info">
                        <div className="user-avatar">
                            <span>{avatarLetter}</span>
                        </div>
                        {!isSidebarCollapsed && (
                            <span className="user-name">{userName}</span>
                        )}
                    </div>
                    <button className="logout-button" onClick={logout}>
                        <FontAwesomeIcon icon={faArrowRightFromBracket} className="logout-icon" />
                    </button>
                </div>
            </div>
        </div>
    );
};

export default Sidebar;
