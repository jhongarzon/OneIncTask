import { useQuery } from '@tanstack/react-query';
import { getJobHistory } from '@/lib/api-client';

export function useJobHistory() {
  return useQuery({
    queryKey: ['jobHistory'],
    queryFn: getJobHistory,
    refetchInterval: 10000,
  });
}
