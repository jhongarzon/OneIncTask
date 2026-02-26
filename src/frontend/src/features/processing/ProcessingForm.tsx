import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';

interface ProcessingFormProps {
  onSubmit: (inputText: string) => void;
  onCancel: () => void;
  isProcessing: boolean;
  isStarting: boolean;
}

export function ProcessingForm({ onSubmit, onCancel, isProcessing, isStarting }: ProcessingFormProps) {
  const [inputText, setInputText] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (inputText.trim()) {
      onSubmit(inputText);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Process Text</CardTitle>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <textarea
            value={inputText}
            onChange={(e) => setInputText(e.target.value)}
            placeholder="Enter text to process..."
            className="w-full min-h-[120px] p-3 border rounded-md resize-y focus:outline-none focus:ring-2 focus:ring-blue-500"
            disabled={isProcessing}
            maxLength={10000}
            data-testid="input-textarea"
          />
          <div className="flex gap-3">
            <Button
              type="submit"
              disabled={isProcessing || isStarting || !inputText.trim()}
              data-testid="process-button"
            >
              {isStarting ? 'Starting...' : 'Process'}
            </Button>
            {isProcessing && (
              <Button
                type="button"
                variant="destructive"
                onClick={onCancel}
                data-testid="cancel-button"
              >
                Cancel
              </Button>
            )}
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
