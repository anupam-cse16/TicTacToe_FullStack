import type { GameState } from '../models/game.models';
import styles from './StatusBanner.module.css';

interface StatusBannerProps {
  game: GameState | null;
  isComputerThinking: boolean;
}

export function StatusBanner({ game, isComputerThinking }: StatusBannerProps) {
  if (!game) return null;

  if (game.status === 'Won') {
    return (
      <div
        className={`${styles.banner} ${styles.won}`}
        role="status"
        aria-live="polite"
      >
        🎉 Player <strong>{game.winner}</strong> wins!
      </div>
    );
  }

  if (game.status === 'Draw') {
    return (
      <div
        className={`${styles.banner} ${styles.draw}`}
        role="status"
        aria-live="polite"
      >
        🤝 It's a draw!
      </div>
    );
  }

  return (
    <div className={styles.banner} role="status" aria-live="polite">
      {isComputerThinking ? (
        <>
          <span className={styles.oText}>O</span> (Computer) is thinking
          <span className={styles.thinkingEllipsis} />
        </>
      ) : (
        <>
          Player{' '}
          <span className={game.currentPlayer === 'X' ? styles.xText : styles.oText}>
            {game.currentPlayer}
          </span>
          's turn
          {game.mode === 'Computer' && game.currentPlayer === 'O' && ' (Computer)'}
        </>
      )}
    </div>
  );
}
