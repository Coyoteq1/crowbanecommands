using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrowbaneCommands.Commands.Converters;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ProjectM.Terrain;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using VampireCommandFramework;

namespace CrowbaneCommands.Commands;
internal class InfoCommands
{
	[Command("whereami", "wai", description: "Gives your current position", adminOnly: true)]
	public static void WhereAmI(ChatCommandContext ctx)
	{
		var pos = ctx.Event.SenderCharacterEntity.Read<LocalToWorld>().Position;
		ctx.Reply($"You are at {pos.x}, {pos.y}, {pos.z} on Territory Index {Core.CastleTerritory.GetTerritoryIndex(pos)}");
	}

	[Command("checklevel", "cl", description: "Check the level of a player", adminOnly: false)]
	public static void CheckLevel(ChatCommandContext ctx, FoundPlayer player)
	{
		var user = player.Value.UserEntity.Read<User>();
		var level = Core.Regions.GetPlayerMaxLevel(user.CharacterName.ToString());
		ctx.Reply($"Player {user.CharacterName} has reached level {level}");
	}

	[Command("playerinfo", "pinfo", description: "Displays information about a player.", adminOnly: true)]
	public static void PlayerInfo(ChatCommandContext ctx, FoundPlayer player)
	{
		var user = player.Value.UserEntity.Read<User>();
		var steamID = user.PlatformId;
		var name = user.CharacterName;
		var online = user.IsConnected;
        var clanName = "Clan: No clan found\n";
        var clanEntity = user.ClanEntity.GetEntityOnServer();
		var lastOnline = user.TimeLastConnected;
		var maxLevel = Core.Regions.GetPlayerMaxLevel(user.CharacterName.ToString());

		if (clanEntity != Entity.Null && clanEntity.Has<ClanTeam>())
		{
			var clanTeam = clanEntity.Read<ClanTeam>();
			clanName = $"Clan: {clanTeam.Name}\n";
		}
		
		var pos = Core.EntityManager.GetComponentData<LocalToWorld>(player.Value.CharEntity).Position;
		var posStr = $"{pos.x:f0}, {pos.z:f0}";


		var castleFound = true;
		var castleInfo = new StringBuilder();
		foreach (var castleTerritoryEntity in Helper.GetEntitiesByComponentType<CastleTerritory>())
		{
			var castleTerritory = castleTerritoryEntity.Read<CastleTerritory>();
			if (castleTerritory.CastleHeart.Equals(Entity.Null)) continue;

			var userOwner = castleTerritory.CastleHeart.Read<UserOwner>();
			if (!userOwner.Owner.GetEntityOnServer().Equals(player.Value.UserEntity)) continue;

			var region = castleTerritoryEntity.Read<TerritoryWorldRegion>().Region;
			var timeRemaining = CastleCommands.GetFuelTimeRemaining(castleTerritory.CastleHeart);
			if (!double.IsPositiveInfinity(timeRemaining))
			{
				var time = TimeSpan.FromSeconds(timeRemaining);
				castleInfo.AppendLine($"Castle {castleTerritory.CastleTerritoryIndex} in {CastleCommands.RegionName(region)} with {time:%d}d {time:%h}h {time:%m}m remaining.");
			}
			else
			{
				castleInfo.AppendLine($"Castle {castleTerritory.CastleTerritoryIndex} in {CastleCommands.RegionName(region)} that will never decay.");
			}
		}
		if(!castleFound)
			castleInfo.AppendLine("No castle found");

		ctx.Reply($"Player Info for {name}\n" +
				  $"SteamID: {steamID}\n" +
				  $"Online: {online}\n" +
				  $"Last Online: {DateTime.FromBinary(lastOnline).ToLocalTime()}\n" +
				  $"Max Level: {maxLevel}\n" +
				  clanName +
				  $"Position: {posStr}\n"+
				  castleInfo.ToString());
	}

	[Command("idcheck", description: "searches for a player by steamid", adminOnly: true)]
	public static void SteamIdCheck(ChatCommandContext ctx, ulong steamid)
	{
		foreach(var userEntity in Helper.GetEntitiesByComponentType<ProjectM.Network.User>())
		{
			var user = userEntity.Read<User>();
			if(user.PlatformId == steamid)
			{
				ctx.Reply($"User found: {user.CharacterName}");
				return;
			}
		}
		
		ctx.Reply("No user found with that steamid");
	}

	[Command("assignsteamID", "asid", description: "Assigns a steamID to a player", adminOnly: true)]
	public static void AssignSteamID(ChatCommandContext ctx, FoundPlayer player, ulong steamid)
	{
		var user = player.Value.UserEntity.Read<User>();
		user.PlatformId = steamid;
		player.Value.UserEntity.Write(user);
		ctx.Reply($"Assigned steamID {steamid} to {user.CharacterName}");
	}

	[Command("longestofflinecastles", "loc", description: "Check when a player was last online.", adminOnly: true)]
	public static void LastOnlineAll(ChatCommandContext ctx, int page=1)
	{
		var userLastSeenWithCastle = new List<User>();
		foreach (var userEntity in Helper.GetEntitiesByComponentType<User>())
		{
			var user = userEntity.Read<User>();

			var charEntity = user.LocalCharacter.GetEntityOnServer();
			if (charEntity.Equals(Entity.Null)) continue;
			var team = charEntity.Read<TeamReference>().Value;
			var teamAllies = Core.EntityManager.GetBufferReadOnly<TeamAllies>(team);

			var foundCastle = false;
			foreach (var ally in teamAllies)
			{
				if(ally.Value.Has<CastleTeamData>())
				{
					foundCastle = true;
					break;
				}
			}

			if (!foundCastle) continue;

			userLastSeenWithCastle.Add(user);
		}

		userLastSeenWithCastle.Sort((a, b) => a.TimeLastConnected.CompareTo(b.TimeLastConnected));

		const int pageSize = 5;
		var sb = new StringBuilder();
		sb.AppendLine($"Longest Offline Castle Owners (Page <color=green>{page}</color>/<color=green>{(userLastSeenWithCastle.Count+ pageSize - 1) / pageSize}</color>)");
		page = Mathf.Max(page, 1) - 1;
		foreach (var user in userLastSeenWithCastle.Skip(pageSize * page).Take(pageSize))
		{
			sb.AppendLine($"<color=white>{user.CharacterName}</color> - <color=yellow>{DateTime.FromBinary(user.TimeLastConnected).ToLocalTime().ToString("g")}</color>");
		}

		ctx.Reply(sb.ToString());
	}

}

