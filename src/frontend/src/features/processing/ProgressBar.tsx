import { cn } from '@/lib/utils';

interface ProgressBarProps {
  processedCharacters: number;
  totalCharacters: number;
  isProcessing: boolean;
  isCompleted: boolean;
  isCancelled: boolean;
  error: string | null;
}

export function ProgressBar({
  processedCharacters,
  totalCharacters,
  isProcessing,
  isCompleted,
  isCancelled,
  error,
}: ProgressBarProps) {
  const percentage = totalCharacters > 0 ? Math.round((processedCharacters / totalCharacters) * 100) : 0;

  const barColor = error
    ? 'bg-red-500'
    : isCancelled
    ? 'bg-red-400'
    : isCompleted
    ? 'bg-green-500'
    : 'bg-blue-500';

  if (totalCharacters === 0 && !isProcessing) return null;

  return (
    <div className="space-y-2" data-testid="progress-bar-container">
      <div className="flex justify-between text-sm text-gray-600">
        <span>
          {processedCharacters} / {totalCharacters} characters
        </span>
        <span data-testid="progress-percentage">{percentage}%</span>
      </div>
      <div className="w-full bg-gray-200 rounded-full h-3 overflow-hidden">
        <div
          className={cn(
            'h-full rounded-full transition-all duration-300',
            barColor,
            isProcessing && 'bg-gradient-to-r from-blue-500 via-blue-400 to-blue-500 bg-[length:200%_100%] animate-[shimmer_2s_infinite]'
          )}
          style={{ width: `${percentage}%` }}
          data-testid="progress-fill"
        />
      </div>
      {error && <p className="text-sm text-red-600">{error}</p>}
      {isCancelled && <p className="text-sm text-red-500">Job was cancelled.</p>}
    </div>
  );
}
