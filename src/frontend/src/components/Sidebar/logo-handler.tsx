
import {
  SidebarMenu,
  SidebarMenuButton,
} from "@/components/ui/sidebar"

export function LogoHandler({
  logoPath,
  name,
}: {
  logoPath: string
  name: string
}) {
  return (
    <SidebarMenu>
      <SidebarMenuButton
        size="lg"
        className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
      >
        <div className="bg-sidebar-primary text-sidebar-primary-foreground flex aspect-square size-8 items-center justify-center rounded-lg">
          <img
            src={logoPath}
            alt="Logo"
            className="h-8 w-8 rounded-lg"/>
        </div>
        <div className="grid flex-1 text-left text-sm leading-tight">
          <span className="truncate font-medium">{name}</span>
        </div>
      </SidebarMenuButton>
    </SidebarMenu>
  )
}
