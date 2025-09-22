using CrowbaneCommands.Attributes;
using CrowbaneCommands.Extensions;
using System.Linq;
using VampireCommandFramework;

namespace CrowbaneCommands.Commands
{
    [CommandGroup(name: "config", shortHand: "cfg")]
    public static class ConfigCommands
    {
        [ConfigurableCommand("config_reload", "reload", "r", "Reload command configuration", adminOnly: true)]
        [Command("reload", "r", "Reload command configuration", adminOnly: true)]
        public static void ReloadConfigCommand(ChatCommandContext ctx)
        {
            try
            {
                Core.CommandConfig.LoadConfig();
                Core.DynamicCommands.ReloadCommands();
                ctx.ReplySuccess("Configuration reloaded successfully!");
                ctx.ReplyInfo("All command names, colors, and settings have been updated.");
            }
            catch (System.Exception ex)
            {
                ctx.ReplyError($"Failed to reload configuration: {ex.Message}");
                Core.Log.LogError($"Config reload error: {ex}");
            }
        }

        [ConfigurableCommand("config_list", "list", "l", "List all configured commands", adminOnly: true)]
        [Command("list", "l", "List all configured commands", adminOnly: true)]
        public static void ListConfigCommand(ChatCommandContext ctx)
        {
            var config = Core.CommandConfig.Config;
            var enabledCommands = config.Commands.Where(kvp => kvp.Value.Enabled).ToList();
            var disabledCommands = config.Commands.Where(kvp => !kvp.Value.Enabled).ToList();

            ctx.ReplyHighlight("=== Command Configuration ===");

            if (enabledCommands.Any())
            {
                ctx.ReplySuccess($"Enabled Commands ({enabledCommands.Count}):");
                foreach (var kvp in enabledCommands.Take(10)) // Limit to prevent spam
                {
                    var cmd = kvp.Value;
                    var shorthand = !string.IsNullOrEmpty(cmd.Shorthand) ? $" ({cmd.Shorthand})" : "";
                    var adminOnly = cmd.AdminOnly ? " [ADMIN]" : "";
                    ctx.ReplyInfo($"• {cmd.Name}{shorthand}{adminOnly} - {cmd.Description}");
                }
                if (enabledCommands.Count > 10)
                {
                    ctx.ReplySecondary($"... and {enabledCommands.Count - 10} more");
                }
            }

            if (disabledCommands.Any())
            {
                ctx.ReplyWarning($"Disabled Commands ({disabledCommands.Count}):");
                foreach (var kvp in disabledCommands.Take(5))
                {
                    ctx.ReplySecondary($"• {kvp.Key} (disabled)");
                }
                if (disabledCommands.Count > 5)
                {
                    ctx.ReplySecondary($"... and {disabledCommands.Count - 5} more");
                }
            }
        }

        [ConfigurableCommand("config_colors", "colors", "c", "Show current color configuration", adminOnly: true)]
        [Command("colors", "c", "Show current color configuration", adminOnly: true)]
        public static void ShowColorsCommand(ChatCommandContext ctx)
        {
            var colors = Core.CommandConfig.Config.Colors;

            ctx.ReplyHighlight("=== Color Configuration ===");
            ctx.ReplyMultiColor(("Success: ", "info"), ("Example success message", "success"));
            ctx.ReplyMultiColor(("Error: ", "info"), ("Example error message", "error"));
            ctx.ReplyMultiColor(("Warning: ", "info"), ("Example warning message", "warning"));
            ctx.ReplyMultiColor(("Info: ", "info"), ("Example info message", "info"));
            ctx.ReplyMultiColor(("Highlight: ", "info"), ("Example highlight message", "highlight"));
            ctx.ReplyMultiColor(("Secondary: ", "info"), ("Example secondary message", "secondary"));
            ctx.ReplyMultiColor(("Accent: ", "info"), ("Example accent message", "accent"));

            ctx.ReplyInfo("Color codes:");
            ctx.ReplySecondary($"Success: {colors.Success} | Error: {colors.Error}");
            ctx.ReplySecondary($"Warning: {colors.Warning} | Info: {colors.Info}");
            ctx.ReplySecondary($"Highlight: {colors.Highlight} | Secondary: {colors.Secondary}");
            ctx.ReplySecondary($"Accent: {colors.Accent}");
        }

        [ConfigurableCommand("config_save", "save", "s", "Save current configuration", adminOnly: true)]
        [Command("save", "s", "Save current configuration", adminOnly: true)]
        public static void SaveConfigCommand(ChatCommandContext ctx)
        {
            try
            {
                Core.CommandConfig.SaveConfig();
                ctx.ReplySuccess("Configuration saved successfully!");
            }
            catch (System.Exception ex)
            {
                ctx.ReplyError($"Failed to save configuration: {ex.Message}");
                Core.Log.LogError($"Config save error: {ex}");
            }
        }

        [ConfigurableCommand("config_info", "info", "i", "Show configuration file information", adminOnly: true)]
        [Command("info", "i", "Show configuration file information", adminOnly: true)]
        public static void ConfigInfoCommand(ChatCommandContext ctx)
        {
            var config = Core.CommandConfig.Config;
            var configPath = System.IO.Path.Combine(Core.ConfigPath, "command_config.json");

            ctx.ReplyHighlight("=== Configuration Information ===");
            ctx.ReplyInfo($"Config file: {configPath}");
            ctx.ReplyInfo($"Total commands: {config.Commands.Count}");
            ctx.ReplyInfo($"Enabled commands: {config.Commands.Count(kvp => kvp.Value.Enabled)}");
            ctx.ReplyInfo($"Admin-only commands: {config.Commands.Count(kvp => kvp.Value.AdminOnly)}");
            ctx.ReplyInfo($"Custom colors enabled: {(config.General.EnableCustomColors ? "Yes" : "No")}");
            ctx.ReplyInfo($"Command customization enabled: {(config.General.EnableCommandCustomization ? "Yes" : "No")}");
            ctx.ReplyInfo($"Auto-reload on file change: {(config.General.ReloadConfigOnChange ? "Yes" : "No")}");
            ctx.ReplyInfo($"Command prefix: {config.General.CommandPrefix}");

            if (System.IO.File.Exists(configPath))
            {
                var fileInfo = new System.IO.FileInfo(configPath);
                ctx.ReplySecondary($"File size: {fileInfo.Length} bytes");
                ctx.ReplySecondary($"Last modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            }
        }
    }
}
