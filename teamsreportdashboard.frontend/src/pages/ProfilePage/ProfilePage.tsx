import React, { useEffect, useState } from "react"
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Button } from "@/components/ui/button"
import { toast } from "sonner"
import { getMe, updateUser } from "@/services/userService"
import { getRoleLabel, getRoleValue } from "@/utils/role"
//import { useUser } from "@/context/UserContext"

const ProfilePage: React.FC = () => {
  // const { user, setUser } = useUser()
  // const [formData, setFormData] = useState({ name: "", email: "" })
  // const [loading, setLoading] = useState(false)

  // useEffect(() => {
  //   if (user) {
  //     setFormData({ name: user.name, email: user.email })
  //   }
  // }, [user])

  // const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
  //   const { name, value } = e.target
  //   setFormData((prev) => ({ ...prev, [name]: value }))
  // }

  // const handleSubmit = async (e: React.FormEvent) => {
  //   e.preventDefault()
  //   if (!user) return

  //   setLoading(true)
  //   try {
  //     const updated = await updateUser({
  //       id: user.id,
  //       name: formData.name,
  //       email: formData.email,
  //       role: user.role, // manter mesmo papel
  //       isActive: user.isActive,
  //     })

  //     setUser(updated) // atualiza contexto global
  //     toast.success("Perfil atualizado com sucesso!")
  //   } catch (error: any) {
  //     const message =
  //       error?.response?.data?.errors?.join(", ") ||
  //       error?.response?.data?.message ||
  //       "Erro ao atualizar perfil."

  //     toast.error(message)
  //   } finally {
  //     setLoading(false)
  //   }
  // }

  // if (!user) return null

  return (
    <div className="flex items-center justify-center min-h-[calc(100vh-80px)] px-4">
      <Card className="w-full max-w-2xl p-6 shadow-xl">
        <CardHeader className="text-center">
          <CardTitle className="text-3xl font-bold">Meu Perfil</CardTitle>
        </CardHeader>

        <CardContent>
          <div className="flex flex-col items-center gap-4 mb-8">
            <Avatar className="h-24 w-24 ring-2 ring-primary shadow-md">
              <AvatarFallback className="text-xl">
                {/* {user.name.charAt(0).toUpperCase()} */}
                {'A'}
              </AvatarFallback>
            </Avatar>
            <p>
              {/* Função: <b>{getRoleLabel(user.role)}</b> */}
              Função: <b>{"a"}</b>
            </p>
          </div>

          <form /*onSubmit={handleSubmit}*/ className="space-y-6">
            <div className="space-y-2">
              <Label htmlFor="name">Nome</Label>
              <Input
                id="name"
                name="name"
                //value={formData.name}
                //onChange={handleChange}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                name="email"
                type="email"
                //value={formData.email}
                //onChange={handleChange}
                required
              />
            </div>

            <div className="pt-4">
              <Button
                type="submit"
                className="w-full text-base font-semibold"
                //disabled={loading}
              >
                {/* {loading ? "Salvando..." : "Salvar alterações"} */}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}

export default ProfilePage
