using System;
using System.Linq;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace CrowbaneCommands.Patches;

internal static class AuditPatchHelper
{
	public static string ProcessDebugEvents(Entity entity, bool isBootstrapSystem = false)
	{
		var fromCharacter = entity.Read<FromCharacter>();
		var user = fromCharacter.User.Read<User>();

		var eventType = "Unknown Event";

		if (entity.Has<DestroyDebugEvent>())
		{
			eventType = "DestroyDebugEvent";
			var destroyEvent = entity.Read<DestroyDebugEvent>();
			Core.AuditService.LogDestroy(user, destroyEvent.What.ToString(), destroyEvent.Where,
				destroyEvent.PrefabGuid, destroyEvent.Position, destroyEvent.Amount);
		}
		else if (entity.Has<GiveDebugEvent>())
		{
			eventType = "GiveDebugEvent";
			var giveEvent = entity.Read<GiveDebugEvent>();
			Core.AuditService.LogGive(user, giveEvent.PrefabGuid, giveEvent.Amount);
		}
		else if (entity.Has<BecomeObserverEvent>())
		{
			eventType = "BecomeObserverEvent";
			var observerEvent = entity.Read<BecomeObserverEvent>();
			Core.AuditService.LogBecomeObserver(user, observerEvent.Mode);
		}
		else if (entity.Has<CastleHeartAdminEvent>())
		{
			eventType = "CastleHeartAdminEvent";
			var castleHeartEvent = entity.Read<CastleHeartAdminEvent>();
			Core.AuditService.LogCastleHeartAdmin(user, castleHeartEvent.EventType,
				castleHeartEvent.CastleHeart, castleHeartEvent.UserIndex);
		}
		else if (eventType == "Unknown Event")
		{
			var componentTypes = Core.EntityManager.GetComponentTypes(entity);
			try
			{
				var eventName = componentTypes.ToArray()
					.Select(x => TypeManager.GetType(x.TypeIndex).Name)
					.Where(n => n.Contains("event", System.StringComparison.InvariantCultureIgnoreCase));

				if (eventName.Any())
				{
					eventType = eventName.First();
				}
			}
			finally
			{
				componentTypes.Dispose();
			}
		}

		Core.Log.LogInfo($"{user.CharacterName} {user.IsAdmin} is sending a {eventType}");

		return eventType;
	}

	public static void SafelyProcessEntities<T>(T system, EntityQuery query, Action<Entity> processAction)
		where T : SystemBase
	{
		var entities = query.ToEntityArray(Allocator.Temp);
		try
		{
			foreach (var entity in entities)
			{
				processAction(entity);
			}
		}
		finally
		{
			entities.Dispose();
		}
	}
}

[HarmonyLib.HarmonyPatch(typeof(PlayerTeleportSystem), "OnUpdate")]
internal class PlayerTeleportSystemPatch
{
	public static void Prefix(PlayerTeleportSystem __instance)
	{
		AuditPatchHelper.SafelyProcessEntities(__instance, __instance._Query, entity =>
		{
			var teleportEvent = entity.Read<PlayerTeleportDebugEvent>();
			var fromCharacter = entity.Read<FromCharacter>();
			var user = fromCharacter.User.Read<User>();

			if (user.IsAdmin)
			{
				Core.Log.LogInfo($"{user.CharacterName} {user.IsAdmin} is teleporting {teleportEvent.Target} to {teleportEvent.Position}");
				Core.AuditService.LogMapTeleport(user, teleportEvent.Target, teleportEvent.Position);
			}
		});
	}
}

[HarmonyLib.HarmonyPatch(typeof(TeleportSystem), "OnUpdate")]
internal class TeleportSystemPatch
{
	public static void Prefix(TeleportSystem __instance)
	{
		AuditPatchHelper.SafelyProcessEntities(__instance, __instance._Query, entity =>
		{
			var teleportEvent = entity.Read<TeleportDebugEvent>();
			var fromCharacter = entity.Read<FromCharacter>();
			var user = fromCharacter.User.Read<User>();

			if (user.IsAdmin)
			{
				Core.Log.LogInfo($"{user.CharacterName} {user.IsAdmin} is teleporting {teleportEvent.Target} to {teleportEvent.Location} {teleportEvent.LocationPosition} {teleportEvent.MousePosition}");
				Core.AuditService.LogTeleport(user, teleportEvent.Target, teleportEvent.MousePosition, teleportEvent.Location, teleportEvent.LocationPosition);
			}
		});
	}
}

[HarmonyLib.HarmonyPatch(typeof(DebugEventsSystem), "OnUpdate")]
internal class DebugEventsSystemPatch
{
	public static void Prefix(DebugEventsSystem __instance)
	{
		AuditPatchHelper.SafelyProcessEntities(__instance, __instance._Query, entity =>
		{
			AuditPatchHelper.ProcessDebugEvents(entity);
		});
	}
}

[HarmonyLib.HarmonyPatch(typeof(ServerBootstrapSystem), "OnUpdate")]
internal class ServerBootstrapSystemPatch
{
	public static void Prefix(ServerBootstrapSystem __instance)
	{
		AuditPatchHelper.SafelyProcessEntities(__instance, __instance.__query_677018907_5, entity =>
		{
			AuditPatchHelper.ProcessDebugEvents(entity, isBootstrapSystem: true);
		});
	}
}

