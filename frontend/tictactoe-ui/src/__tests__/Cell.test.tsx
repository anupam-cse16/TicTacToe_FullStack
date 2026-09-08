import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { Cell } from '../components/Cell';

describe('Cell', () => {
  it('renders empty cell', () => {
    render(
      <Cell value={null} row={0} col={0} isWinning={false} isDisabled={false} onClick={vi.fn()} />
    );
    const btn = screen.getByRole('button');
    expect(btn).toBeInTheDocument();
    expect(btn).not.toBeDisabled();
  });

  it('renders X value', () => {
    render(
      <Cell value="X" row={0} col={0} isWinning={false} isDisabled={false} onClick={vi.fn()} />
    );
    expect(screen.getByText('X')).toBeInTheDocument();
  });

  it('renders O value', () => {
    render(
      <Cell value="O" row={0} col={0} isWinning={false} isDisabled={false} onClick={vi.fn()} />
    );
    expect(screen.getByText('O')).toBeInTheDocument();
  });

  it('calls onClick when empty cell clicked', () => {
    const onClick = vi.fn();
    render(
      <Cell value={null} row={1} col={2} isWinning={false} isDisabled={false} onClick={onClick} />
    );
    fireEvent.click(screen.getByRole('button'));
    expect(onClick).toHaveBeenCalledWith(1, 2);
  });

  it('does not call onClick when cell has value', () => {
    const onClick = vi.fn();
    render(
      <Cell value="X" row={0} col={0} isWinning={false} isDisabled={false} onClick={onClick} />
    );
    // Button is disabled when it has a value
    expect(screen.getByRole('button')).toBeDisabled();
  });

  it('is disabled when isDisabled is true', () => {
    render(
      <Cell value={null} row={0} col={0} isWinning={false} isDisabled={true} onClick={vi.fn()} />
    );
    expect(screen.getByRole('button')).toBeDisabled();
  });

  it('handles arrow key navigation across cells', () => {
    render(
      <div>
        <Cell value={null} row={0} col={0} isWinning={false} isDisabled={false} onClick={vi.fn()} />
        <Cell value={null} row={0} col={1} isWinning={false} isDisabled={false} onClick={vi.fn()} />
      </div>
    );

    const btn00 = screen.getByRole('button', { name: /Cell row 1 col 1/i });
    const btn01 = screen.getByRole('button', { name: /Cell row 1 col 2/i });

    btn00.focus();
    fireEvent.keyDown(btn00, { key: 'ArrowRight' });
    expect(document.activeElement).toBe(btn01);

    fireEvent.keyDown(btn01, { key: 'ArrowLeft' });
    expect(document.activeElement).toBe(btn00);

    fireEvent.keyDown(btn00, { key: 'ArrowDown' });
    fireEvent.keyDown(btn00, { key: 'ArrowUp' });
    fireEvent.keyDown(btn00, { key: 'Enter' }); // Non-arrow key does not throw
  });
});
