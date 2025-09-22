
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using VampireCommandFramework;
using CrowbaneCommands.Models;
using ProjectM;

namespace CrowbaneCommands
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gg.deca.VampireCommandFramework")]
    public class Plugin : BasePlugin
    {
        internal static Harmony Harmony;
        internal static ManualLogSource PluginLog;
        public static ManualLogSource LogInstance { get; private set; }

        public override void Load()
        {
            if (Application.productName != "VRisingServer")
                return;

            PluginLog = Log;
            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION} is loaded!");
            LogInstance = Log;
            Database.InitConfig();
            // Harmony patching
            Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            Harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

            // Register all commands in the assembly with VCF
            CommandRegistry.RegisterAll();

            // Initialize dynamic command system after core initialization
            Core.StartCoroutine(InitializeDynamicCommandsCoroutine());
        }

        public override bool Unload()
        {
            CommandRegistry.UnregisterAssembly();
            Harmony?.UnpatchSelf();
            return true;
        }

        public void OnGameInitialized()
        {
            if (!HasLoaded())
            {
                Log.LogDebug("Attempt to initialize before everything has loaded.");
                return;
            }

            Core.InitializeAfterLoaded();
        }

        private static bool HasLoaded()
        {
            // Hack, check to make sure that entities loaded enough because this function
            // will be called when the plugin is first loaded, when this will return 0
            // but also during reload when there is data to initialize with.
            var collectionSystem = Core.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
            return collectionSystem?.SpawnableNameToPrefabGuidDictionary.Count > 0;
        }

        private static System.Collections.IEnumerator InitializeDynamicCommandsCoroutine()
        {
            // Wait a frame to ensure everything is loaded
            yield return null;

            // Wait for core initialization
            while (!HasLoaded())
            {
                yield return new UnityEngine.WaitForSeconds(1f);
            }

            try
            {
                // Register configurable commands
                Core.DynamicCommands.RegisterConfigurableCommands();
                LogInstance.LogInfo("Dynamic command system initialized successfully");
            }
            catch (System.Exception ex)
            {
                LogInstance.LogError($"Failed to initialize dynamic command system: {ex.Message}");
            }
        }
    }
}
