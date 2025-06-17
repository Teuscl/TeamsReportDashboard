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

import {getDepartments } from '@/services/departmentService';
import { createRequester, updateRequester} from '@/services/requesterService';
import { CreateRequesterDto, RequesterDto, UpdateRequesterDto } from '@/types/Requester';
import { Department } from '@/types/Department';

interface RequesterFormData {
 name: string;
 email: string;
 departmentId: number | null;
}

interface RequesterFormModalProps {
 mode: 'create' | 'edit';
 requesterToEdit?: RequesterDto | null;
 isOpen: boolean;
 onClose: () => void;
 onSaveSuccess: () => void;
}

const DEFAULT_FORM_DATA: RequesterFormData = {
 name: '',
 email: '',
 departmentId: null,
};

export const RequesterFormModal: React.FC<RequesterFormModalProps> = ({
 mode,
 requesterToEdit,
 isOpen,
 onClose,
 onSaveSuccess,
}) => {
 const [formData, setFormData] = useState<RequesterFormData>(DEFAULT_FORM_DATA);
 const [isSaving, setIsSaving] = useState(false);
 const [departments, setDepartments] = useState<Department[]>([]);

 const resetFormAndClose = () => {
  setFormData(DEFAULT_FORM_DATA);
  setIsSaving(false);
  onClose();
 };

 useEffect(() => {
  if (isOpen) {
   getDepartments()
    .then(setDepartments)
    .catch(() => toast.error("Erro ao carregar lista de departamentos."));

   if (mode === 'edit' && requesterToEdit) {
    setFormData({
     name: requesterToEdit.name,
     email: requesterToEdit.email,
     departmentId: requesterToEdit.departmentId || null,
    });
   } else {
    setFormData(DEFAULT_FORM_DATA);
   }
  }
 }, [isOpen, mode, requesterToEdit]);

 const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
  const { name, value } = e.target;
  setFormData(prev => ({ ...prev, [name]: value }));
 };

 const handleDepartmentChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
  const { value } = e.target;
  setFormData(prev => ({ ...prev, departmentId: value === 'null' ? null : parseInt(value, 10) }));
 };

 const handleSubmit = async (e: FormEvent) => {
  e.preventDefault();
  setIsSaving(true);

  const payload: CreateRequesterDto | UpdateRequesterDto = {
   name: formData.name,
   email: formData.email,
   departmentId: formData.departmentId,
  };

  try {
   if (mode === 'edit' && requesterToEdit) {
    await updateRequester(requesterToEdit.id, payload as UpdateRequesterDto);
    toast.success("Solicitante atualizado com sucesso!");
   } else if (mode === 'create') {
    await createRequester(payload as CreateRequesterDto);
    toast.success("Solicitante criado com sucesso!");
   }
   onSaveSuccess();
   resetFormAndClose();
  } catch (error: any) {
   toast.error(error?.response?.data?.message || `Erro ao ${mode === 'create' ? 'criar' : 'atualizar'} solicitante.`);
  } finally {
   setIsSaving(false);
  }
 };

 const dialogTitle = mode === 'create' ? "Criar Novo Solicitante" : "Editar Solicitante";
 const saveButtonText = mode === 'create' ? "Criar Solicitante" : "Salvar Alterações";

 return (
  <Dialog open={isOpen} onOpenChange={(open) => !open && !isSaving && resetFormAndClose()}>
   <DialogContent className="sm:max-w-md">
    <DialogHeader>
     <DialogTitle>{dialogTitle}</DialogTitle>
     <DialogDescription>
      {mode === 'create' ? 'Preencha os dados para criar um novo solicitante.' : `Editando informações de ${requesterToEdit?.name}.`}
     </DialogDescription>
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

     <div className="space-y-1">
      <Label htmlFor="department-form">Departamento</Label>
      <select
       id="department-form"
       name="departmentId"
       value={formData.departmentId === null ? 'null' : formData.departmentId}
       onChange={handleDepartmentChange}
       className="flex h-10 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
       disabled={isSaving}
      >
       <option value="null">Sem Departamento</option>
       {departments.map(dept => (
        <option key={dept.id} value={dept.id}>{dept.name}</option>
       ))}
      </select>
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