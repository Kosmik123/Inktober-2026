# Relic (day 2): game memory

Specific to the Relic game. Shared knowledge is in `memory.md`.

## Branches and pages

- Branch prefix `2-relic/`, integration branch `2-relic/main`.
- itch.io page: https://kosmik123.itch.io/inktober-2026-relic (butler target `kosmik123/inktober-2026-relic`, channel `web`). The apple test page is not for this game. Build and push only when the user says so (as of 2026-11-21 the user said "not a deploy yet").

## Design decisions

- 3D "arrows" puzzle in a 5x5x5 voxel volume (the size is set in the test UI). Bent snake-like pieces fill the volume, a golden relic cube sits in cell `size/2`. Clearing every piece reveals the relic.
- **Removal is all-or-nothing**: a piece can be removed only when the whole line from its head to the edge of the grid is clear (its own body does not block, the relic does). A blocked piece does not move, it only bumps. Partial sliding (several clicks per piece) was discussed and put off as a possible later variant, because it needs a state-graph solver or a reverse generator. Ask before changing the rule.
- Pieces never aim through the relic.
- The pieces are rigid stone blocks (chamfered cubes, a pyramid-shaped head), grey with washed-out tints (terracotta, turquoise, ochre, patina), not organic bending meshes. The relic is the only saturated gold. The user polishes the look themselves.
- Removal animation: the stones slide along the path of the piece and straight out of the grid, one behind another, without turning. A blocked piece bumps toward what blocks it and back. The logic removes the piece at once; the animation is only the view.
- No Bloom, no hint highlighting for now.

## Controls

- One global input component, not a handler per piece. Left click or a quick tap (under 0.5 s, little movement) removes a piece; right mouse drag or a dragging finger orbits the camera around the relic, the rotation equals the pointer movement (no joystick). A press that starts on the interface is ignored by the puzzle.

## Architecture

- `ArrowGrid` is plain C# (with events); `PuzzleBoard` wraps it and raises `OnGridCreated`, `OnPieceRemoved`, `OnRemovalBlocked`; `PuzzleView` builds stones and runs the animations; `GameFlow` keeps Ready, Playing, Won (a generic reusable GameFlow is a future idea).
- Generator: cuts the volume (minus the relic) into random self-avoiding paths, then simulates the solution ("peels" pieces that have a clear ray) so every puzzle is solvable; holes only when filling fails after the attempts. The alternative the user suggested (place pieces from the last removed to the first, each with a clear ray) is valid; it was not chosen because pockets cut off from the edge could not be filled. Could be tried as a second mode.

## Measured: generation time (editor, 30 seeds)

3x3x3 0.6 ms; 5x5x5 22 ms (one 369 ms spike); 7x7x7 59 ms; 8x8x8 199 ms (max 638 ms), all filled without holes. 10x10x10 mean 2.1 s (max 13 s); 12x12x12 25 s with holes in most seeds. Generation blocks the main thread. `PuzzleBoard.SetSize` clamps every axis to 1..12.

## Test UI

Three size fields (X, Y, Z, 5, 5, 5 at the start) and a New game button at the top, an FPS counter in the bottom left corner, a "Relic found!" panel on win. The button reads the fields, gives the size to the board and restarts the round.

## Open items

- The camera stays at a fixed distance: it does not fit the grid size (large grids leave the frame).
- On a phone the TMP input fields may not open the on-screen keyboard in WebGL; the fallback is +/- buttons.
- Nothing about the look, the touch controls or the UI was seen in Play Mode or on a phone by the agent.
