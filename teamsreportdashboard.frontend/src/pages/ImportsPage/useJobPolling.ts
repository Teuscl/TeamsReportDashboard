import { useEffect, useMemo, useRef, useCallback } from 'react';
import { getJobStatus } from '@/services/analysisService';
import { AnalysisJob } from '@/types/AnalysisJob';

// Sistema de logging condicional (apenas em desenvolvimento)
const isDevelopment = process.env.NODE_ENV === 'development';

const logger = {
  debug: (...args: any[]) => {
    if (isDevelopment) {
      console.log('[POLLING DEBUG]', ...args);
    }
  },
  error: (...args: any[]) => {
    // Erros sempre são logados, mas com prefixo identificador
    console.error('[POLLING ERROR]', ...args);
  },
  warn: (...args: any[]) => {
    if (isDevelopment) {
      console.warn('[POLLING WARN]', ...args);
    }
  }
};

export const useJobPolling = (
  jobs: AnalysisJob[],
  setJobs: React.Dispatch<React.SetStateAction<AnalysisJob[]>>
) => {
  // ✅ Memoização correta dos IDs pendentes
  const pendingJobIds = useMemo(() => 
    jobs.filter(j => j.status === 'Pending').map(j => j.id),
    [jobs]
  );

  // ✅ Dependência estável baseada em string ordenada
  const pendingJobIdsString = useMemo(() => 
    JSON.stringify([...pendingJobIds].sort()),
    [pendingJobIds]
  );

  const intervalRef = useRef<NodeJS.Timeout | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  // ✅ Função memoizada para verificar status com melhor tratamento de erros
  const checkStatus = useCallback(async () => {
    if (pendingJobIds.length === 0) {
      logger.debug('Nenhum job pendente para verificar');
      return;
    }

    logger.debug(`Verificando status de ${pendingJobIds.length} jobs pendentes`);

    // Cancelar requisição anterior se ainda estiver em andamento
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    // Criar novo AbortController para esta verificação
    abortControllerRef.current = new AbortController();
    const { signal } = abortControllerRef.current;

    // Timeout de 30 segundos para evitar requests infinitos
    const timeoutId = setTimeout(() => {
      abortControllerRef.current?.abort();
    }, 30000);

    try {
      const promises = pendingJobIds.map(id => 
        getJobStatus(id).catch(error => ({ error, id })) // Capturar erro com ID
      );
      
      const results = await Promise.allSettled(promises);
      clearTimeout(timeoutId);

      // Verificar se a operação foi cancelada
      if (signal.aborted) {
        logger.debug('Verificação de status cancelada');
        return;
      }

      setJobs(currentJobs => {
        let hasChanges = false;
        const updatedJobs = [...currentJobs];
        
        results.forEach((result, index) => {
          const jobId = pendingJobIds[index];
          
          if (result.status === 'fulfilled') {
            const response = result.value;
            
            // Verificar se é um erro capturado
            if ('error' in response) {
              logger.error(`Falha ao buscar status do job ${jobId}:`, response.error);
              return;
            }
            
            const updatedJob = response as AnalysisJob;
            const jobIndex = updatedJobs.findIndex(j => j.id === updatedJob.id);
            
            if (jobIndex !== -1) {
              const existingJob = updatedJobs[jobIndex];
              
              // ✅ Verificar se realmente houve mudança antes de atualizar
              const hasStatusChange = existingJob.status !== updatedJob.status;
              const hasErrorChange = existingJob.errorMessage !== updatedJob.errorMessage;
              const hasCompletionChange = existingJob.completedAt !== updatedJob.completedAt;
              
              if (hasStatusChange || hasErrorChange || hasCompletionChange) {
                logger.debug(`Job ${updatedJob.id} mudou de status: ${existingJob.status} -> ${updatedJob.status}`);
                updatedJobs[jobIndex] = updatedJob;
                hasChanges = true;
              }
            }
          } else {
            // Erro não capturado no Promise.allSettled
            logger.error(`Falha crítica ao buscar status do job ${jobId}:`, result.reason);
          }
        });
        
        // ✅ Retornar array reordenado apenas se houve mudanças
        if (hasChanges) {
          logger.debug(`${updatedJobs.filter(j => hasChanges).length} jobs foram atualizados`);
          return updatedJobs.sort((a, b) => 
            new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
          );
        }
        
        // Retornar o mesmo array se nada mudou (evita re-renders desnecessários)
        return currentJobs;
      });
      
    } catch (error) {
      clearTimeout(timeoutId);
      
      if (error instanceof DOMException && error.name === 'AbortError') {
        logger.debug('Requisições de polling canceladas (timeout ou abort)');
      } else {
        logger.error('Erro crítico no polling:', error);
      }
    }
  }, [pendingJobIds, setJobs]);

  // ✅ Effect melhorado com cleanup robusto
  useEffect(() => {
    // Limpar interval anterior
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }

    // Cancelar requisições em andamento
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }

    if (pendingJobIds.length > 0) {
      logger.debug(`Iniciando polling para ${pendingJobIds.length} jobs pendentes`);
      
      // Primeira verificação imediata (opcional, mas útil para responsividade)
      checkStatus();
      
      // Depois polling regular a cada 15 segundos
      intervalRef.current = setInterval(() => {
        checkStatus();
      }, 15000);
    } else {
      logger.debug('Nenhum job pendente - polling parado');
    }

    // ✅ Cleanup robusto
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
        logger.debug('Polling interval limpo');
      }
      
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
        abortControllerRef.current = null;
        logger.debug('Requisições de polling canceladas');
      }
    };
  }, [pendingJobIdsString, checkStatus]);

  // ✅ Cleanup adicional quando o componente é desmontado
  useEffect(() => {
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, []);

  // ✅ Retornar informações úteis para debugging (opcional)
  return useMemo(() => ({
    pendingJobsCount: pendingJobIds.length,
    isPolling: intervalRef.current !== null
  }), [pendingJobIds.length]);
};