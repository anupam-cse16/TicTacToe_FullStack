import type { KeyboardEvent } from 'react';
import type { WinningCell } from '../models/game.models';
import styles from './Cell.module.css';

interface CellProps {
  value: string | null;
  row: number;
  col: number;
  isWinning: boolean;
  isDisabled: boolean;
  boardSize?: number;
  onClick: (row: number, col: number) => void;
}

export function Cell({ value, row, col, isWinning, isDisabled, boardSize = 3, onClick }: CellProps) {
  const handleClick = () => {
    if (!isDisabled && !value) {
      onClick(row, col);
    }
  };

  const handleKeyDown = (e: KeyboardEvent<HTMLButtonElement>) => {
    let targetRow = row;
    let targetCol = col;

    if (e.key === 'ArrowUp') targetRow = (row - 1 + boardSize) % boardSize;
    else if (e.key === 'ArrowDown') targetRow = (row + 1) % boardSize;
    else if (e.key === 'ArrowLeft') targetCol = (col - 1 + boardSize) % boardSize;
    else if (e.key === 'ArrowRight') targetCol = (col + 1) % boardSize;
    else return;

    e.preventDefault();
    const nextBtn = document.querySelector<HTMLButtonElement>(`[data-cell="${targetRow}-${targetCol}"]`);
    nextBtn?.focus();
  };

  const className = [
    styles.cell,
    value === 'X' ? styles.x : value === 'O' ? styles.o : '',
    isWinning ? styles.winning : '',
    !value && !isDisabled ? styles.clickable : '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button
      className={className}
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      disabled={isDisabled || !!value}
      data-cell={`${row}-${col}`}
      aria-label={`Cell row ${row + 1} col ${col + 1}${value ? ` - ${value}` : ''}`}
    >
      {value}
    </button>
  );
}

export function isWinningCell(winningCells: WinningCell[], row: number, col: number): boolean {
  return winningCells.some((c) => c.row === row && c.col === col);
}
