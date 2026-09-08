# Tic Tac Toe — Full-Stack Application

A browser-based Tic Tac Toe game with a **React + TypeScript** frontend and a **.NET 9 Web API** backend, communicating via REST APIs.

---

## Project Overview

| Layer | Tech |
|---|---|
| Frontend | React 19 + TypeScript + Vite |
| Backend | .NET 9 Web API (ASP.NET Core) |
| API Style | REST |
| Storage | SQLite (Entity Framework Core 9) |
| Backend Tests | xUnit + FluentAssertions |
| Frontend Tests | Vitest + React Testing Library |

---

## Features Implemented

- ✅ 3×3 interactive game board
- ✅ Two Player mode (X vs O)
- ✅ Computer opponent mode (O plays automatically with strategy)
- ✅ Turn display and alternation
- ✅ Win detection (rows, columns, diagonals) with cell highlighting
- ✅ Draw detection
- ✅ Move history table (1-indexed positions)
- ✅ Undo last move (1 move in Two-Player, 2 moves in Computer mode)
- ✅ Session scoreboard (X Wins / O Wins / Draws)
- ✅ Reset Game (board resets, scoreboard unchanged)
- ✅ Reset Scoreboard
- ✅ SQLite database persistence with EF Core 9 (survives process restarts)
- ✅ Repository Pattern (`IGameRepository`, `IScoreboardRepository`) & Unit of Work (`IUnitOfWork`)
- ✅ AutoMapper 16.2.0 for declarative entity/domain/DTO mapping
- ✅ EF Core optimizations: `AsNoTracking()` queries, B-Tree index on `LastActiveUtc`, and SQLite WAL mode
- ✅ Backend is source of truth for all game state
- ✅ CORS configured for local development
- ✅ Interactive Swagger UI at `/swagger` & Scalar UI at `/scalar/v1`
- ✅ Enterprise structured logging & request tracing with Correlation ID (`X-Correlation-ID` header, ASP.NET Core HTTP logging, and scoped domain event logs)

---

## Getting Started from GitHub

### Prerequisites

Make sure the following tools are installed before cloning:

| Tool | Minimum Version | Download |
|---|---|---|
| .NET SDK | 9.0 | https://dotnet.microsoft.com/download |
| Node.js | 20.x | https://nodejs.org |
| npm | 10.x | Comes bundled with Node.js |
| Git | Any recent version | https://git-scm.com |

---

### Step 1 — Clone the Repository

```bash
git clone https://github.com/anupam-cse16/TicTacToe_FullStack.git
cd TicTacToe_FullStack
```

---

### Step 2 — Start the Backend

```bash
cd backend/TicTacToe.API
dotnet run
```

- The API starts at **`http://localhost:5043`**
- SQLite database (`tictactoe.db`) is **created automatically** on first run — no manual DB setup needed
- Interactive API docs available at:
  - **Swagger UI**: http://localhost:5043/swagger
  - **Scalar UI**: http://localhost:5043/scalar/v1
  - **OpenAPI JSON**: http://localhost:5043/openapi/v1.json

> Keep this terminal open and open a **new terminal** for the frontend step.

---

### Step 3 — Start the Frontend

```bash
cd frontend/tictactoe-ui
npm install
npm run dev
```

- The app starts at **`http://localhost:5173`**
- Open your browser and navigate to **http://localhost:5173** to play

---

### Step 4 (Optional) — Run Tests

**Backend Tests** (from the `backend/` folder):
```bash
cd backend
dotnet test
```

With code coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```
Expected: **90 tests passing · 96.15% line coverage**

---

**Frontend Tests** (from the `frontend/tictactoe-ui/` folder):
```bash
cd frontend/tictactoe-ui
npm test
```

With coverage report:
```bash
npm test -- --coverage
```
Expected: **56 tests passing · 100% line coverage · 97.74% branch coverage**

---

## API Endpoint Summary

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/games` | Create a new game session |
| `GET` | `/api/games/{id}` | Get current game state |
| `POST` | `/api/games/{id}/moves` | Submit a player move |
| `POST` | `/api/games/{id}/undo` | Undo last move(s) |
| `POST` | `/api/games/{id}/reset` | Reset the current game |
| `GET` | `/api/scoreboard` | Get the scoreboard |
| `POST` | `/api/scoreboard/reset` | Reset the scoreboard |

### Request / Response Examples

**Create Game**
```json
POST /api/games
{ "mode": "TwoPlayer" }   // or "Computer"
```

**Make Move**
```json
POST /api/games/{id}/moves
{ "player": "X", "row": 0, "col": 1 }
```

**Game State Response**
```json
{
  "id": "guid",
  "board": [[null, "X", null], ["O", null, null], [null, null, null]],
  "currentPlayer": "X",
  "mode": "TwoPlayer",
  "status": "InProgress",
  "winner": null,
  "winningCells": [],
  "moveHistory": [
    { "moveNumber": 1, "player": "X", "row": 0, "col": 1 }
  ]
}
```

`status` values: `InProgress` | `Won` | `Draw`

---

## Design & Architectural Decisions

### 1. Undo After Game Completion — Option A (Disabled)
Once a game is won or drawn, **Undo is disabled**. The scoreboard remains final. This keeps scoreboard state simple and consistent without needing to adjust historical records.

### 2. Server-Side Computer Move Orchestration
When in Computer mode, the backend calculates and applies the computer's move in the same `/moves` API call. The frontend displays an artificial thinking animation with a guaranteed minimum 1-second delay (`Promise.all`) to ensure a natural gameplay pace.

### 3. Strategy Pattern for AI (`IMoveStrategy`)
Move selection is decoupled from `ComputerMoveService`:
- **`RuleBasedMoveStrategy`**: Fast greedy heuristic (Win → Block → Center → Corner → Any).
- **`MinimaxMoveStrategy`**: Game-theory optimal minimax algorithm with depth scoring (unbeatable in 3×3).
- Configurable at runtime via `appsettings.json` (`GameSettings:AiStrategy`).

### 4. Zero-Allocation Hot-Path Detection
`WinDetectionService` avoids LINQ allocations (`.Select()`, `.ToArray()`, `.Where()`) during move evaluations. All patterns and coordinate checks are computed with direct 2D indexed lookups, producing zero GC pressure.

### 5. Thread-Safe Concurrency & Session Eviction
- `GameService` uses `ConcurrentDictionary<Guid, GameSession>` with per-session locking for atomic move validation.
- `ScoreboardService` uses thread synchronization (`lock`) for safe multi-client updates.
- `SessionCleanupBackgroundService` runs as an `IHostedService` to evict stale idle sessions (>2 hours), preventing memory leaks.

### 6. Centralized Global Exception Handling
`GlobalExceptionMiddleware` intercepts domain exceptions and maps them to standard HTTP status codes:
- `KeyNotFoundException` → 404 Not Found
- `InvalidOperationException` / `ArgumentException` → 400 Bad Request
- Uncaught exceptions → 500 Internal Server Error
This keeps `GamesController` clean and declarative without repetitive try-catch blocks.

### 7. Zero Hardcoded Constants
All environment and domain constants are externalized:
- **Backend**: `appsettings.json` and `appsettings.Development.json` (`GameSettings`: board size, player markers, computer player; `CorsSettings`: allowed origins).
- **Frontend**: `.env` (`VITE_API_BASE_URL`).

---

## Manual Review, Steering & Interventions by Reviewer

Throughout the development lifecycle, the human reviewer provided critical manual steering, design decisions, and code quality audits:

1. **Frontend Tech Stack Pivot (Prompt 1 / User Correction)**:
   - *Reviewer Decision*: When Angular was proposed, the reviewer explicitly directed: *"Use React instead"*. Switched to React 19 + TypeScript + Vite.
2. **UI & Layout Alignment Review (Prompt 4 & 6)**:
   - *Reviewer Decision*: Identified visual misalignment between mode toggles and game action buttons from browser screenshots. Directed merging controls into a unified horizontal baseline row with consistent button heights, spacing, and subtle dividers.
3. **UX Pacing & Thinking Animation (Prompt 5 & 7)**:
   - *Reviewer Decision*: Identified that the computer played instantly, causing jarring visual jumps. Directed adding a blurred board overlay with bouncing robot animation, pulsing dots, cell pop-in animations, and enforcing a minimum 1-second thinking delay.
4. **Code Quality & Anti-Hardcoding Audit in Win Detection (Prompt 8)**:
   - *Reviewer Decision*: Pointed out that lines 10–19 in `WinDetectionService.cs` contained hardcoded coordinate arrays. Directed refactoring to a dynamic mathematical pattern generator that derives all lines from a configurable `Size` variable.
5. **Architectural & SOLID Principles Review (Prompt 9 & 10)**:
   - *Reviewer Decision*: Audited backend code against SOLID principles. Directed extracting `IWinDetectionService` and `IComputerMoveService` (DIP/LSP), removing `ScoreboardUpdated` state pollution from the `GameSession` domain model (SRP), and enabling pluggable AI move strategies (OCP).
6. **Strict Configuration Externalization (Prompt 11)**:
   - *Reviewer Decision*: Mandated that zero constants remain hardcoded in code files. Directed externalizing all board dimensions, player markers, starting turn, AI strategy, and CORS URLs into `appsettings.json` via `IOptions<T>` and `.env`.
7. **Strict Code Coverage Standard (Prompt 12)**:
   - *Reviewer Decision*: Set the bar for both backend and frontend code coverage to **exceed 90%**, requiring comprehensive coverage of negative boundary scenarios, occupied cell violations, game-over guards, network errors, and HTTP 400/404/500 responses.
8. **Engineering & Backend Optimization Review (Prompt 13)**:
   - *Reviewer Decision*: Approved and directed full implementation of thread safety (`ConcurrentDictionary`), zero-allocation hot paths, global exception handling middleware, Minimax AI strategy, and background session TTL cleanup.
9. **Frontend Architecture, UX Pacing & Quality Audit (Prompt 16 & 17)**:
   - *Reviewer Decision*: Directed complete frontend refactoring: optimistic user mark rendering with thinking overlay, separation of concerns into `useTicTacToe` custom hook, standalone accessible `StatusBanner`, CSS variables design tokens in `index.css`, and full keyboard navigation (Arrow keys). Expanded frontend tests to 52 tests (100% lines, 95.9% branches).
10. **Interactive API Documentation & Swagger UI (Prompt 18)**:
   - *Reviewer Decision*: Questioned missing visual Swagger documentation (due to .NET 9 removing Swashbuckle by default in favor of raw `/openapi/v1.json`). Directed implementing full interactive Swagger UI (`/swagger`) via `Swashbuckle.AspNetCore.SwaggerUI` alongside modern `Scalar.AspNetCore` (`/scalar/v1`), maintaining 100% test pass rate across all 70 backend tests.
11. **Full Stack Senior Lead Review & Enterprise Hardening (Prompt 19)**:
   - *Reviewer Decision*: Requested comprehensive Senior Lead Developer audit. Directed eliminating in-memory leak in `ScoreboardService` via timestamped TTL pruning, externalizing cleanup intervals to `appsettings.json`, decoupling fallback strategy in `MinimaxMoveStrategy`, zero-allocation corner checks, RFC 7807 `ProblemDetails` exception middleware, parameterizing frontend `boardSize` and `VITE_COMPUTER_THINK_MS`, and adding `AbortSignal` HTTP cancellation support (72 backend tests at 96.54% coverage, 56 frontend tests at 100% lines and 97.74% branches).

---

### What the AI Generated vs. Manually Reviewed & Directed

| Component | AI Generated | Human Reviewer Directed / Corrected |
|---|---|---|
| **Tech Stack** | Initial Angular scaffold | Overridden to React + Vite + TypeScript |
| **Backend Architecture** | Initial models, services, controllers | Enforced SOLID interfaces, DI decoupling, and model purity |
| **Algorithm Design** | Initial 8 hardcoded win arrays | Refactored to dynamic board-size pattern generator & zero-allocation loops |
| **AI Opponent** | Heuristic greedy logic | Abstracted to Strategy pattern with optimal Minimax algorithm |
| **Configuration** | Hardcoded constants & URLs | Mandated full externalization to `appsettings.json` & `.env` |
| **Concurrency & Memory** | Standard `Dictionary` | Converted to `ConcurrentDictionary`, locks, and background TTL cleanup |
| **UI / UX** | Default layout & instant AI moves | Redesigned controls bar alignment, added 1s AI delay & thinking overlay |
| **Error Handling** | Repetitive try/catch in controllers | Replaced with centralized `GlobalExceptionMiddleware` + RFC 7807 `ProblemDetails` |
| **Test Quality** | Basic happy-path tests | Mandated >90% coverage with extensive negative & edge case suites |
| **Frontend Architecture** | Monolithic `App.tsx` state & simultaneous mark drop | Refactored to `useTicTacToe` hook, optimistic UI, CSS design tokens & keyboard a11y |
| **API Documentation** | Raw `/openapi/v1.json` only (.NET 9 default) | Added interactive Swagger UI (`/swagger`) and Scalar UI (`/scalar/v1`) |
| **Enterprise Hardening** | Unbounded `ScoreboardService` set & hardcoded TTLs | Added timestamped pruning, externalized TTLs, AbortSignal support, and zero-allocation corners |

---

### Chronological Prompt & Decision Log

| # | Prompt (summarised) | Human Review Decision & What Was Implemented |
|---|---|---|
| 1 | Full requirements document for a Tic Tac Toe app with Angular/React frontend and .NET backend | Formulated implementation plan. Reviewer provided immediate critical pivot: *"Use React instead of Angular"*. |
| 2 | "Proceed" | Approved implementation plan. Generated .NET 9 Web API backend, xUnit test suite (36 tests), React + TS frontend, and Vitest suite (14 tests). |
| 3 | "Run both the frontend and backend" | Started backend (`dotnet run` on port 5043) and frontend (`npm run dev` on port 5173). |
| 4 | "Can we make the UI a bit aligned and spacious. Also in future, update the readme with summary of all the prompts I am using" | Reviewer reviewed screenshot and mandated UI redesign (larger 110px cells, wider gaps, uppercase scoreboard tracking) and initiated this living prompt log. |
| 5 | "Can we give a thinking or buffering animation when the computer is thinking instead of just jumping the screen" | Reviewer identified instant AI moves as poor UX. Added blurred overlay with bouncing robot icon, pulsing dots, cell pop-in animations, and dimming filter. |
| 6 | "Still see, these two are not properly aligned — Two Player, vs Computer, Reset, Undo should be properly aligned" | Reviewer identified vertical stacking misalignment. Merged all controls into a single flex row with a divider and consistent button heights. |
| 7 | "It looks the computer instantly plays its turn, add a delay of 1 sec with a thinking" | Reviewer enforced a natural gameplay pace. Added `COMPUTER_THINK_MS = 1000` via `Promise.all` so thinking animation is guaranteed for ≥1s regardless of server response speed. |
| 8 | "The hardcoded lines 10-19 in WinDetectionService.cs doesn't look good, isn't there a better approach?" | Reviewer caught hardcoded coordinate lines in win detection. Replaced with `GenerateWinPatterns()` parameterized by board size (`Size`). |
| 9 | "Are all the SOLID principles getting followed in the backend code?" | Reviewer triggered a full SOLID audit across all backend classes. |
| 10 | "yes please" (approve SOLID refactoring) | Reviewer approved decoupling: extracted `IWinDetectionService` & `IComputerMoveService`, removed `ScoreboardUpdated` state pollution from domain model, registered interfaces in DI. |
| 11 | "There should not be any hard coded constants in the files, get those from configuration files like appsettings.json" | Reviewer mandated 100% externalization: created `GameSettings` and `CorsSettings` bound via `IOptions<T>` from `appsettings.json` and `.env` on frontend. Added `ConfigurationTests.cs`. |
| 12 | "The code coverage of both backend and frontend should be greater than 90% covering negative scenarios as well as edge cases" | Reviewer set strict quality standard. Expanded backend tests to 60 tests (98.47% line coverage) and frontend to 39 tests (100% line coverage, 94.39% branch coverage), testing negative scenarios and boundaries. |
| 13 | "In the backend service, is there any refactoring or optimization that can be done? - All of the above" | Reviewer approved and directed 5 major optimizations: Thread-safety (`ConcurrentDictionary`), Zero-allocation loops in win detection, GlobalExceptionMiddleware, Strategy pattern with Minimax AI, and session TTL background cleanup. (70 backend tests passing). |
| 14 | "Also are you updating the readme with all the manual decisions and review I am doing?" | Reviewer ensured complete accountability and documentation of all human steering, architectural decisions, and design reviews in the README. |
| 15 | "Create another .md file and there keep the log of all the prompts used" | Created dedicated standalone log file [PROMPT_LOG.md](PROMPT_LOG.md) containing the full detailed narrative of all prompts, decisions, and outcomes. |
| 16 | "In the front end code do you see any refactoring, optimization, code quality issues that can be fixed?" | Reviewer prompted full frontend audit: identified simultaneous move drop, state bloat in App.tsx, missing CSS design tokens, and lacking keyboard navigation. |
| 17 | "all" | Reviewer approved complete frontend refactoring: extracted `useTicTacToe` custom hook, optimistic user mark rendering with 1s AI thinking overlay, standalone `StatusBanner`, CSS variables, and Arrow key grid navigation (52 tests passing). |
| 18 | "Isn't there swagger implemented for backend?" | Reviewer noted absence of visual Swagger UI (due to .NET 9 removing Swashbuckle from default templates). Added interactive Swagger UI (`/swagger`) and Scalar UI (`/scalar/v1`) consuming native OpenAPI endpoint. |
| 19 | "Now suppose you are a Full Stack Senior Lead developer, you have been given the task of reviewing this project... - yes please" | Conducted exhaustive Senior Lead review. Implemented timestamped scoreboard pruning, externalized background cleanup intervals, decoupled Minimax fallback, zero-allocation corners, RFC 7807 ProblemDetails, dynamic boardSize keyboard navigation, VITE_COMPUTER_THINK_MS env variable, and AbortSignal cancellation (72 backend / 56 frontend tests passing). |
| 20 | "Can't we use sqlite?" | Migrated storage from volatile in-memory to persistent SQLite with EF Core 9 (`tictactoe.db`). Added `GameSessionEntity` and `ScoreboardEntity`, bidirectional `GameSessionMapper`, scoped DI, restart resilience, and isolated in-memory test fixtures (78 backend tests passing with 96.74% line coverage). |
| 21 | "Are we using proper Reporsitory patter with uow(if needed) and automapper for mapping. And check for any EF COre optimizations" | Introduced `IGameRepository`, `IScoreboardRepository`, and `IUnitOfWork` to cleanly decouple domain services from EF Core. Integrated `AutoMapper` 16.2.0 with `GameMappingProfile`. Applied EF Core optimizations: B-Tree index on `LastActiveUtc`, `AsNoTracking()` queries, and SQLite WAL mode (84 backend tests passing with 96.55% line coverage). |
| 22 | "Is proper logging also implemented? - sure" | Mandated enterprise structured logging audit. Implemented `CorrelationIdMiddleware` for end-to-end request tracing via `X-Correlation-ID` header and log scoping, ASP.NET Core HTTP request logging (`AddHttpLogging`), and structured domain event logging with semantic parameters in `GameService` and `ScoreboardService` (90 backend tests passing with 96.15% line coverage). |

> 📖 **Full Prompt Log**: See [PROMPT_LOG.md](PROMPT_LOG.md) for complete prompt texts, reviewer steering decisions, and implementation breakdowns.

## Clarifications and Assumptions

1. **Clarification 1: Backend State Ownership**:
   - The backend (.NET 9 Web API) is the absolute single source of truth for all game rules, move validation, win/draw detection, move history, and scoreboard tracking. The React frontend maintains optimistic UI marks and thinking overlays for user responsiveness, but reconciles state strictly with API responses.

2. **Clarification 2: Scoreboard and Undo**:
   - **Option A Selected**: Once a game reaches a terminal state (`Won` or `Draw`), **Undo is disabled**. The scoreboard remains final and irreversible for that game.

3. **Frontend Framework Selection**:
   - As directed in Prompt 1 review, React 19 with TypeScript and Vite 5 was selected instead of Angular to provide lightweight state management, custom hook isolation (`useTicTacToe`), and high-performance DOM reconciliation.

4. **Session Identification & Persistence**:
   - Each game session is uniquely identified by a `Guid` generated by the backend and persisted directly in SQLite (`GameSessions` table). Games survive backend restarts and can be retrieved or resumed at any time.

5. **Player Roles in Computer Mode**:
   - The human player is assigned Player `X` and always takes the opening turn. The computer opponent is assigned Player `O` and plays automatically upon receiving a valid human move.

---

## Known Limitations

- No multi-user authentication system (sessions are identified by unique GUIDs).
- WebSockets are not used; communication follows pure REST with optimistic UI state and artificial pacing delay.

---

## Future Improvements

- [x] SQLite persistence via EF Core for multi-session historical storage (Implemented in Prompt 20)
- [ ] Real-time two-player multiplayer across devices via SignalR WebSockets
- [ ] Player profile customization and personal statistics
- [ ] Sound effects and haptic feedback
