DECISIONS.md — AI and decision log

Game: 2D platformer (Unity 6, mobile). Assignment 10 — Three Enemy Types.
Agent used: Claude. I lead, the agent helps; 

Week 11 (9–15 Nov) — log

2026-11-10  I had problems with my enemy scripts and solved them one by one, asking Claude how to fix each step. 
Problem 1: the old Enemy.cs hit the player 0.2 s after the attack animation started (no visible wind-up) and also hurt on plain touch — unfair. Fix: states Patrol - Chase - WindUp - Strike - Recover. Wind-up windUpTime 0.5 s = red blinking + red "!" above the head + warn sound, then cooldown attackCooldown 1.3 s. Touch damage is off by default contactDamage.

2026-11-10  Problem 2: how to get three different enemies without copy-paste. Decision: Enemy became the base class (health, drops, death, vision, wind-up telegraph, "no attacks from off-screen"), and EnemyShooter / EnemyCharger only override Think() with their own state machine. Because they are still Enemy, the player's attack, the portal lock and the HUD counter work without changes. I asked Claude to create new script files only when really necessary: only 2 new scripts, the shuriken reuses BossFireball.

2026-11-10  Problem 3: the enemy "saw" me through walls because it only compared distance. Fix: CanSeePlayer() checks distance, view angle (viewAngle) and a Raycast (Physics2D.RaycastAll + IsSolid filter), so walls and the pillar block the view. My player's pivot is at the feet, so the ray and the aim go to the centre of the player's collider, not to transform.position.

2026-11-10  Shinobi (shooter): Patrol - Reload - Aim - Fire - Reload, Retreat when I come closer than keepDistance. Aim = wind-up 0.6 s with a red aim line; the line freezes for the last 0.2 s aimLockTime so I can dodge. If I hide behind the pillar the shot is cancelled.

2026-11-11 My own enemy — Charger (boar): Patrol - WindUp - Charge - Stunned / Recover. Wind-up 0.7 s: scraping the ground, red blink, "!" and a red lane that shows where it will run. It only hurts during the charge, never charges from off-screen, never runs off a cliff. Agent mistakes I fixed: the tusk on the first boar looked like a bandage on the snout; the death animation frames were cropped; a charge that ended at a cliff edge had no consequence, so now the boar skids and is stunned (shorter than after a wall hit).

2026-11-11  Bug found while thinking about Level 3: an enemy knocked into a pit would stay "alive" and the portal would never open. Added a check in Enemy.Update (below y = -12 counts as defeated). Also the hard-coded "Level1" checks in Enemy, PortalTransition, HudController were replaced by Enemy.IsEnemyLevel() so Level 3 works the same way.

2026-11-11  Level 3 added (all three types, 6 enemies). Order: Level 1 - Level 2 - Level 3 - Boss.Absolutely all assets (for Shinobi, Charger, and all other enemies) were found by me online on various asset websites.(not hand-drawn by me).

2026-11-11 DECISION and AI log was written by Claude

Script map 

Enemy.cs  (base class + type 1 PATROLLER, Fighter sprites)
 |  shared: health, drops, death, TakeDamage (interrupts wind-up), CanSeePlayer() = distance + angle + Raycast,
 |          BeginTelegraph()/EndTelegraph() (red blink, "!", sound), ShowLine(), OnScreen(), serialized tuning
 |  Patrol --sees player--> Chase --in reach + cooldown ready + on screen--> WindUp (0.5 s) --> Strike --> Recover --> Chase
 |                                   \--lost player > loseSightTime--> Patrol
 +-- EnemyShooter.cs  (type 2 SHINOBI)   Think() override
 |     Patrol -> Reload -> Aim (0.6 s, red line, locks last 0.2 s) -> Fire -> Reload ;  Retreat if player too close
 |     Fire calls BossFireball.SpawnShuriken()  (projectile script shared with the boss)
 +-- EnemyCharger.cs  (type 3 CHARGER, boar)   Think() override
       Patrol -> WindUp (0.7 s, red lane) -> Charge -> Stunned (wall / cliff) or Recover (tired) -> Patrol

PlayerController --TakeDamage(1)--> Enemy          Enemy --TakeDamage()--> PlayerController
Enemy.enemiesRemaining --> PortalTransition (locks portal) and HudController ("ENEMIES LEFT")
Enemy.IsEnemyLevel(scene) --> Level1 and Level3 lock the portal until all enemies are dead


Where everything is

- Scripts: Assets/Scripting/Enemy.cs, EnemyShooter.cs, EnemyCharger.cs, BossFireball.cs (shuriken and ets.).
- Prefabs: Assets/Gameplay/Enemy/Enemy.prefab, EnemyShinobi.prefab, EnemyCharger.prefab 
- Sprites loaded at runtime: Assets/Resources/Shinobi, Assets/Resources/Charger.
- Level 3: Assets/Scenes/Level3.unity 
