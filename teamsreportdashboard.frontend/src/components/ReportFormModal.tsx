// src/components/ReportFormModal.tsx

import React, { useEffect, useState } from 'react';
import { useForm, SubmitHandler, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { toast } from "sonner";
import { cn } from "@/lib/utils";

import { Report, CreateReportPayload, UpdateReportPayload } from '@/types/Report';
import { RequesterDto } from '@/types/Requester'; // Assuming you have this type defined
import { getRequesters } from '@/services/requesterService';
import { createReport, updateReport } from '@/services/reportService';

import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogClose } from "@/components/ui/dialog";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { CaretSortIcon, CheckIcon } from "@radix-ui/react-icons";

// Form data interface
interface ReportFormData {
  requesterId: string;
  technicianName?: string;
  requestDate: string;
  reportedProblem: string;
  category: string;
  firstResponseTime: string;
  averageHandlingTime: string;
}

// Zod validation schema em português
const reportFormSchema = z.object({
  requesterId: z.string().min(1, "O solicitante é obrigatório."),
  technicianName: z.string().max(50).optional(),
  requestDate: z.string().min(1, "A data é obrigatória."),
  reportedProblem: z.string().min(1, "A descrição do problema é obrigatória.").max(255),
  category: z.string().min(1, "A categoria é obrigatória.").max(50),
  firstResponseTime: z.string().regex(/^([01]\d|2[0-3]):([0-5]\d):([0-5]\d)$/, "Formato de hora inválido (HH:MM:SS)."),
  averageHandlingTime: z.string().regex(/^([01]\d|2[0-3]):([0-5]\d):([0-5]\d)$/, "Formato de hora inválido (HH:MM:SS)."),
});

// Date utility functions
const formatDateTimeForInput = (isoDateString?: string): string => {
  if (!isoDateString) return '';
  const date = new Date(isoDateString);
  // Adjust for timezone offset to display local time correctly in the input
  return new Date(date.getTime() - (date.getTimezoneOffset() * 60000)).toISOString().slice(0, 16);
};
const formatInputDateTimeToISOString = (inputDateTime: string): string => new Date(inputDateTime).toISOString();

interface ReportFormModalProps {
  mode: 'create' | 'edit';
  reportToEdit?: Report | null;
  isOpen: boolean;
  onClose: () => void;
  onSaveSuccess: () => void;
  uniqueCategories: string[];
}

export const ReportFormModal: React.FC<ReportFormModalProps> = ({ mode, reportToEdit, isOpen, onClose, onSaveSuccess, uniqueCategories }) => {
  const [requesters, setRequesters] = useState<RequesterDto[]>([]);
  const [dataLoading, setDataLoading] = useState(false);

  const { control, register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<ReportFormData>({
    resolver: zodResolver(reportFormSchema),
  });

  // Fetches requesters when the modal opens
  useEffect(() => {
    if (isOpen) {
      const fetchDropdownData = async () => {
        setDataLoading(true);
        try {
          setRequesters(await getRequesters());
        } catch (error) {
          toast.error("Falha ao carregar a lista de solicitantes.");
        } finally {
          setDataLoading(false);
        }
      };
      fetchDropdownData();
    }
  }, [isOpen]);

  // Populates the form for creation or editing
  useEffect(() => {
    if (isOpen && !dataLoading) {
      if (mode === 'edit' && reportToEdit) {
        reset({
          requesterId: String(reportToEdit.requesterId),
          technicianName: reportToEdit.technicianName || '',
          requestDate: formatDateTimeForInput(reportToEdit.requestDate),
          reportedProblem: reportToEdit.reportedProblem,
          category: reportToEdit.category,
          firstResponseTime: reportToEdit.firstResponseTime || '00:00:00',
          averageHandlingTime: reportToEdit.averageHandlingTime || '00:00:00',
        });
      } else {
        reset({
          requesterId: '',
          technicianName: '',
          requestDate: formatDateTimeForInput(new Date().toISOString()),
          reportedProblem: '',
          category: '',
          firstResponseTime: '00:00:00',
          averageHandlingTime: '00:00:00',
        });
      }
    }
  }, [isOpen, dataLoading, mode, reportToEdit, reset]);

  const onSubmit: SubmitHandler<ReportFormData> = async (data) => {
  try {
    if (mode === 'edit' && reportToEdit) {
      // **PAYLOAD PARA EDIÇÃO**
      // Envie o requesterId, e não o nome e o e-mail.
      const updatePayload: Partial<UpdateReportPayload> = {
        requesterId: parseInt(data.requesterId, 10), // A correção principal está aqui!
        technicianName: data.technicianName?.trim() === '' ? null : data.technicianName,
        requestDate: formatInputDateTimeToISOString(data.requestDate),
        reportedProblem: data.reportedProblem,
        category: data.category,
        firstResponseTime: data.firstResponseTime,
        averageHandlingTime: data.averageHandlingTime,
      };
      await updateReport(reportToEdit.id, updatePayload as UpdateReportPayload);
      toast.success("Relatório atualizado com sucesso!");

    } else {
      // **PAYLOAD PARA CRIAÇÃO**
      // Esta parte provavelmente já estava correta. Encontra o solicitante para obter nome/e-mail.
      const selectedRequester = requesters.find(r => String(r.id) === data.requesterId);
      if (!selectedRequester) {
        toast.error("O solicitante selecionado é inválido.");
        return;
      }
      const createPayload: CreateReportPayload = {
        requesterName: selectedRequester.name,
        requesterEmail: selectedRequester.email,
        technicianName: data.technicianName?.trim() === '' ? null : data.technicianName,
        requestDate: formatInputDateTimeToISOString(data.requestDate),
        reportedProblem: data.reportedProblem,
        category: data.category,
        firstResponseTime: data.firstResponseTime,
        averageHandlingTime: data.averageHandlingTime,
      };
      await createReport(createPayload);
      toast.success("Relatório criado com sucesso!");
    }
    onSaveSuccess();
  } catch (error: any) {
    const actionVerb = mode === 'edit' ? 'editar' : 'criar';
    const message = error?.response?.data?.message || `Falha ao ${actionVerb} o relatório.`;
    toast.error(message);
  }
};

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{mode === 'create' ? 'Criar Novo Relatório' : 'Editar Relatório'}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-2 max-h-[70vh] overflow-y-auto pr-2">

          <div className="space-y-1">
            <Label>Solicitante</Label>
            <Controller name="requesterId" control={control} render={({ field }) => (
              <Popover><PopoverTrigger asChild>
                <Button variant="outline" role="combobox" className={cn("w-full justify-between", !field.value && "text-muted-foreground")} disabled={dataLoading || isSubmitting}>
                  {field.value ? requesters.find((r) => String(r.id) === field.value)?.name : "Selecione um solicitante..."}
                  <CaretSortIcon className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                </Button>
              </PopoverTrigger><PopoverContent className="w-[--radix-popover-trigger-width] p-0">
                <Command><CommandInput placeholder="Buscar solicitante..." /><CommandList><CommandEmpty>Nenhum solicitante encontrado.</CommandEmpty><CommandGroup>
                  {requesters.map((req) => (
                    <CommandItem value={req.name} key={req.id} onSelect={() => field.onChange(String(req.id))}>
                      <CheckIcon className={cn("mr-2 h-4 w-4", String(req.id) === field.value ? "opacity-100" : "opacity-0")} />
                      <div><p>{req.name}</p><p className="text-xs text-muted-foreground">{req.email}</p></div>
                    </CommandItem>
                  ))}
                </CommandGroup></CommandList></Command>
              </PopoverContent></Popover>
            )} />
            {errors.requesterId && <p className="text-xs text-red-500 mt-1">{errors.requesterId.message}</p>}
          </div>

          <div className="space-y-1">
            <Label>Categoria</Label>
            <Controller name="category" control={control} render={({ field }) => (
              <Popover><PopoverTrigger asChild>
                <Button variant="outline" role="combobox" className={cn("w-full justify-between", !field.value && "text-muted-foreground")} disabled={isSubmitting}>
                  {field.value || "Selecione ou digite uma categoria..."}
                  <CaretSortIcon className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                </Button>
              </PopoverTrigger><PopoverContent className="w-[--radix-popover-trigger-width] p-0">
                <Command>
                  <CommandInput placeholder="Buscar ou criar categoria..." value={field.value || ''} onValueChange={field.onChange} />
                  <CommandList><CommandEmpty>Nenhuma categoria encontrada.</CommandEmpty><CommandGroup>
                    {uniqueCategories.map((catName) => (
                      <CommandItem value={catName} key={catName} onSelect={() => field.onChange(catName)}>
                        <CheckIcon className={cn("mr-2 h-4 w-4", catName === field.value ? "opacity-100" : "opacity-0")} />
                        {catName}
                      </CommandItem>
                    ))}
                  </CommandGroup></CommandList></Command>
                </PopoverContent></Popover>
            )} />
            {errors.category && <p className="text-xs text-red-500 mt-1">{errors.category.message}</p>}
          </div>

          <div className="space-y-1">
            <Label htmlFor="technicianName">Nome do Técnico (Opcional)</Label>
            <Input id="technicianName" {...register("technicianName")} disabled={isSubmitting} />
          </div>

          <div className="space-y-1">
            <Label htmlFor="requestDate">Data da Solicitação</Label>
            <Input id="requestDate" type="datetime-local" {...register("requestDate")} disabled={isSubmitting} />
          </div>

          <div className="space-y-1">
            <Label htmlFor="reportedProblem">Descrição do Problema</Label>
            <Textarea id="reportedProblem" {...register("reportedProblem")} disabled={isSubmitting} rows={3} />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-1">
              <Label htmlFor="firstResponseTime">Tempo de 1ª Resposta (HH:MM:SS)</Label>
              <Input id="firstResponseTime" {...register("firstResponseTime")} disabled={isSubmitting} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="averageHandlingTime">Tempo Médio de Atendimento (HH:MM:SS)</Label>
              <Input id="averageHandlingTime" {...register("averageHandlingTime")} disabled={isSubmitting} />
            </div>
          </div>

          <DialogFooter className="pt-5">
            <DialogClose asChild><Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>Cancelar</Button></DialogClose>
            <Button type="submit" disabled={isSubmitting || dataLoading}>
              {isSubmitting ? "Salvando..." : (mode === 'create' ? "Criar Relatório" : "Salvar Alterações")}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};