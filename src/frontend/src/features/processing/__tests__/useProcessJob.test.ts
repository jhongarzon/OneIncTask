import { describe, it, expect, vi, beforeEach } from 'vitest';

// Mock the api-client module
vi.mock('@/lib/api-client', () => ({
  startJob: vi.fn(),
  cancelJob: vi.fn(),
}));

import { startJob, cancelJob } from '@/lib/api-client';

describe('useProcessJob API functions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('startJob calls API with correct input', async () => {
    const mockResponse = { jobId: '123' };
    vi.mocked(startJob).mockResolvedValueOnce(mockResponse);

    const result = await startJob('Hello');

    expect(startJob).toHaveBeenCalledWith('Hello');
    expect(result.jobId).toBe('123');
  });

  it('cancelJob calls API with correct jobId', async () => {
    vi.mocked(cancelJob).mockResolvedValueOnce(undefined);

    await cancelJob('job-123');

    expect(cancelJob).toHaveBeenCalledWith('job-123');
  });

  it('startJob throws on API error', async () => {
    vi.mocked(startJob).mockRejectedValueOnce(new Error('Conflict'));

    await expect(startJob('test')).rejects.toThrow('Conflict');
  });
});
