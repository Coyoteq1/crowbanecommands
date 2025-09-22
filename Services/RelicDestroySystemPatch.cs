using HarmonyLib;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Shared;
using Unity.Collections;
using Unity.Entities;

namespace CrowbaneCommands.Services;

[HarmonyPatch(typeof(RelicDestroySystem), nameof(RelicDestroySystem.OnUpdate))]
internal class RelicDestroySystemPatch
{
	static EntityQueryDesc relicDestroyQueryDesc = new EntityQueryDesc
	{
		All = new ComponentType[] { 
			ComponentType.ReadOnly(Il2CppType.Of<ItemData>()),
			ComponentType.ReadOnly(Il2CppType.Of<Relic>()),
			ComponentType.ReadOnly(Il2CppType.Of<DestroyTag>())
		},
	};
	static EntityQuery relicDestroyQuery;

	static bool queryInitialized = false;

	public static void Prefix()
	{
		if (!queryInitialized)
		{
			var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
				.AddAll(new(Il2CppType.Of<ItemData>(), ComponentType.AccessMode.ReadOnly))
				.AddAll(new(Il2CppType.Of<Relic>(), ComponentType.AccessMode.ReadOnly))
				.AddAll(new(Il2CppType.Of<DestroyTag>(), ComponentType.AccessMode.ReadOnly));
			relicDestroyQuery = Core.EntityManager.CreateEntityQuery(ref entityQueryBuilder);
			queryInitialized = true;
		}

		var entities = relicDestroyQuery.ToEntityArray(Allocator.Temp);
		foreach (var entity in entities)
		{
			Core.SoulshardService.HandleSoulshardDestroy(entity);
		}
		entities.Dispose();
	}

	public static void Postfix()
	{
		Core.SoulshardService.RefreshWillDrop();
	}
}

