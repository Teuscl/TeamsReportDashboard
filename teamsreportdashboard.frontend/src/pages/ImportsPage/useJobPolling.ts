// Caminho: src/hooks/useJobPolling.ts

import { useEffect, useMemo, useRef, useCallback } from 'react';
import { getJobStatus } from '@/services/analysisService';
import { AnalysisJob } from '@/types/AnalysisJob';

export const useJobPolling = (
  jobs: AnalysisJob[],
  setJobs: React.Dispatch<React.SetStateAction<AnalysisJob[]>>
) => {
  // CORRIGIDO: Monitora tanto 'Pending' quanto 'Processing'
  const activeJobIds = useMemo(() => 
    jobs.filter(j => j.status === 'Pending' || j.status === 'Processing').map(j => j.id),
    [jobs]
  );

  const activeJobIdsString = useMemo(() => 
    JSON.stringify([...activeJobIds].sort()),
    [activeJobIds]
  );

  const intervalRef = useRef<NodeJS.Timeout | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  const checkStatus = useCallback(async () => {
    if (activeJobIds.length === 0) return;

    if (abortControllerRef.current) abortControllerRef.current.abort();
    abortControllerRef.current = new AbortController();
    const { signal } = abortControllerRef.current;

    const promises = activeJobIds.map(id => getJobStatus(id).catch(error => ({ error, id })));
    const results = await Promise.allSettled(promises);

    if (signal.aborted) return;

    setJobs(currentJobs => {
      let hasChanges = false;
      const updatedJobsMap = new Map(currentJobs.map(j => [j.id, j]));

      results.forEach(result => {
        if (result.status === 'fulfilled') {
          const updatedJob = result.value as AnalysisJob;
          const existingJob = updatedJobsMap.get(updatedJob.id);

          if (existingJob && existingJob.status !== updatedJob.status) {
            updatedJobsMap.set(updatedJob.id, updatedJob);
            hasChanges = true;
          }
        }
      });

      if (hasChanges) {
        return Array.from(updatedJobsMap.values()).sort((a, b) => 
          new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
        );
      }
      return currentJobs;
    });
  }, [activeJobIdsString, setJobs]);

  useEffect(() => {
    if (intervalRef.current) clearInterval(intervalRef.current);
    if (abortControllerRef.current) abortControllerRef.current.abort();

    if (activeJobIds.length > 0) {
      checkStatus(); // Verificação imediata
      intervalRef.current = setInterval(checkStatus, 15000); // Polling a cada 15s
    }

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
      if (abortControllerRef.current) abortControllerRef.current.abort();
    };
  }, [activeJobIdsString, checkStatus]);
};