import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { ProgressBar } from '../ProgressBar';

describe('ProgressBar', () => {
  const defaultProps = {
    processedCharacters: 0,
    totalCharacters: 0,
    isProcessing: false,
    isCompleted: false,
    isCancelled: false,
    error: null,
  };

  it('does not render when total is 0 and not processing', () => {
    render(<ProgressBar {...defaultProps} />);

    expect(screen.queryByTestId('progress-bar-container')).not.toBeInTheDocument();
  });

  it('shows 0% initially when processing starts', () => {
    render(
      <ProgressBar {...defaultProps} totalCharacters={10} isProcessing={true} />
    );

    expect(screen.getByTestId('progress-percentage')).toHaveTextContent('0%');
  });

  it('calculates correct percentage', () => {
    render(
      <ProgressBar {...defaultProps} processedCharacters={5} totalCharacters={10} isProcessing={true} />
    );

    expect(screen.getByTestId('progress-percentage')).toHaveTextContent('50%');
  });

  it('shows 100% when completed', () => {
    render(
      <ProgressBar
        {...defaultProps}
        processedCharacters={20}
        totalCharacters={20}
        isCompleted={true}
      />
    );

    expect(screen.getByTestId('progress-percentage')).toHaveTextContent('100%');
  });

  it('shows character count', () => {
    render(
      <ProgressBar {...defaultProps} processedCharacters={3} totalCharacters={10} isProcessing={true} />
    );

    expect(screen.getByText('3 / 10 characters')).toBeInTheDocument();
  });

  it('displays error message when error is present', () => {
    render(
      <ProgressBar
        {...defaultProps}
        processedCharacters={5}
        totalCharacters={10}
        error="Something went wrong"
      />
    );

    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
  });

  it('shows cancelled message when cancelled', () => {
    render(
      <ProgressBar
        {...defaultProps}
        processedCharacters={5}
        totalCharacters={10}
        isCancelled={true}
      />
    );

    expect(screen.getByText('Job was cancelled.')).toBeInTheDocument();
  });

  it('applies green color when completed', () => {
    render(
      <ProgressBar
        {...defaultProps}
        processedCharacters={10}
        totalCharacters={10}
        isCompleted={true}
      />
    );

    const fill = screen.getByTestId('progress-fill');
    expect(fill.className).toContain('bg-green-500');
  });

  it('applies red color when error', () => {
    render(
      <ProgressBar
        {...defaultProps}
        processedCharacters={5}
        totalCharacters={10}
        error="Error occurred"
      />
    );

    const fill = screen.getByTestId('progress-fill');
    expect(fill.className).toContain('bg-red-500');
  });
});
