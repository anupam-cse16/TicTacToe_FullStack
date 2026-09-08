import type { ScoreboardState } from '../models/game.models';
import styles from './Scoreboard.module.css';

interface ScoreboardProps {
  scoreboard: ScoreboardState;
  onReset: () => void;
}

export function Scoreboard({ scoreboard, onReset }: ScoreboardProps) {
  return (
    <div className={styles.container}>
      <h3 className={styles.title}>Scoreboard</h3>
      <div className={styles.scores}>
        <div className={styles.score}>
          <span className={`${styles.label} ${styles.x}`}>X Wins</span>
          <span className={styles.value}>{scoreboard.xWins}</span>
        </div>
        <div className={styles.divider} />
        <div className={styles.score}>
          <span className={styles.label}>Draws</span>
          <span className={styles.value}>{scoreboard.draws}</span>
        </div>
        <div className={styles.divider} />
        <div className={styles.score}>
          <span className={`${styles.label} ${styles.o}`}>O Wins</span>
          <span className={styles.value}>{scoreboard.oWins}</span>
        </div>
      </div>
      <button className={styles.resetBtn} onClick={onReset}>
        Reset Scoreboard
      </button>
    </div>
  );
}
