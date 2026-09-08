import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import type { GameState, ScoreboardState } from '../models/game.models';
import { gameService } from '../services/gameService';
import App from '../App';

// Mock the game service
vi.mock('../services/gameService');

const mockGame: GameState = {
  id: 'test-game-id',
  board: [
    [null, null, null],
    [null, null, null],
    [null, null, null],
  ],
  currentPlayer: 'X',
  mode: 'TwoPlayer',
  status: 'InProgress',
  winner: null,
  winningCells: [],
  moveHistory: [],
};

const mockScoreboard: ScoreboardState = { xWins: 1, oWins: 0, draws: 0 };

beforeEach(() => {
  vi.restoreAllMocks();
  vi.mocked(gameService.createGame).mockResolvedValue(mockGame);
  vi.mocked(gameService.getScoreboard).mockResolvedValue(mockScoreboard);
  vi.mocked(gameService.reset).mockResolvedValue(mockGame);
  vi.mocked(gameService.resetScoreboard).mockResolvedValue({ xWins: 0, oWins: 0, draws: 0 });
  vi.mocked(gameService.undo).mockResolvedValue(mockGame);
  vi.mocked(gameService.makeMove).mockResolvedValue({
    ...mockGame,
    board: [['X', null, null], [null, null, null], [null, null, null]],
    currentPlayer: 'O',
    moveHistory: [{ moveNumber: 1, player: 'X', row: 0, col: 0 }],
  });
});

describe('App Component', () => {
  it('renders the game title and initial state', async () => {
    render(<App />);
    expect(screen.getByText('Tic Tac Toe')).toBeInTheDocument();
    const cells = await screen.findAllByRole('button', { name: /Cell row/i });
    expect(cells).toHaveLength(9);
    expect(document.body.textContent).toMatch(/X.*turn/i);
    expect(screen.getByText('1')).toBeInTheDocument(); // xWins
  });

  it('handles cell click in TwoPlayer mode', async () => {
    render(<App />);
    const cells = await screen.findAllByRole('button', { name: /Cell row/i });

    await act(async () => {
      fireEvent.click(cells[0]);
    });

    expect(gameService.makeMove).toHaveBeenCalledWith('test-game-id', 'X', 0, 0);
  });

  it('handles mode change to Computer', async () => {
    const computerGame: GameState = {
      ...mockGame,
      mode: 'Computer',
    };
    vi.mocked(gameService.createGame).mockResolvedValue(computerGame);

    render(<App />);
    await screen.findAllByRole('button', { name: /Cell row/i });

    const compBtn = screen.getByRole('button', { name: /vs Computer/i });
    await act(async () => {
      fireEvent.click(compBtn);
    });

    expect(gameService.createGame).toHaveBeenCalledWith('Computer');
  });

  it('handles cell click in Computer mode with timer and scoreboard refresh on finish', async () => {
    vi.useFakeTimers();

    const computerGame: GameState = {
      ...mockGame,
      mode: 'Computer',
    };
    vi.mocked(gameService.createGame).mockResolvedValue(computerGame);
    vi.mocked(gameService.makeMove).mockResolvedValue({
      ...computerGame,
      status: 'Won',
      winner: 'X',
      winningCells: [{ row: 0, col: 0 }, { row: 0, col: 1 }, { row: 0, col: 2 }],
    });

    render(<App />);
    // Let mount promises resolve
    await act(async () => {
      await vi.runAllTicks();
    });

    const cells = screen.getAllByRole('button', { name: /Cell row/i });
    act(() => {
      fireEvent.click(cells[0]);
    });

    // Advance 1s timer for computer thinking
    await act(async () => {
      vi.advanceTimersByTime(1000);
      await vi.runAllTicks();
    });

    expect(gameService.makeMove).toHaveBeenCalled();
    expect(gameService.getScoreboard).toHaveBeenCalled();

    vi.useRealTimers();
  });

  it('shows winning banner when game is Won', async () => {
    const wonGame: GameState = {
      ...mockGame,
      status: 'Won',
      winner: 'X',
    };
    vi.mocked(gameService.createGame).mockResolvedValue(wonGame);

    render(<App />);
    await screen.findAllByRole('button', { name: /Cell row/i });

    expect(screen.getByText(/Player.*wins!/i)).toBeInTheDocument();
  });

  it('shows draw banner when game is Draw', async () => {
    const drawGame: GameState = {
      ...mockGame,
      status: 'Draw',
    };
    vi.mocked(gameService.createGame).mockResolvedValue(drawGame);

    render(<App />);
    await screen.findAllByRole('button', { name: /Cell row/i });

    expect(screen.getByText(/It's a draw!/i)).toBeInTheDocument();
  });

  it('resets game successfully on button click', async () => {
    render(<App />);
    await screen.findAllByRole('button', { name: /Cell row/i });

    const resetBtn = screen.getByRole('button', { name: /Reset Game/i });
    await act(async () => {
      fireEvent.click(resetBtn);
    });

    expect(gameService.reset).toHaveBeenCalledWith('test-game-id');
  });

  it('displays error and allows dismissing it', async () => {
    vi.mocked(gameService.reset).mockRejectedValue(new Error('Network failure'));

    render(<App />);
    await screen.findAllByRole('button', { name: /Cell row/i });

    const resetBtn = screen.getByRole('button', { name: /Reset Game/i });
    await act(async () => {
      fireEvent.click(resetBtn);
    });

    expect(screen.getByText(/Network failure/i)).toBeInTheDocument();

    const dismissBtn = screen.getByRole('button', { name: /Dismiss error/i });
    fireEvent.click(dismissBtn);

    expect(screen.queryByText(/Network failure/i)).not.toBeInTheDocument();
  });

  it('handles undo click when moves exist', async () => {
    const gameWithMoves: GameState = {
      ...mockGame,
      moveHistory: [{ moveNumber: 1, player: 'X', row: 0, col: 0 }],
    };
    vi.mocked(gameService.createGame).mockResolvedValue(gameWithMoves);

    render(<App />);
    await screen.findAllByRole('button', { name: /Cell row/i });

    const undoBtn = screen.getByRole('button', { name: /Undo/i });
    expect(undoBtn).not.toBeDisabled();

    await act(async () => {
      fireEvent.click(undoBtn);
    });

    expect(gameService.undo).toHaveBeenCalledWith('test-game-id');
  });

  it('resets scoreboard on button click', async () => {
    render(<App />);
    await screen.findAllByRole('button', { name: /Cell row/i });

    const resetScoreBtn = screen.getByRole('button', { name: /Reset Scoreboard/i });
    await act(async () => {
      fireEvent.click(resetScoreBtn);
    });

    expect(gameService.resetScoreboard).toHaveBeenCalled();
  });

  it('catches scoreboard refresh error gracefully', async () => {
    vi.mocked(gameService.makeMove).mockResolvedValue({
      ...mockGame,
      status: 'Won',
      winner: 'X',
    });
    vi.mocked(gameService.getScoreboard)
      .mockResolvedValueOnce(mockScoreboard)
      .mockRejectedValueOnce(new Error('Scoreboard error'));

    render(<App />);
    const cells = await screen.findAllByRole('button', { name: /Cell row/i });

    await act(async () => {
      fireEvent.click(cells[0]);
    });

    // Should not crash the UI
    expect(screen.getByText('Tic Tac Toe')).toBeInTheDocument();
  });
});
