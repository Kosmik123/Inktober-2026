# Apple Peeler (day 1): game memory

Specific to the Apple game. Shared knowledge is in `memory.md`.

## Branches and page

- Branch prefix `1-apple/`, one feature per branch. The integration branch is `1-apple/develop` (older work went to `1-apple/main`, which stopped at `558384c`). Finished branches are fast-forwarded into it.
- itch.io test page: `kosmik123/inktober-2026-apple`, channel `web` (renamed from `inktober-2026` on 2026-11-21). Orientation and frame size are set on the itch.io page, not in the template.
- Builds and deploys go only through the session "Inktober Apple (Builds)" (it manages its own build worktree), never from the development session. The development session asks it for a deploy when the user says so. The user approved in advance that it builds, pushes to this page and tags when the development session asks. Older tags end with `apple/restart-and-cursor-pause`.

## Game rules

- A procedural apple (radius 0.5 units, 1 unit is about 10 cm) sits at the origin and spins under a stationary knife. The peel is painted into an R8 mask. The goal is the longest peel (the score). The only loss is the trail crossing itself (centre lines only, width ignored on purpose, confirmed by the user), or the knife cutting between two older lines (`PeelEnclosure`): every `PeelPoint` keeps the width it was peeled with, and the knife is enclosed when older strips, each with its own width, cover the whole span of the newest one and there is such a line on both sides.
- Game over shows the score and the best score (`BestScore`, stored in PlayerPrefs). States in `GameFlow`: Ready, Playing, Paused, GameOver.
- Controls (`Steer` action, a Vector2): the first axis turns the path. The second axis is a throttle lever from -1 to 1 that stays where it was left: forward gives a faster spin (x1.8) and a wider peel (x1.6), back gives slower (x0.4) and narrower (x0.5). The multipliers, the range of the axis (60) and the return rate (0) are fields of `KnifeController`. The width is read through `PeelTrail.Width`.
- Esc pauses. In a browser Esc does not reach the game, so `GameFlow` pauses when the cursor lock is lost. The pause shows Resume and Restart (`GameFlowButton` and `RestartButton`); Restart reloads the scene.

## Peel particles (replace the strip)

- On 2026-11-25 the user replaced the peeled skin strip with particles, because the strip peeled in an ugly way. `KnifeParticles` (a `ParticleSystem` on the `Particles` child of the Knife prefab) follows `GameFlow` (Playing plays, Paused pauses, the rest stops), sets the width of the emitter shape to `PeelTrail.Width` and the emission rate to `baseEmissionRate` times the knife `SpeedScale`. The Table prefab has a `Destroy Particle Trigger`.
- The `Peel Strip` object is still in the scene, switched off. Its prefab, the scripts in `Peel/Strip`, their tests and the branch `1-apple/peel-strip-physics` are not removed; ask the user before removing them. The old UV bug at the poles and the strip physics are dropped with it.

## Ready spin

- In the Ready state `ReadySpin` (on the Knife prefab) turns the apple slowly at random (`Tumble`: an axis and a speed of 8..22 deg/s that drift to new random ones every 3..7 s). The knife stays where the scene put it and does not touch the apple. `KnifeController` initializes itself on its first update, that is when the round starts: it lays the walker on the apple under the knife and the look of the knife eases down from the scene pose. The round starts from the pose the apple has when Start is pressed, so the player picks the starting point. The size used for the speed is measured at the start rotation of the apple, so the turn does not change the speed. Not tried in Play Mode yet.

## Peel sound

- `ContinuousAudio` (generic, knows nothing about the game: `Play`, `Pause`, `Stop`) plays `apple_peel_long.wav` (a few seconds, not a loop) without end; `GameFlowAudio` is only the bridge that subscribes to `GameFlow` (Playing plays, Paused pauses, the rest stops). Both are on the prefab `Peel Sound`, in the scene. Voices overlap: the next one starts a random min..max seconds before the previous ends and crossfades with it (equal power, `LoopCrossfade`) over a random time between `minCrossfadeDuration` and `maxCrossfadeDuration`, drawn first and then clamped to the overlap; the old voice is released when silent. Voices come from an `ObjectPool` of `maxVoices`. Pitch of each voice wanders around `pitchCenter` by `pitchSpread` (a fraction) along 2D Perlin noise: time moves along Y, `Randomness` (runtime property) moves along X. `PitchMultiplier` (runtime property) scales the mean at once. `KnifeAudio` is the second bridge: pitch multiplier = knife `SpeedScale` ^ `speedToPitch`, Randomness = |turn rate| / max turn rate, both smoothed. The signed turn also drives `Pan` (stereo, `panAmount`, negative inverts) and `Muffle` (low-pass cutoff from `openCutoff` to `mutedCutoff`, `muffleAmount`); every voice is a child object with its own `AudioLowPassFilter`. Pause holds the sound, stop drops the voices. The user tried it in the editor; not yet checked on a phone (low-pass and pan in WebGL included). The output goes to the audio mixer; the values are fields on the prefab.

## Music and restart

- The music is in the `Boot` scene and should live as a singleton (the user's own work). `GameFlow.Restart` reloads the scene; on 2026-11-25 the user decided not to use the `Scenes-Loading` module for it. `LoadingManager.LoadContext` does not reload scenes that are already loaded.

## Procedural meshes

- `DoubleSidedQuadMesh` (a `ProceduralMesh` of the `Procedural-Mesh` module, submesh 0 front, 1 back) with the asset in `Assets/_Project/Meshes`.

## Open items

- Sound: calm ASMR style, short realistic sounds. The peel sound and the music are in; the cut (game over) and UI sounds are not. The user may record sounds or take CC0 ones, UI sounds can be generated in Python.
- A hint text for the touch start, a difficulty ramp (discuss first), a round cap at the back of the brush (optional), removing `CameraFollow`, cleaning up old branches.
- Check on a phone: FPS with the particles, the sound filters, the direction of the pan, the speed of the knife easing down after Start.
- The pointers of the submodules (`Assets/Bipolar/...`) may show as modified; the user pushes and updates them, do not commit them.
