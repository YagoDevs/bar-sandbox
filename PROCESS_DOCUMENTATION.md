## Yago Phellipe Matos Lopes — 588542

## Project Documentation (Process + Sandbox Rationale)

### Project overview (what this is)
This project is a small **bar-themed sandbox prototype** built in **Unity 2022.3 LTS** with the **ACDH Sandbox Framework**. The goal is not a polished game, but a **technical prototype of interconnected systems** (physics + state + triggers + timed processes + feedback) that support **playful experimentation** and **emergent outcomes**.

The core loop is:
- The customer requests a drink on the order sign.
- The player chooses a container, prepares the correct drink using machines, and delivers it.
- The delivery zone validates the drink state, the customer reacts (happy/sad), the container is removed/reset, and a new request is generated.

---

### Design intention (player experience)
The intended player experience is **curiosity + controlled chaos**:
- The player is encouraged to **try combinations** (different containers, stations, drink variants).
- The world is physical and “toy-like”: objects are grabbed and moved, not selected from menus.
- Progress is readable through **clear visual states** and **sound cues**.
- Failure is not a dead-end: the wash station enables quick recovery and iteration.

---

## How the sandbox is built (systems mindset)

### Key idea: “state is visible”
Instead of hidden variables, the most important object states are represented by **visible variants** (enabled/disabled child objects and materials). This makes the sandbox readable, teachable, and easy to playtest.

### Key idea: “systems are modular, not scripted sequences”
The experience is produced by modular building blocks:
- **Triggers** decide when actions are possible (socket gating, delivery validation).
- **Timers** create machine-like processing delays (water/flavor/mix, wash).
- **State machines** define what an object currently “is” (empty, beer, soda lemon, etc.).
- **Audio feedback** communicates process, success, and failure.

---

## Development process (how it evolved)

### Phase 1 — Establishing the core framework loop
1. Imported the ACDH Sandbox Framework and validated baseline functionality.
2. Copied the player/controller setup from a working example scene to ensure:
   - selection highlighting,
   - dragging/grabbing,
   - UI item description display,
   - and input routing
   worked identically in the custom scene.
3. Built a minimal bar environment with:
   - counter / table,
   - customer placement,
   - and space for stations.

**Outcome:** A stable interaction foundation: pick objects, move them, click on interactable parts, and see descriptions.

---

### Phase 2 — Building the central object: the container + visual states
The container is the “carrier” of drink state. The design requirement was: **no hidden state logic**, only **visible state changes**.

Steps:
1. Created the container as a root object with physics + interaction components.
2. Added child objects representing different drink states (visual variants).
3. Used the framework’s `StateMachine` to:
   - define named states (e.g., `Empty`, `Beer`, `Soda_Lemon`, etc.),
   - and toggle the correct visuals in each state via `OnStart` events.

**Outcome:** A single physical object with multiple visible states controlled by event-driven transitions.

---

### Phase 3 — Stations: gating + timed processes (machine feel)
Stations were designed as **physical interaction points** that:
- only allow correct actions at the correct time,
- and simulate “processing” with delay + sound.

Implementation pattern used repeatedly:
- A **Socket Trigger** detects when a container is placed.
- A **ConditionalTrigger** checks requirements (tag + state) and enables the correct button(s).
- A **TimerTrigger** drives a short process (start sound + set intermediate state + set final state).

This pattern was applied to:
- Beer machine (simple transform: Empty → Beer)
- Soda machine (multi-step transform: Water → Flavor → Mix, with 3 soda variants)
- Wash station (reset any prepared container back to Empty)

**Outcome:** Stations feel mechanical and readable, while remaining fully modular.

---

### Phase 4 — Expanding drink variety (3 soda flavors)
To satisfy “variations” and improve sandbox combinatorics, soda was expanded into three distinct outcomes:
- `Soda_Lemon`
- `Soda_Orange`
- `Soda_Passion`

The player selects flavor via distinct buttons. The final mix step produces the correct final state using the selected flavor.

**Outcome:** A single station supports multiple meaningful outputs via consistent rules (multi-step process + flavor choice).

---

### Phase 5 — Orders, delivery validation, and feedback (complete loop)
The delivery zone validates the container state against the current order. The system is:
- **event-driven** (no polling UI logic),
- **readable** (state names match the order text),
- and **recoverable** (wash station).

The customer has simple emotional feedback states:
- Neutral
- Happy (correct delivery)
- Sad (wrong delivery)

**Outcome:** End-to-end gameplay loop exists: request → prepare → validate → react → next request.

---

## Object categories and variations (sandbox requirement)

### Category 1 — Containers (3 variations)
Containers are different physical affordances that carry the same state logic:
- Cup
- Mug
- Bottle

They share the same state names, but are visually and physically distinct (shape/scale/material).

### Category 2 — Stations (3 variations)
Stations are interaction hubs:
- BeerMachine
- SodaMachine
- WashStation

Each station changes the container state via distinct rules and timing.

### Category 3 — Drinks (3+ variations)
Drink outcomes as final states:
- Beer
- Soda_Lemon
- Soda_Orange
- Soda_Passion

(This exceeds the minimum 3 variations.)

---

## How the sandbox creates emergent behavior
Although each rule is simple, the combination supports non-trivial play:
- The player can choose different containers for the same request.
- Multi-step soda creation invites experimentation and ordering mistakes (e.g., mixing too early).
- The wash station enables “try, fail, reset, retry” quickly.
- Physical manipulation can cause accidental outcomes (dropping, misplacing, mixing timing, grabbing the wrong item).

The result is **consistent emergence**: unpredictable moments arise from stable rules, not random scripted events.

---

## Assets and visual approach
The visual approach is deliberately minimal and functional:
- Uses **free assets** where possible.
- Uses **simple primitives** (cubes/cylinders) during prototyping for fast iteration.
- Some props were assembled by hand in Unity (basic shapes + materials).

The main priority was always: **clarity of state** and **clarity of interaction**.

---

## In-game guidance (readability)
The prototype is designed to communicate “what to do” using:
- **Selectable descriptions** (short instructions when aiming at objects)
- **Order text** showing the current request
- **Audio cues** when machines run and when the customer reacts
- **Clear state visuals** on the container

---

## Player-facing story (short “storytelling” explanation)
You are working at a small experimental bar. A single customer stands at the counter and requests a drink. You pick a container, use the bar’s stations to produce the correct drink, and deliver it to the customer. The bar is intentionally more like a toy-box than a strict simulator: you can experiment, fail, wash, retry, and discover interactions through play.

---

## Where things are (interaction map)
This is the intended spatial layout and responsibilities:
- **BeerMachine**: produces `Beer` from `Empty`.
- **SodaMachine**: produces `Soda_Lemon`, `Soda_Orange`, or `Soda_Passion` through Water → Flavor → Mix steps.
- **WashStation**: resets the selected container back to `Empty`.
- **DeliveryZone**: validates the current order and triggers customer reactions.
- **Order sign (`OrderText`)**: displays the current request.

---

## Audio feedback (what sounds exist and why)
Audio is used as functional feedback:
- **Machine running sounds**: communicate that a timed process started and is progressing.
- **Success sound**: reinforces correct delivery.
- **Failure sound**: reinforces incorrect delivery.

This makes the sandbox readable even without heavy UI.

---

## Technical notes (framework usage + small extensions)
Most logic is implemented using framework components, UnityEvents, trigger gating, and inspector configuration:
- `ConditionalTrigger` for validation and gating
- `TimerTrigger` for processing delays
- `StateMachine` for explicit object states
- `MouseListener` for button-like interactions

Two small helper scripts were added to keep the sandbox modular while scaling up:
- `CupRespawner`: manages selecting between container variants, respawning the selected one, and applying state changes to the currently selected container.

These extensions were used to preserve the “no hardcoded single object” limitation and to support multiple container variants cleanly.

---

## Playtests and iteration (TO DO / to be documented)
To fully meet the “Sufficient” sandbox design requirement, this prototype still needs:
- At least **10 playtests**
- Documented feedback
- Documented changes made based on that feedback

A recommended structure for the playtest log:
- Player ID / date
- What they tried (freeform)
- Confusions/frictions encountered
- Fun moments / surprising outcomes
- Changes applied after the test

