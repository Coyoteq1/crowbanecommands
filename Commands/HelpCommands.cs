using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CrowbaneCommands.Extensions;
using CrowbaneCommands.Services;
using VampireCommandFramework;

namespace CrowbaneCommands.Commands;

internal static class HelpCommands
{
    private static readonly PropertyInfo AssemblyCommandMapProperty = typeof(CommandRegistry).GetProperty("AssemblyCommandMap", BindingFlags.NonPublic | BindingFlags.Static);

    [Command("help", "?", description: "Show available commands. Use .help <name> for details or .help admin/player.", adminOnly: false)]
    public static void ShowHelp(ChatCommandContext ctx, string query = "")
    {
        var trimmedQuery = query?.Trim() ?? string.Empty;
        var isAdmin = ctx.IsAdmin;

        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            SendSummary(ctx, isAdmin);
            return;
        }

        if (TryHandleCategory(ctx, trimmedQuery, isAdmin))
        {
            return;
        }

        if (TryShowInternalCommand(ctx, trimmedQuery, isAdmin))
        {
            return;
        }

        if (TryShowExternalCommand(ctx, trimmedQuery, isAdmin))
        {
            return;
        }

        ctx.ReplyWarning($"No command or category matching '{trimmedQuery}' was found.");
    }

    private static void SendSummary(ChatCommandContext ctx, bool isAdmin)
    {
        ctx.ReplyHighlight("=== Crowbane Command Help ===");
        ctx.ReplyInfo("Use .help player or .help admin to list commands. .help <command> shows details. .help all includes other mods.");

        var descriptors = Core.CommandConfig.CommandDescriptors
            .Where(d => !d.AdminOnly)
            .OrderBy(d => d.PrimaryAlias)
            .ToList();

        if (descriptors.Count > 0)
        {
            SendCommandList(ctx, "Player Commands", descriptors);
        }

        if (isAdmin)
        {
            var adminDescriptors = Core.CommandConfig.CommandDescriptors
                .Where(d => d.AdminOnly)
                .OrderBy(d => d.PrimaryAlias)
                .ToList();

            if (adminDescriptors.Count > 0)
            {
                SendCommandList(ctx, "Admin Commands", adminDescriptors);
            }
        }

        var externalSummary = GetExternalCommands()
            .Where(cmd => !cmd.AdminOnly || isAdmin)
            .OrderBy(cmd => cmd.Route)
            .Take(10)
            .Select(cmd => cmd.Route)
            .ToList();

        if (externalSummary.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Other Mods (first 10):");
            foreach (var route in externalSummary)
            {
                sb.AppendLine($"• {route}");
            }
            ctx.ReplySecondary(sb.ToString().TrimEnd());
        }
    }

    private static bool TryHandleCategory(ChatCommandContext ctx, string query, bool isAdmin)
    {
        switch (query.ToLowerInvariant())
        {
            case "player":
                {
                    var entries = Core.CommandConfig.CommandDescriptors
                        .Where(d => !d.AdminOnly)
                        .OrderBy(d => d.PrimaryAlias)
                        .ToList();
                    if (entries.Count == 0)
                    {
                        ctx.ReplyWarning("No player commands were found.");
                        return true;
                    }
                    SendCommandList(ctx, "Player Commands", entries);
                    return true;
                }
            case "admin":
                {
                    if (!isAdmin)
                    {
                        ctx.ReplyError("Admin command list is restricted.");
                        return true;
                    }

                    var entries = Core.CommandConfig.CommandDescriptors
                        .Where(d => d.AdminOnly)
                        .OrderBy(d => d.PrimaryAlias)
                        .ToList();

                    if (entries.Count == 0)
                    {
                        ctx.ReplyWarning("No admin commands were found.");
                        return true;
                    }

                    SendCommandList(ctx, "Admin Commands", entries);
                    return true;
                }
            case "all":
                {
                    var externalCommands = GetExternalCommands()
                        .Where(cmd => !cmd.AdminOnly || isAdmin)
                        .OrderBy(cmd => cmd.Route)
                        .ToList();

                    if (externalCommands.Count == 0)
                    {
                        ctx.ReplyWarning("No additional mod commands detected.");
                        return true;
                    }

                    SendExternalList(ctx, externalCommands);
                    return true;
                }
            default:
                return false;
        }
    }

    private static bool TryShowInternalCommand(ChatCommandContext ctx, string alias, bool isAdmin)
    {
        if (!Core.CommandConfig.TryGetDescriptor(alias, out var descriptor))
        {
            return false;
        }

        if (descriptor.AdminOnly && !isAdmin)
        {
            ctx.ReplyError("You do not have permission to view this command.");
            return true;
        }

        SendDescriptorDetails(ctx, descriptor);
        return true;
    }

    private static bool TryShowExternalCommand(ChatCommandContext ctx, string alias, bool isAdmin)
    {
        var candidates = GetExternalCommands()
            .Where(cmd => cmd.Matches(alias))
            .ToList();

        if (candidates.Count == 0)
        {
            return false;
        }

        var visible = candidates.Where(cmd => !cmd.AdminOnly || isAdmin).ToList();
        if (visible.Count == 0)
        {
            ctx.ReplyError("Matching command exists but is administrator-only.");
            return true;
        }

        foreach (var command in visible)
        {
            SendExternalDetails(ctx, command);
        }

        return true;
    }

    private static void SendDescriptorDetails(ChatCommandContext ctx, CommandDescriptor descriptor)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Command: {descriptor.Route.ToCommandString()}");
        if (descriptor.Aliases.Count > 1)
        {
            sb.AppendLine($"Aliases: {string.Join(", ", descriptor.Aliases.Skip(1))}");
        }
        if (!string.IsNullOrWhiteSpace(descriptor.Attribute?.Usage))
        {
            sb.AppendLine($"Usage: {descriptor.Attribute.Usage}");
        }
        if (!string.IsNullOrWhiteSpace(descriptor.Description))
        {
            sb.AppendLine($"Description: {descriptor.Description}");
        }
        sb.AppendLine($"Category: {(descriptor.AdminOnly ? "Admin" : "Player")}");
        ctx.ReplyInfo(sb.ToString().TrimEnd());
    }

    private static void SendExternalDetails(ChatCommandContext ctx, ExternalCommandInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Command: {info.Route}");
        if (info.Aliases.Count > 0)
        {
            sb.AppendLine($"Aliases: {string.Join(", ", info.Aliases)}");
        }
        if (!string.IsNullOrWhiteSpace(info.Description))
        {
            sb.AppendLine($"Description: {info.Description}");
        }
        sb.AppendLine($"Category: {(info.AdminOnly ? "Admin" : "Player")}");
        if (info.SourceAssembly != null)
        {
            sb.AppendLine($"Source: {info.SourceAssembly.GetName().Name}");
        }
        ctx.ReplySecondary(sb.ToString().TrimEnd());
    }

    private static void SendCommandList(ChatCommandContext ctx, string header, IReadOnlyList<CommandDescriptor> descriptors)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{header} ({descriptors.Count})");
        foreach (var descriptor in descriptors)
        {
            sb.Append("• ").Append(descriptor.PrimaryAlias);
            var fallbackAliases = descriptor.Aliases.Skip(1).Take(2).ToList();
            if (fallbackAliases.Count > 0)
            {
                sb.Append(" [").Append(string.Join(", ", fallbackAliases)).Append(']');
            }
            if (!string.IsNullOrWhiteSpace(descriptor.Description))
            {
                sb.Append(" — ").Append(descriptor.Description);
            }
            if (descriptor.AdminOnly)
            {
                sb.Append(" [admin]");
            }
            sb.AppendLine();

            if (sb.Length > Core.MAX_REPLY_LENGTH - 80)
            {
                ctx.ReplyInfo(sb.ToString().TrimEnd());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            ctx.ReplyInfo(sb.ToString().TrimEnd());
        }
    }

    private static void SendExternalList(ChatCommandContext ctx, IReadOnlyList<ExternalCommandInfo> commands)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"External Commands ({commands.Count})");
        foreach (var cmd in commands)
        {
            sb.Append("• ").Append(cmd.Route);
            if (!string.IsNullOrWhiteSpace(cmd.Description))
            {
                sb.Append(" — ").Append(cmd.Description);
            }
            if (cmd.AdminOnly)
            {
                sb.Append(" [admin]");
            }
            if (cmd.SourceAssembly != null)
            {
                sb.Append(" (" + cmd.SourceAssembly.GetName().Name + ")");
            }
            sb.AppendLine();

            if (sb.Length > Core.MAX_REPLY_LENGTH - 80)
            {
                ctx.ReplySecondary(sb.ToString().TrimEnd());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
        {
            ctx.ReplySecondary(sb.ToString().TrimEnd());
        }
    }

    private static IEnumerable<ExternalCommandInfo> GetExternalCommands()
    {
        var map = AssemblyCommandMapProperty?.GetValue(null) as IDictionary;
        if (map == null)
        {
            yield break;
        }

        foreach (DictionaryEntry assemblyEntry in map)
        {
            if (assemblyEntry.Key is not Assembly assembly)
            {
                continue;
            }

            if (assembly == Assembly.GetExecutingAssembly())
            {
                continue;
            }

            if (assemblyEntry.Value is not IDictionary commandMap)
            {
                continue;
            }

            foreach (DictionaryEntry commandEntry in commandMap)
            {
                var info = ExternalCommandInfo.FromMetadata(commandEntry.Key, assembly);
                if (info == null)
                {
                    continue;
                }

                if (commandEntry.Value is IEnumerable aliases)
                {
                    foreach (var alias in aliases)
                    {
                        if (alias is string aliasString && !string.IsNullOrWhiteSpace(aliasString))
                        {
                            info.Aliases.Add(aliasString);
                        }
                    }
                }

                yield return info;
            }
        }
    }

    private sealed class ExternalCommandInfo
    {
        private ExternalCommandInfo()
        {
        }

        public string Route { get; private set; }
        public string Description { get; private set; }
        public bool AdminOnly { get; private set; }
        public List<string> Aliases { get; } = new();
        public Assembly SourceAssembly { get; private set; }

        public static ExternalCommandInfo FromMetadata(object metadata, Assembly sourceAssembly)
        {
            if (metadata == null)
            {
                return null;
            }

            var type = metadata.GetType();
            var attribute = type.GetProperty("Attribute")?.GetValue(metadata) as CommandAttribute;
            if (attribute == null)
            {
                return null;
            }

            var groupAttribute = type.GetProperty("GroupAttribute")?.GetValue(metadata) as CommandGroupAttribute;
            var groupName = groupAttribute?.Name;
            var route = string.IsNullOrWhiteSpace(groupName) ? attribute.Name : $"{groupName} {attribute.Name}";

            return new ExternalCommandInfo
            {
                Route = route,
                Description = attribute.Description ?? string.Empty,
                AdminOnly = attribute.AdminOnly,
                SourceAssembly = sourceAssembly
            };
        }

        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return false;
            }

            if (Route.Equals(query, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return Aliases.Any(alias => alias.Equals(query, StringComparison.OrdinalIgnoreCase));
        }
    }
}

