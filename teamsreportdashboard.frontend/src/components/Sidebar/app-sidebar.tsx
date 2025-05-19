import { Home, FileText, Users } from "lucide-react"
import "../../index.css"
import { NavUser } from "./nav-user"
import { getCurrentUser } from "../../utils/auth"
import { useEffect, useState } from "react"

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
} from "@/components/ui/sidebar"
import { LogoHandler } from "./logo-handler"

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const [user, setUser] = useState({
    name: "Usuário",
    email: "",
    avatar: "U",
  })

  useEffect(() => {
    const loggedUser = getCurrentUser()
    if (loggedUser) {
      setUser({
        name: loggedUser.name,
        email: loggedUser.email,
        avatar: loggedUser.name?.[0].toUpperCase() || "U",
      })
    }
  }, [])

  const items = [
    {
      title: "Dashboard",
      url: "#",
      icon: Home,
    },
    {
      title: "Atendimentos",
      url: "#",
      icon: FileText,
    },
  ]

  if (getCurrentUser()?.role === "Master") {
    items.push({
      title: "Usuários",
      url: "/users",
      icon: Users,
    })
  }

  return (
    <Sidebar
      collapsible="icon"
      variant="sidebar"
      className="bg-primary bg-red-500 bg-secondary dark text-white"
      {...props}
    >
      <SidebarHeader>
        <LogoHandler
          logoPath="/pecege.png"
          name="Sistema de Relatórios"
        />
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Menu</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {items.map((item) => (
                <SidebarMenuItem key={item.title}>
                  <SidebarMenuButton asChild>
                    <a href={item.url}>
                      <item.icon />
                      <span>{item.title}</span>
                    </a>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              ))}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <NavUser user={user} />
      </SidebarFooter>
    </Sidebar>
  )
}
