import { renderHook, act, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { useTicTacToe } from '../hooks/useTicTacToe';
import { gameService } from '../services/gameService';
import type { GameState, ScoreboardState } from '../models/game.models';

vi.mock('../services/gameService');

const mockGame: GameState = {
  id: 'game-1',
  board: [[null, null, null], [null, null, null], [null, null, null]],
  currentPlayer: 'X',
  mode: 'TwoPlayer',
  status: 'InProgress',
  winner: null,
  winningCells: [],
  moveHistory: [],
};

const mockScoreboard: ScoreboardState = { xWins: 1, oWins: 0, draws: 0 };

describe('useTicTacToe', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.mocked(gameService.createGame).mockResolvedValue(mockGame);
    vi.mocked(gameService.getScoreboard).mockResolvedValue(mockScoreboard);
    vi.mocked(gameService.makeMove).mockResolvedValue({
      ...mockGame,
      board: [['X', null, null], [null, null, null], [null, null, null]],
      currentPlayer: 'O',
      moveHistory: [{ moveNumber: 1, player: 'X', row: 0, col: 0 }],
    });
    vi.mocked(gameService.reset).mockResolvedValue(mockGame);
    vi.mocked(gameService.undo).mockResolvedValue(mockGame);
    vi.mocked(gameService.resetScoreboard).mockResolvedValue({ xWins: 0, oWins: 0, draws: 0 });
  });

  it('initializes game and scoreboard on mount', async () => {
    const { result } = renderHook(() => useTicTacToe());

    await waitFor(() => {
      expect(result.current.game).not.toBeNull();
    });

    expect(result.current.game).toEqual(mockGame);
    expect(result.current.scoreboard).toEqual(mockScoreboard);
  });

  it('handles mode change', async () => {
    const computerGame = { ...mockGame, mode: 'Computer' as const };
    vi.mocked(gameService.createGame).mockResolvedValue(computerGame);

    const { result } = renderHook(() => useTicTacToe());

    await waitFor(() => {
      expect(result.current.game).not.toBeNull();
    });

    await act(async () => {
      result.current.handleModeChange('Computer');
    });

    expect(result.current.selectedMode).toBe('Computer');
    expect(result.current.game?.mode).toBe('Computer');
  });

  it('optimistically places mark and manages AI thinking in Computer mode', async () => {
    vi.useFakeTimers();

    const computerGame: GameState = { ...mockGame, mode: 'Computer' };
    vi.mocked(gameService.createGame).mockResolvedValue(computerGame);
    vi.mocked(gameService.makeMove).mockResolvedValue({
      ...computerGame,
      board: [['X', 'O', null], [null, null, null], [null, null, null]],
      currentPlayer: 'X',
      moveHistory: [
        { moveNumber: 1, player: 'X', row: 0, col: 0 },
        { moveNumber: 2, player: 'O', row: 0, col: 1 },
      ],
    });

    const { result } = renderHook(() => useTicTacToe());

    await act(async () => {
      await vi.runAllTicks();
    });

    // User clicks cell (0, 0)
    act(() => {
      result.current.handleCellClick(0, 0);
    });

    // Optimistic check: X is already on board immediately, and isComputerThinking is true
    expect(result.current.game?.board[0][0]).toBe('X');
    expect(result.current.isComputerThinking).toBe(true);

    // Fast-forward 1s timer
    await act(async () => {
      vi.advanceTimersByTime(1000);
      await vi.runAllTicks();
    });

    // Server state resolved: O is now placed, thinking is done
    expect(result.current.game?.board[0][1]).toBe('O');
    expect(result.current.isComputerThinking).toBe(false);

    vi.useRealTimers();
  });

  it('rolls back optimistic state if API call fails in Computer mode', async () => {
    vi.useFakeTimers();

    const computerGame: GameState = { ...mockGame, mode: 'Computer' };
    vi.mocked(gameService.createGame).mockResolvedValue(computerGame);
    vi.mocked(gameService.makeMove).mockRejectedValue(new Error('Network disconnected'));

    const { result } = renderHook(() => useTicTacToe());

    await act(async () => {
      await vi.runAllTicks();
    });

    act(() => {
      result.current.handleCellClick(0, 0);
    });

    // Optimistically placed
    expect(result.current.game?.board[0][0]).toBe('X');

    // Fast-forward
    await act(async () => {
      vi.advanceTimersByTime(1000);
      await vi.runAllTicks();
    });

    // Rolled back to empty and error set
    expect(result.current.game?.board[0][0]).toBeNull();
    expect(result.current.error).toBe('Network disconnected');

    // Dismiss error
    act(() => {
      result.current.dismissError();
    });
    expect(result.current.error).toBeNull();

    vi.useRealTimers();
  });

  it('handles TwoPlayer cell click', async () => {
    const { result } = renderHook(() => useTicTacToe());

    await waitFor(() => {
      expect(result.current.game).not.toBeNull();
    });

    await act(async () => {
      result.current.handleCellClick(0, 0);
    });

    expect(gameService.makeMove).toHaveBeenCalledWith('game-1', 'X', 0, 0);
  });

  it('ignores cell clicks when cell is already occupied or game is completed', async () => {
    const fullGame: GameState = {
      ...mockGame,
      status: 'Won',
      winner: 'X',
      board: [['X', null, null], [null, null, null], [null, null, null]],
    };
    vi.mocked(gameService.createGame).mockResolvedValue(fullGame);

    const { result } = renderHook(() => useTicTacToe());

    await waitFor(() => {
      expect(result.current.game).not.toBeNull();
    });

    act(() => {
      result.current.handleCellClick(0, 0);
    });

    expect(gameService.makeMove).not.toHaveBeenCalled();
  });

  it('handles reset and undo', async () => {
    const { result } = renderHook(() => useTicTacToe());

    await waitFor(() => {
      expect(result.current.game).not.toBeNull();
    });

    await act(async () => {
      result.current.handleReset();
    });
    expect(gameService.reset).toHaveBeenCalledWith('game-1');

    await act(async () => {
      result.current.handleUndo();
    });
    expect(gameService.undo).toHaveBeenCalledWith('game-1');

    await act(async () => {
      result.current.handleResetScoreboard();
    });
    expect(gameService.resetScoreboard).toHaveBeenCalled();
  });

  it('handles non-Error thrown gracefully and supports null game guard', async () => {
    vi.mocked(gameService.createGame).mockRejectedValueOnce('A string error');

    const { result } = renderHook(() => useTicTacToe());

    await waitFor(() => {
      expect(result.current.error).toBe('Something went wrong');
    });

    // Reset and undo when game is null should be safe no-op
    expect(result.current.game).toBeNull();
    act(() => {
      result.current.handleReset();
      result.current.handleUndo();
    });
  });

  it('optimistically handles O current player in Computer mode', async () => {
    vi.useFakeTimers();
    const computerGameO: GameState = { ...mockGame, mode: 'Computer', currentPlayer: 'O' };
    vi.mocked(gameService.createGame).mockResolvedValue(computerGameO);
    vi.mocked(gameService.makeMove).mockResolvedValue({
      ...computerGameO,
      board: [['O', null, null], [null, null, null], [null, null, null]],
      currentPlayer: 'X',
      moveHistory: [{ moveNumber: 1, player: 'O', row: 0, col: 0 }],
    });

    const { result } = renderHook(() => useTicTacToe());

    await act(async () => {
      await vi.runAllTicks();
    });

    act(() => {
      result.current.handleCellClick(0, 0);
    });

    expect(result.current.game?.currentPlayer).toBe('X');

    await act(async () => {
      vi.advanceTimersByTime(1000);
      await vi.runAllTicks();
    });

    vi.useRealTimers();
  });
});
