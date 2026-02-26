import { useJobHistory } from './useJobHistory';
import { Badge } from '@/components/ui/badge';
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card';
import type { BadgeProps } from '@/components/ui/badge';

const STATUS_LABELS: Record<number, string> = {
  0: 'Pending',
  1: 'Running',
  2: 'Completed',
  3: 'Cancelled',
  4: 'Failed',
};

function getStatusLabel(status: number | string): string {
  if (typeof status === 'number') return STATUS_LABELS[status] ?? `Unknown (${status})`;
  return status;
}

function getStatusBadgeVariant(status: number | string): BadgeProps['variant'] {
  const label = getStatusLabel(status);
  switch (label) {
    case 'Completed': return 'success';
    case 'Running':
    case 'Pending': return 'default';
    case 'Cancelled': return 'warning';
    case 'Failed': return 'destructive';
    default: return 'secondary';
  }
}

export function HistoryPage() {
  const { data: jobs, isLoading, error } = useJobHistory();

  return (
    <Card>
      <CardHeader>
        <CardTitle>Job History</CardTitle>
      </CardHeader>
      <CardContent>
        {isLoading && <p className="text-gray-500">Loading...</p>}
        {error && <p className="text-red-500">Error loading history.</p>}
        {jobs && jobs.length === 0 && <p className="text-gray-500">No jobs yet.</p>}
        {jobs && jobs.length > 0 && (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left p-3 font-medium">Input</th>
                  <th className="text-left p-3 font-medium">Status</th>
                  <th className="text-left p-3 font-medium">Progress</th>
                  <th className="text-left p-3 font-medium">Created</th>
                  <th className="text-left p-3 font-medium">Completed</th>
                </tr>
              </thead>
              <tbody>
                {jobs.map((job) => (
                  <tr key={job.jobId} className="border-b hover:bg-gray-50">
                    <td className="p-3 max-w-[200px] truncate" title={job.inputText}>
                      {job.inputText}
                    </td>
                    <td className="p-3">
                      <Badge variant={getStatusBadgeVariant(job.status)}>{getStatusLabel(job.status)}</Badge>
                    </td>
                    <td className="p-3">
                      {job.processedCharacters}/{job.totalCharacters}
                    </td>
                    <td className="p-3">{new Date(job.createdAt).toLocaleString()}</td>
                    <td className="p-3">
                      {job.completedAt ? new Date(job.completedAt).toLocaleString() : '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
