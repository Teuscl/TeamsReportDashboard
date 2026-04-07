// src/pages/ResetPasswordPage/ResetPasswordPage.tsx (NOVO ARQUIVO)
import React, { useEffect, useState } from 'react';
import { useForm, SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { toast } from 'sonner';
import { resetPassword, ResetPasswordPayload } from '@/services/authService';

const resetPasswordSchema = z.object({
  newPassword: z.string().min(8, "Nova senha deve ter no mínimo 8 caracteres."),
  confirmPassword: z.string(),
}).refine(data => data.newPassword === data.confirmPassword, {
  message: "As senhas não coincidem.",
  path: ["confirmPassword"],
});

type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

const ResetPasswordPage: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');
  const [error, setError] = useState<string | null>(null);

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
  });

  useEffect(() => {
    if (!token) {
      setError("Token de redefinição inválido ou ausente. Por favor, solicite um novo link.");
    }
  }, [token]);

  const onSubmit: SubmitHandler<ResetPasswordFormData> = async (data) => {
    if (!token) return;

    try {
      const payload: ResetPasswordPayload = {
        token,
        newPassword: data.newPassword,
        confirmPassword: data.confirmPassword,
      };
      const response = await resetPassword(payload);
      toast.success(response.message || "Senha redefinida com sucesso!");
      // Redireciona para o login com uma mensagem de sucesso no estado
      navigate('/', { state: { message: "Senha redefinida! Você já pode fazer login.", type: 'success' } });
    } catch (err: any) {
      const message = err?.response?.data?.message || "Ocorreu um erro. O token pode ser inválido ou expirado.";
      setError(message);
      toast.error(message);
    }
  };
  
  return (
    <div className="flex min-h-svh w-full items-center justify-center p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">Definir Nova Senha</CardTitle>
          <CardDescription>
            Escolha uma nova senha segura para sua conta.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {error ? (
            <div className="text-center text-destructive">{error}</div>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="space-y-1">
                <Label htmlFor="newPassword">Nova Senha</Label>
                <Input id="newPassword" type="password" {...register("newPassword")} disabled={isSubmitting} />
                {errors.newPassword && <p className="text-sm text-destructive mt-1">{errors.newPassword.message}</p>}
              </div>
              <div className="space-y-1">
                <Label htmlFor="confirmPassword">Confirmar Nova Senha</Label>
                <Input id="confirmPassword" type="password" {...register("confirmPassword")} disabled={isSubmitting} />
                {errors.confirmPassword && <p className="text-sm text-destructive mt-1">{errors.confirmPassword.message}</p>}
              </div>
              <Button type="submit" className="w-full" disabled={isSubmitting}>
                {isSubmitting ? "Salvando..." : "Redefinir Senha"}
              </Button>
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  );
};

export default ResetPasswordPage;