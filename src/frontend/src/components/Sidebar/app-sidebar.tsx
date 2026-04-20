// src/components/layout/app-sidebar.tsx (ou o caminho correto)
import { Home, FileText, Users, Building, BookUser, Import, BotMessageSquare } from "lucide-react";
import { Link } from "react-router-dom"; // 👈 Importar Link para navegação SPA
import "../../index.css"; // Verifique se este caminho está correto ou se é necessário
import { NavUser } from "./nav-user";

import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar"; // Componentes da sua UI de Sidebar
import { LogoHandler } from "./logo-handler";
import { useAuth } from "@/context/AuthContext"; // 👈 Importar useAuth
import { RoleEnum } from "@/utils/role";      // 👈 Importar RoleEnum (ajuste o caminho se necessário)

// Defina um tipo para os itens do menu para melhor tipagem
interface MenuItemType {
  title: string;
  url: string;
  icon: React.ElementType; // Para componentes de ícone como Home, FileText
}

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const { user, isLoading } = useAuth(); // 👈 Obter usuário e estado de carregamento do contexto

  // Itens base do menu que todos os usuários logados veem
  const baseItems: MenuItemType[] = [
    {
      title: "Dashboard",
      url: "/dashboard", 
      icon: Home,
    },
    {
      title: "Atendimentos",
      url: "/reports", 
      icon: FileText,
    },
  ];

  let finalItems: MenuItemType[] = [...baseItems];

   // 👇 2. Lógica atualizada para múltiplos perfis e menus
  if (!isLoading && user) {
    // Itens para Admin e Master
    if (user.role === RoleEnum.Admin || user.role === RoleEnum.Master) {
      finalItems.push({
        title: "Departamentos",
        url: "/departments",
        icon: Building, // Usando o novo ícone
      },
      {
        title: "Solicitantes",
        url: "/requesters",
        icon: BookUser, // Usando o ícone de usuários
      }
    );
    }

    // Itens exclusivos para Master
    if (user.role === RoleEnum.Master) {
      finalItems.push(
        {
          title: "Usuários",
          url: "/users",
          icon: Users,
        },
        {
          title: "Importações",
          url: "/imports",
          icon: Import,
        },
        {
          title: "Prompt de IA",
          url: "/prompt",
          icon: BotMessageSquare,
        }
      );
    }
  }

  return (
    <Sidebar
      collapsible="icon"
      variant="sidebar"
      // className="bg-primary bg-red-500 bg-secondary dark text-white" // Revise esta linha, parece ter múltiplos bgs
      className="bg-secondary dark text-white" // Exemplo com um BG, ajuste conforme seu tema
      {...props}
    >
      <SidebarHeader>
        <LogoHandler
          logoPath="/pecege.png" // Verifique se este é o caminho correto no seu diretório public
          name="Sistema de Relatórios"
        />
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Menu</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {finalItems.map((item) => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton asChild>
                    {/* ✨ Use o componente Link para navegação interna SPA */}
                    <Link to={item.url}>
                      <item.icon className="mr-2 h-5 w-5" /> {/* Adicionado margin e tamanho */}
                      <span>{item.title}</span>
                    </Link>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <NavUser /> 
      </SidebarFooter>
    </Sidebar>
  );
}