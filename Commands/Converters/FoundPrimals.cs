using System.Collections.Generic;
using System.Linq;
using CrowbaneCommands.Data;
using ProjectM;
using Stunlock.Core;
using VampireCommandFramework;

namespace CrowbaneCommands.Commands.Converters;
public record struct FoundPrimal(PrefabGUID Value, string Name);

public class FoundPrimalConverter : CommandArgumentConverter<FoundPrimal>
{
	public readonly static Dictionary<string, PrefabGUID> NameToPrimalPrefab = new()
	{
		{"Lidia", Prefabs.CHAR_Bandit_Chaosarrow_GateBoss_Minor },
		{"Rufus", Prefabs.CHAR_Bandit_Foreman_VBlood_GateBoss_Minor },
		{"Errol", Prefabs.CHAR_Bandit_StoneBreaker_VBlood_GateBoss_Minor },
		{"Quincey", Prefabs.CHAR_Bandit_Tourok_GateBoss_Minor },
		{"Keely", Prefabs.CHAR_Frostarrow_GateBoss_Minor },
		//{"Pixie", Prefabs.CHAR_GateBoss_Pixie },
		{"Angram", Prefabs.CHAR_Gloomrot_Purifier_VBlood_GateBoss_Major },
		{"Domina", Prefabs.CHAR_Gloomrot_Voltage_VBlood_GateBoss_Major },
		{"Vincent", Prefabs.CHAR_Militia_Guard_VBlood_GateBoss_Minor },
		{"Octavian", Prefabs.CHAR_Militia_Leader_VBlood_GateBoss_Major },
		{"Poloma", Prefabs.CHAR_Poloma_VBlood_GateBoss_Minor },
		//{"Spider Melee", Prefabs.CHAR_Spider_Melee_GateBoss_Summon },
		{"Ungora", Prefabs.CHAR_Spider_Queen_VBlood_GateBoss_Major },
		{"Goreswine", Prefabs.CHAR_Undead_BishopOfDeath_VBlood_GateBoss_Minor },
		{"Leandra", Prefabs.CHAR_Undead_BishopOfShadows_VBlood_GateBoss_Major },
		{"Bane", Prefabs.CHAR_Undead_Infiltrator_VBlood_GateBoss_Major },
		{"Kriig", Prefabs.CHAR_Undead_Leader_Vblood_GateBoss_Minor },
		//{"Shadow Soldier", Prefabs.CHAR_Undead_ShadowSoldier_GateBoss },
		{"Foulrot", Prefabs.CHAR_Undead_ZealousCultist_VBlood_GateBoss_Major },
		{"Jade", Prefabs.CHAR_VHunter_Jade_VBlood_GateBoss_Major },
		{"Tristan", Prefabs.CHAR_VHunter_Leader_GateBoss_Minor },
		{"Ben", Prefabs.CHAR_Villager_CursedWanderer_VBlood_GateBoss_Major },
		{"Frostmaw", Prefabs.CHAR_Wendigo_GateBoss_Major },
		{"Willfred", Prefabs.CHAR_WerewolfChieftain_VBlood_GateBoss_Major },
		{"Terrorclaw", Prefabs.CHAR_Winter_Yeti_VBlood_GateBoss_Major }

	};

	public readonly static Dictionary<PrefabGUID, string> VBloodPrefabToName = NameToPrimalPrefab.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

	public override FoundPrimal Parse(ICommandContext ctx, string input)
	{
		// First check if the string is a boss PrefabGUID
		if (Core.Prefabs.TryGetItem(input, out var prefab) && VBloodPrefabToName.TryGetValue(prefab, out var name))
		{
			return new FoundPrimal(prefab, name);
		}

		var matches = NameToPrimalPrefab.Where(kvp => kvp.Key.ToLower().Replace(" ", "").Contains(input.Replace(" ", "").ToLower()));

		if (matches.Count() == 1)
		{
			var theMatch = matches.First();
			return new FoundPrimal(theMatch.Value, theMatch.Key);
		}

		if (matches.Count() > 1)
		{
			throw ctx.Error($"Multiple bosses found matching {input}. Please be more specific.\n" + string.Join("\n", matches.Select(x => x.Key)));
		}

		throw ctx.Error("Could not find boss");
	}

	static public bool Parse(string input, out FoundPrimal foundVBlood)
	{
		// First check if the string is a boss PrefabGUID
		if (Core.Prefabs.TryGetItem(input, out var prefab) && VBloodPrefabToName.TryGetValue(prefab, out var name))
		{
			foundVBlood = new FoundPrimal(prefab, name);
			return true;
		}

		var matches = NameToPrimalPrefab.Where(kvp => kvp.Key.ToLower().Replace(" ", "").Contains(input.Replace(" ", "").ToLower()));

		if (matches.Count() == 1)
		{
			var theMatch = matches.First();
			foundVBlood = new FoundPrimal(theMatch.Value, theMatch.Key);
			return true;
		}

		foundVBlood = new FoundPrimal();
		return false;
	}
}

