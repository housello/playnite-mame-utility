# MAME Utility – Machine tags addition

This source tree starts from the clean `playnite-mame-utility-main` source archive supplied for this task.

## Added action

`MAME Utility > Cataloguer > Tag machines`

The action recalculates only tags beginning with `Machine: ` for the selected games. It removes old tags in that namespace and adds the currently detected machine-family tag. Other tags and Playnite game metadata are left unchanged.

Machine matching uses the existing exact MAME ROM-name / ROM-path lookup first. If a game has no local ROM path (for example, an entry imported from RomM), it also tries an exact, case-insensitive match between the Playnite game name and the MAME XML description. If duplicate descriptions map to different outcomes, it skips the ambiguous match rather than guessing.

## Current machine-family mappings

- `Machine: Sega Naomi / Naomi 2` — MAME `naomi` driver, except Atomiswave sets.
- `Machine: Sammy Atomiswave` — MAME `naomi` driver with `romof="awbios"`.
- `Machine: Sega Hikaru` — `hikaru` driver.
- `Machine: Capcom CPS-1` — `cps1` driver.
- `Machine: Capcom CPS-2` — `cps2` driver.
- `Machine: Capcom CPS-3` — `cps3` driver.
- `Machine: Sega Model 2` — `model2` driver.
- `Machine: Sega Model 3` — `model3` driver.
- `Machine: PlayStation-based Arcade` — Namco System 11/12 (`namcos11` / `namcos12`), Sony ZN (`zn`) and Taito G-NET (`taitogn`).

The action requires MAME as the selected source format. It does not change platforms, categories, game names, ROM paths, or tags outside the `Machine: ` namespace.

## Build note

The project targets .NET Framework 4.7.2 and Playnite SDK 6.0, as in the supplied original source. This environment did not have MSBuild/.NET Framework build tools available, so the C# project could not be compiled here; build and test it in the Windows development environment before installing it in Playnite.
