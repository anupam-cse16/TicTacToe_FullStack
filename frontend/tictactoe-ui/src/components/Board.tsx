import type { GameState } from '../models/game.models';
import { Cell, isWinningCell } from './Cell';
import styles from './Board.module.css';

interface BoardProps {
  game: GameState;
  isComputerThinking: boolean;
  onCellClick: (row: number, col: number) => void;
}

export function Board({ game, isComputerThinking, onCellClick }: BoardProps) {
  const isBoardDisabled =
    game.status !== 'InProgress' ||
    isComputerThinking ||
    (game.mode === 'Computer' && game.currentPlayer === 'O');

  return (
    <div className={styles.boardWrapper}>
      {/* Grid */}
      <div
        className={`${styles.board} ${isComputerThinking ? styles.dimmed : ''}`}
        aria-label="Tic Tac Toe board"
        aria-busy={isComputerThinking}
      >
        {game.board.map((row, rIdx) =>
          row.map((cell, cIdx) => (
            <Cell
              key={`${rIdx}-${cIdx}`}
              value={cell}
              row={rIdx}
              col={cIdx}
              isWinning={isWinningCell(game.winningCells, rIdx, cIdx)}
              isDisabled={isBoardDisabled}
              boardSize={game.board.length}
              onClick={onCellClick}
            />
          ))
        )}

        {/* Thinking overlay — rendered inside .board so it covers it perfectly */}
        {isComputerThinking && (
          <div className={styles.thinkingOverlay} role="status" aria-label="Computer is thinking">
            <span className={styles.thinkingIcon}>🤖</span>
            <div className={styles.thinkingDots}>
              <span className={styles.dot} />
              <span className={styles.dot} />
              <span className={styles.dot} />
            </div>
            <span className={styles.thinkingLabel}>Computer thinking…</span>
          </div>
        )}
      </div>
    </div>
  );
}
