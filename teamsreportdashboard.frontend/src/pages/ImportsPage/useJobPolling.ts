import { useEffect, useRef } from 'react';
import { getJobStatus } from '@/services/analysisService';
import { AnalysisJob } from '@/types/AnalysisJob';

export const useJobPolling = (
  jobs: AnalysisJob[],
  setJobs: React.Dispatch<React.SetStateAction<AnalysisJob[]>>
) => {
  const pendingJobIds = jobs.filter(j => j.status === 'Pending').map(j => j.id);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    const checkStatus = async () => {
      if (pendingJobIds.length === 0) return;

      const promises = pendingJobIds.map(id => getJobStatus(id));
      const results = await Promise.allSettled(promises);

      setJobs(currentJobs => {
        const updatedJobs = [...currentJobs];
        results.forEach((result, index) => {
          if (result.status === 'fulfilled') {
            const updatedJob = result.value;
            const jobIndex = updatedJobs.findIndex(j => j.id === updatedJob.id);
            if (jobIndex !== -1) {
              updatedJobs[jobIndex] = updatedJob;
            }
          } else {
            // Log do erro, mas não para o polling
            console.error(`Falha ao buscar status do job ${pendingJobIds[index]}:`, result.reason);
          }
        });
        return updatedJobs.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
      });
    };

    if (pendingJobIds.length > 0) {
      // Inicia o polling
      intervalRef.current = setInterval(checkStatus, 15000); // Verifica a cada 15 segundos
    }

    // Limpeza: para o polling se o componente for desmontado ou se não houver mais jobs pendentes
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, [pendingJobIds.length, setJobs]); // Dependência principal é a quantidade de jobs pendentes
};