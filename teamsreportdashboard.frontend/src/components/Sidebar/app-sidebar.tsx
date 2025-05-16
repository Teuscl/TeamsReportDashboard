import { Home, FileText} from "lucide-react"
import "../../index.css"
import { NavUser } from "./nav-user"
import { getCurrentUser } from "../../utils/auth"

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

const loggedUser = getCurrentUser()
console.log("loggedUser", loggedUser)

const data = {
  user: {
    name: loggedUser?.name || "Usuário",
    email: loggedUser?.email || "",
    avatar: loggedUser?.name[0].toUpperCase() || "",
  },
}
console.log("loggedUser", data.user)

// Menu items.
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

export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  return (
    <Sidebar collapsible="icon" variant="sidebar" className="bg-primary bg-red-500  bg-secondary dark  text-white" {...props} >
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
      <SidebarFooter className="">
        <NavUser user={data.user} />
      </SidebarFooter>
    </Sidebar>
  )
}
