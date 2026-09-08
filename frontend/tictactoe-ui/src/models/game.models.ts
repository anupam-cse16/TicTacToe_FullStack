// Game domain models matching the backend response shape

export type Player = 'X' | 'O';
export type GameMode = 'TwoPlayer' | 'Computer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

export interface WinningCell {
  row: number;
  col: number;
}

export interface MoveRecord {
  moveNumber: number;
  player: Player;
  row: number;
  col: number;
}

export interface GameState {
  id: string;
  board: (string | null)[][];
  currentPlayer: Player;
  mode: GameMode;
  status: GameStatus;
  winner: Player | null;
  winningCells: WinningCell[];
  moveHistory: MoveRecord[];
}

export interface ScoreboardState {
  xWins: number;
  oWins: number;
  draws: number;
}
