import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { Board } from '../components/Board';
import type { GameState } from '../models/game.models';

const baseGame: GameState = {
  id: 'game-1',
  board: [
    ['X', null, null],
    [null, 'O', null],
    [null, null, null],
  ],
  currentPlayer: 'X',
  mode: 'TwoPlayer',
  status: 'InProgress',
  winner: null,
  winningCells: [],
  moveHistory: [],
};

describe('Board', () => {
  it('renders all 9 cells with current values', () => {
    render(
      <Board game={baseGame} isComputerThinking={false} onCellClick={vi.fn()} />
    );

    expect(screen.getByText('X')).toBeInTheDocument();
    expect(screen.getByText('O')).toBeInTheDocument();
    expect(screen.getAllByRole('button')).toHaveLength(9);
  });

  it('renders thinking overlay when isComputerThinking is true', () => {
    render(
      <Board game={baseGame} isComputerThinking={true} onCellClick={vi.fn()} />
    );

    expect(screen.getByText('Computer thinking…')).toBeInTheDocument();
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('triggers onCellClick when empty cell is clicked', () => {
    const onCellClick = vi.fn();
    render(
      <Board game={baseGame} isComputerThinking={false} onCellClick={onCellClick} />
    );

    const emptyCell = screen.getByRole('button', { name: /cell row 1 col 2/i });
    fireEvent.click(emptyCell);

    expect(onCellClick).toHaveBeenCalledWith(0, 1);
  });

  it('highlights winning cells', () => {
    const winningGame: GameState = {
      ...baseGame,
      status: 'Won',
      winner: 'X',
      winningCells: [
        { row: 0, col: 0 },
        { row: 0, col: 1 },
        { row: 0, col: 2 },
      ],
    };

    render(
      <Board game={winningGame} isComputerThinking={false} onCellClick={vi.fn()} />
    );

    const winningBtn = screen.getByRole('button', { name: /cell row 1 col 1/i });
    expect(winningBtn.className).toContain('winning');
  });
});
