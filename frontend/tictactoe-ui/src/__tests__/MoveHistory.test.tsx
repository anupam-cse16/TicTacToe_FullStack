import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { MoveHistory } from '../components/MoveHistory';
import type { MoveRecord } from '../models/game.models';

describe('MoveHistory', () => {
  it('renders empty message when no moves are present', () => {
    render(<MoveHistory moves={[]} />);
    expect(screen.getByText('No moves yet.')).toBeInTheDocument();
  });

  it('renders move table with 1-indexed rows and cols', () => {
    const moves: MoveRecord[] = [
      { moveNumber: 1, player: 'X', row: 0, col: 0 },
      { moveNumber: 2, player: 'O', row: 1, col: 2 },
    ];

    render(<MoveHistory moves={moves} />);

    expect(screen.getByText('Move History')).toBeInTheDocument();
    expect(screen.getByText('Row 1, Column 1')).toBeInTheDocument();
    expect(screen.getByText('Row 2, Column 3')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
  });
});
