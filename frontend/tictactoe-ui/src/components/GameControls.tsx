import type { GameMode, GameState } from '../models/game.models';
import styles from './GameControls.module.css';

interface GameControlsProps {
  game: GameState | null;
  selectedMode: GameMode;
  isLoading: boolean;
  isComputerThinking: boolean;
  onModeChange: (mode: GameMode) => void;
  onReset: () => void;
  onUndo: () => void;
}

export function GameControls({
  game,
  selectedMode,
  isLoading,
  isComputerThinking,
  onModeChange,
  onReset,
  onUndo,
}: GameControlsProps) {
  const canUndo =
    game !== null &&
    game.status === 'InProgress' &&
    game.moveHistory.length > 0 &&
    !isComputerThinking;

  return (
    <div className={styles.container}>
      {/* Mode label */}
      <span className={styles.modeLabel}>Mode:</span>

      {/* Mode toggles */}
      <button
        className={`${styles.modeBtn} ${selectedMode === 'TwoPlayer' ? styles.active : ''}`}
        onClick={() => onModeChange('TwoPlayer')}
        disabled={isLoading}
      >
        👥 Two Player
      </button>
      <button
        className={`${styles.modeBtn} ${selectedMode === 'Computer' ? styles.active : ''}`}
        onClick={() => onModeChange('Computer')}
        disabled={isLoading}
      >
        🤖 vs Computer
      </button>

      {/* Divider */}
      <div className={styles.divider} />

      {/* Action buttons */}
      <button
        className={`${styles.btn} ${styles.resetBtn}`}
        onClick={onReset}
        disabled={isLoading || isComputerThinking}
      >
        🔄 Reset Game
      </button>
      <button
        className={`${styles.btn} ${styles.undoBtn}`}
        onClick={onUndo}
        disabled={!canUndo}
      >
        ↩ Undo
      </button>
    </div>
  );
}
