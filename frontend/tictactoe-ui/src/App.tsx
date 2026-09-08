import { useTicTacToe } from './hooks/useTicTacToe';
import { StatusBanner } from './components/StatusBanner';
import { Board } from './components/Board';
import { MoveHistory } from './components/MoveHistory';
import { Scoreboard } from './components/Scoreboard';
import { GameControls } from './components/GameControls';
import styles from './App.module.css';

export default function App() {
  const {
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
  } = useTicTacToe();

  return (
    <div className={styles.app}>
      <header className={styles.header}>
        <h1 className={styles.title}>Tic Tac Toe</h1>
      </header>

      {error && (
        <div className={styles.error} role="alert">
          <span>⚠️ {error}</span>
          <button onClick={dismissError} aria-label="Dismiss error">✕</button>
        </div>
      )}

      <GameControls
        game={game}
        selectedMode={selectedMode}
        isLoading={isLoading}
        isComputerThinking={isComputerThinking}
        onModeChange={handleModeChange}
        onReset={handleReset}
        onUndo={handleUndo}
      />

      <main className={styles.main}>
        <div className={styles.leftPanel}>
          <StatusBanner game={game} isComputerThinking={isComputerThinking} />
          {game ? (
            <Board
              game={game}
              isComputerThinking={isComputerThinking}
              onCellClick={handleCellClick}
            />
          ) : (
            <div className={styles.loading} role="status">Loading…</div>
          )}
        </div>

        <div className={styles.rightPanel}>
          <Scoreboard scoreboard={scoreboard} onReset={handleResetScoreboard} />
          {game && <MoveHistory moves={game.moveHistory} />}
        </div>
      </main>
    </div>
  );
}
