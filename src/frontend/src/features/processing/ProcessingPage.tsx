import { useAuth } from '@/providers/AuthProvider';
import { useJobProgress } from './useJobProgress';
import { useStartJob, useCancelJob } from './useProcessJob';
import { ProcessingForm } from './ProcessingForm';
import { ResultDisplay } from './ResultDisplay';
import { ProgressBar } from './ProgressBar';

export function ProcessingPage() {
  const { token } = useAuth();
  const progress = useJobProgress(token);

  const startJobMutation = useStartJob((jobId) => {
    progress.setJobId(jobId);
  });

  const cancelJobMutation = useCancelJob();

  const handleSubmit = (inputText: string) => {
    progress.resetState();
    startJobMutation.mutate(inputText);
  };

  const handleCancel = () => {
    if (progress.jobId) {
      cancelJobMutation.mutate(progress.jobId);
    }
  };

  return (
    <div className="space-y-6">
      <ProcessingForm
        onSubmit={handleSubmit}
        onCancel={handleCancel}
        isProcessing={progress.isProcessing}
        isStarting={startJobMutation.isPending}
      />

      <ProgressBar
        processedCharacters={progress.processedCharacters}
        totalCharacters={progress.totalCharacters}
        isProcessing={progress.isProcessing}
        isCompleted={progress.isCompleted}
        isCancelled={progress.isCancelled}
        error={progress.error}
      />

      <ResultDisplay
        result={progress.result}
        isProcessing={progress.isProcessing}
        isCompleted={progress.isCompleted}
      />
    </div>
  );
}
