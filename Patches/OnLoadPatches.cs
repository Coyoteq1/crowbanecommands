
using HarmonyLib;
using ProjectM;
using Unity.Entities;

namespace CrowbaneCommands.Patches;

[HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
public static class InitializationPatch
{
	[HarmonyPostfix]
	public static void OneShot_AfterLoad_InitializationPatch()
	{
		Core.InitializeAfterLoaded();
		Plugin.Harmony.Unpatch(typeof(SpawnTeamSystem_OnPersistenceLoad).GetMethod("OnUpdate"), typeof(InitializationPatch).GetMethod("OneShot_AfterLoad_InitializationPatch"));
	}
}

[HarmonyPatch]
public class OnLoadPatches
{
    [HarmonyPatch(typeof(LoadPersistenceSystemV2), "SetLoadState")]
    [HarmonyPostfix]
    public static void ServerStartupStateChange_Postfix(ServerStartupState.State loadState, LoadPersistenceSystemV2 __instance)
    {
        if (loadState == ServerStartupState.State.SuccessfulStartup)
        {
            Plugin.PluginLog.LogInfo("Server is online. Initializing...");
            Core.InitializeAfterLoaded();
        }
    }
}

