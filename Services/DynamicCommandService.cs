using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CrowbaneCommands.Attributes;
using VampireCommandFramework;

namespace CrowbaneCommands.Services
{
    public class DynamicCommandService
    {
        private readonly Dictionary<string, MethodInfo> _registeredCommands = new();
        private readonly Dictionary<string, object> _commandInstances = new();

        public void RegisterConfigurableCommands()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var commandMethods = GetConfigurableCommandMethods(assembly);

                foreach (var (method, attribute, instance) in commandMethods)
                {
                    RegisterConfigurableCommand(method, attribute, instance);
                }

                Core.Log.LogInfo($"Registered {_registeredCommands.Count} configurable commands");
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Failed to register configurable commands: {ex.Message}");
            }
        }

        private void RegisterConfigurableCommand(MethodInfo method, ConfigurableCommandAttribute attribute, object instance)
        {
            if (!attribute.IsEnabled())
            {
                Core.Log.LogDebug($"Command {attribute.ConfigKey} is disabled in configuration");
                return;
            }

            var configuredName = attribute.GetConfiguredName();
            var configuredShorthand = attribute.GetConfiguredShorthand();
            // Note: Do not construct CommandAttribute instances here. VCF picks up attributes at compile time.

            try
            {
                // Note: VCF doesn't expose RegisterCommand directly, so we'll track the configuration
                // The actual registration happens through the standard [Command] attributes

                _registeredCommands[attribute.ConfigKey] = method;
                _commandInstances[attribute.ConfigKey] = instance;

                var shorthandText = !string.IsNullOrEmpty(configuredShorthand) ? $" (shorthand: {configuredShorthand})" : "";
                Core.Log.LogDebug($"Registered command config: {configuredName}{shorthandText}");
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Failed to register command {configuredName}: {ex.Message}");
            }
        }

        private static IEnumerable<(MethodInfo method, ConfigurableCommandAttribute attribute, object instance)> GetConfigurableCommandMethods(Assembly assembly)
        {
            var commandClasses = assembly.GetTypes()
                .Where(t => t.GetMethods().Any(m => m.GetCustomAttribute<ConfigurableCommandAttribute>() != null))
                .ToList();

            foreach (var commandClass in commandClasses)
            {
                object instance = null;
                try
                {
                    // Try to create an instance of the command class
                    instance = Activator.CreateInstance(commandClass);
                }
                catch (Exception ex)
                {
                    Core.Log.LogWarning($"Could not create instance of {commandClass.Name}: {ex.Message}");
                    continue;
                }

                var methods = commandClass.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => m.GetCustomAttribute<ConfigurableCommandAttribute>() != null);

                foreach (var method in methods)
                {
                    var attribute = method.GetCustomAttribute<ConfigurableCommandAttribute>();
                    if (attribute != null)
                    {
                        yield return (method, attribute, method.IsStatic ? null : instance);
                    }
                }
            }
        }

        public void ReloadCommands()
        {
            try
            {
                // Unregister existing commands
                UnregisterAllCommands();

                // Re-register with updated configuration
                RegisterConfigurableCommands();

                Core.Log.LogInfo("Commands reloaded with updated configuration");
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Failed to reload commands: {ex.Message}");
            }
        }

        private void UnregisterAllCommands()
        {
            foreach (var kvp in _registeredCommands)
            {
                try
                {
                    // Note: VCF doesn't have a direct unregister method, so we'll need to work around this
                    // For now, we'll just clear our tracking dictionaries
                    Core.Log.LogDebug($"Unregistering command: {kvp.Key}");
                }
                catch (Exception ex)
                {
                    Core.Log.LogWarning($"Failed to unregister command {kvp.Key}: {ex.Message}");
                }
            }

            _registeredCommands.Clear();
            _commandInstances.Clear();
        }

        public bool IsCommandRegistered(string configKey)
        {
            return _registeredCommands.ContainsKey(configKey);
        }

        public IReadOnlyDictionary<string, MethodInfo> GetRegisteredCommands()
        {
            // Provide a read-only wrapper for external consumers
            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, MethodInfo>(_registeredCommands);
        }
    }
}
