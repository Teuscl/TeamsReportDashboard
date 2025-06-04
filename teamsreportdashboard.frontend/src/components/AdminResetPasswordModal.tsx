// src/components/AdminResetPasswordModal.tsx (NOVO ARQUIVO)
import React, { useEffect } from 'react';
import { useForm, SubmitHandler } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';

import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { toast } from 'sonner';
import { User } from '@/types/User'; // Para tipar o usuário que está sendo editado
import { AdminChangePasswordPayload } from '@/services/userService'; // Ou onde você definiu
import { adminChangeUserPassword } from '@/services/userService';

// Schema de validação com Zod
const adminResetPasswordSchema = z.object({
  newPassword: z.string().min(8, "Nova senha deve ter no mínimo 8 caracteres."),
  newPasswordConfirm: z.string().min(1, "Confirmação da nova senha é obrigatória."),
}).refine(data => data.newPassword === data.newPasswordConfirm, {
  message: "As senhas não coincidem.",
  path: ["newPasswordConfirm"],
});

type AdminResetPasswordFormData = z.infer<typeof adminResetPasswordSchema>;

interface AdminResetPasswordModalProps {
  userToReset: User | null; // O usuário cuja senha será resetada
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void; // Callback para sucesso
}

const AdminResetPasswordModal: React.FC<AdminResetPasswordModalProps> = ({
  userToReset,
  isOpen,
  onClose,
  onSuccess,
}) => {
  const { 
    register, 
    handleSubmit: rhfHandleSubmit,
    formState: { errors, isSubmitting },
    reset 
  } = useForm<AdminResetPasswordFormData>({
    resolver: zodResolver(adminResetPasswordSchema),
    defaultValues: {
      newPassword: '',
      newPasswordConfirm: '',
    }
  });

  const resetFormAndClose = () => {
    reset({ newPassword: '', newPasswordConfirm: '' });
    onClose();
  };

  // Reseta o formulário quando o modal é aberto ou o usuário muda
  useEffect(() => {
    if (isOpen) {
      reset({ newPassword: '', newPasswordConfirm: '' });
    }
  }, [isOpen, reset]);

  const onSubmit: SubmitHandler<AdminResetPasswordFormData> = async (data) => {
    if (!userToReset) {
      toast.error("Nenhum usuário selecionado para resetar a senha.");
      return;
    }

    const payload: AdminChangePasswordPayload = {
      newPassword: data.newPassword,
      newPasswordConfirm: data.newPasswordConfirm,
    };

    try {
      await adminChangeUserPassword(userToReset.id, payload);
      toast.success(`Senha para ${userToReset.name} alterada com sucesso!`);
      onSuccess(); // Chama o callback de sucesso (ex: fechar modal, mostrar feedback)
      resetFormAndClose();
    } catch (error: any) {
      console.error("Erro ao alterar senha do usuário:", error);
      const message = 
        error?.response?.data?.errors?.map((e: any) => e.ErrorMessage || e.errorMessage || e).join(", ") ||
        error?.response?.data?.message || 
        "Falha ao alterar a senha do usuário.";
      toast.error(message);
    }
  };
  
  if (!isOpen || !userToReset) return null;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && !isSubmitting && resetFormAndClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Resetar Senha para {userToReset.name}</DialogTitle>
          <DialogDescription>
            Defina uma nova senha para o usuário. Ele precisará usá-la no próximo login.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={rhfHandleSubmit(onSubmit)} className="space-y-4 py-2 pb-4">
          <div className="space-y-1.5">
            <Label htmlFor="admin-newPassword">Nova Senha</Label>
            <Input 
              id="admin-newPassword" 
              type="password"
              {...register("newPassword")} 
              disabled={isSubmitting}
              autoComplete="new-password" // Importante para gerenciadores de senha
            />
            {errors.newPassword && <p className="text-sm text-destructive mt-1">{errors.newPassword.message}</p>}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="admin-newPasswordConfirm">Confirmar Nova Senha</Label>
            <Input 
              id="admin-newPasswordConfirm" 
              type="password"
              {...register("newPasswordConfirm")} 
              disabled={isSubmitting}
              autoComplete="new-password"
            />
            {errors.newPasswordConfirm && <p className="text-sm text-destructive mt-1">{errors.newPasswordConfirm.message}</p>}
          </div>
        
          <DialogFooter className="pt-4">
            <DialogClose asChild>
              <Button type="button" variant="outline" onClick={resetFormAndClose} disabled={isSubmitting}>Cancelar</Button>
            </DialogClose>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Salvando..." : "Definir Nova Senha"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};

export default AdminResetPasswordModal;