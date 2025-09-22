using System;
using System.Collections.Generic;
using Stunlock.Core;

namespace CrowbaneCommands.Data;
internal static class Character
{
	public static void Populate()
	{
		foreach(var prefab in Core.PrefabCollectionSystem._PrefabGuidToEntityMap)
		{
			var name = Core.PrefabCollectionSystem._PrefabLookupMap.GetName(prefab.Key);
			if (!name.StartsWith("CHAR")) continue;
			Named[name] = prefab.Key;
			NameFromPrefab[prefab.Key.GuidHash] = name;
		}
	}
	public static Dictionary<string, PrefabGUID> Named = new(StringComparer.OrdinalIgnoreCase);
	public static Dictionary<int, string> NameFromPrefab = new();

}

