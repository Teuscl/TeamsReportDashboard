// src/components/UserFormModal.tsx
import React, { useEffect, useState, FormEvent } from 'react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
  DialogClose,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { toast } from "sonner";
import { User } from "@/types/User"; // Sua interface User global: { id, name, email, role: RoleEnum, isActive }
import { RoleEnum, getRoleLabel } from '@/utils/role'; // Seus utilitários de Role
import { createUser, updateUser, CreateUserPayload, UpdateUserPayload } from '@/services/userService'; // Seus serviços

// Interface para o estado interno do formulário
interface UserFormData {
  name: string;
  email: string;
  role: RoleEnum;
  isActive: boolean;
  password?: string; // Senha é opcional e só para modo 'create'
}

// Props do modal
interface UserFormModalProps {
  mode: 'create' | 'edit';
  userToEdit?: User | null; // Usuário para editar (apenas no modo 'edit')
  isOpen: boolean;
  onClose: () => void;
  onSaveSuccess: () => void; // Callback único para sucesso (create ou edit)
}

// Opções para o select de Role, baseadas no seu RoleEnum e roleLabels
const roleOptionsForSelect = Object.values(RoleEnum)
  .filter(value => typeof value === 'number') // Pega apenas os valores numéricos do enum
  .map(roleValue => ({
    value: roleValue as RoleEnum,
    label: getRoleLabel(roleValue as RoleEnum),
  }));

const DEFAULT_FORM_DATA: UserFormData = {
  name: '',
  email: '',
  role: RoleEnum.Viewer, // Role padrão para criação
  isActive: true,
  password: '',
};

export const UserFormModal: React.FC<UserFormModalProps> = ({
  mode,
  userToEdit,
  isOpen,
  onClose,
  onSaveSuccess,
}) => {
  const [formData, setFormData] = useState<UserFormData>(DEFAULT_FORM_DATA);
  const [isSaving, setIsSaving] = useState(false);

  const resetFormAndClose = () => {
    setFormData(DEFAULT_FORM_DATA);
    setIsSaving(false);
    onClose();
  };

  useEffect(() => {
    if (isOpen) {
      if (mode === 'edit' && userToEdit) {
        setFormData({
          name: userToEdit.name,
          email: userToEdit.email,
          role: userToEdit.role,
          isActive: userToEdit.isActive,
          password: '', // Senha não é preenchida ou editada diretamente aqui
        });
      } else {
        setFormData(DEFAULT_FORM_DATA); // Reset para modo 'create' ou se não houver userToEdit
      }
    }
  }, [isOpen, mode, userToEdit]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const target = e.target as HTMLInputElement | HTMLSelectElement;
    const { name, type } = target;
    
    let value: string | number | boolean;
    if (type === 'checkbox') {
      value = (target as HTMLInputElement).checked;
    } else if (name === 'role') {
      value = Number(target.value) as RoleEnum; // Role é sempre numérico do select
    } else {
      value = target.value;
    }
    setFormData(prev => ({ ...prev, [name]: value }));
  };
  
  const handleCheckedChange = (checked: boolean | 'indeterminate') => {
    // Para o componente Checkbox do shadcn/ui, onCheckedChange pode retornar 'indeterminate'
    if (typeof checked === 'boolean') {
      setFormData(prev => ({ ...prev, isActive: checked }));
    }
  };


  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault(); // Previne o comportamento padrão do formulário
    setIsSaving(true);
    try {
      if (mode === 'edit' && userToEdit) {
        const payload: UpdateUserPayload = {
          id: userToEdit.id,
          name: formData.name,
          email: formData.email,
          role: formData.role,
          isActive: formData.isActive,
        };
        await updateUser(payload); // userService.updateUser agora espera apenas o payload
        toast.success("Usuário atualizado com sucesso!");
      } else if (mode === 'create') {
        if (!formData.password || formData.password.trim() === '') {
            toast.error("O campo senha é obrigatório para criar um usuário.");
            setIsSaving(false);
            return;
        }
        const payload: CreateUserPayload = {
          name: formData.name,
          email: formData.email,
          password: formData.password,
          role: formData.role,
          isActive: formData.isActive,
        };
        await createUser(payload);
        toast.success("Usuário criado com sucesso!");
      }
      onSaveSuccess();
      resetFormAndClose();
    } catch (error: any) {
      console.error(`Erro ao ${mode === 'create' ? 'criar' : 'atualizar'} usuário:`, error);
      const message = error?.response?.data?.errors?.map((err: any) => err.ErrorMessage || err.errorMessage || err).join(", ") || // Ajustado para pegar ErrorMessage
                      error?.response?.data?.message ||
                      `Erro ao ${mode === 'create' ? 'criar' : 'atualizar'} usuário.`;
      toast.error(message);
    } finally {
      setIsSaving(false);
    }
  };

  const dialogTitle = mode === 'create' ? "Criar Novo Usuário" : "Editar Usuário";
  const saveButtonText = mode === 'create' ? "Criar Usuário" : "Salvar Alterações";

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && !isSaving && resetFormAndClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{dialogTitle}</DialogTitle>
          {mode === 'edit' && userToEdit && (
            <DialogDescription>
              Editando informações de {userToEdit.name}. Clique em "{saveButtonText}" quando terminar.
            </DialogDescription>
          )}
           {mode === 'create' && (
            <DialogDescription>
              Preencha os dados para criar um novo usuário.
            </DialogDescription>
          )}
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2 pb-4">
          <div className="space-y-1">
            <Label htmlFor="name-form">Nome</Label>
            <Input id="name-form" name="name" value={formData.name} onChange={handleChange} required disabled={isSaving} />
          </div>

          <div className="space-y-1">
            <Label htmlFor="email-form">Email</Label>
            <Input id="email-form" name="email" type="email" value={formData.email} onChange={handleChange} required disabled={isSaving} />
          </div>

          {mode === 'create' && (
            <div className="space-y-1">
              <Label htmlFor="password-form">Senha</Label>
              <Input id="password-form" name="password" type="password" value={formData.password || ''} onChange={handleChange} required={mode === 'create'} disabled={isSaving} />
            </div>
          )}

          <div className="space-y-1">
            <Label htmlFor="role-form">Função</Label>
            <select
              id="role-form"
              name="role"
              value={formData.role}
              onChange={handleChange}
              required
              disabled={isSaving}
              className="flex h-10 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {roleOptionsForSelect.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </div>

          <div className="flex items-center space-x-2 pt-2">
            <Checkbox 
              id="isActive-form" 
              name="isActive" // O name não é usado diretamente pelo onCheckedChange do Shadcn Checkbox
              checked={formData.isActive} 
              onCheckedChange={handleCheckedChange} // Shadcn Checkbox usa onCheckedChange
              disabled={isSaving}
            />
            <Label htmlFor="isActive-form" className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
              Usuário Ativo
            </Label>
          </div>
        
          <DialogFooter className="pt-4">
            {/* DialogClose é usado para o botão Cancelar fechar o modal */}
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isSaving}>Cancelar</Button>
            </DialogClose>
            <Button type="submit" disabled={isSaving}>
              {isSaving ? "Salvando..." : saveButtonText}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};