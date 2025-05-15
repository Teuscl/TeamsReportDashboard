import { useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'
import { DataTable } from './components/CustomTable/DataTable'
import { Sidebar, Users } from 'lucide-react'
import UsersPage from './pages/UsersPage/UsersPage'
import Layout from './components/Layout/layout'
import { SidebarProvider } from './components/ui/sidebar'
import LoginPage from './pages/LoginPage/LoginPage'

function App() {
  const [count, setCount] = useState(0)

  return (
    // <SidebarProvider>
    //   <Layout>
    //     <UsersPage />
    //   </Layout>
    // </SidebarProvider>
    <LoginPage></LoginPage>
  )
}

export default App
