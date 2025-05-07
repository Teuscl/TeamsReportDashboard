import { Link } from "react-router-dom";
import "./sidebar.css";
import classNames from "classnames";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faBars, faHome, faFileLines, faUserGear, faArrowRightFromBracket } from "@fortawesome/free-solid-svg-icons";
import { Fragment } from "react";

type SidebarProps = {
    isSidebarCollapsed: boolean;
    changeIsSidebarCollapsed: (isSidebarCollapsed: boolean) => void;
};

const Sidebar = ({
                     isSidebarCollapsed,
                     changeIsSidebarCollapsed,
                 }: SidebarProps) => {

    const sidebarItems = [
        { routerLink: "dashboard", icon: faHome, label: "Dashboard" },
        { routerLink: "atendimentos", icon: faFileLines, label: "Atendimentos" },
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
