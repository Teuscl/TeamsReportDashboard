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
import { toast } from "sonner";
import { Department } from "@/types/Department"; // A interface que já criamos
import { createDepartment, updateDepartment } from '@/services/departmentService'; // Nossos serviços de API

// Interface para o estado interno do formulário
interface DepartmentFormData {
  name: string;
}

// Props do modal (muito similar ao UserFormModal)
interface DepartmentFormModalProps {
  mode: 'create' | 'edit';
  departmentToEdit?: Department | null;
  isOpen: boolean;
  onClose: () => void;
  onSaveSuccess: () => void;
}

const DEFAULT_FORM_DATA: DepartmentFormData = {
  name: '',
};

export const DepartmentFormModal: React.FC<DepartmentFormModalProps> = ({
  mode,
  departmentToEdit,
  isOpen,
  onClose,
  onSaveSuccess,
}) => {
  const [formData, setFormData] = useState<DepartmentFormData>(DEFAULT_FORM_DATA);
  const [isSaving, setIsSaving] = useState(false);

  const resetFormAndClose = () => {
    setFormData(DEFAULT_FORM_DATA);
    setIsSaving(false);
    onClose();
  };

  // Efeito para popular o formulário quando o modal abre
  useEffect(() => {
    if (isOpen) {
      if (mode === 'edit' && departmentToEdit) {
        setFormData({ name: departmentToEdit.name });
      } else {
        setFormData(DEFAULT_FORM_DATA);
      }
    }
  }, [isOpen, mode, departmentToEdit]);

  // Handler genérico para o input de nome
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };
  
  // Função de submissão do formulário
  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    
    if (formData.name.trim().length < 2) {
      toast.error("O nome do departamento deve ter pelo menos 2 caracteres.");
      return;
    }

    setIsSaving(true);
    try {
      if (mode === 'edit' && departmentToEdit) {
        // Lógica de UPDATE
        await updateDepartment(departmentToEdit.id, formData);
      } else {
        // Lógica de CREATE
        await createDepartment(formData);
      }
      onSaveSuccess(); // Chama o callback de sucesso para atualizar a tabela na página
      resetFormAndClose(); // Fecha o modal e limpa o formulário
    } catch (error: any) {
      console.error(`Erro ao ${mode === 'create' ? 'criar' : 'atualizar'} departamento:`, error);
      const message = error?.response?.data?.message || `Erro ao salvar departamento.`;
      toast.error(message);
    } finally {
      setIsSaving(false);
    }
  };

  const dialogTitle = mode === 'create' ? "Criar Novo Departamento" : "Editar Departamento";
  const saveButtonText = mode === 'create' ? "Criar" : "Salvar Alterações";

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && resetFormAndClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{dialogTitle}</DialogTitle>
          <DialogDescription>
            {mode === 'create' ? 'Preencha o nome para o novo departamento.' : `Editando o departamento: ${departmentToEdit?.name}`}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4 py-2 pb-4">
          <div className="space-y-1">
            <Label htmlFor="name-form">Nome</Label>
            <Input 
              id="name-form" 
              name="name" 
              value={formData.name} 
              onChange={handleChange} 
              required 
              disabled={isSaving} 
            />
          </div>
        
          <DialogFooter className="pt-4">
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