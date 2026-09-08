import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { StatusBanner } from '../components/StatusBanner';
import type { GameState } from '../models/game.models';

const baseGame: GameState = {
  id: 'game-1',
  board: [[null, null, null], [null, null, null], [null, null, null]],
  currentPlayer: 'X',
  mode: 'TwoPlayer',
  status: 'InProgress',
  winner: null,
  winningCells: [],
  moveHistory: [],
};

describe('StatusBanner', () => {
  it('returns null when game is null', () => {
    const { container } = render(<StatusBanner game={null} isComputerThinking={false} />);
    expect(container.firstChild).toBeNull();
  });

  it('renders winning banner when game is Won', () => {
    render(<StatusBanner game={{ ...baseGame, status: 'Won', winner: 'X' }} isComputerThinking={false} />);
    expect(screen.getByText(/Player.*wins!/i)).toBeInTheDocument();
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('renders draw banner when game is Draw', () => {
    render(<StatusBanner game={{ ...baseGame, status: 'Draw' }} isComputerThinking={false} />);
    expect(screen.getByText(/It's a draw!/i)).toBeInTheDocument();
  });

  it('renders computer thinking indicator when isComputerThinking is true', () => {
    render(<StatusBanner game={{ ...baseGame, mode: 'Computer', currentPlayer: 'O' }} isComputerThinking={true} />);
    expect(screen.getByText(/is thinking/i)).toBeInTheDocument();
  });

  it('renders player turn banner during normal game', () => {
    render(<StatusBanner game={baseGame} isComputerThinking={false} />);
    expect(screen.getByText(/turn/i)).toBeInTheDocument();
  });
});
