import { useMutation } from '@tanstack/react-query';
import { startJob, cancelJob } from '@/lib/api-client';

export function useStartJob(onSuccess: (jobId: string) => void) {
  return useMutation({
    mutationFn: (inputText: string) => startJob(inputText),
    onSuccess: (data) => {
      onSuccess(data.jobId);
    },
  });
}

export function useCancelJob(onSuccess?: () => void) {
  return useMutation({
    mutationFn: (jobId: string) => cancelJob(jobId),
    onSuccess: () => {
      onSuccess?.();
    },
  });
}
