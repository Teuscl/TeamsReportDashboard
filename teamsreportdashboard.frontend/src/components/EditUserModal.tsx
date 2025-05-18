import React, { useEffect, useState } from 'react';
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { User } from "@/types/User";
import { toast } from "sonner";
// Roles disponíveis
const roleOptions = [
  { label: "Master", value: "Master" },
  { label: "Admin", value: "Admin" },
  { label: "Viewer", value: "Viewer" },
];

interface Props {
  user: User | null;
  isOpen: boolean;
  onClose: () => void;
  onSave: (updatedUser: User) => void;
}

export const EditUserModal: React.FC<Props> = ({ user, isOpen, onClose, onSave }) => {
  const [formData, setFormData] = useState<User | null>(user);

  useEffect(() => {
    setFormData(user);
  }, [user]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    if (!formData) return;
    const target = e.target as HTMLInputElement | HTMLSelectElement;
    const { name, type } = target;
    const value = type === 'checkbox' ? target.checked : target.value;
    setFormData({ ...formData, [name]: value });
  };

  const handleSubmit = async () => {
    if (!formData) return;

    try {
      const updated = await import('@/services/userService').then(m => m.updateUser(formData));
      onSave(updated || formData);
      onClose();
    } catch (error: any) {
      console.error("Erro ao atualizar usuário:", error);

      let message = "Erro inesperado. Tente novamente.";

      if (error?.response?.data) {
        const data = error.response.data;

        if (Array.isArray(data.errors)) {
          message = data.errors.join(", ");
        } else if (typeof data.message === "string") {
          message = data.message;
        }
      }

      toast("Erro ao atualizar usuário: " + message)
     
    }
  };

  if (!formData) return null;

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="w-full max-w-md rounded-lg p-6 space-y-6">
        <DialogTitle className="text-2xl font-semibold text-gray-800">Editar Usuário</DialogTitle>

        <form onSubmit={(e) => { e.preventDefault(); handleSubmit(); }} className="space-y-4">
          {/* Nome */}
          <div className="flex flex-col gap-1">
            <Label htmlFor="name" className="text-sm font-medium text-gray-700">Nome</Label>
            <Input
              id="name"
              name="name"
              value={formData.name}
              onChange={handleChange}
              className="rounded-md border border-gray-300 focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {/* Email */}
          <div className="flex flex-col gap-1">
            <Label htmlFor="email" className="text-sm font-medium text-gray-700">Email</Label>
            <Input
              id="email"
              name="email"
              value={formData.email}
              onChange={handleChange}
              className="rounded-md border border-gray-300 focus:ring-2 focus:ring-blue-500"
            />
          </div>

          {/* Role */}
          <div className="flex flex-col gap-1">
            <Label htmlFor="role" className="text-sm font-medium text-gray-700">Função</Label>
            <select
              id="role"
              name="role"
              value={formData.role}
              onChange={handleChange}
              className="rounded-md border border-gray-300 px-3 py-2 focus:ring-2 focus:ring-blue-500"
            >
              {roleOptions.map((option) => (
                <option key={option.value} value={option.value}>{option.label}</option>
              ))}
            </select>
          </div>

          {/* Ativo */}
          <div className="flex items-center gap-3 pt-2">
            <input
              type="checkbox"
              id="isActive"
              name="isActive"
              checked={formData.isActive}
              onChange={handleChange}
              className="h-4 w-4 rounded border-gray-300 focus:ring-2 focus:ring-blue-500"
            />
            <Label htmlFor="isActive" className="text-sm font-medium text-gray-700">
              Usuário Ativo
            </Label>
          </div>

          {/* Botões */}
          <div className="flex justify-end gap-2 pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={onClose}
              className="px-4 py-2"
            >
              Cancelar
            </Button>
            <Button type="submit" className="px-4 py-2">
              Salvar
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
};
