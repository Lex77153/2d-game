# Samurai: Demon Gate

A 2D pixel-art action platformer for **Android / iOS (landscape)**. A lone samurai fights through a haunted
village at dusk, survives a rain of bombs and slays the demon guarding the gate.

> Game Development Fundamentals · RK2 · *Mobile Game Kickoff* — my midterm game grown into a full mobile game.

| | |
|---|---|
| Engine | Unity **6000.6.0f1** (2D, built-in renderer) |
| Input | Unity Input System (keyboard in the editor, on-screen controls on a phone) |
| UI | uGUI + TextMeshPro, Canvas Scaler *Scale With Screen Size* 1920×1080, Match 1, SafeArea panel |
| Design doc | [`docs/GDD_Samurai_Demon_Gate.pdf`](docs/GDD_Samurai_Demon_Gate.pdf) |

## How to run

1. Open the folder with **Unity Hub → Add project from disk** (Unity 6000.6.0f1).
2. Open the scene `Assets/Scenes/MainMenu.unity` and press **Play**.
3. For the phone layouts: Game view → dropdown → **Simulator** → pick a device
   (16:9 phone, a 19.5:9 notch phone such as iPhone 14 / Pixel, a 4:3 tablet such as iPad).
4. *(optional)* Menu **Samurai ▸ Bake Mobile UI Into Scenes** saves the UI into the scene files so it can be
   inspected in the Hierarchy (`MobileUI` → `SafeArea` → `Hud` …). Without baking the same UI is built when the game starts.

## Controls

| Action | Phone | PC (editor) |
|---|---|---|
| Run | on-screen stick (bottom-left) | A / D (Shift = walk slowly) |
| Jump | orange button (bottom-right) | Space |
| Attack | red button (left of Jump) | E |
| Pause | II button (top-right) | Esc or P |

## The three UI screens

| Screen | What it contains |
|---|---|
| **Main Menu** | title, **Play**, **Quit**, dusk village scene with the hero and the demon |
| **HUD** | hearts, **score** (coins), level name / boss health bar, **pause button**, stick, **Jump**, **Attack** |
| **Pause** | **Resume**, **Restart**, **Main Menu**; `Time.timeScale = 0`, music ducks |

How the UI meets the requirements:

* **Canvas Scaler** – Scale With Screen Size, 1920 × 1080, Match = 1 (landscape).
* **Anchors** – every element is pinned to a corner, an edge or the centre (`MobileUI.cs`); full-screen dim is stretched.
* **Safe area** – all buttons and text are inside the `SafeArea` panel (`SafeAreaPanel.cs` follows `Screen.safeArea`).
* **Thumbs** – every button ≥ 120 × 120 px (pause 160, jump 208, attack 176, menu buttons 580–640 × 150–160),
  controls sit in the bottom corners, text is 36+ px with a dark outline.
* **It works** – Play loads Level 1, Pause stops time, Resume continues, Restart reloads, Menu returns (`GameFlow.cs`).

## Project structure

```
Assets/
  Scenes/          MainMenu, Level1, Level2, LevelBoss
  Scripting/       gameplay + UI code (UIKit, MobileUI, HudController, GameFlow, Touch*, WorldStyler, TerrainPainter …)
  Editor/          texture import rules, MobileUIBaker
  Resources/
    UIKit/         pixel-art UI sprites (buttons, panels, icons, stick)
    Village/       textures from the Village Props pack (props sheet, ground tileset)
    Hero, Enemy, Boss, Audio, FX …
Packages/ ProjectSettings/
docs/              GDD
```

## Credits

* Village Props, ground tileset – *Pixel Art Platformer – Village Props* by **Cainos** (Unity Asset Store).
  Used under the Asset Store EULA – please keep the repository private (invite the teacher) if you do not want to
  redistribute the raw textures.
* Hero, enemy and demon sprites, music and sound effects – from my midterm project: **TODO: write the original sources / authors here.**
* UI kit sprites – pixel art generated with a script to match the game palette.

## AI usage

AI assistance is documented in [`AI_LOG.txt`](AI_LOG.txt).
