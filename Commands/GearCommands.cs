using System.Text;
using CrowbaneCommands.Commands.Converters;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using System.Collections.Generic;
using System.Collections;
using Unity.Transforms;
using UnityEngine;

namespace CrowbaneCommands.Commands;
internal static class DurabilityCommands
{
	[CommandGroup("item", "it")]
	internal class GearCommands
	{
		[Command("repair", "r", description: "Repairs all gear.", adminOnly: true)]
		public static void RepairCommand(ChatCommandContext ctx, FoundPlayer player = null)
		{
			var targetEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			Helper.RepairGear(targetEntity);
			ctx.Reply($"Gear repaired for {targetEntity.Read<PlayerCharacter>().Name}.");
		}

		[Command("break", "b", description: "Breaks all gear.", adminOnly: true)]
		public static void BreakGearCommand(ChatCommandContext ctx, FoundPlayer player = null)
		{
			var targetEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;
			Helper.RepairGear(targetEntity, false);
			ctx.Reply($"Gear broken for {targetEntity.Read<PlayerCharacter>().Name}.");
		}

		[Command("repairall", "ra", description: "Repairs all gear within a range.", adminOnly: true)]
		public static void RepairAllCommand(ChatCommandContext ctx, float range = 10)
		{
			var senderPos = ctx.Event.SenderCharacterEntity.Read<LocalToWorld>().Position;
			var playerEntities = Helper.GetEntitiesByComponentType<PlayerCharacter>();
			foreach (var playerEntity in playerEntities)
			{
				var pos = playerEntity.Read<LocalToWorld>().Position;
				if (Vector3.Distance(senderPos, pos) > range) continue;
				Helper.RepairGear(playerEntity);
			}

			ctx.Reply($"Gear repaired for all players within {range}m.");
		}

		[Command("breakall", "ba", description: "Breaks all gear within a range.", adminOnly: true)]
		public static void BreakAllCommand(ChatCommandContext ctx, float range = 10)
		{
			var senderPos = ctx.Event.SenderCharacterEntity.Read<LocalToWorld>().Position;
			var playerEntities = Helper.GetEntitiesByComponentType<PlayerCharacter>();
			foreach (var playerEntity in playerEntities)
			{
				var pos = playerEntity.Read<LocalToWorld>().Position;
				if (Vector3.Distance(senderPos, pos) > range) continue;
				Helper.RepairGear(playerEntity, false);
			}

			ctx.Reply($"Gear broken for all players within {range}m.");
		}

		[Command("headgear", "hg", description: "Toggles headgear loss on death.", adminOnly: true)]
		public static void HeadgearBloodBoundCommand(ChatCommandContext ctx)
		{
			if(Core.GearService.ToggleHeadgearBloodbound())
			{
				ctx.Reply("Headgear will not be lost on death.");
			}
			else
			{
				ctx.Reply("Headgear will be lost on death.");
			}
		}

		[Command("soulshardflight", "ssf", description: "Toggles soulshard flight restrictions.", adminOnly: true)]
		public static void SoulshardsFlightRestrictedCommand(ChatCommandContext ctx)
		{
			if (Core.GearService.ToggleShardsFlightRestricted())
			{
				if (Core.ServerGameSettingsSystem._Settings.BatBoundShards)
					ctx.Reply("Soulshards will not allowing flying.");
				else
					ctx.Reply("Current game settings have BatBoundShards set false which CrowbaneCommands can't override");
			}
			else
			{
				ctx.Reply("Soulshards will allow flying.");
			}
		}

		[Command("togglesoulsharddropmanagement", "tssdm", description: "Toggles whether CrowbaneCommands will do soulshard drop management.", adminOnly: true)]
		public static void ToggleSoulshardDropManagementCommand(ChatCommandContext ctx)
		{
			if (Core.SoulshardService.ToggleShardDropManagement())
			{
				ctx.Reply("Soulshard drop management by CrowbaneCommands enabled.");
			}
			else
			{
				ctx.Reply("Soulshard drop management by CrowbaneCommands disabled.");
			}
		}

		[Command("soulshardlimit", "ssl", description: "How many soulshards can be dropped before a boss won't drop a new one if the relic Unique setting is active.", adminOnly: true)]
		public static void SoulshardLimitCommand(ChatCommandContext ctx, int limit, RelicType shardType=RelicType.None)
		{
			if (limit < 0)
			{
				throw ctx.Error("Limit must be zero or greater.");
			}
			Core.SoulshardService.SetShardDropLimit(limit, shardType);
			if (shardType == RelicType.None)
			{
				ctx.Reply($"Soulshard limit set to {limit} for all soulshards.");
			}
			else
			{
				ctx.Reply($"Soulshard limit set to {limit} for {shardType}.");
			}
		}

		[Command("soulshardstatus", "sss", description: "Reports the current status of soulshards.", adminOnly: false)]
		public static void SoulshardStatusCommand(ChatCommandContext ctx)
		{
			var sb = new StringBuilder();
			var soulshardStatus = Core.SoulshardService.GetSoulshardStatus();
			sb.AppendLine("\nSoulshard Status");
			var soulshardFlightAllowed = !Core.ConfigSettings.SoulshardsFlightRestricted || !Core.ServerGameSettingsSystem._Settings.BatBoundShards;
			sb.AppendLine($"Can Fly: {(soulshardFlightAllowed ? "<color=green>Yes</color>" : "<color=red>No</color>")}");

			var notPlentiful = Core.ServerGameSettingsSystem._Settings.RelicSpawnType == RelicSpawnType.Unique;

			var managesDrops = notPlentiful && Core.ConfigSettings.ShardDropManagementEnabled;
			var theMonster = soulshardStatus[(int)RelicType.TheMonster];
			var solarus = soulshardStatus[(int)RelicType.Solarus];
			var wingedHorror = soulshardStatus[(int)RelicType.WingedHorror];
			var megara = soulshardStatus[(int)RelicType.Morgana];
			var dracula = soulshardStatus[(int)RelicType.Dracula];
			sb.Append($"The Monster: <color=white>{theMonster.droppedCount}</color>x");
			if (managesDrops) sb.Append($" out of <color=white>{Core.ConfigSettings.ShardMonsterDropLimit}</color>x");
			sb.AppendLine($" dropped <color=white>{theMonster.spawnedCount}</color>x spawned{(notPlentiful ? (theMonster.willDrop ? " <color=green>Will</color> drop" : " <color=red>Won't</color> drop") : "")}");

			sb.Append($"Solarus: <color=white>{solarus.droppedCount}</color>x ");
			if (managesDrops) sb.Append($"out of <color=white>{Core.ConfigSettings.ShardSolarusDropLimit}</color>x");
			sb.AppendLine($" dropped <color=white>{solarus.spawnedCount}</color>x spawned{(notPlentiful ? (solarus.willDrop ? " <color=green>Will</color> drop" : " <color=red>Won't</color> drop") : "")}");

			ctx.Reply(sb.ToString());
			sb.Clear();

			sb.Append($"Winged Horror: <color=white>{wingedHorror.droppedCount}</color>x");
			if (managesDrops) sb.Append($" out of <color=white>{Core.ConfigSettings.ShardWingedHorrorDropLimit}</color>x");
			sb.AppendLine($" dropped <color=white>{wingedHorror.spawnedCount}</color>x spawned{(notPlentiful ? (wingedHorror.willDrop ? " <color=green>Will</color> drop" : " <color=red>Won't</color> drop") : "")}");

			sb.Append($"Megara: <color=white>{megara.droppedCount}</color>x");
			if (managesDrops) sb.Append($" out of <color=white>{Core.ConfigSettings.ShardMonsterDropLimit}</color>x");
			sb.AppendLine($" dropped <color=white>{megara.spawnedCount}</color>x spawned{(notPlentiful ? (megara.willDrop ? " <color=green>Will</color> drop" : " <color=red>Won't</color> drop") : "")}");

			sb.Append($"Dracula: <color=white>{dracula.droppedCount}</color>x");
			if (managesDrops) sb.Append($" out of <color=white>{Core.ConfigSettings.ShardDraculaDropLimit}</color>x");
			sb.AppendLine($" dropped <color=white>{dracula.spawnedCount}</color>x spawned{(notPlentiful ? (dracula.willDrop ? " <color=green>Will</color> drop" : " <color=red>Won't</color> drop") : "")}");
			ctx.Reply(sb.ToString());
		}

		[Command("soulsharddurability", "ssd", description: "Sets the durability on all soulshards in the player's inventory to a specified amount", adminOnly: true)]
		public static void ShardDurabilityCommand(ChatCommandContext ctx, float durability, FoundPlayer player = null)
		{
			var charEntity = player?.Value.CharEntity ?? ctx.Event.SenderCharacterEntity;

			if (durability < 0)
			{
				throw ctx.Error("Durability must be zero or greater.");
			}

			var shardsChanged = 0;
			foreach(var shard in Helper.GetEntitiesByComponentType<Relic>())
			{
				var relic = shard.Read<Relic>();
				if (relic.RelicType == RelicType.None) continue;

				if (!shard.Has<InventoryItem>()) continue;

			    var itemContainer = shard.Read<InventoryItem>().ContainerEntity;
				if (itemContainer.Equals(Entity.Null)) continue;

				if (!itemContainer.Has<InventoryConnection>()) continue;
				
				var inventoryOwner = itemContainer.Read<InventoryConnection>().InventoryOwner;
				if (inventoryOwner != charEntity) continue;

				var durabilityData = shard.Read<Durability>();
				durabilityData.Value = durability;
				shard.Write(durabilityData);

				shardsChanged += 1;
			}

			var playerName = charEntity.Read<PlayerCharacter>().Name;
			if (shardsChanged > 0)
				ctx.Reply($"<color=white>{shardsChanged}</color>x soulshards had their durability set to {durability} for <color=yellow>{playerName}</color>.");
			else
				ctx.Reply($"No soulshards found in <color=yellow>{playerName}</color>'s inventory.");
		}

		[Command("soulsharddurabilitytime", "ssdt", description: "How many seconds will soulshards last before they break", adminOnly: true)]
		public static void ShardDurabilityTimeCommand(ChatCommandContext ctx, int? seconds=null)
		{
			if (seconds.HasValue && seconds < 0)
			{
				throw ctx.Error("Time must be zero or greater.");
			}
			Core.SoulshardService.SetShardDurabilityTime(seconds);
			if (seconds == null)
			{
				ctx.Reply("Soulshard durability time restored to default.");
			}
			else
			{
				ctx.Reply($"Soulshard durability time set to {seconds} seconds.");
			}
		}


		[Command("destroyallshards", description: "Destroys all soulshards in the world, containers, and inventories", adminOnly: true)]
		public static void DestroyAllShards(ChatCommandContext ctx)
		{
			foreach (var shard in Helper.GetEntitiesByComponentType<Relic>())
			{
				if (!shard.Has<Durability>()) continue;

				var durability = shard.Read<Durability>();
				if (durability.Value == 0)
				{
					durability.Value = durability.MaxDurability;
					shard.Write(durability);
				}
			}
			Core.StartCoroutine(BreakShardsInAMoment(ctx));
		}

		static IEnumerator BreakShardsInAMoment(ChatCommandContext ctx)
		{
			yield return null;
			var destroyedPrefabs = new Dictionary<PrefabGUID, int>();
			foreach (var shard in Helper.GetEntitiesByComponentType<Relic>())
			{
				if (!shard.Has<Durability>()) continue;

				var durability = shard.Read<Durability>();
				if (durability.Value == 0)
					continue;

				durability.Value = 0;
				durability.DestroyItemWhenBroken = true;
				shard.Write(durability);

				if (shard.Has<PrefabGUID>())
				{
					var guid = shard.Read<PrefabGUID>();
					if (!destroyedPrefabs.TryGetValue(guid, out var count))
						count = 0;
					destroyedPrefabs[guid] = count + 1;
				}
			}

			foreach (var (guid, count) in destroyedPrefabs)
			{
				ctx.Reply($"Destroyed <color=white>{count}</color>x <color=yellow>{guid.LookupName()}</color>");
				Core.Log.LogInfo($"Destroyed {count}x {guid.LookupName()}");
			}
		}
		/*
		   [Command("showhair", "sh", description: "Toggles hair visibility.")]
		   public static void ShowHairCommand(ChatCommandContext ctx)
		   {
			   var charEntity = ctx.Event.SenderCharacterEntity;
			   var equipment = charEntity.Read<Equipment>();

			   if (equipment.ArmorHeadgearSlot.Equals(Entity.Null))
			   {
				   ctx.Reply("No headgear equipped.");
				   return;
			   }

			   var headgear = equipment.ArmorHeadgearSlot.GetType();
			   var equipmentToggleData = headgear.Read<EquipmentToggleData>();
			   equipmentToggleData.HideCharacterHairOnEquip = !equipmentToggleData.HideCharacterHairOnEquip;
			   headgear.Write(equipmentToggleData);

			   ctx.Reply("Hair is " + (equipmentToggleData.HideCharacterHairOnEquip ? " hidden" : "visible") + " with current headgear");
		   }

	   */

	}
}




