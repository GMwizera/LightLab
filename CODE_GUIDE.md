# Code guide

How the scripts in `Assets/_Project/Scripts` fit together, in plain words.

## How one frame works

1. `PointerInteractor` reads the mouse or finger (New Input System) and moves or rotates the selected mirror.
2. `LaserEmitter` fires its beam: raycast, bounce off mirrors, bend through glass, stop at a desk.
3. `StudentDesk` checks if a beam touched it this frame. If yes, the student lights up and its own `LaserEmitter` (the relay beam) turns on.
4. `GameManager` hears that a student changed. When every student is lit, it celebrates and shows the quiz.
5. `UIManager` updates the screen: student icons, moves, angle readout.

## Core

| Script | What it does |
|---|---|
| `GameManager` | Runs the game: menu, start a level, count moves, pause, quiz, finish. It is the only script that changes the game state. |
| `LevelRoot` | One per level. Holds the level's title, lesson, quiz and star target, and resets the level's mirrors and desks. |
| `ProgressStore` | Saves unlocked levels, best moves and stars with `PlayerPrefs`. |
| `Sfx` | Plays sounds and background music. Any script can call `Sfx.Click()`, `Sfx.DeskLit()` and so on. |
| `CameraBump` | Quick camera zoom when the whole class is lit. |

## Optics

| Script | What it does |
|---|---|
| `LaserEmitter` | Fires a ray from the laser. Mirror: reflect with `Vector3.Reflect`. Glass: ask `GlassBlock` to bend it. Desk: light it up. Each hit point is added to the Line Renderer. |
| `Mirror` | Marks an object as a mirror and remembers the angle of incidence for the readout. |
| `GlassBlock` | Bends the beam into and out of the glass with Snell's law. `TryBend` does the maths; `TryFindExit` finds where the beam leaves the block. |
| `StudentDesk` | A student. Dark until a beam hits the desk, then: full colour, glow, sparkles, a jump, and its relay beam turns on. The gold arrows show where the relay beam will go. |
| `BeamEffects` | Makes a beam shimmer and puts a glowing dot at every point where it bounces. |

## Interaction

| Script | What it does |
|---|---|
| `PointerInteractor` | Uses the `LabControls` input actions. Raycasts from the camera to pick up a mirror, drags it across the bench, drops it, and rotates it with Q/E or the buttons. |
| `Grabbable` | A piece that can be picked up. Uses a Rigidbody, snaps to a pad when dropped on one (trigger), and plays a sound when it lands (collision). |
| `PlacementSlot` | A pad on the bench. Its collider is a trigger. It glows and pulses while a mirror is over it. |
| `SelectionRing` | A gold Line Renderer circle around the selected mirror. |

## UI

| Script | What it does |
|---|---|
| `UIManager` | Shows the right screen (menu, HUD, lesson, quiz, level complete, pause, finish) and keeps the HUD up to date. |
| `ButtonFeedback` | Buttons shrink when pressed, grow on hover, and click. |
| `PopIn` | Cards pop in with a small bounce when they appear. |

## Editor tool

`Editor/ProjectBuilder.cs` is an editor-only tool. It generated the scene, prefabs, materials, sounds and UI, and it runs the WebGL and Android builds. It is not part of the game itself.
