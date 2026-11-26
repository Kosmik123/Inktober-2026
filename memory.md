# Inktober 2026: shared memory

General knowledge for every game of the series. It is committed to `master` (see "Repository and workflow").
Facts that belong to one game only go to that game's memory file (see below), not here.

## Per-game memory

Every game has its own memory file in the repo root, named `<theme>-memory.md` (for example `relic-memory.md`).
It lives on the branches of that game. **When starting a new game, create its memory file first** if it does not exist,
and keep there: the rules and design of the game, its branch prefix and integration branch, its itch.io page, numbers measured for it, open items.
Put here only what holds for all the games.

**Project name.** At the start of a game's work (after the concept is settled) set the product name in ProjectSettings (Player settings) to `Inktober - <theme>`, for example `Inktober - Relic`. This name stays until the real title of the game is invented.

## Concept phase comes first

- **The agent never starts a game on its own.** A new game begins with a concept phase: the agent and the user talk the theme through, define the goals, the rules and the scope of the game, and the user makes the decisions.
- Until the user says explicitly that the concept is settled and implementation can start, the agent does not create branches, scenes, scripts or prefabs, and does not set up the project. Reading, asking questions and proposing options is fine.
- Write the decisions of the concept phase to the game's memory file (`<theme>-memory.md`) so they survive between sessions. Open questions go there as open items.
- If the user's instruction is unclear or looks unfinished, ask instead of guessing and starting work.

## Target platforms

- We do not target mobile devices only. Usually a game is multiplatform (desktop and mobile, played in the browser from WebGL), so input, UI layout and performance must work on both.
- Sometimes a game is made for PC only. Which one it is gets decided in the concept phase and written to the game's memory file. For a PC-only game the mobile-specific rules (touch input, the 750 units wide HUD, the phone performance budget) do not have to be followed.

## Repository and workflow

- One game per daily theme. Branch prefix per theme: `<day>-<theme>/` (for example `1-apple/`, `2-relic/`), one feature per branch, e.g. `2-relic/<feature>`.
- `master` holds the shared things (every game branches from it). `<day>-<theme>/main` is the integration branch of a game.
- **`master` is only a source for the games' branches. Nothing is merged into `master`.** A game's work stays on its own branches. The only thing that goes to `master`, and only as a **cherry-pick** of the specific commits, is what every game needs: a change in this `memory.md`, a submodule that other games will use too, or any other change that is required in all the games. Game-specific code, scenes and memory files never go to `master`.
- A change to this `memory.md` reaches `master` either way: as a cherry-pick of its commit, or by fast-forwarding `master` (`git fetch . HEAD:master`) from a branch that holds only memory changes.
- A game's integration branch takes in `master` by **merge** (not rebase), only when there is a reason, for example a change in a shared thing.
- Feature branches are **not deleted** after they are merged. Deployed builds are marked with tags (see "Testing and deploying").
- **Merging is the agent's job**: the user works remotely by chat, so the agent merges finished feature branches into the integration branch (fast-forward with `git fetch . branch:<integration-branch>`). Nothing is pushed anywhere unless asked (the repo has no remote).
- **Development and builds are separate sessions.** The dev session writes features and does not build or deploy. A separate build and deploy session works on its own branch `<day>-<theme>/builds` (for example `2-relic/builds`) and merges the game's `main` into it whenever a new feature lands in `main`. It builds, tags and pushes from there.
- One persistent worktree per agent session, with its own open Unity Editor (avoids a full re-import). The user's main checkout is theirs: do not touch it.
- Commits made by the agent are authored as the agent, never as the system git user (usually `Kosmik`): override per command, `git -c user.name="Claude" -c user.email="noreply@anthropic.com" commit ...`, never change git config. Keep the Co-Authored-By trailer.
- Do not commit unrelated ProjectSettings side effects (for example `runInBackground`, `preloadedAssets` toggled by the Input System during builds). Run In Background is meant to be ON in every project.
- **A game's asmdefs are named `Inktober2026.<Theme>`**, for example `Inktober2026.Apple`. Every subordinate asmdef takes that whole root: `Inktober2026.<Theme>.Editor`, `Inktober2026.<Theme>.Tests.EditMode`. **Namespace = asmdef name**: the code of each asmdef is in the namespace of the same name (also set it as the asmdef's `rootNamespace`).
- `BuildScript`, `FpsDisplay` and `CameraFraming` are shared and live on `master`. **Shared code stays in its own `Inktober2026` asmdefs** (`Inktober2026`, `Inktober2026.Editor`), separate from the game's `Inktober2026.<Theme>` asmdefs: a game adds its own asmdefs and does not rename or put its code into the shared ones.

## Testing and deploying

- The user does the Play Mode testing themselves, usually from a phone. The agent checks that the code compiles and runs the EditMode tests (`unity test --mode EditMode`), and says plainly what is unverified.
- Phone test loop: build WebGL (`Inktober2026.Editor.BuildScript.BuildWebGL`, batch mode; confirm the log has `BuildScript: Succeeded` before pushing, otherwise butler uploads the stale output), then `butler push` to the itch.io page of that game. One itch.io page per game; pushing to a page the user did not name is not allowed, ask first. Do not deploy until the user says so.
- Tag every deployed build with an annotated local git tag `<theme>/<what-this-build-adds>`, and give every butler push a descriptive version label with the same wording (not the bare auto number).
- Do not open two Unity editors on one project, and a batch run cannot start while the project is open elsewhere. Temporary editor scripts for creating prefabs or scenes go in `Assets/TempEditor/Editor/` (outside the project's own asmdefs, which do not reference TMP or the Input System); delete them before committing.
- A git operation that changes the open scene or prefabs makes the agent's editor show a blocking prompt. Save the scene and open an empty scratch scene first, and reopen the game scene after. The scratch-scene script itself can hang on that prompt, so the safest way is to close the agent's editor (`unity close . --force` when a prompt blocks it) before switching branches, and to reopen it after. Start the agent's editor only after the builds are finished (two editors at once made a build take 26 minutes).
- Unity CLI: run `unity` from the project (worktree) root, from another folder it says "isn't a Unity project". `unity test --mode EditMode -o <xml>` needs the agent's editor closed. `unity command script run <file> --timeout N` runs a class with `public static string Run()` in the open editor.
- New scripts, tests and prefabs get their `.meta` files when the editor imports them (also during a test run), so commit the `.meta` files together with the new files.
- Every object added to a scene is a prefab; the scene keeps only reference overrides.

## Mobile WebGL performance

- Post-processing with several full-resolution passes (Bloom) cost about 33 ms per frame on the phone in WebGL (apple game: 20 FPS with it, 60 FPS without). Shadows, HDR, MSAA and render scale did not matter. Tonemapping and Vignette are cheap.
- Before adding a post-processing effect or another full-screen pass, check the FPS counter (`FpsDisplay`, bottom left corner of the HUD) on the phone.
- Do not set `Application.targetFrameRate` in WebGL (it switches the loop from requestAnimationFrame to a timer). WebGL quality level 0 (Mobile) uses `Mobile_RPAsset`.

## Web builds

- In a browser Esc releases the pointer lock and the game gets no key event for it. A game that locks the cursor while playing pauses when the lock is lost (see `GameFlow` of the apple game), so the pause screen still appears.

## Code design

- In games with short play sessions (hypercasual and smaller casual ones) **a restart does not reload the scene**. The same scene stays loaded, and the objects have a mechanism to return to their starting values (state, transforms, physics velocities, spawned objects, UI).
- Flat MonoBehaviour hierarchy: one base component with the implementations as strategies (serialized plain classes), not several MonoBehaviours implementing one interface. Never put an interface in `[RequireComponent]`: any implementer satisfies it, so Unity refuses to remove or swap the current one.
- Logic that does not need Unity is a plain C# class with events; a MonoBehaviour wraps it and forwards the events, and the view builds visuals from those events.

## UI

- Canvas reference resolution is **1000 x 1000** (`CanvasScaler`, Scale With Screen Size, Match 0.5). With this the canvas has a constant area of 1,000,000 units: about 1333 x 750 in landscape and about 750 x 1333 in portrait on a 16:9 screen. Lay the HUD out so that it fits 750 units of width.
- The FPS counter sits in the bottom left corner.
- The EventSystem uses `InputSystemUIInputModule`; the puzzle input ignores a press that starts over the interface.
