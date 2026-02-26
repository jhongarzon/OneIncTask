import { useState, useEffect, useCallback, useRef } from 'react';
import { getConnection, stopConnection } from '@/lib/signalr-client';
import { getJobStatus } from '@/lib/api-client';
import type { HubConnection } from '@microsoft/signalr';
import { HubConnectionState } from '@microsoft/signalr';

interface JobProgressState {
  jobId: string | null;
  result: string;
  processedCharacters: number;
  totalCharacters: number;
  isProcessing: boolean;
  isCompleted: boolean;
  isCancelled: boolean;
  error: string | null;
}

const initialState: JobProgressState = {
  jobId: null,
  result: '',
  processedCharacters: 0,
  totalCharacters: 0,
  isProcessing: false,
  isCompleted: false,
  isCancelled: false,
  error: null,
};

export function useJobProgress(token: string | null) {
  const [state, setState] = useState<JobProgressState>(initialState);
  const connectionRef = useRef<HubConnection | null>(null);

  const resetState = useCallback(() => {
    setState(initialState);
  }, []);

  const setJobId = useCallback((jobId: string) => {
    setState(prev => ({ ...prev, jobId, isProcessing: true, result: '', processedCharacters: 0, isCompleted: false, isCancelled: false, error: null }));
  }, []);

  useEffect(() => {
    if (!token) return;

    const conn = getConnection(token);
    connectionRef.current = conn;

    conn.on('JobStarted', (jobId: string, totalCharacters: number) => {
      setState(prev => prev.jobId === jobId ? { ...prev, totalCharacters, isProcessing: true } : prev);
    });

    conn.on('ReceiveCharacter', (jobId: string, character: string, currentIndex: number, totalCount: number) => {
      setState(prev => {
        if (prev.jobId !== jobId) return prev;
        return {
          ...prev,
          result: prev.result + character,
          processedCharacters: currentIndex + 1,
          totalCharacters: totalCount,
        };
      });
    });

    conn.on('JobCompleted', (jobId: string, fullResult: string) => {
      setState(prev => prev.jobId === jobId ? {
        ...prev,
        result: fullResult,
        isProcessing: false,
        isCompleted: true,
        processedCharacters: fullResult.length,
        totalCharacters: fullResult.length,
      } : prev);
    });

    conn.on('JobCancelled', (jobId: string) => {
      setState(prev => prev.jobId === jobId ? { ...prev, isProcessing: false, isCancelled: true } : prev);
    });

    conn.on('JobFailed', (jobId: string, error: string) => {
      setState(prev => prev.jobId === jobId ? { ...prev, isProcessing: false, error } : prev);
    });

    conn.onreconnected(async () => {
      const currentState = state;
      if (currentState.jobId && currentState.isProcessing) {
        try {
          const status = await getJobStatus(currentState.jobId);
          setState(prev => ({
            ...prev,
            result: status.currentResult,
            processedCharacters: status.processedCharacters,
            totalCharacters: status.totalCharacters,
            isProcessing: status.status === 'Running' || status.status === 'Pending',
            isCompleted: status.status === 'Completed',
            isCancelled: status.status === 'Cancelled',
            error: status.errorMessage,
          }));
        } catch {
          // Recovery failed, state may be stale
        }
      }
    });

    if (conn.state === HubConnectionState.Disconnected) {
      conn.start().catch(console.error);
    }

    return () => {
      conn.off('JobStarted');
      conn.off('ReceiveCharacter');
      conn.off('JobCompleted');
      conn.off('JobCancelled');
      conn.off('JobFailed');
      stopConnection();
    };
  }, [token]);

  return { ...state, setJobId, resetState };
}
