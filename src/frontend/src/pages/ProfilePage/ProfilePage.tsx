// src/pages/Profile/ProfilePage.tsx (ou o caminho correto)
import React, { useEffect, useState } from "react";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
// Assumindo que updateUser e User (para o DTO) estão definidos em userService
// e getRoleLabel em utils/role
import { updateUser } from "@/services/userService"; // Seu serviço para atualizar o usuário
import { User } from "@/types/User"; // Sua interface User
import { getRoleLabel } from "@/utils/role"; // Sua função utilitária para o nome da role
import { useAuth } from "@/context/AuthContext"; // 👈 Nosso AuthContext

// Função para gerar iniciais (pode ser movida para um utilitário se usada em mais lugares)
const getInitials = (name: string): string => {
  if (!name) return "?";
  const parts = name.trim().split(/\s+/);
  if (parts.length === 0 || parts[0] === "") return "?";
  if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
  return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
};

// Define o tipo para os dados do formulário
interface ProfileFormData {
  name: string;
  email: string;
}

const ProfilePage: React.FC = () => {
  const { user, isLoading: authIsLoading, checkAuthStatus } = useAuth();
  const [formData, setFormData] = useState<ProfileFormData>({ name: "", email: "" });
  const [isSubmitting, setIsSubmitting] = useState(false); // Renomeado de 'loading' para evitar conflito

  useEffect(() => {
    if (user) {
      setFormData({ name: user.name, email: user.email });
    }
  }, [user]); // Atualiza o formulário se o usuário do contexto mudar

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user) {
      toast.error("Usuário não encontrado. Por favor, faça login novamente.");
      return;
    }

    setIsSubmitting(true);
    try {
      // Crie o payload para a API de atualização.
      // Adapte isso conforme o que seu backend espera no DTO de UpdateUser.
      // Geralmente, para atualizar o próprio perfil, você não muda role ou isActive aqui.
      const updatePayload: User = {
        id: user.id, // Se seu backend /user/update precisar do ID no corpo
        name: formData.name,
        email: formData.email,
        role: user.role, // Envia a role atual se o backend esperar
        isActive: true, // Envia o status atual se o backend esperar
      };
      
      // Seu serviço updateUser deve fazer a chamada PUT/PATCH para o backend
      // Ex: await axiosConfig.put(`/user/${user.id}`, { name: formData.name, email: formData.email });
      // Ou se o updateUser já faz isso:
      console.log("Atualizando usuário com payload:", updatePayload);
      await updateUser(updatePayload); // Ajuste updateUser para aceitar o payload correto

      toast.success("Perfil atualizado com sucesso!");
      await checkAuthStatus(); // 👈 Revalida o usuário no AuthContext para pegar as novas infos
    } catch (error: any) {
      const message =
        error?.response?.data?.errors?.join(", ") ||
        error?.response?.data?.message ||
        "Erro ao atualizar perfil.";
      toast.error(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  if (authIsLoading) {
    return <div className="flex items-center justify-center min-h-[calc(100vh-80px)]">Carregando perfil...</div>;
  }

  if (!user) {
    // Isso não deveria acontecer se a rota estiver protegida por ProtectedRoute,
    // mas é uma boa guarda.
    return <div className="flex items-center justify-center min-h-[calc(100vh-80px)]">Usuário não encontrado.</div>;
  }

  const initials = user.name[0].charAt(0).toUpperCase();

  return (
    <div className="flex items-center justify-center min-h-[calc(100vh-80px)] px-4 py-8">
      <Card className="w-full max-w-2xl p-4 md:p-6 shadow-xl">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl md:text-3xl font-bold">Meu Perfil</CardTitle>
        </CardHeader>

        <CardContent>
          <div className="flex flex-col items-center gap-4 mb-6 md:mb-8">
            <Avatar className="h-20 w-20 md:h-24 md:w-24 ring-2 ring-primary shadow-md">
              {/* Se você tiver uma URL de avatar em user.avatarUrl, poderia usá-la em AvatarImage */}
              <AvatarFallback className="text-xl md:text-2xl">
                {initials}
              </AvatarFallback>
            </Avatar>
            <p className="text-sm md:text-base">
              Função: <b>{getRoleLabel(user.role)}</b> {/* Usando a função utilitária */}
            </p>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4 md:space-y-6">
            <div className="space-y-2">
              <Label htmlFor="name" className="text-sm font-medium">Nome</Label>
              <Input
                id="name"
                name="name"
                value={formData.name}
                onChange={handleChange}
                required
                className="text-base"
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="email" className="text-sm font-medium">Email</Label>
              <Input
                id="email"
                name="email"
                type="email"
                value={formData.email}
                onChange={handleChange}
                required
                className="text-base"
              />
            </div>

            <div className="pt-2 md:pt-4">
              <Button
                type="submit"
                className="w-full text-base font-semibold"
                disabled={isSubmitting || authIsLoading}
              >
                {isSubmitting ? "Salvando..." : "Salvar Alterações"}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
};

export default ProfilePage;