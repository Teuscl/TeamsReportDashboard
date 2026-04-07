import { SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar"
import { AppSidebar } from "@/components/Sidebar/app-sidebar"
import { Toaster } from "sonner"
import { ThemeProvider } from "../theme-provider"

export default function Layout({ children }: { children: React.ReactNode }) {
  return (
    <ThemeProvider defaultTheme="dark" storageKey="vite-ui-theme">
      <SidebarProvider>
        <AppSidebar />
          <div className="flex items-start justify-between">
              <SidebarTrigger/>
          </div>          
      <main className="flex flex-1 flex-col  p-4 pt-0 w-full h-screen">
        
        {children}
        <Toaster/>
      </main>      
    </SidebarProvider>
    </ThemeProvider>
    
  )
}
