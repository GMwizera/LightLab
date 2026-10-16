# Pass the Light

An optics lab you can play on a phone or in a browser. Guide a laser beam with mirrors and glass to light up every student in the class. Each student you reach passes the light on to the next one.

| | |
|---|---|
| **Play in browser** | https://gmwizera.github.io/LightLab/ |
| **Android app** | [Download PassTheLight.apk](https://github.com/GMwizera/LightLab/releases/download/v1.0/PassTheLight.apk) |
| **Video demo** | `<YouTube / Drive link>` |
| **Built with** | Unity 6000.5.10f1, URP, New Input System |

---

## My GCGO and mission

**Grand Challenge:** Education

> "My mission is to empower others to discover their potential, develop their abilities, and become leaders who create meaningful and lasting change in their communities. Through leadership, technology, and service, I strive to open doors of opportunity, inspire growth, and build a future where others are equipped to thrive and empower those who come after them."

## The problem

Many secondary schools teach light and optics without any lab equipment. Students memorise "the angle of incidence equals the angle of reflection" without ever seeing a beam bounce. *(Add a sourced statistic here, for example from MINEDUC or UNESCO.)*

This matters because science sticks when you can try it yourself. Without labs, the students who already have the least fall further behind.

Pass the Light gives them a lab that fits in a pocket. Its main idea also mirrors my mission: once a student is reached, they light the way for the next. Empowered people empower others.

## The simulation

You look down at a lab bench in a classroom. A laser fires a beam. You drag mirrors onto glowing pads and rotate them to steer the light to each student's desk. When the light reaches a student, they light up and show their dream: future engineer, doctor, teacher.

**Who it is for:** students aged about 12 to 16 learning about light, and teachers who want a quick demo to share.

| Level | What you learn | The idea behind it |
|---|---|---|
| 1. First light | Reflection: angle in = angle out | Reaching one learner |
| 2. Around the barrier | Using two mirrors and the normal | Getting past barriers |
| 3. Pass it on | Refraction through glass (Snell's law) | Students who are lit pass the light on |

Each level starts with a short lesson, ends with a quiz question, and gives 1 to 3 stars depending on how few moves you used.

**Controls:** drag a mirror with your mouse or finger. Rotate it with **Q / E** or the round buttons. **Esc** or the pause icon pauses the game.

## How I used the Unity mechanics

**UI.** A single scalable canvas that works on desktop and phone screens. It includes the main menu, a heads-up display (student icons that light up, moves, star target, angle readout), lesson cards, a quiz, and pause and finish screens. Icons replace text wherever possible.

**Scripting.** Each frame follows the same flow:
1. `PointerInteractor` reads the mouse or finger and moves or rotates the selected mirror.
2. `LaserEmitter` traces the beam: it bounces off mirrors, bends through glass, and stops at a desk.
3. `StudentDesk` lights up if the beam touched it, and turns on its own relay beam.
4. `GameManager` notices when every student is lit, celebrates, and shows the quiz.
5. `UIManager` updates the screen.

**Collision.** Mirrors are Rigidbodies. The pads are trigger colliders: when a mirror is over a pad, the pad glows, and when you drop the mirror it snaps to the centre (`OnTriggerEnter`). When a mirror lands on the bench, `OnCollisionEnter` plays a thud.

**Raycasting.** The beam itself is a chain of raycasts. When a ray hits a mirror, `Vector3.Reflect` gives the new direction. When it hits glass, Snell's law bends it. A second raycast from the camera finds the mirror under your finger.

**Line Renderer.** Every point where the beam hits something becomes a point on a Line Renderer, so you see the exact path of the light. The laser is red, and the light passed on by students is gold. A second Line Renderer draws the gold ring around the selected mirror.

**New Input System.** One action map (`LabControls`) with four actions: Point, Select, Rotate and Pause. They use `<Pointer>` bindings, so the same code works with a mouse and a touchscreen. The EventSystem uses `InputSystemUIInputModule`, and the old Input Manager is switched off.

## Extra features

- **Sound and music:** background music with a mute button, plus sounds for every action
- **Particles:** sparkles when a student lights up, and confetti when the whole class is lit
- **Saved progress:** unlocked levels and best stars are kept between sessions (PlayerPrefs)
- **Glow effects:** URP bloom makes the beams and lit students glow
- **Animation:** pop-in cards, bouncing buttons, jumping students, and a tutorial arrow
- **Quiz and stars:** one question per level, a star rating, and locked levels
- **Custom web page:** a full-screen loading page instead of Unity's default frame

## Builds

- **WebGL:** https://gmwizera.github.io/LightLab/ (served from the `docs/` folder)
- **Android:** [v1.0 release](https://github.com/GMwizera/LightLab/releases/tag/v1.0). Download the APK on your Android phone, allow "Install unknown apps", then open it.

**Run it in Unity:** install Unity 6000.5.10f1 with the WebGL and Android modules, open the project, open `Assets/_Project/Scenes/Lab.unity`, and press Play.

## Reflection

- **The beam hit the same mirror twice.** Each new ray started exactly on the mirror's surface, so it hit that mirror again straight away. Starting each new ray a tiny distance away fixed it.
- **Finding where light leaves the glass.** A ray that starts inside a collider cannot detect it, so I cast a ray backwards from outside the block to find the exit point.
- **Android showed only half the screen.** Switching the graphics from Vulkan to OpenGL ES 3 fixed it on my phone.
- **With more time** I would add more levels (prisms and colour filters), a Kinyarwanda version, and a mode where teachers build their own levels.

## Credits

3D models, icons and font by [Kenney](https://kenney.nl) (CC0). Made by Gisele Mwizera, African Leadership University.
