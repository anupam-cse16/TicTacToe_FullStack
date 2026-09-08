import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { GameControls } from '../components/GameControls';
import type { GameState } from '../models/game.models';

const mockGame: GameState = {
  id: 'game-1',
  board: [[null, null, null], [null, null, null], [null, null, null]],
  currentPlayer: 'X',
  mode: 'TwoPlayer',
  status: 'InProgress',
  winner: null,
  winningCells: [],
  moveHistory: [{ moveNumber: 1, player: 'X', row: 0, col: 0 }],
};

describe('GameControls', () => {
  it('calls onModeChange when a mode button is clicked', () => {
    const onModeChange = vi.fn();
    render(
      <GameControls
        game={mockGame}
        selectedMode="TwoPlayer"
        isLoading={false}
        isComputerThinking={false}
        onModeChange={onModeChange}
        onReset={vi.fn()}
        onUndo={vi.fn()}
      />
    );

    const compBtn = screen.getByRole('button', { name: /vs computer/i });
    fireEvent.click(compBtn);

    expect(onModeChange).toHaveBeenCalledWith('Computer');
  });

  it('calls onReset when reset button is clicked', () => {
    const onReset = vi.fn();
    render(
      <GameControls
        game={mockGame}
        selectedMode="TwoPlayer"
        isLoading={false}
        isComputerThinking={false}
        onModeChange={vi.fn()}
        onReset={onReset}
        onUndo={vi.fn()}
      />
    );

    const resetBtn = screen.getByRole('button', { name: /reset game/i });
    fireEvent.click(resetBtn);

    expect(onReset).toHaveBeenCalled();
  });

  it('calls onUndo when undo button is clicked and moves exist', () => {
    const onUndo = vi.fn();
    render(
      <GameControls
        game={mockGame}
        selectedMode="TwoPlayer"
        isLoading={false}
        isComputerThinking={false}
        onModeChange={vi.fn()}
        onReset={vi.fn()}
        onUndo={onUndo}
      />
    );

    const undoBtn = screen.getByRole('button', { name: /undo/i });
    expect(undoBtn).not.toBeDisabled();
    fireEvent.click(undoBtn);

    expect(onUndo).toHaveBeenCalled();
  });

  it('disables undo when game is completed', () => {
    const wonGame: GameState = { ...mockGame, status: 'Won', winner: 'X' };
    render(
      <GameControls
        game={wonGame}
        selectedMode="TwoPlayer"
        isLoading={false}
        isComputerThinking={false}
        onModeChange={vi.fn()}
        onReset={vi.fn()}
        onUndo={vi.fn()}
      />
    );

    const undoBtn = screen.getByRole('button', { name: /undo/i });
    expect(undoBtn).toBeDisabled();
  });

  it('disables buttons when loading or computer thinking', () => {
    render(
      <GameControls
        game={mockGame}
        selectedMode="TwoPlayer"
        isLoading={true}
        isComputerThinking={true}
        onModeChange={vi.fn()}
        onReset={vi.fn()}
        onUndo={vi.fn()}
      />
    );

    expect(screen.getByRole('button', { name: /two player/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /vs computer/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /reset game/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /undo/i })).toBeDisabled();
  });
});
