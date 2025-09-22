using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using BepInEx.Logging;
using CrowbaneCommands.Models;
using VampireCommandFramework;

namespace CrowbaneCommands.Services
{
    public class CommandConfigService : IDisposable
    {
        private static readonly string ConfigFileName = "command_config.json";
        private static string ConfigFilePath => Path.Combine(Core.ConfigPath, ConfigFileName);

        private readonly Dictionary<string, CommandDescriptor> _descriptors = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CommandRoute> _aliasMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _orderedAliases = new();
        private IReadOnlyList<CommandReflectionInfo> _reflectionCache = Array.Empty<CommandReflectionInfo>();

        public CommandConfig Config { get; private set; }
        private FileSystemWatcher _fileWatcher;
        private DateTime _lastWriteTime;

        private static readonly Dictionary<string, string> AliasOverrides = new(StringComparer.OrdinalIgnoreCase)
        {
            ["assignsteamid"] = "idset",
            ["autoadminauth"] = "autoauth",
            ["bloodpotion"] = "bloodpot",
            ["bloodpotionmix"] = "bloodmix",
            ["cleancontainerlessshards"] = "cleanshard",
            ["customspawn"] = "spawnmod",
            ["customspawnat"] = "spawnat",
            ["despawnnpc"] = "npcclr",
            ["everyonedaywalker"] = "dayon",
            ["flyheight"] = "flyhi",
            ["flylevel"] = "flylvl",
            ["flyobstacleheight"] = "flyclear",
            ["frozenhearts"] = "heartlist",
            ["gruelsettings"] = "gruelset",
            ["grueltransform"] = "gruelform",
            ["killplayer"] = "killp",
            ["playerheartcount"] = "heartcnt",
            ["plotinfo"] = "plotdata",
            ["reloadadmin"] = "adminreload",
            ["relocatereset"] = "relreset",
            ["removestaff"] = "staffrm",
            ["revealmapforallplayers"] = "mapall",
            ["spawnban"] = "spawnbans",
            ["spectate"] = "spect",
            ["staydown"] = "stickdown",
            ["swapplayers"] = "swapid",
            ["teleporthorse"] = "tphorse",
            ["thawheart"] = "heartthaw",
            ["toggleadmin"] = "admintoggle",
            ["unbindplayer"] = "unbind",
            ["whereami"] = "where",
            ["playerinfo"] = "pinfo",
            ["god"] = "+",
            ["mortal"] = "-",
            ["teleport"] = "tpt",
            ["fly"] = "^",
            ["flydown"] = "fd"
        };

        public CommandConfigService()
        {
            LoadConfig();
            SetupFileWatcher();
        }

        public IReadOnlyCollection<CommandDescriptor> CommandDescriptors => _descriptors.Values;

        public void LoadConfig()
        {
            try
            {
                EnsureConfigDirectory();

                if (!File.Exists(ConfigFilePath))
                {
                    CreateDefaultConfig();
                    return;
                }

                var json = File.ReadAllText(ConfigFilePath);
                Config = JsonSerializer.Deserialize<CommandConfig>(json, GetJsonOptions()) ?? new CommandConfig();
                NormalizeConfigDictionaries();

                var changed = EnsureCommandEntries();
                RebuildCaches();

                if (changed)
                {
                    SaveConfig();
                }

                Core.Log.LogInfo($"Command configuration loaded from {ConfigFilePath}");
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Failed to load command configuration: {ex.Message}");
                Config = new CommandConfig();
                CreateDefaultConfig();
            }
        }

        public void SaveConfig()
        {
            try
            {
                EnsureConfigDirectory();
                var json = JsonSerializer.Serialize(Config, GetJsonOptions());
                _lastWriteTime = DateTime.Now;
                File.WriteAllText(ConfigFilePath, json);
                Core.Log.LogInfo("Command configuration saved successfully");
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Failed to save command configuration: {ex.Message}");
            }
        }

        public CommandSettings GetCommandSettings(string commandKey)
        {
            if (string.IsNullOrWhiteSpace(commandKey))
            {
                return null;
            }

            var normalized = CommandRoute.NormalizeKey(commandKey);
            return Config.Commands.TryGetValue(normalized, out var settings) ? settings : null;
        }

        public string GetCommandName(string commandKey)
        {
            var descriptor = TryGetDescriptor(commandKey, out var desc) ? desc : null;
            return descriptor?.Settings?.Name ?? commandKey;
        }

        public string GetCommandShorthand(string commandKey)
        {
            var descriptor = TryGetDescriptor(commandKey, out var desc) ? desc : null;
            return descriptor?.Settings?.Shorthand ?? string.Empty;
        }

        public bool IsCommandEnabled(string commandKey)
        {
            var settings = GetCommandSettings(commandKey);
            return settings?.Enabled ?? true;
        }

        public string NormalizeIncomingCommand(string messageWithoutPrefix)
        {
            if (string.IsNullOrWhiteSpace(messageWithoutPrefix))
            {
                return messageWithoutPrefix;
            }

            if (TryResolveAlias(messageWithoutPrefix, out var normalized))
            {
                return normalized;
            }

            return messageWithoutPrefix.TrimStart();
        }

        public bool TryResolveAlias(string input, out string normalizedCommand)
        {
            normalizedCommand = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var trimmed = input.TrimStart();
            foreach (var alias in _orderedAliases)
            {
                if (!trimmed.StartsWith(alias, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (trimmed.Length > alias.Length && !char.IsWhiteSpace(trimmed[alias.Length]))
                {
                    continue;
                }

                if (!_aliasMap.TryGetValue(alias, out var route))
                {
                    continue;
                }

                var remainder = trimmed.Length > alias.Length ? trimmed.Substring(alias.Length).TrimStart() : string.Empty;
                normalizedCommand = route.ToCommandString();
                if (!string.IsNullOrEmpty(remainder))
                {
                    normalizedCommand = $"{normalizedCommand} {remainder}";
                }
                return true;
            }

            return false;
        }

        public bool TryGetDescriptor(string commandOrAlias, out CommandDescriptor descriptor)
        {
            descriptor = null;
            if (string.IsNullOrWhiteSpace(commandOrAlias))
            {
                return false;
            }

            if (TryResolveAlias(commandOrAlias, out var normalized))
            {
                var normalizedKey = CommandRoute.FromCommandString(normalized).Key;
                return _descriptors.TryGetValue(normalizedKey, out descriptor);
            }

            var directKey = CommandRoute.NormalizeKey(commandOrAlias);
            if (_descriptors.TryGetValue(directKey, out descriptor))
            {
                return true;
            }

            descriptor = _descriptors.Values.FirstOrDefault(desc => desc.Aliases.Any(alias => alias.Equals(commandOrAlias, StringComparison.OrdinalIgnoreCase)));
            return descriptor != null;
        }

        public string FormatMessage(string message, string colorType = "info")
        {
            if (!Config.General.EnableCustomColors) return message;

            var color = colorType.ToLower() switch
            {
                "success" => Config.Colors.Success,
                "error" => Config.Colors.Error,
                "warning" => Config.Colors.Warning,
                "info" => Config.Colors.Info,
                "highlight" => Config.Colors.Highlight,
                "secondary" => Config.Colors.Secondary,
                "accent" => Config.Colors.Accent,
                _ => Config.Colors.Info
            };

            return $"<color={color}>{message}</color>";
        }

        private void CreateDefaultConfig()
        {
            Config = new CommandConfig
            {
                Commands = new Dictionary<string, CommandSettings>(StringComparer.OrdinalIgnoreCase)
            };

            EnsureCommandEntries();
            RebuildCaches();
            SaveConfig();
            Core.Log.LogInfo("Created default command configuration");
        }

        private void EnsureConfigDirectory()
        {
            if (!Directory.Exists(Core.ConfigPath))
            {
                Directory.CreateDirectory(Core.ConfigPath);
            }
        }

        private void NormalizeConfigDictionaries()
        {
            var normalized = new Dictionary<string, CommandSettings>(StringComparer.OrdinalIgnoreCase);
            if (Config.Commands != null)
            {
                foreach (var kvp in Config.Commands)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key))
                    {
                        continue;
                    }

                    var key = CommandRoute.NormalizeKey(kvp.Key);
                    normalized[key] = kvp.Value ?? new CommandSettings();
                }
            }

            Config.Commands = normalized;
        }

        private bool EnsureCommandEntries()
        {
            var changed = false;
            var infos = EnumeratePluginCommands().ToList();
            var usedAliases = CollectExistingAliases();

            foreach (var info in infos)
            {
                var key = info.Route.Key;
                if (!Config.Commands.TryGetValue(key, out var settings))
                {
                    settings = new CommandSettings();
                    Config.Commands[key] = settings;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(settings.Name))
                {
                    settings.Name = info.Route.ToCommandString();
                    changed = true;
                }

                if (settings.Aliases == null)
                {
                    settings.Aliases = new List<string>();
                }

                if (settings.Category == null || string.IsNullOrWhiteSpace(settings.Category))
                {
                    settings.Category = info.CommandAttribute.AdminOnly ? "admin" : "player";
                    changed = true;
                }

                if (settings.AdminOnly != info.CommandAttribute.AdminOnly)
                {
                    settings.AdminOnly = info.CommandAttribute.AdminOnly;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(settings.Description) && !string.IsNullOrWhiteSpace(info.CommandAttribute.Description))
                {
                    settings.Description = info.CommandAttribute.Description;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(settings.Shorthand))
                {
                    var shorthand = GenerateSingleTokenAlias(info, usedAliases);
                    if (!string.IsNullOrWhiteSpace(shorthand))
                    {
                        settings.Shorthand = shorthand;
                        usedAliases.Add(shorthand.ToLowerInvariant());
                        changed = true;
                    }
                }
                else
                {
                    usedAliases.Add(settings.Shorthand.ToLowerInvariant());
                }

                if (settings.Aliases.Count == 0)
                {
                    var alias = GenerateMultiTokenAlias(info, usedAliases);
                    if (!string.IsNullOrWhiteSpace(alias) && !alias.Equals(settings.Shorthand, StringComparison.OrdinalIgnoreCase))
                    {
                        settings.Aliases.Add(alias);
                        usedAliases.Add(alias.ToLowerInvariant());
                        changed = true;
                    }
                }
                else
                {
                    foreach (var alias in settings.Aliases)
                    {
                        if (!string.IsNullOrWhiteSpace(alias))
                        {
                            usedAliases.Add(alias.ToLowerInvariant());
                        }
                    }
                }
            }

            var validKeys = new HashSet<string>(infos.Select(info => info.Route.Key), StringComparer.OrdinalIgnoreCase);
            var keysToRemove = Config.Commands.Keys.Where(key => !validKeys.Contains(key)).ToList();
            foreach (var extraKey in keysToRemove)
            {
                Config.Commands.Remove(extraKey);
                changed = true;
            }

            _reflectionCache = infos;
            return changed;
        }

        private HashSet<string> CollectExistingAliases()
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var settings in Config.Commands.Values)
            {
                if (settings == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(settings.Shorthand))
                {
                    used.Add(settings.Shorthand.ToLowerInvariant());
                }

                if (settings.Aliases == null) continue;

                foreach (var alias in settings.Aliases)
                {
                    if (!string.IsNullOrWhiteSpace(alias))
                    {
                        used.Add(alias.ToLowerInvariant());
                    }
                }
            }

            return used;
        }

        private string GenerateSingleTokenAlias(CommandReflectionInfo info, HashSet<string> usedAliases)
        {
            if (AliasOverrides.TryGetValue(info.Route.Key, out var overrideAlias))
            {
                return EnsureUniqueAlias(overrideAlias, usedAliases);
            }

            if (!string.IsNullOrWhiteSpace(info.CommandAttribute.ShortHand))
            {
                return EnsureUniqueAlias(info.CommandAttribute.ShortHand, usedAliases);
            }

            var tokens = Tokenize(info.CommandAttribute.Name);
            if (tokens.Count == 0)
            {
                tokens = Tokenize(info.Route.Segments.Last());
            }

            string alias;
            if (tokens.Count == 1)
            {
                var token = tokens[0].ToLowerInvariant();
                alias = token.Length <= 5 ? token : token[..3] + token[^2..];
            }
            else
            {
                alias = string.Concat(tokens.Select(t => t[0])).ToLowerInvariant();
                if (alias.Length < 3)
                {
                    alias = tokens[0].Substring(0, Math.Min(3, tokens[0].Length)).ToLowerInvariant();
                }
            }

            return EnsureUniqueAlias(alias, usedAliases);
        }

        private string GenerateMultiTokenAlias(CommandReflectionInfo info, HashSet<string> usedAliases)
        {
            if (AliasOverrides.TryGetValue(info.Route.Key, out var overrideAlias))
            {
                return EnsureUniqueAlias(overrideAlias, usedAliases);
            }

            if (info.GroupAttribute == null)
            {
                return string.Empty;
            }

            var groupAlias = !string.IsNullOrWhiteSpace(info.GroupAttribute.ShortHand)
                ? info.GroupAttribute.ShortHand
                : info.GroupAttribute.Name;

            var commandTokens = Tokenize(info.CommandAttribute.Name);
            var commandAlias = commandTokens.Count > 0 ? string.Concat(commandTokens.Select(t => t[0])).ToLowerInvariant() : info.CommandAttribute.Name.ToLowerInvariant();
            var combined = $"{groupAlias} {commandAlias}";
            return EnsureUniqueAlias(combined, usedAliases);
        }

        private static List<string> Tokenize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            var matches = Regex.Matches(value, "[A-Z]?[a-z]+|[A-Z]+(?![a-z])|\\d+");
            if (matches.Count == 0)
            {
                return new List<string> { value.ToLowerInvariant() };
            }

            return matches.Select(m => m.Value.ToLowerInvariant()).ToList();
        }

        private string EnsureUniqueAlias(string candidate, HashSet<string> usedAliases)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }

            var alias = candidate.ToLowerInvariant().Trim();
            if (!usedAliases.Contains(alias))
            {
                return alias;
            }

            var baseAlias = alias;
            var counter = 2;
            while (usedAliases.Contains($"{baseAlias}{counter}"))
            {
                counter++;
            }

            return $"{baseAlias}{counter}";
        }

        private IEnumerable<CommandReflectionInfo> EnumeratePluginCommands()
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes())
            {
                var groupAttribute = type.GetCustomAttribute<CommandGroupAttribute>();
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var commandAttributes = method.GetCustomAttributes<CommandAttribute>();
                    foreach (var commandAttribute in commandAttributes)
                    {
                        if (string.IsNullOrWhiteSpace(commandAttribute.Name))
                        {
                            continue;
                        }

                        var route = BuildRoute(groupAttribute, commandAttribute);
                        yield return new CommandReflectionInfo(route, commandAttribute, groupAttribute, method);
                    }
                }
            }
        }

        private static CommandRoute BuildRoute(CommandGroupAttribute groupAttribute, CommandAttribute commandAttribute)
        {
            if (groupAttribute == null)
            {
                return new CommandRoute(commandAttribute.Name);
            }

            return new CommandRoute(groupAttribute.Name, commandAttribute.Name);
        }

        private void RebuildCaches()
        {
            BuildDescriptorCache();
            BuildAliasCache();
        }

        private void BuildDescriptorCache()
        {
            _descriptors.Clear();
            foreach (var info in _reflectionCache)
            {
                if (!Config.Commands.TryGetValue(info.Route.Key, out var settings))
                {
                    continue;
                }

                var aliases = new List<string>
                {
                    info.Route.ToCommandString()
                };

                if (!string.IsNullOrWhiteSpace(settings.Name))
                {
                    aliases.Add(settings.Name);
                }

                if (!string.IsNullOrWhiteSpace(settings.Shorthand))
                {
                    aliases.Add(settings.Shorthand);
                }

                if (settings.Aliases != null)
                {
                    aliases.AddRange(settings.Aliases.Where(alias => !string.IsNullOrWhiteSpace(alias)));
                }

                var distinctAliases = aliases
                    .Where(alias => !string.IsNullOrWhiteSpace(alias))
                    .Select(alias => alias.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var descriptor = new CommandDescriptor(info.Route, settings, info.CommandAttribute, info.GroupAttribute, distinctAliases);
                _descriptors[info.Route.Key] = descriptor;
            }
        }

        private void BuildAliasCache()
        {
            _aliasMap.Clear();
            foreach (var descriptor in _descriptors.Values)
            {
                foreach (var alias in descriptor.Aliases)
                {
                    RegisterAlias(alias, descriptor.Route);
                }
            }

            _orderedAliases.Clear();
            _orderedAliases.AddRange(_aliasMap.Keys);
            _orderedAliases.Sort((a, b) => b.Length.CompareTo(a.Length));
        }

        private void RegisterAlias(string alias, CommandRoute route)
        {
            if (string.IsNullOrWhiteSpace(alias))
            {
                return;
            }

            var normalized = alias.Trim().ToLowerInvariant();
            _aliasMap[normalized] = route;
        }

        private void SetupFileWatcher()
        {
            if (!Config.General.ReloadConfigOnChange) return;

            try
            {
                _fileWatcher = new FileSystemWatcher(Core.ConfigPath, ConfigFileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
                };
                _fileWatcher.Changed += OnConfigFileChanged;
                _fileWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Core.Log.LogWarning($"Could not setup file watcher for command config: {ex.Message}");
            }
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                var lastWrite = File.GetLastWriteTime(ConfigFilePath);
                if (lastWrite <= _lastWriteTime.AddMilliseconds(250)) return;

                System.Threading.Thread.Sleep(100);
                LoadConfig();
                Core.Log.LogInfo("Command configuration reloaded due to file change");
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Error reloading command configuration: {ex.Message}");
            }
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public void Dispose()
        {
            _fileWatcher?.Dispose();
        }

        private readonly struct CommandReflectionInfo
        {
            public CommandReflectionInfo(CommandRoute route, CommandAttribute commandAttribute, CommandGroupAttribute groupAttribute, MethodInfo method)
            {
                Route = route;
                CommandAttribute = commandAttribute;
                GroupAttribute = groupAttribute;
                Method = method;
            }

            public CommandRoute Route { get; }
            public CommandAttribute CommandAttribute { get; }
            public CommandGroupAttribute GroupAttribute { get; }
            public MethodInfo Method { get; }
        }
    }

    public sealed class CommandDescriptor
    {
        public CommandDescriptor(CommandRoute route, CommandSettings settings, CommandAttribute attribute, CommandGroupAttribute groupAttribute, IReadOnlyList<string> aliases)
        {
            Route = route;
            Settings = settings;
            Attribute = attribute;
            GroupAttribute = groupAttribute;
            Aliases = aliases;
        }

        public CommandRoute Route { get; }
        public CommandSettings Settings { get; }
        public CommandAttribute Attribute { get; }
        public CommandGroupAttribute GroupAttribute { get; }
        public IReadOnlyList<string> Aliases { get; }
        public bool AdminOnly => Attribute?.AdminOnly ?? Settings?.AdminOnly ?? false;
        public string Description => !string.IsNullOrWhiteSpace(Settings?.Description) ? Settings.Description : Attribute?.Description ?? string.Empty;
        public string Category => !string.IsNullOrWhiteSpace(Settings?.Category) ? Settings.Category : (AdminOnly ? "admin" : "player");
        public string PrimaryAlias => Aliases.FirstOrDefault() ?? Route.ToCommandString();
    }

    public readonly struct CommandRoute : IEquatable<CommandRoute>
    {
        public CommandRoute(params string[] segments)
        {
            Segments = segments
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .Select(segment => segment.Trim().ToLowerInvariant())
                .ToArray();
        }

        public IReadOnlyList<string> Segments { get; }

        public string Key => string.Join(".", Segments);

        public string ToCommandString() => string.Join(" ", Segments);

        public static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var segments = key.Split(new[] { '.', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.Trim().ToLowerInvariant());
            return string.Join(".", segments);
        }

        public static CommandRoute FromKey(string key)
        {
            var segments = key.Split(new[] { '.', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return new CommandRoute(segments);
        }

        public static CommandRoute FromCommandString(string command)
        {
            var segments = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return new CommandRoute(segments);
        }

        public bool Equals(CommandRoute other)
        {
            if (Segments.Count != other.Segments.Count)
            {
                return false;
            }

            for (var i = 0; i < Segments.Count; i++)
            {
                if (!Segments[i].Equals(other.Segments[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is CommandRoute other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = 17;
            foreach (var segment in Segments)
            {
                hash = hash * 31 + segment.GetHashCode();
            }

            return hash;
        }
    }
}



