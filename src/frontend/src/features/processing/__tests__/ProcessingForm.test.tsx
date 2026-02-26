import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { ProcessingForm } from '../ProcessingForm';

describe('ProcessingForm', () => {
  const defaultProps = {
    onSubmit: vi.fn(),
    onCancel: vi.fn(),
    isProcessing: false,
    isStarting: false,
  };

  it('renders input textarea and process button', () => {
    render(<ProcessingForm {...defaultProps} />);

    expect(screen.getByTestId('input-textarea')).toBeInTheDocument();
    expect(screen.getByTestId('process-button')).toBeInTheDocument();
  });

  it('process button is disabled when input is empty', () => {
    render(<ProcessingForm {...defaultProps} />);

    expect(screen.getByTestId('process-button')).toBeDisabled();
  });

  it('process button is enabled when input has text', async () => {
    const user = userEvent.setup();
    render(<ProcessingForm {...defaultProps} />);

    await user.type(screen.getByTestId('input-textarea'), 'Hello');

    expect(screen.getByTestId('process-button')).toBeEnabled();
  });

  it('calls onSubmit when form is submitted', async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    render(<ProcessingForm {...defaultProps} onSubmit={onSubmit} />);

    await user.type(screen.getByTestId('input-textarea'), 'Hello');
    await user.click(screen.getByTestId('process-button'));

    expect(onSubmit).toHaveBeenCalledWith('Hello');
  });

  it('disables input and button when processing', () => {
    render(<ProcessingForm {...defaultProps} isProcessing={true} />);

    expect(screen.getByTestId('input-textarea')).toBeDisabled();
    expect(screen.getByTestId('process-button')).toBeDisabled();
  });

  it('shows cancel button only when processing', () => {
    const { rerender } = render(<ProcessingForm {...defaultProps} />);

    expect(screen.queryByTestId('cancel-button')).not.toBeInTheDocument();

    rerender(<ProcessingForm {...defaultProps} isProcessing={true} />);

    expect(screen.getByTestId('cancel-button')).toBeInTheDocument();
  });

  it('calls onCancel when cancel button is clicked', async () => {
    const onCancel = vi.fn();
    const user = userEvent.setup();
    render(<ProcessingForm {...defaultProps} isProcessing={true} onCancel={onCancel} />);

    await user.click(screen.getByTestId('cancel-button'));

    expect(onCancel).toHaveBeenCalledTimes(1);
  });

  it('shows Starting... text when isStarting is true', () => {
    render(<ProcessingForm {...defaultProps} isStarting={true} />);

    expect(screen.getByTestId('process-button')).toHaveTextContent('Starting...');
  });
});
