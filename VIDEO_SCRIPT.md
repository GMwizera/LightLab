# Video demo script (target 7 to 8 minutes)

Record your screen with QuickTime (File > New Screen Recording) or OBS. Talk naturally; the lines below are a guide, not something to read word for word.

## 1. Intro and GCGO (0:00 to 1:00)
- Show the main menu of the WebGL build.
- "Hi, I'm Gisele. My GCGO is Education." Read your mission statement, or summarise it: helping others discover their potential and empowering those who come after them.
- "Pass the Light is an optics lab simulation. Every student you reach lights the way for the next."

## 2. The problem (1:00 to 1:45)
- Many schools teach light and optics without lab equipment, so students memorise rules like "i = r" without ever seeing them.
- This simulation lets students experiment on a phone or in a browser.
- The relay mechanic is my mission in game form: a learner who is empowered passes it on.

## 3. New Input System setup (1:45 to 3:00)
- In Unity, open `Assets/_Project/Input/LabControls`.
- Show the Lab action map: Point (`<Pointer>/position`), Select (`<Pointer>/press`), Rotate (Q/E 1D axis), Pause (Escape).
- Explain: `<Pointer>` means one binding works for both the mouse and touch.
- Show **Generate C# Class** ticked and the `LabControls.cs` file.
- Show Project Settings > Player > Active Input Handling = **Input System Package (New)**.
- Show the EventSystem object using **InputSystemUIInputModule**.

## 4. Play the simulation (3:00 to 5:30)
- Level 1: drag the mirror onto the pad in the beam, then press E three times (45°). Point out the angle readout i = r. The student lights up, with sparkles and a sound. Answer the quiz.
- Level 2: use two mirrors to get around the wall.
- Level 3: show the beam bending through the glass block. Light the first student and watch the gold relay beams pass the light to the others.
- Show the Pause menu and Restart.

## 5. Unity mechanics (5:30 to 7:00)
Open each script briefly:
- **Raycast + Line Renderer:** `LaserEmitter.cs`. Explain the loop you wrote: `Physics.Raycast`, `points.Add`, `Vector3.Reflect`, and why `SurfaceOffset` is needed.
- **Refraction:** `GlassBlock.cs`, Snell's law.
- **Collision and triggers:** `Grabbable.cs` `OnTriggerEnter` for the snap pads, `OnCollisionEnter` for the drop sound. `PlacementSlot` has "Is Trigger" ticked.
- **Camera raycast:** `PointerInteractor.TryGrab`.
- **UI:** `UIManager.cs` and the Canvas hierarchy.
- **Prefabs:** the Prefabs folder; editing one prefab updates every level.

## 6. Extra features and builds (7:00 to 8:00)
- Audio (generated in code), particles, saved progress (show "Best: x moves" on the menu after replaying), the student animation and glow, and the quiz system.
- Show the Android APK running on a phone (or an emulator), and the WebGL link.
- Close: "With more time, I would add more levels and a Kinyarwanda translation."
