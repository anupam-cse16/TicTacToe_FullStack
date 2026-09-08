import { useState, useCallback, useEffect, useRef } from 'react';
import type { GameMode, GameState, ScoreboardState } from '../models/game.models';
import { gameService } from '../services/gameService';

const DEFAULT_SCOREBOARD: ScoreboardState = { xWins: 0, oWins: 0, draws: 0 };
const COMPUTER_THINK_MS = Number(import.meta.env.VITE_COMPUTER_THINK_MS ?? 1000);

export function useTicTacToe() {
  const [game, setGame] = useState<GameState | null>(null);
  const [scoreboard, setScoreboard] = useState<ScoreboardState>(DEFAULT_SCOREBOARD);
  const [selectedMode, setSelectedMode] = useState<GameMode>('TwoPlayer');
  const [isLoading, setIsLoading] = useState(false);
  const [isComputerThinking, setIsComputerThinking] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Tracks active request sequence to prevent race conditions from stale responses
  const activeRequestIdRef = useRef(0);

  const refreshScoreboard = useCallback(async () => {
    try {
      const sb = await gameService.getScoreboard();
      setScoreboard(sb);
    } catch {
      // Non-critical fallback
    }
  }, []);

  const withLoading = useCallback(async (fn: () => Promise<void>) => {
    setIsLoading(true);
    setError(null);
    try {
      await fn();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Something went wrong');
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Initialize game on mount
  useEffect(() => {
    const reqId = ++activeRequestIdRef.current;
    withLoading(async () => {
      const [newGame, sb] = await Promise.all([
        gameService.createGame(selectedMode),
        gameService.getScoreboard(),
      ]);
      if (reqId === activeRequestIdRef.current) {
        setGame(newGame);
        setScoreboard(sb);
      }
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Mode change
  const handleModeChange = useCallback(
    (mode: GameMode) => {
      const reqId = ++activeRequestIdRef.current;
      setSelectedMode(mode);
      setIsComputerThinking(false);
      withLoading(async () => {
        const newGame = await gameService.createGame(mode);
        if (reqId === activeRequestIdRef.current) {
          setGame(newGame);
        }
      });
    },
    [withLoading]
  );

  // Cell click with optimistic user move and AI thinking delay
  const handleCellClick = useCallback(
    (row: number, col: number) => {
      if (!game || isLoading || isComputerThinking || game.status !== 'InProgress' || game.board[row][col] !== null) {
        return;
      }

      const reqId = ++activeRequestIdRef.current;
      const isComputerMode = game.mode === 'Computer';
      const prevGame = game;

      if (isComputerMode) {
        // Optimistically render the human's mark immediately on the board
        const optimisticBoard = game.board.map((r) => [...r]);
        optimisticBoard[row][col] = game.currentPlayer;
        const nextPlayer = game.currentPlayer === 'X' ? 'O' : 'X';

        setGame({
          ...game,
          board: optimisticBoard,
          currentPlayer: nextPlayer,
          moveHistory: [
            ...game.moveHistory,
            {
              moveNumber: game.moveHistory.length + 1,
              player: game.currentPlayer,
              row,
              col,
            },
          ],
        });

        setIsComputerThinking(true);

        withLoading(async () => {
          try {
            const [updated] = await Promise.all([
              gameService.makeMove(game.id, game.currentPlayer, row, col),
              new Promise<void>((resolve) => setTimeout(resolve, COMPUTER_THINK_MS)),
            ]);

            if (reqId === activeRequestIdRef.current) {
              setGame(updated);
              if (updated.status !== 'InProgress') {
                await refreshScoreboard();
              }
            }
          } catch (err) {
            // Roll back to previous game state on error
            if (reqId === activeRequestIdRef.current) {
              setGame(prevGame);
            }
            throw err;
          }
        }).finally(() => {
          if (reqId === activeRequestIdRef.current) {
            setIsComputerThinking(false);
          }
        });
      } else {
        // Two-Player mode
        withLoading(async () => {
          const updated = await gameService.makeMove(game.id, game.currentPlayer, row, col);
          if (reqId === activeRequestIdRef.current) {
            setGame(updated);
            if (updated.status !== 'InProgress') {
              await refreshScoreboard();
            }
          }
        });
      }
    },
    [game, isLoading, isComputerThinking, withLoading, refreshScoreboard]
  );

  const handleReset = useCallback(() => {
    if (!game) return;
    const reqId = ++activeRequestIdRef.current;
    setIsComputerThinking(false);
    withLoading(async () => {
      const updated = await gameService.reset(game.id);
      if (reqId === activeRequestIdRef.current) {
        setGame(updated);
      }
    });
  }, [game, withLoading]);

  const handleUndo = useCallback(() => {
    if (!game) return;
    const reqId = ++activeRequestIdRef.current;
    setIsComputerThinking(false);
    withLoading(async () => {
      const updated = await gameService.undo(game.id);
      if (reqId === activeRequestIdRef.current) {
        setGame(updated);
      }
    });
  }, [game, withLoading]);

  const handleResetScoreboard = useCallback(() => {
    withLoading(async () => {
      const sb = await gameService.resetScoreboard();
      setScoreboard(sb);
    });
  }, [withLoading]);

  const dismissError = useCallback(() => {
    setError(null);
  }, []);

  return {
    game,
    scoreboard,
    selectedMode,
    isLoading,
    isComputerThinking,
    error,
    dismissError,
    handleModeChange,
    handleCellClick,
    handleReset,
    handleUndo,
    handleResetScoreboard,
  };
}
