import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';

interface ResultDisplayProps {
  result: string;
  isProcessing: boolean;
  isCompleted: boolean;
}

export function ResultDisplay({ result, isProcessing, isCompleted }: ResultDisplayProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>
          Result
          {isProcessing && <span className="ml-2 text-sm text-blue-600 animate-pulse">Processing...</span>}
          {isCompleted && <span className="ml-2 text-sm text-green-600">Complete</span>}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <textarea
          readOnly
          value={result}
          placeholder="Result will appear here..."
          className="w-full min-h-[120px] p-3 border rounded-md font-mono text-sm bg-gray-50 resize-y"
          data-testid="result-textarea"
        />
      </CardContent>
    </Card>
  );
}
