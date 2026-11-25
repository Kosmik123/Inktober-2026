# Apple Peeler (day 1): game memory

Specific to the Apple game. Shared knowledge is in `memory.md`.

## Branches and page

- Branch prefix `1-apple/`, one feature per branch. The integration branch is `1-apple/develop` (older work went to `1-apple/main`, which stopped at `558384c`). Finished branches are fast-forwarded into it.
- itch.io test page: `kosmik123/inktober-2026-apple`, channel `web` (renamed from `inktober-2026` on 2026-11-21). Orientation and frame size are set on the itch.io page, not in the template.
- Builds and deploys go only through the session "Gra Inktober 2026 Apple Builds" (worktree `.claude/worktrees/builds-web`, branch `1-apple/builds-web`, fast-forwarded to the requested branch), and only when the user says so. Tags so far end with `apple/restart-and-cursor-pause`.

## Game rules

- A procedural apple (radius 0.5 units, 1 unit is about 10 cm) sits at the origin and spins under a stationary knife. The peel is painted into an R8 mask. The goal is the longest peel (the score). The only loss is the trail crossing itself, or the knife cutting between two older lines (`PeelEnclosure`).
- Game over shows the score and the best score (`BestScore`, stored in PlayerPrefs). States in `GameFlow`: Ready, Playing, Paused, GameOver.
- Controls (`Steer` action, a Vector2): the first axis turns the path. The second axis is a throttle lever from -1 to 1 that stays where it was left: forward gives a faster spin (x1.8) and a wider peel (x1.6), back gives slower (x0.4) and narrower (x0.5). The multipliers, the range of the axis (60) and the return rate (0) are fields of `KnifeController`. The width is read through `PeelTrail.Width`.
- Esc pauses. In a browser Esc does not reach the game, so `GameFlow` pauses when the cursor lock is lost. The pause shows Resume and Restart (`GameFlowButton` and `RestartButton`); Restart reloads the scene.

## Strip (the peeled skin)

- Pipeline: `PeelWidthProfile` (a section every 0.03 units) goes to an `IPeelStripPath` (nodes) and to `PeelStripMesh` (geometry in `PeelStripGeometry`, two submeshes: skin and flesh).
- The strip is moved so that its pivot is at the newest section and rotated by `-angle` around the newest node's up. Lesson: rotate positions about the pivot (subtract the pivot, multiply by the rotation), but only multiply direction vectors by the rotation, and rotate by the opposite angle to undo a heading.
- The design with `[RequireComponent(typeof(IPeelStripPath))]` stays for now (the user decided). It means the path component cannot be removed in the editor; the prefab had to be edited by hand once.
- **Known bug, not fixed:** the UVs break for good when the knife centre crosses a pole of the apple, because the left and right edges are unwrapped independently (`Mathf.Round` per edge) and land 1 - 2 epsilon apart in U. Proposed fix: unwrap only the left edge against the history, then `delta = right - left; delta -= Round(delta); right = left + delta`, with a test through the pole. The user may instead add a lock that keeps the knife from crossing a pole, as on a real apple.

## Physics of the strip (paused)

- Branch `1-apple/peel-strip-physics` (`427aab9`, based on an older develop, not merged, not deployed): `PeelChain` is a Verlet chain with the head pinned to the rest curve, gravity pushed a bit away from the apple, damping, a pull to the rest curve and a sphere (centre and radius of the apple) that pushes points out; `PhysicalPeelStripPath` runs it in a fixed step of 1/120 s; `PeelRestCurve` is the old curved path. 13 tests.
- The user paused it on 2026-11-21: they saw that something was off and thinks damping is needed. They will iterate on it by hand before it is merged. The `apple` field of the path is still empty. Not checked on the phone: up to 4000 sections are rebuilt per frame.

## Ready spin

- In the Ready state `ReadySpin` (on the Knife prefab) turns the apple slowly at random (`Tumble`: an axis and a speed of 8..22 deg/s that drift to new random ones every 3..7 s). The knife stays where the scene put it and does not touch the apple. `KnifeController` initializes itself on its first update, that is when the round starts: it lays the walker on the apple under the knife and the look of the knife eases down from the scene pose. The round starts from the pose the apple has when Start is pressed, so the player picks the starting point. The size used for the speed is measured at the start rotation of the apple, so the turn does not change the speed. Not tried in Play Mode yet.

## Peel sound

- `ContinuousAudio` (generic, knows nothing about the game: `Play`, `Pause`, `Stop`) plays `apple_peel_long.wav` (a few seconds, not a loop) without end; `GameFlowAudio` is only the bridge that subscribes to `GameFlow` (Playing plays, Paused pauses, the rest stops). Both are on the prefab `Peel Sound`, in the scene. Voices overlap: the next one starts a random min..max seconds before the previous ends and crossfades with it (equal power, `LoopCrossfade`) over a random time between `minCrossfadeDuration` and `maxCrossfadeDuration`, drawn first and then clamped to the overlap; the old voice is released when silent. Voices come from an `ObjectPool` of `maxVoices`. Pitch of each voice wanders around `pitchCenter` by `pitchSpread` (a fraction) along 2D Perlin noise: time moves along Y, `Randomness` (runtime property) moves along X. `PitchMultiplier` (runtime property) scales the mean at once. `KnifeAudio` is the second bridge: pitch multiplier = knife `SpeedScale` ^ `speedToPitch`, Randomness = |turn rate| / max turn rate, both smoothed. The signed turn also drives `Pan` (stereo, `panAmount`, negative inverts) and `Muffle` (low-pass cutoff from `openCutoff` to `mutedCutoff`, `muffleAmount`); every voice is a child object with its own `AudioLowPassFilter`. Pause holds the sound, stop drops the voices. Not yet heard on a device; the values are fields on the prefab.

## Open items

- Sound and music: calm ASMR style, short realistic sounds (peel, cut, UI), one quiet melody that the user will find; the user may record the apple sounds or take CC0 ones, UI sounds can be generated in Python. Audio events come later.
- A round cap at the back of the brush (optional), a hint text for the touch start, a difficulty ramp, removing `CameraFollow`, cleaning up old branches.
- The pointers of the submodules `Assets/Bipolar/Procedural-Mesh` and `Touch-Sticks` show as modified; the user pushes and updates them, do not commit them.
