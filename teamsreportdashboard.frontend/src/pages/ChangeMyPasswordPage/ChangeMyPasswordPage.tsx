// src/pages/ChangeMyPasswordPage/ChangeMyPasswordPage.tsx

import React from 'react'; // Removido useState se não for mais usado para passwordChangeSuccess local
import { useForm, SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { useNavigate } from 'react-router-dom';

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { ChangeMyPasswordPayload } from '@/types/ChangeMyPasswordPayload'; // Certifique-se que este tipo está definido
import { changeMyPassword } from '@/services/userService'; // Seu serviço

// Schema de validação com Zod (como definido e validado anteriormente)
const changeMyPasswordSchema = z.object({
  oldPassword: z.string().min(1, "Senha antiga é obrigatória."),
  newPassword: z.string().min(8, "Nova senha deve ter no mínimo 8 caracteres."),
  newPasswordConfirm: z.string().min(1, "Confirmação da nova senha é obrigatória."),
}).refine(data => data.newPassword === data.newPasswordConfirm, {
  message: "As senhas não coincidem.",
  path: ["newPasswordConfirm"],
});

type ChangeMyPasswordFormData = z.infer<typeof changeMyPasswordSchema>;

const ChangeMyPasswordPage: React.FC = () => {
  const { user, logout, isLoading: authIsLoading } = useAuth(); // user pode ser necessário para o ID se o backend ainda precisar
  const navigate = useNavigate();

  const { 
    register, 
    handleSubmit: rhfHandleSubmit,
    formState: { errors, isSubmitting },
    reset 
  } = useForm<ChangeMyPasswordFormData>({ /* ... */ });

  const onSubmit: SubmitHandler<ChangeMyPasswordFormData> = async (data) => {
    const payload: ChangeMyPasswordPayload = {
      oldPassword: data.oldPassword,
      newPassword: data.newPassword,
      newPasswordConfirm: data.newPasswordConfirm,
    };

    try {
      // Se o backend foi atualizado para NÃO precisar do ID (ideal):
      await changeMyPassword(payload);
      // OU se o backend AINDA precisa do ID:
      // if (!user) { toast.error("Sessão inválida."); return; }
      // await changeMyPassword(user.id, payload);
      
      reset(); // Limpa o formulário

      // NÃO chama logout() aqui.
      // Navega para a página de sucesso passando um estado.
      navigate('/auth/password-changed-successfully', { 
        state: { 
          passwordJustChanged: true // Flag para a página de sucesso saber que precisa fazer o logout
        } 
      });

    } catch (error: any) {
      console.error("Erro ao alterar senha:", error);
      const message = 
        error?.response?.data?.errors?.map((e: any) => e.ErrorMessage || e.errorMessage || e).join(", ") ||
        error?.response?.data?.message || 
        "Falha ao alterar a senha. Verifique a senha antiga e tente novamente.";
      toast.error(message);
    }
  };

  // ... (JSX do componente e guardas de loading/!user como antes) ...
  if (authIsLoading) {
    return <div className="flex min-h-[calc(100vh-var(--header-height,80px))] w-full items-center justify-center">Carregando...</div>;
  }
  if (!user && !authIsLoading) {
    navigate('/'); // Se por algum motivo user for null após o loading, volta pro login
    return null; 
  }

  return (
    // JSX do formulário como na resposta #40 (sem a tela de sucesso embutida aqui)
    <div className="flex min-h-[calc(100vh-var(--header-height,80px))] w-full items-center justify-center p-4 md:p-6 bg-muted/40">
      <Card className="w-full max-w-md shadow-lg">
        <CardHeader className="text-center space-y-1">
          <CardTitle className="text-2xl font-bold">Alterar Minha Senha</CardTitle>
          <CardDescription>
            Para sua segurança, escolha uma senha forte.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={rhfHandleSubmit(onSubmit)} className="space-y-4">
            {/* Campos do formulário como antes */}
            <div className="space-y-1.5">
              <Label htmlFor="oldPassword">Senha Antiga</Label>
              <Input id="oldPassword" type="password" {...register("oldPassword")} disabled={isSubmitting} autoComplete="current-password"/>
              {errors.oldPassword && <p className="text-sm text-destructive mt-1">{errors.oldPassword.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="newPassword">Nova Senha</Label>
              <Input id="newPassword" type="password" {...register("newPassword")} disabled={isSubmitting} autoComplete="new-password"/>
              {errors.newPassword && <p className="text-sm text-destructive mt-1">{errors.newPassword.message}</p>}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="newPasswordConfirm">Confirmar Nova Senha</Label>
              <Input id="newPasswordConfirm" type="password" {...register("newPasswordConfirm")} disabled={isSubmitting} autoComplete="new-password"/>
              {errors.newPasswordConfirm && <p className="text-sm text-destructive mt-1">{errors.newPasswordConfirm.message}</p>}
            </div>
            <Button type="submit" className="w-full mt-6" disabled={isSubmitting}>
              {isSubmitting ? "Salvando..." : "Alterar Senha"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
};

export default ChangeMyPasswordPage;