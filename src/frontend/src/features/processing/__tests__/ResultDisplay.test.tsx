import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { ResultDisplay } from '../ResultDisplay';

describe('ResultDisplay', () => {
  it('renders empty placeholder when no result', () => {
    render(<ResultDisplay result="" isProcessing={false} isCompleted={false} />);

    const textarea = screen.getByTestId('result-textarea');
    expect(textarea).toBeInTheDocument();
    expect(textarea).toHaveValue('');
  });

  it('displays accumulating result text', () => {
    render(<ResultDisplay result="H1e1" isProcessing={true} isCompleted={false} />);

    expect(screen.getByTestId('result-textarea')).toHaveValue('H1e1');
  });

  it('shows Processing... indicator when processing', () => {
    render(<ResultDisplay result="abc" isProcessing={true} isCompleted={false} />);

    expect(screen.getByText('Processing...')).toBeInTheDocument();
  });

  it('shows Complete indicator when completed', () => {
    render(<ResultDisplay result="done" isProcessing={false} isCompleted={true} />);

    expect(screen.getByText('Complete')).toBeInTheDocument();
  });

  it('textarea is read-only', () => {
    render(<ResultDisplay result="test" isProcessing={false} isCompleted={false} />);

    expect(screen.getByTestId('result-textarea')).toHaveAttribute('readonly');
  });

  it('displays full completed result', () => {
    const fullResult = ' 1!1,1H1W1d1e1l3o2r1/SGVsbG8sIFdvcmxkIQ==';
    render(<ResultDisplay result={fullResult} isProcessing={false} isCompleted={true} />);

    expect(screen.getByTestId('result-textarea')).toHaveValue(fullResult);
  });
});
