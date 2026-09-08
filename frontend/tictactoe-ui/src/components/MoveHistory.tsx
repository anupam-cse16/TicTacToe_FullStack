import type { MoveRecord } from '../models/game.models';
import styles from './MoveHistory.module.css';

interface MoveHistoryProps {
  moves: MoveRecord[];
}

export function MoveHistory({ moves }: MoveHistoryProps) {
  if (moves.length === 0) {
    return (
      <div className={styles.container}>
        <h3 className={styles.title}>Move History</h3>
        <p className={styles.empty}>No moves yet.</p>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <h3 className={styles.title}>Move History</h3>
      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Move</th>
              <th>Player</th>
              <th>Position</th>
            </tr>
          </thead>
          <tbody>
            {moves.map((m) => (
              <tr key={m.moveNumber} className={m.player === 'X' ? styles.xRow : styles.oRow}>
                <td>{m.moveNumber}</td>
                <td className={m.player === 'X' ? styles.x : styles.o}>{m.player}</td>
                <td>
                  Row {m.row + 1}, Column {m.col + 1}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
