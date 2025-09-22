# Crowbane Commands

**Full-featured administrative toolkit for V Rising dedicated servers**

## Overview

Crowbane Commands is the most comprehensive command suite for V Rising servers, featuring smart aliases, global help system, and extensive administrative tools. Perfect for managing large communities and keeping servers stable.

## Key Features

🎯 **Smart Command System**
- Global `.help` command lists all available commands
- Auto-generated aliases for faster command entry
- Category-based organization (player/admin commands)
- Live configuration reloading

🛠️ **Administrative Tools**
- Player management (Steam ID binding, swapping)
- Castle heart controls (freeze/thaw states)
- Spawn utilities with coordinate targeting
- Comprehensive audit logging

👥 **Player-Friendly**
- AFK toggle system
- Level checking utilities
- Server time and ping commands
- Safe command subset for regular players

## Installation

1. Install **BepInEx** for V Rising
2. Install **VampireCommandFramework** (dependency)
3. Extract this mod to `BepInEx/plugins/`
4. Start your server - configuration auto-generates

## Quick Start

- `.help` - Show all available commands
- `.help player` - Player-safe commands only
- `.help admin` - Administrative commands
- `.config reload` - Reload configuration changes

## Configuration

The mod creates `BepInEx/config/CrowbaneCommands/command_config.json` with:
- Custom aliases for any command
- Category assignments
- Command descriptions
- All changes apply without restart

## Support

Join the **Crowbane Discord**: https://discord.gg/ZnGGfj69zv

## Dependencies

- BepInEx for V Rising
- VampireCommandFramework

## License

MIT License - Free to use and modify