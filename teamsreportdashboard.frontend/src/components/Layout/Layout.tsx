import { SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar"
import { AppSidebar } from "@/components/Sidebar/app-sidebar"
import App from "@/App"

export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <SidebarProvider>
      <AppSidebar />
      <main className="flex flex-1 flex-col  p-4 pt-0 w-full h-screen">
        <div className="flex items-left justify-between">
          <SidebarTrigger/>
        </div>
        <div className="flex gap-3 items-center">
        </div>
        {children}
      </main>      
    </SidebarProvider>
  )
}
