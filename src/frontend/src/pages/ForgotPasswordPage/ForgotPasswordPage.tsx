// src/pages/ForgotPasswordPage/ForgotPasswordPage.tsx (NOVO ARQUIVO)
import React, { useState } from 'react';
import { useForm, SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { toast } from 'sonner';
import { forgotPassword } from '@/services/authService';
import { Link } from 'react-router-dom';

const forgotPasswordSchema = z.object({
  email: z.string().min(1, "Email é obrigatório.").email("Formato de email inválido."),
});

type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

const ForgotPasswordPage: React.FC = () => {
  const [isSubmitted, setIsSubmitted] = useState(false);
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<ForgotPasswordFormData>({
    resolver: zodResolver(forgotPasswordSchema),
  });

  const onSubmit: SubmitHandler<ForgotPasswordFormData> = async (data) => {
    try {
      const response = await forgotPassword(data.email);
      toast.success(response.message); // Exibe a mensagem do backend
      setIsSubmitted(true);
    } catch (error: any) {
      // Mesmo com erro, exibimos uma mensagem genérica por segurança
      toast.success("Se um usuário com este email existir, um link foi enviado.");
      setIsSubmitted(true);
    }
  };

  return (
    <div className="bg-background dark flex min-h-svh w-full items-center justify-center p-4">
      <Card className="w-full max-w-md bg-background shadow-md border dark:border-slate-700">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">Esqueceu sua Senha?</CardTitle>
          <CardDescription className='text-white'>
            {isSubmitted 
              ? "Verifique sua caixa de entrada (e spam) pelo link de redefinição." 
              : "Digite seu email para receber um link de redefinição de senha."}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isSubmitted ? (
            <div className="text-center">
              <p className="mb-4">Se você não receber o email em alguns minutos, por favor, tente novamente mais tarde.</p>
              <Button asChild><Link to="/">Voltar para Login</Link></Button>
            </div>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="space-y-1">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" placeholder="seu@email.com" {...register("email")} disabled={isSubmitting} />
                {errors.email && <p className="text-sm text-destructive mt-1">{errors.email.message}</p>}
              </div>
              <Button type="submit" className="w-full" disabled={isSubmitting}>
                {isSubmitting ? "Enviando..." : "Enviar Link de Redefinição"}
              </Button>
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  );
};

export default ForgotPasswordPage;