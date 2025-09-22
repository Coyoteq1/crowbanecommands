using System;

namespace CrowbaneCommands.Attributes
{
    /// <summary>
    /// A marker attribute that indicates a command can be configured through the command configuration system
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ConfigurableCommandAttribute : Attribute
    {
        public string ConfigKey { get; }
        public string DefaultName { get; }
        public string DefaultShorthand { get; }
        public string Description { get; }
        public bool AdminOnly { get; }
        public string Usage { get; }

        public ConfigurableCommandAttribute(string configKey, string defaultName, string defaultShorthand = "", string description = "", bool adminOnly = false, string usage = "")
        {
            ConfigKey = configKey;
            DefaultName = defaultName;
            DefaultShorthand = defaultShorthand;
            Description = description;
            AdminOnly = adminOnly;
            Usage = usage;
        }

        /// <summary>
        /// Gets the configured command name, falling back to default if not configured
        /// </summary>
        public string GetConfiguredName()
        {
            if (Core.CommandConfig == null) return DefaultName;

            var settings = Core.CommandConfig.GetCommandSettings(ConfigKey);
            return settings?.Enabled == true && !string.IsNullOrEmpty(settings.Name) ? settings.Name : DefaultName;
        }

        /// <summary>
        /// Gets the configured shorthand, falling back to default if not configured
        /// </summary>
        public string GetConfiguredShorthand()
        {
            if (Core.CommandConfig == null) return DefaultShorthand;

            var settings = Core.CommandConfig.GetCommandSettings(ConfigKey);
            return settings?.Enabled == true && !string.IsNullOrEmpty(settings.Shorthand) ? settings.Shorthand : DefaultShorthand;
        }

        /// <summary>
        /// Checks if the command is enabled in configuration
        /// </summary>
        public bool IsEnabled()
        {
            if (Core.CommandConfig == null) return true;
            return Core.CommandConfig.IsCommandEnabled(ConfigKey);
        }
    }
}
