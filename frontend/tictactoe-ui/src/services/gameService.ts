import type { GameMode, GameState, ScoreboardState } from '../models/game.models';

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5043';

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const headers: Record<string, string> = {};
  if (options?.body) {
    headers['Content-Type'] = 'application/json';
  }

  const response = await fetch(`${BASE_URL}${url}`, {
    ...options,
    headers: {
      ...headers,
      ...options?.headers,
    },
  });

  if (!response.ok) {
    const error = await response.json().catch(() => null);
    throw new Error(error?.error ?? error?.detail ?? `HTTP ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export const gameService = {
  createGame: (mode: GameMode, signal?: AbortSignal): Promise<GameState> =>
    request('/api/games', {
      method: 'POST',
      body: JSON.stringify({ mode }),
      signal,
    }),

  getGame: (id: string, signal?: AbortSignal): Promise<GameState> =>
    request(`/api/games/${id}`, { signal }),

  makeMove: (id: string, player: string, row: number, col: number, signal?: AbortSignal): Promise<GameState> =>
    request(`/api/games/${id}/moves`, {
      method: 'POST',
      body: JSON.stringify({ player, row, col }),
      signal,
    }),

  undo: (id: string, signal?: AbortSignal): Promise<GameState> =>
    request(`/api/games/${id}/undo`, { method: 'POST', signal }),

  reset: (id: string, signal?: AbortSignal): Promise<GameState> =>
    request(`/api/games/${id}/reset`, { method: 'POST', signal }),

  getScoreboard: (signal?: AbortSignal): Promise<ScoreboardState> =>
    request('/api/scoreboard', { signal }),

  resetScoreboard: (signal?: AbortSignal): Promise<ScoreboardState> =>
    request('/api/scoreboard/reset', { method: 'POST', signal }),
};
