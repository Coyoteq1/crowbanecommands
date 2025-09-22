![Crowbane Commands Logo](logo.png)\n\n# Crowbane Commands

Crowbane Commands is a full-featured administrative toolkit for V Rising dedicated servers. This build refresh focuses on faster command entry, discoverability, and clearer messaging so staff can keep large shards stable while players keep slaying.

## Highlights
- **Global `.help` command** – Lists every command registered through VampireCommandFramework, including other mods. Supports categories (`.help player`, `.help admin`, `.help all`) and detailed lookups (`.help teleport`).
- **Smart aliases everywhere** – Each command now ships with unique short names and shorthand tokens auto-generated at runtime. Type the new alias, the original command, or any custom entry in `command_config.json` and the server normalizes it before VCF sees it.
- **Prefix preserved, coloring improved** – Commands continue to use the `.` prefix by default, but replies marked as `info` now render in blue for better contrast while errors stay red.
- **Live configuration** – The refreshed `command_config.json` tracks every command (player and admin). Edit aliases, categories, or descriptions and reload in game with `.config reload`.

## Installation
1. Install [BepInEx](https://docs.bepinex.dev/master/articles/user_guide/intro.html) on your V Rising dedicated server.
2. Drop the built `CrowbaneCommands.dll` inside `BepInEx/plugins/`.
3. Launch the server; the plugin writes its config to `BepInEx/config/CrowbaneCommands/command_config.json` and begins tracking aliases automatically.

## Using the command system
- All commands start with the configured prefix (defaults to `.`). Example: `.help` or `.tpplayer`.
- Run `.help player` for the full player-safe list, `.help admin` for staff tools (requires admin auth), or `.help <name>` for exact usage, aliases, and category info.
- Aliases are case-insensitive; the server also understands tokens separated by spaces or dots (e.g. `.help clan.add`).

### Player quick actions
A sample of the player-accessible set (see `.help player` for the current runtime list):
- `afk` – Toggle the AFK lock animation.
- `checklevel` – Check a player level summary.
- `ping` – Show your latency.
- `time` – Report server time.

### Admin automation staples
Key administrative commands now have short aliases ready out of the box:
- `idset` (aka `assignsteamid`) – Bind a SteamID to a character instance.
- `autoauth` – Toggle auto-admin authorization for yourself.
- `cleanshard` – Purge containerless shards safely.
- `spawnmod` / `spawnat` – Spawn modified NPCs at you or a coordinate.
- `heartthaw` / `heartlist` – Manage castle heart freeze states.
- `swapid` – Swap SteamIDs between two players.
Use `.help <alias>` to see the canonical syntax plus every available synonym.

## Configuration
The generated `command_config.json` now contains:
- `category`: `player` or `admin` for every command (updates automatically if new commands ship).
- `shorthand`: single-token alias used for quick chat commands.
- `aliases`: optional list of additional strings you define. Any alias you add is normalized at runtime with no restart.
- `description`: displayed inside `.help` output.

Changes can be reloaded without rebooting: `.config reload`.

## Support & Resources
- **Crowbane Discord** – <https://discord.gg/ZnGGfj69zv>
- **V Rising Mods Wiki** – <https://vrisingmods.com/wiki>

## Credits
- Maintained by **Coyoteq1** and the Crowbane team.

## License
This project is released under the [MIT License](LICENSE). Contributions and forks are welcome – please keep the credits intact.

