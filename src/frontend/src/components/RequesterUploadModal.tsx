import React, { useState, useCallback } from 'react';
import { Button } from "@/components/ui/button";
import { 
  Dialog, 
  DialogContent, 
  DialogHeader, 
  DialogTitle, 
  DialogDescription, 
  DialogFooter 
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { toast } from 'sonner';
import { Upload, FileText, X } from 'lucide-react';
import { bulkInsertRequesters } from '@/services/requesterService';

interface RequesterUploadModalProps {
  isOpen: boolean;
  onClose: () => void;
  onUploadSuccess: () => void;
}

export const RequesterUploadModal: React.FC<RequesterUploadModalProps> = ({ 
  isOpen, 
  onClose, 
  onUploadSuccess 
}) => {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isUploading, setIsUploading] = useState(false);

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    if (event.target.files && event.target.files.length > 0) {
      const file = event.target.files[0];
      if (file.type !== "text/csv") {
        toast.error("Formato inválido.", { description: "Por favor, selecione um arquivo .csv" });
        return;
      }
      setSelectedFile(file);
    }
  };

  const handleUpload = useCallback(async () => {
    if (!selectedFile) {
      toast.error("Nenhum arquivo selecionado.");
      return;
    }

    setIsUploading(true);
    const toastId = toast.loading("Enviando arquivo...", { description: "Aguarde enquanto processamos os dados." });

    try {
      const result = await bulkInsertRequesters(selectedFile);
      
      toast.dismiss(toastId);

      if (!result.hasErrors) {
        toast.success("Importação concluída com sucesso!", {
          description: `${result.successfulInserts} solicitantes foram adicionados.`,
        });
      } else {
        // Exibe um resumo com sucessos e falhas
        toast.warning("Importação concluída com avisos.", {
            description: (
              <div>
                <p>{result.successfulInserts} solicitantes importados.</p>
                <p>{result.failures.length} linhas falharam.</p>
                <p className="mt-2 text-xs">Verifique o console para mais detalhes.</p>
              </div>
            ),
            duration: 8000
        });
        
        // Log detalhado no console para o desenvolvedor ou usuário avançado
        console.error("Falhas na importação:", result.failures);
      }
      
      onUploadSuccess();

    } catch (error: any) {
      toast.dismiss(toastId);
      const errorMessage = error?.response?.data?.errors?.[0] || 'Ocorreu uma falha no servidor.';
      toast.error("Erro na importação", { description: errorMessage });
    } finally {
      setIsUploading(false);
    }
  }, [selectedFile, onUploadSuccess]);

  const clearFile = () => setSelectedFile(null);

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Importar Solicitantes via CSV</DialogTitle>
          <DialogDescription>
            O arquivo deve conter as colunas: <strong>Nome do colaborador; Departamento; E-mail profissional</strong>.
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 py-4">
          {!selectedFile ? (
            <div className="flex flex-col items-center justify-center p-6 border-2 border-dashed rounded-md">
              <Upload className="w-10 h-10 text-muted-foreground mb-2" />
              <label htmlFor="file-upload" className="relative cursor-pointer bg-primary text-primary-foreground hover:bg-primary/90 px-4 py-2 rounded-md font-semibold">
                <span>Selecionar Arquivo</span>
                <Input id="file-upload" type="file" className="sr-only" accept=".csv" onChange={handleFileChange} />
              </label>
              <p className="text-xs text-muted-foreground mt-2">Somente arquivos .csv</p>
            </div>
          ) : (
            <div className="flex items-center justify-between p-3 border rounded-md bg-muted">
                <div className="flex items-center gap-3">
                    <FileText className="h-6 w-6 text-primary" />
                    <span className="text-sm font-medium truncate">{selectedFile.name}</span>
                </div>
                <Button variant="ghost" size="icon" onClick={clearFile} className="h-6 w-6">
                    <X className="h-4 w-4" />
                </Button>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={isUploading}>Cancelar</Button>
          <Button onClick={handleUpload} disabled={!selectedFile || isUploading}>
            {isUploading ? 'Enviando...' : 'Enviar Arquivo'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};