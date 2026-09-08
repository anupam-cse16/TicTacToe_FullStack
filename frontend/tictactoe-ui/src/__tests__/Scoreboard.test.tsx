import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { Scoreboard } from '../components/Scoreboard';

describe('Scoreboard', () => {
  it('renders wins and draws correctly', () => {
    render(
      <Scoreboard
        scoreboard={{ xWins: 3, oWins: 2, draws: 1 }}
        onReset={vi.fn()}
      />
    );

    expect(screen.getByText('Scoreboard')).toBeInTheDocument();
    expect(screen.getByText('3')).toBeInTheDocument();
    expect(screen.getByText('2')).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
  });

  it('triggers onReset when button is clicked', () => {
    const onReset = vi.fn();
    render(
      <Scoreboard
        scoreboard={{ xWins: 0, oWins: 0, draws: 0 }}
        onReset={onReset}
      />
    );

    const btn = screen.getByRole('button', { name: /reset scoreboard/i });
    fireEvent.click(btn);

    expect(onReset).toHaveBeenCalled();
  });
});
