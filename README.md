# EnternalKonfire

A 2D survival game prototype developed during a 3-week game development course at Mercantec.

![Gameplay Screenshot](Main.png)

## 🎮 Gameplay & Mechanics
* **Core Objective:** Keep the bonfire lit while defending it against waves of ghost cats.
* **Key Features:**
  * **Resource Management:** Chop trees to gather wood and refuel the campfire.
  * **Procedural Forest Placement:** Radius-based tree placement logic with collision checks (`ForestManager.cs`).
  * **Enemy AI:** Ghost cat targeting and movement logic tracking the bonfire (`GhostCatAI.cs`).
  * **State Architecture:** Centralized game loop handling wave progression, victory, and game-over states via a singleton pattern (`GameManager.cs`).

## 🛠 Tech Stack
* **Game Engine:** Unity
* **Language:** C#
* **IDE:** JetBrains Rider
* **Assets:** Custom 2D pixel art and original soundtrack (`Lost in the Pixel Pines.mp3`).

## 🕹 Controls
* `W, A, S, D` / Arrow Keys — Movement
* `Left Mouse Button` / `E` — Interact (gather wood / fuel bonfire)
