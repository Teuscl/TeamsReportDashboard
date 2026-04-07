// src/components/EditJobModal.tsx (ou onde preferir)
// import React, { useState, useEffect } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Loader2 } from "lucide-react";
import { toast } from "sonner";
import { AnalysisJob } from "@/types/AnalysisJob";
import { updateAnalysisJob } from "@/services/analysisService";
import { useEffect, useState } from "react";

interface EditJobModalProps {
  job: AnalysisJob | null; // O job a ser editado
  isOpen: boolean;
  onClose: () => void;
  onSaveSuccess: () => void; // Para a página pai atualizar a lista de jobs
}

export const EditJobModal: React.FC<EditJobModalProps> = ({
  job,
  isOpen,
  onClose,
  onSaveSuccess,
}) => {
  const [jobName, setJobName] = useState("");
  const [isSaving, setIsSaving] = useState(false);

  // Efeito para popular o input quando o modal abrir com um job válido
  useEffect(() => {
    if (job) {
      setJobName(job.name);
    } else {
      // Limpa o nome se não houver job (boa prática)
      setJobName("");
    }
  }, [job, isOpen]); // Roda sempre que o job ou o estado de abertura mudar

  const handleSave = async () => {
    if (!job || !jobName.trim()) return;

    setIsSaving(true);
    try {
      await updateAnalysisJob(job.id, { name: jobName.trim() });
      toast.success("Nome do job atualizado com sucesso!");
      onSaveSuccess(); // Avisa o pai para recarregar os dados
      onClose();       // Avisa o pai para fechar o modal
    } catch (error: any) {
      const message =
        error?.response?.data?.errors?.[0] ??
        error?.response?.data?.detail ??
        error?.response?.data?.message ??
        "Erro desconhecido ao atualizar job.";
      toast.error(`Falha na atualização: ${message}`);
    } finally {
      setIsSaving(false);
    }
  };

  // Previne fechar o modal se estiver salvando
  const handleOpenChange = (open: boolean) => {
    if (!open && !isSaving) {
      onClose();
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Editar Nome do Job</DialogTitle>
          <DialogDescription>
            Altere o nome do job{" "}
            <span className="font-semibold">{job?.name}</span>
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <Input
            value={jobName}
            onChange={(e) => setJobName(e.target.value)}
            placeholder="Nome do job"
            disabled={isSaving}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && !isSaving && jobName.trim()) {
                handleSave();
              }
            }}
            autoFocus
          />
        </div>
        <DialogFooter>
          <Button 
            variant="outline" 
            onClick={onClose}
            disabled={isSaving}
          >
            Cancelar
          </Button>
          <Button 
            onClick={handleSave} 
            disabled={!jobName.trim() || isSaving}
          >
            {isSaving ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Salvando...
              </>
            ) : (
              'Salvar'
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};