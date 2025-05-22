import { Home, FileText, Users } from "lucide-react"
import "../../index.css"
import { NavUser } from "./nav-user"

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
   //const { user } = useUser()

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

  // if (user?.role === 0 /* ou 'Master', dependendo do seu tipo */) {
  //   items.push({
  //     title: "Usuários",
  //     url: "/users",
  //     icon: Users,
  //   })
  // }

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
        <NavUser
          user={{
            name: "Usuário",
            email:  "",
            avatar: "U",
          }}
        />
      </SidebarFooter>
    </Sidebar>
  )
}
