# Changelog

## [0.1.10] - 2025-01-27

### Added
- Comprehensive audit service with file-based logging
- All audit methods implemented (LogDestroy, LogGive, LogTeleport, etc.)
- Thread-safe logging with UTC timestamps
- Exception handling for logging failures

### Features
- Global `.help` command with category support
- Smart aliases for all commands
- Live configuration reloading with `.config reload`
- Enhanced command discovery and documentation
- Improved messaging with color-coded responses

### Administrative Tools
- Player management commands (`idset`, `autoauth`, `swapid`)
- Castle heart management (`heartthaw`, `heartlist`)
- Spawn utilities (`spawnmod`, `spawnat`)
- Shard cleanup tools (`cleanshard`)

### Player Features
- AFK toggle system
- Level checking utilities
- Ping and time commands
- Player-safe command subset

### Technical
- Full MIT License compatibility
- BepInEx plugin architecture
- VampireCommandFramework integration
- Harmony patching for command execution auditing

### Configuration
- Auto-generated `command_config.json`
- Customizable aliases and descriptions
- Category-based command organization
- Runtime configuration updates