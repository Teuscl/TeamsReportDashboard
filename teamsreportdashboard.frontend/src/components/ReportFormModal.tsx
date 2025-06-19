// src/components/ReportFormModal.tsx
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
import { Textarea } from "@/components/ui/textarea";
import { toast } from "sonner";
import { Report, CreateReportPayload, UpdateReportPayload } from '@/types/Report';
import { createReport, updateReport } from '@/services/reportService';

// Interface para os dados do formulário (usada pelo React Hook Form)
interface ReportFormData {
  requesterName: string;
  requesterEmail: string;
  technicianName: string; // No formulário, será string (pode ser vazia)
  requestDate: string;    // Formato yyyy-MM-ddTHH:MM
  reportedProblem: string;
  firstResponseTime: string; // Formato HH:MM:SS
  averageHandlingTime: string;
  category: string // Formato HH:MM:SS
}

// Schema Zod para validar os dados do formulário
const reportFormSchema = z.object({
  requesterName: z.string().min(1, "Nome do solicitante é obrigatório.").max(55),
  requesterEmail: z.string().min(1).email().max(100),
  technicianName: z.string().max(50),
  requestDate: z.string().min(1),
  reportedProblem: z.string().min(1).max(255),
  firstResponseTime: z.string()
    .min(1)
    .regex(/^([0-1][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$/, { message: "Tempo 1ª Resp. inválido (HH:MM:SS)." }),
  averageHandlingTime: z.string()
    .min(1)
    .regex(/^([0-1][0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$/, { message: "Tempo Médio Atend. inválido (HH:MM:SS)." }),
  category: z.string().min(1, "Categoria é obrigatória.").max(50, "Máximo de 50 caracteres."), // <- novo
});

interface ReportFormModalProps {
  mode: 'create' | 'edit';
  reportToEdit?: Report | null;
  isOpen: boolean;
  onClose: () => void;
  onSaveSuccess: () => void;
}

const formatDateTimeForInput = (isoDateString?: string): string => {
  if (!isoDateString) return '';
  try {
    const date = new Date(isoDateString);
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    const hours = date.getHours().toString().padStart(2, '0');
    const minutes = date.getMinutes().toString().padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  } catch (e) { return ''; }
};
const formatInputDateTimeToISOString = (inputDateTime: string): string => {
    if (!inputDateTime) return ''; // Deveria ser pego pela validação do Zod se obrigatório
    try { return new Date(inputDateTime).toISOString(); } catch (e) { return ''; }
};

const DEFAULT_FORM_VALUES: ReportFormData = {
  requesterName: '',
  requesterEmail: '',
  technicianName: '',
  requestDate: formatDateTimeForInput(new Date().toISOString()),
  reportedProblem: '',
  firstResponseTime: '00:00:00',
  averageHandlingTime: '00:00:00',
  category: '', // <- novo
};

export const ReportFormModal: React.FC<ReportFormModalProps> = ({
  mode,
  reportToEdit,
  isOpen,
  onClose,
  onSaveSuccess,
}) => {
  const { 
    register, 
    handleSubmit: rhfHandleSubmit,
    reset, 
    formState: { errors, dirtyFields, isSubmitting } // dirtyFields será usado agora
  } = useForm<ReportFormData>({
    resolver: zodResolver(reportFormSchema), // Seu schema Zod
    defaultValues: DEFAULT_FORM_VALUES,
  });

  // Guarda o estado inicial do formulário no modo de edição para comparação
  // Isso é importante para a lógica de PATCH condicional

 const resetFormAndClose = () => {
    reset(DEFAULT_FORM_VALUES);
    onClose();
  };

  useEffect(() => {
    if (isOpen) {
      if (mode === 'edit' && reportToEdit) {
        reset({ // Popula o RHF com os valores iniciais para edição
          requesterName: reportToEdit.requesterName,
          requesterEmail: reportToEdit.requesterEmail,
          technicianName: reportToEdit.technicianName || '',
          requestDate: formatDateTimeForInput(reportToEdit.requestDate),
          reportedProblem: reportToEdit.reportedProblem,
          firstResponseTime: reportToEdit.firstResponseTime || '00:00:00',
          averageHandlingTime: reportToEdit.averageHandlingTime || '00:00:00',
        });
      } else { // Modo 'create'
        reset({...DEFAULT_FORM_VALUES, requestDate: formatDateTimeForInput(new Date().toISOString())});
      }
    }
  }, [isOpen, mode, reportToEdit, reset]);

  const onSubmit: SubmitHandler<ReportFormData> = async (data) => {
    // 'data' contém os valores atuais do formulário, validados pelo Zod
    try {
      if (mode === 'edit' && reportToEdit) {
        const changedValuesPayload: Partial<UpdateReportPayload> = {};
        let hasRegisteredDirtyField = false;

        // Itera sobre as chaves que o React Hook Form marcou como 'dirty'
        for (const key in dirtyFields) {
          if (dirtyFields[key as keyof ReportFormData]) { // Verifica se o campo específico está 'dirty'
            hasRegisteredDirtyField = true;
            const typedKey = key as keyof ReportFormData;
            let valueToSet: any = data[typedKey]; // Pega o valor atual do formulário para o campo 'dirty'

            if (typedKey === 'requestDate') {
              valueToSet = formatInputDateTimeToISOString(data.requestDate);
            } else if (typedKey === 'technicianName') {
              // Envia null se a string do formulário estiver vazia
              valueToSet = data.technicianName.trim() === '' ? null : data.technicianName;
            }
            // Adiciona ao payload APENAS se a chave for válida para UpdateReportPayload
            // Esta verificação de 'key in ...' garante que estamos mapeando para campos do DTO de update
             if (Object.prototype.hasOwnProperty.call({} as UpdateReportPayload, typedKey) || 
                ["requesterName", "requesterEmail", "technicianName", "requestDate", "reportedProblem", "firstResponseTime", "averageHandlingTime"].includes(typedKey)) {
                (changedValuesPayload as any)[typedKey] = valueToSet;
            }
          }
        }
        
        if (!hasRegisteredDirtyField) {
          toast.info("Nenhuma alteração detectada para salvar.");
          onClose(); 
          return;
        }
        
        await updateReport(reportToEdit.id, changedValuesPayload);
        toast.success("Relatório atualizado com sucesso!");

      } else if (mode === 'create') {
        const createPayload: CreateReportPayload = {
          requesterName: data.requesterName,
          requesterEmail: data.requesterEmail,
          technicianName: data.technicianName.trim() === '' ? undefined : data.technicianName,
          requestDate: formatInputDateTimeToISOString(data.requestDate),
          reportedProblem: data.reportedProblem,
          firstResponseTime: data.firstResponseTime,
          averageHandlingTime: data.averageHandlingTime,
          category: data.category
        };
        await createReport(createPayload);
        toast.success("Relatório criado com sucesso!");
      }
      onSaveSuccess();
      resetFormAndClose();
    } catch (error: any) {
      console.error(`Erro ao ${mode === 'create' ? 'criar' : 'atualizar'} relatório:`, error);
      const message = error?.response?.data?.errors?.map((err: any) => err.ErrorMessage || err.errorMessage || err).join(", ") ||
                      error?.response?.data?.message ||
                      `Erro ao ${mode === 'create' ? 'criar' : 'atualizar'} relatório.`;
      toast.error(message);
    }
  };

  const dialogTitle = mode === 'create' ? "Criar Novo Relatório" : "Editar Relatório";
  const saveButtonText = mode === 'create' ? "Criar Relatório" : "Salvar Alterações";

  return (
    <Dialog open={isOpen} onOpenChange={(open) => { if (!open && !isSubmitting) resetFormAndClose(); }}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{dialogTitle}</DialogTitle>
          {mode === 'edit' && reportToEdit && (
            <DialogDescription>
              Editando relatório de {reportToEdit.requesterName}. {/* Pode usar reportToEdit aqui para o nome original */}
            </DialogDescription>
          )}
           {mode === 'create' && (
            <DialogDescription>
              Preencha os detalhes do novo relatório.
            </DialogDescription>
            )}
        </DialogHeader>

        <form onSubmit={rhfHandleSubmit(onSubmit)} className="space-y-3 py-2 max-h-[70vh] overflow-y-auto pr-1">
          {/* Nome do Solicitante */}
          <div className="space-y-1">
            <Label htmlFor="requesterName">Nome do Solicitante</Label>
            <Input id="requesterName" {...register("requesterName")} disabled={isSubmitting} />
            {errors.requesterName && <p className="text-xs text-red-500 mt-1">{errors.requesterName.message}</p>}
          </div>

          {/* Email do Solicitante */}
          <div className="space-y-1">
            <Label htmlFor="requesterEmail">Email do Solicitante</Label>
            <Input id="requesterEmail" type="email" {...register("requesterEmail")} disabled={isSubmitting} />
            {errors.requesterEmail && <p className="text-xs text-red-500 mt-1">{errors.requesterEmail.message}</p>}
          </div>
          
          {/* Nome do Técnico */}
          <div className="space-y-1">
            <Label htmlFor="technicianName">Nome do Técnico (Opcional)</Label>
            <Input id="technicianName" {...register("technicianName")} disabled={isSubmitting} />
            {errors.technicianName && <p className="text-xs text-red-500 mt-1">{errors.technicianName.message}</p>}
          </div>
          
          {/* Data da Solicitação */}
          <div className="space-y-1">
            <Label htmlFor="requestDate">Data da Solicitação</Label>
            <Input id="requestDate" type="datetime-local" {...register("requestDate")} disabled={isSubmitting} />
            {errors.requestDate && <p className="text-xs text-red-500 mt-1">{errors.requestDate.message}</p>}
          </div>

          {/* Problema Relatado */}
          <div className="space-y-1">
            <Label htmlFor="reportedProblem">Problema Relatado</Label>
            <Textarea id="reportedProblem" {...register("reportedProblem")} disabled={isSubmitting} rows={3}/>
            {errors.reportedProblem && <p className="text-xs text-red-500 mt-1">{errors.reportedProblem.message}</p>}
          </div>
          <div className="space-y-1">
            <Label htmlFor="category">Categoria</Label>
            <Input id="category" {...register("category")} disabled={isSubmitting} />
            {errors.category && <p className="text-xs text-red-500 mt-1">{errors.category.message}</p>}
        </div>

          {/* Tempos de Resposta e Atendimento */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label htmlFor="firstResponseTime">Tempo 1ª Resposta (HH:MM:SS)</Label>
              <Input id="firstResponseTime" placeholder="00:00:00" {...register("firstResponseTime")} disabled={isSubmitting} />
              {errors.firstResponseTime && <p className="text-xs text-red-500 mt-1">{errors.firstResponseTime.message}</p>}
            </div>
            <div className="space-y-1">
              <Label htmlFor="averageHandlingTime">Tempo Médio Atend. (HH:MM:SS)</Label>
              <Input id="averageHandlingTime" placeholder="00:00:00" {...register("averageHandlingTime")} disabled={isSubmitting} />
              {errors.averageHandlingTime && <p className="text-xs text-red-500 mt-1">{errors.averageHandlingTime.message}</p>}
            </div>
          </div>
        
          <DialogFooter className="pt-5">
            <DialogClose asChild>
              <Button type="button" variant="outline" onClick={resetFormAndClose} disabled={isSubmitting}>Cancelar</Button>
            </DialogClose>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? (mode === 'create' ? "Criando..." : "Salvando...") : saveButtonText}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};