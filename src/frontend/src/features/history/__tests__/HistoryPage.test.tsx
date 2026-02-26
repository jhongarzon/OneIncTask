import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { HistoryPage } from '../HistoryPage';

// Mock the useJobHistory hook
vi.mock('../useJobHistory', () => ({
  useJobHistory: vi.fn(),
}));

import { useJobHistory } from '../useJobHistory';

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
}

describe('HistoryPage', () => {
  it('shows loading state', () => {
    vi.mocked(useJobHistory).mockReturnValue({
      data: undefined,
      isLoading: true,
      error: null,
    } as ReturnType<typeof useJobHistory>);

    renderWithProviders(<HistoryPage />);

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('shows empty state when no jobs', () => {
    vi.mocked(useJobHistory).mockReturnValue({
      data: [],
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useJobHistory>);

    renderWithProviders(<HistoryPage />);

    expect(screen.getByText('No jobs yet.')).toBeInTheDocument();
  });

  it('shows error state', () => {
    vi.mocked(useJobHistory).mockReturnValue({
      data: undefined,
      isLoading: false,
      error: new Error('Failed'),
    } as unknown as ReturnType<typeof useJobHistory>);

    renderWithProviders(<HistoryPage />);

    expect(screen.getByText('Error loading history.')).toBeInTheDocument();
  });

  it('renders job history table', () => {
    vi.mocked(useJobHistory).mockReturnValue({
      data: [
        {
          jobId: '1',
          inputText: 'Hello',
          status: 'Completed',
          processedCharacters: 10,
          totalCharacters: 10,
          createdAt: '2024-01-01T00:00:00Z',
          completedAt: '2024-01-01T00:01:00Z',
        },
      ],
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useJobHistory>);

    renderWithProviders(<HistoryPage />);

    expect(screen.getByText('Hello')).toBeInTheDocument();
    // "Completed" appears as both table header and badge
    const completedElements = screen.getAllByText('Completed');
    expect(completedElements.length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('10/10')).toBeInTheDocument();
  });

  it('renders multiple jobs', () => {
    vi.mocked(useJobHistory).mockReturnValue({
      data: [
        {
          jobId: '1',
          inputText: 'First',
          status: 'Completed',
          processedCharacters: 5,
          totalCharacters: 5,
          createdAt: '2024-01-01T00:00:00Z',
          completedAt: '2024-01-01T00:01:00Z',
        },
        {
          jobId: '2',
          inputText: 'Second',
          status: 'Cancelled',
          processedCharacters: 3,
          totalCharacters: 8,
          createdAt: '2024-01-02T00:00:00Z',
          completedAt: null,
        },
      ],
      isLoading: false,
      error: null,
    } as unknown as ReturnType<typeof useJobHistory>);

    renderWithProviders(<HistoryPage />);

    expect(screen.getByText('First')).toBeInTheDocument();
    expect(screen.getByText('Second')).toBeInTheDocument();
    // "Completed" appears as both table header and badge
    const completedElements = screen.getAllByText('Completed');
    expect(completedElements.length).toBeGreaterThanOrEqual(2);
    expect(screen.getByText('Cancelled')).toBeInTheDocument();
  });
});
