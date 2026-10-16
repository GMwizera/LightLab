# Pass the Light

A Unity optics simulation where every student you reach lights the way for the next.

| | |
|---|---|
| WebGL (play in browser) | `https://gmwizera.github.io/<REPO-NAME>/` (replace after enabling GitHub Pages) |
| Android APK | `<GitHub Release link to PassTheLight.apk>` |
| Video demo | `<YouTube / Drive link>` |
| Unity version | 6000.5.10f1 (URP), New Input System only |

---

## GCGO statement

**Grand Challenge:** Education

**Mission:**

> "My mission is to empower others to discover their potential, develop their abilities, and become leaders who create meaningful and lasting change in their communities. Through leadership, technology, and service, I strive to open doors of opportunity, inspire growth, and build a future where others are equipped to thrive and empower those who come after them, all while reflecting God's love in everything I do."

## Problem context

**The problem.** Many secondary schools teach optics (reflection and refraction) without lab equipment. Students learn rules such as "the angle of incidence equals the angle of reflection" from a textbook, without ever seeing a beam of light bounce or bend. *(Add a sourced statistic here, for example from MINEDUC or UNESCO, on the share of schools without science labs.)*

**Why it matters.** Science is easier to understand and remember when students can experiment and see what happens. Without labs, students who are already disadvantaged fall further behind, and fewer of them choose science careers.

**Connection to my GCGO.** Pass the Light uses technology to give students a lab they can open on a phone or in a browser. Its main mechanic reflects my mission: a student who receives the light becomes a source of light for the next student, just as people who are empowered go on to empower those who come after them.

## Simulation overview

**What it does.** The player looks down on a classroom lab bench. A laser fires a beam of light. The player picks up mirrors, places them on glowing pads and rotates them to guide the beam to students' desks. When a student is reached, they light up in full colour and show their dream ("Future engineer", "Future doctor"...). In the last level, students who are lit fire their own gold beam and pass the light to the next student.

**Target users.** Lower secondary students (about 12 to 16) learning about light, and teachers who want a quick demonstration they can project or share as a link.

**Levels**

| # | Level | Physics taught | Mission idea |
|---|---|---|---|
| 1 | First light | Reflection, i = r | Reaching one learner |
| 2 | Around the barrier | Using several mirrors, the normal | Getting past barriers that keep learners out |
| 3 | Pass it on | Refraction through glass (Snell's law) | Lit students pass the light on; gold arrows show which way each one will shine |

Each level opens with a short lesson card, ends with a quiz question, and awards 1 to 3 stars based on how few moves you used. A correct answer unlocks the next level.

**Key interactions**

| Action | Mouse / keyboard | Touch (Android) |
|---|---|---|
| Pick up and move a mirror | Click and drag | Touch and drag |
| Select a mirror | Click it | Tap it |
| Rotate the selected mirror (15° steps) | Q / E | On-screen Rotate buttons |
| Pause | Esc or the pause icon | Pause icon |
| Open the lesson again | **?** button | **?** button |
| Music on/off | Music icon | Music icon |
| Answer the quiz and use the menus | Click | Tap |

## Unity mechanics implemented

### UI
Built with Unity UI (uGUI) on one canvas with a `CanvasScaler` set to scale with screen size, so it fits both desktop and phone screens.
The UI uses icons rather than text wherever possible (Kenney Game Icons).
- **Main menu:** numbered level cards with the best star rating, padlocks on locked levels, three icon steps for how to play, a music toggle, and Reset progress.
- **HUD:** home, level progress dots, a row of **student icons that light up** as each student is reached, a move counter, music and pause icons, a one-line tip with a **?** button for the full lesson, round rotate buttons (labelled Q and E), and a live angle readout (`i 45°  r 45°`) shown only while a mirror is selected.
- **Lesson card, quiz, Level complete, Pause and Finish screens.**
- **Level 1 tutorial arrow** that bounces between the mirror and the target pad until the first move.
- **World-space labels** above each desk that change from "Waiting for light" to the student's dream.

`UIManager.cs` switches between screens and updates them from the game state. `ButtonFeedback.cs` and `PopIn.cs` add the animations.

### Scripting
See [CODE_GUIDE.md](CODE_GUIDE.md) for a plain-English walk-through of how the scripts work together.

| Script | Purpose |
|---|---|
| `Core/GameManager.cs` | Game flow and state (Menu, Playing, Paused, Quiz, LevelComplete, Finished), moves, pausing |
| `Core/LevelRoot.cs` | A level's lesson and quiz data; finds its desks and movable pieces; resets the level |
| `Core/ProgressStore.cs` | Saves unlocked levels and best scores (PlayerPrefs) |
| `Core/Sfx.cs` | Sound effects generated in code |
| `Optics/LaserEmitter.cs` | Traces the beam with raycasts and draws it with a Line Renderer |
| `Optics/Mirror.cs` | Marks a mirror and records the angle of incidence |
| `Optics/GlassBlock.cs` | Refraction with Snell's law, including total internal reflection |
| `Optics/StudentDesk.cs` | Lights up when the beam hits it; turns on its relay beam |
| `Interaction/PointerInteractor.cs` | New Input System controls and camera raycasts for picking, dragging, dropping and rotating |
| `Interaction/Grabbable.cs` | Picking up and dropping with a Rigidbody; snapping to slots; collision sounds |
| `Interaction/PlacementSlot.cs` | Glowing trigger pads that pieces snap to |
| `UI/UIManager.cs` | All screens and HUD updates, star reveal, quiz feedback, tutorial arrow |
| `UI/ButtonFeedback.cs` | Buttons squash when pressed, grow on hover, and click |
| `UI/PopIn.cs` | Cards pop in with a small overshoot and fade |
| `Optics/BeamEffects.cs` | Beam shimmer and glow points where the beam bounces or ends |
| `Core/CameraBump.cs` | Short camera zoom when the class is lit |
| `Editor/ProjectBuilder.cs` | Editor tool that generates materials, prefabs, levels and UI, and runs the builds |

### Collision
- **Triggers:** each `PlacementSlot` has a trigger collider. `Grabbable.OnTriggerEnter` and `OnTriggerExit` highlight the pad the mirror is over, and when the mirror is dropped it snaps to that pad's centre.
- **Collisions:** mirrors have a `Rigidbody`. While held they are kinematic. When dropped, physics lets them fall onto the bench, and `OnCollisionEnter` plays a thud based on the impact speed.
- Walls, desks, the laser body and the glass block use solid colliders, so the beam's raycasts can hit them.

### Raycasting
1. **The light beam** (`LaserEmitter.TraceBeam`). A loop calls `Physics.Raycast` from the laser along its forward direction:
   - If it hits a `Mirror`, the direction becomes `Vector3.Reflect(direction, hit.normal)`, and a new ray starts just off the surface.
   - If it hits a `GlassBlock`, the beam bends on entry and exit using the vector form of Snell's law, `n1 sin i = n2 sin r`. A ray that starts inside a collider can't hit it, so the exit point is found by casting backwards from beyond the block.
   - If it hits a `StudentDesk`, the desk lights up. Anything else absorbs the beam.
2. **Picking pieces** (`PointerInteractor`). `Camera.ScreenPointToRay` plus `Physics.Raycast` finds the mirror under the pointer. A ray against a flat `Plane` at carry height moves the mirror while you drag.
3. **UI blocking.** `EventSystem.RaycastAll` checks whether the pointer is over a button, so a tap on a button doesn't also grab the mirror behind it.

### Line Renderer
Each beam is a `LineRenderer` in world space. Every raycast hit adds a point, so the line shows the beam's exact path: straight lines, bounces and bends. The laser's beam is red. The relay beams that lit students fire are gold, to show light that has been passed on. The beam materials use HDR colours, so bloom makes them glow.

A second `LineRenderer` (`SelectionRing.cs`) draws a pulsing gold ring around the selected mirror, so players can see which piece the rotate buttons will turn.

### New Input System
- `Assets/_Project/Input/LabControls.inputactions` holds one action map, **Lab**:
  - `Point`: Value, Vector2, `<Pointer>/position`
  - `Select`: Button, `<Pointer>/press`
  - `Rotate`: Value, Axis, 1D composite with Q as negative and E as positive
  - `Pause`: Button, `<Keyboard>/escape`
- `<Pointer>` covers both the mouse and the touchscreen, so the same code runs on WebGL and Android.
- The C# class `LabControls.cs` is generated from the asset and used by `PointerInteractor`.
- The EventSystem uses `InputSystemUIInputModule`.
- **Active Input Handling** is set to *Input System Package (New)* only. No `UnityEngine.Input` calls are used anywhere.

## Additional features (beyond the module)

1. **Audio system.** Background music (a looping arpeggio) with a mute toggle that is remembered, plus sound effects for every button, picking up, dropping, a desk lighting up, the class being lit, each star, and right or wrong answers. All the clips are synthesised by the editor tool from sine waves and saved as WAV files, so no audio was downloaded.
2. **Particle systems.** Gold sparkles when a student is reached, and a burst of confetti when the whole class is lit.
3. **Data persistence.** `PlayerPrefs` saves unlocked levels and best move counts per level. This works on both WebGL (browser storage) and Android.
4. **Procedural animation and lighting.** Students "jump" with a short scale animation, change from grey to full colour, and a point light glows above their desk.
5. **Physics-based refraction.** Snell's law with total internal reflection, plus a live angle readout so students can check that i = r.
6. **Mobile support.** On-screen rotate buttons, landscape orientation, the mobile URP quality asset, and a 60 FPS target.
7. **Quiz, stars and level progression.** One question per level (a wrong answer shakes the card and shows ✗, a right answer turns green with ✓), a 1 to 3 star rating per level, and locked levels.
8. **UI animation ("game feel").** Pop-in cards, squashy buttons, a pulsing drop pad, mirrors that grow slightly when lifted, a shimmering beam with glowing bounce points, and a camera bump on success.
9. **Post-processing (URP Volume).** Bloom makes the beams, mirror caps and lit students glow; a vignette and colour grading give the classroom a warm look.
10. **Custom WebGL page.** A full-window page with a branded "Pass the Light" loading screen and progress bar (`Assets/WebGLTemplates/PassTheLight`), instead of Unity's default frame.
11. **Clarity aids.** A bobbing "!" over every student still waiting for light, a goal line on each lesson card, a visible star target ("★★★ in 4 moves"), rotate buttons that only light up when a mirror is selected, and a wood grid on the bench for lining pieces up.

## Project structure

```
Assets/_Project/
  Art/Kenney/      Furniture Kit, Mini Characters and Game Icons (CC0)
  Art/UI/          Generated circle sprite, bench grid and floor textures
  Settings/        Post-processing volume profile
  Audio/           Generated WAV sound effects and music
  Editor/          ProjectBuilder (generates content, runs builds)
  Input/           LabControls.inputactions + generated LabControls.cs
  Materials/       URP materials
  Prefabs/         Laser, RelayBeam, Mirror, GlassBlock, StudentDesk, Wall, PlacementSlot
  Scenes/          Lab.unity
  Scripts/Core | Interaction | Optics | UI
```

**Prefabs** are used for every repeated object. Changing the `Mirror` prefab, for example, updates every mirror in all three levels.

## Build information

**WebGL:** `<GitHub Pages link>`. The build is in the `docs/` folder. Compression is disabled so it runs on GitHub Pages without extra server setup.

**Android APK:** `<GitHub Release link>` (IL2CPP, ARM64, landscape).

**Run from source**
1. Install Unity **6000.5.10f1** with the WebGL and Android build modules.
2. Open the project folder in Unity Hub.
3. Open `Assets/_Project/Scenes/Lab.unity` and press Play.
4. To build: **File > Build Profiles**, choose WebGL or Android, and click Build. Or from a terminal:
   ```
   Unity -batchmode -quit -projectPath . -buildTarget WebGL -executeMethod PassTheLight.EditorTools.ProjectBuilder.BuildWebGL
   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod PassTheLight.EditorTools.ProjectBuilder.BuildAndroid
   ```

**Install the APK:** download it on an Android phone, allow "Install unknown apps" for your browser or file manager, then open the file.

## Reflection

- **Challenge:** the first beam ray kept hitting the mirror it had just bounced off, because the new ray started exactly on the mirror's surface. Starting the next ray a tiny distance (`SurfaceOffset`) along the new direction fixed it.
- **Challenge:** a ray that starts inside the glass block can't detect the block's far side. I solved this by casting a ray backwards from outside the block to find the exit point.
- **Cross-platform:** using `<Pointer>` bindings plus on-screen rotate buttons meant one input setup for both mouse and touch. WebGL compression was disabled so GitHub Pages can serve the build.
- **With more time:** more levels (prisms, coloured filters), a Kinyarwanda translation, and a teacher mode for building custom levels.

## Credits

- 3D models, icons and the title font: [Kenney](https://kenney.nl) Furniture Kit, Mini Characters, Game Icons and Kenney Fonts (CC0 1.0).
- Developed by Gisele Mwizera, African Leadership University.
