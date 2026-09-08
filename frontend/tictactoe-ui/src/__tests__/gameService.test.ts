import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { gameService } from '../services/gameService';

describe('gameService', () => {
  const originalFetch = global.fetch;

  beforeEach(() => {
    vi.restoreAllMocks();
  });

  afterEach(() => {
    global.fetch = originalFetch;
  });

  it('createGame sends POST to /api/games', async () => {
    const mockData = { id: '123', mode: 'TwoPlayer' };
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockData,
    } as Response);

    const result = await gameService.createGame('TwoPlayer');
    expect(result).toEqual(mockData);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/games'),
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ mode: 'TwoPlayer' }),
      })
    );
  });

  it('getGame sends GET to /api/games/:id', async () => {
    const mockData = { id: '123' };
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockData,
    } as Response);

    const result = await gameService.getGame('123');
    expect(result).toEqual(mockData);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/games/123'),
      expect.anything()
    );
  });

  it('makeMove sends POST to /api/games/:id/moves', async () => {
    const mockData = { id: '123' };
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockData,
    } as Response);

    const result = await gameService.makeMove('123', 'X', 1, 2);
    expect(result).toEqual(mockData);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/games/123/moves'),
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ player: 'X', row: 1, col: 2 }),
      })
    );
  });

  it('undo sends POST to /api/games/:id/undo', async () => {
    const mockData = { id: '123' };
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockData,
    } as Response);

    const result = await gameService.undo('123');
    expect(result).toEqual(mockData);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/games/123/undo'),
      expect.objectContaining({ method: 'POST' })
    );
  });

  it('reset sends POST to /api/games/:id/reset', async () => {
    const mockData = { id: '123' };
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockData,
    } as Response);

    const result = await gameService.reset('123');
    expect(result).toEqual(mockData);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/games/123/reset'),
      expect.objectContaining({ method: 'POST' })
    );
  });

  it('getScoreboard sends GET to /api/scoreboard', async () => {
    const mockData = { xWins: 2, oWins: 1, draws: 0 };
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockData,
    } as Response);

    const result = await gameService.getScoreboard();
    expect(result).toEqual(mockData);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/scoreboard'),
      expect.anything()
    );
  });

  it('resetScoreboard sends POST to /api/scoreboard/reset', async () => {
    const mockData = { xWins: 0, oWins: 0, draws: 0 };
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => mockData,
    } as Response);

    const result = await gameService.resetScoreboard();
    expect(result).toEqual(mockData);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/scoreboard/reset'),
      expect.objectContaining({ method: 'POST' })
    );
  });

  it('throws error with server message when response is not ok', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 400,
      json: async () => ({ error: 'Cell is already occupied' }),
    } as Response);

    await expect(gameService.makeMove('123', 'X', 0, 0)).rejects.toThrow(
      'Cell is already occupied'
    );
  });

  it('throws error with HTTP status when json parsing fails on non-ok response', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
      json: async () => {
        throw new Error('Invalid JSON');
      },
    } as unknown as Response);

    await expect(gameService.createGame('TwoPlayer')).rejects.toThrow('HTTP 500');
  });

  it('throws error with detail message when detail property is present', async () => {
    global.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 400,
      json: async () => ({ detail: 'RFC 7807 problem detail' }),
    } as Response);

    await expect(gameService.makeMove('123', 'X', 0, 0)).rejects.toThrow(
      'RFC 7807 problem detail'
    );
  });

  it('passes AbortSignal to fetch request when provided', async () => {
    const controller = new AbortController();
    global.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: async () => ({ id: '123' }),
    } as Response);

    await gameService.getGame('123', controller.signal);
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/api/games/123'),
      expect.objectContaining({ signal: controller.signal })
    );
  });
});
