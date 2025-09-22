using HarmonyLib;
using CrowbaneCommands.Data;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;

namespace CrowbaneCommands.Patches;

[HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
internal class AbilityRunScriptsSystemPatch
{
	public static void Prefix(AbilityRunScriptsSystem __instance)
	{
		var entities = __instance._OnCastStartedQuery.ToEntityArray(Allocator.Temp);
		foreach (var entity in entities)
		{
			var acse = entity.Read<AbilityCastStartedEvent>();
			if (!Core.ConfigSettings.SoulshardsFlightRestricted && acse.Ability.Read<PrefabGUID>() == Prefabs.AB_Shapeshift_Bat_TakeFlight_Cast)
				Core.GearService.SetShardsRestricted(false);
		}
		entities.Dispose();
	}

	public static void Postfix(AbilityRunScriptsSystem __instance)
	{
		var entities = __instance._OnCastStartedQuery.ToEntityArray(Allocator.Temp);
		foreach (var entity in entities)
		{
			var acse = entity.Read<AbilityCastStartedEvent>();
			if (!Core.ConfigSettings.SoulshardsFlightRestricted && acse.Ability.Read<PrefabGUID>() == Prefabs.AB_Shapeshift_Bat_TakeFlight_Cast)
				Core.GearService.SetShardsRestricted(true);
		}
		entities.Dispose();
	}
}

