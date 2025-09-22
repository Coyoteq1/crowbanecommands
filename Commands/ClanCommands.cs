using System;
using System.Collections.Generic;
using System.Linq;
using CrowbaneCommands.Commands.Converters;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ProjectM.Terrain;
using Unity.Entities;
using UnityEngine;
using VampireCommandFramework;

namespace CrowbaneCommands.Commands;

[CommandGroup("coven", "cv")]
class ClanCommands
{
    [Command("add", "a", description: "Adds a player to a clan", adminOnly: true)]
    public static void AddToClan(ChatCommandContext ctx, OnlinePlayer playerToAdd, string clanName)
    {
        var userToAddEntity = playerToAdd.Value.UserEntity;
		var user = userToAddEntity.Read<User>();
		var limitType = CastleHeartLimitType.User;
		if (!user.ClanEntity.Equals(NetworkedEntity.Empty))
		{
			var clanTeam = user.ClanEntity.GetEntityOnServer().Read<ClanTeam>();
			ctx.Reply($"Player is in an existing clan of '{clanTeam.Name}'");
			return;
		}

		if (!FindClan(clanName, out var clanEntity))
        {
            ctx.Reply($"No clan found matching name '{clanName}'");
            return;
        }

        TeamUtility.AddUserToClan(Core.EntityManager, clanEntity, userToAddEntity, ref user, limitType);
        userToAddEntity.Write<User>(user);

        var members = Core.EntityManager.GetBuffer<ClanMemberStatus>(clanEntity);
        var userBuffer = Core.EntityManager.GetBuffer<SyncToUserBuffer>(clanEntity);

        for (var i = 0; i < members.Length; ++i)
        {
            var member = members[i];
            var userBufferEntry = userBuffer[i];
            var userToTest = userBufferEntry.UserEntity.Read<User>();
            if (userToTest.CharacterName.Equals(user.CharacterName))
            {
                member.ClanRole = ClanRoleEnum.Member;
                members[i] = member;
            }
        }

        ctx.Reply($"{playerToAdd.Value.CharacterName} added to clan {clanEntity.Read<ClanTeam>().Name}");
    }

	[Command("kick", "k", description: "Removes a player from a clan", adminOnly: true)]
	public static void RemoveFromClan(ChatCommandContext ctx, OnlinePlayer playerToRemove)
	{
		var clanEntity = playerToRemove.Value.UserEntity.Read<User>().ClanEntity.GetEntityOnServer();
		if (clanEntity.Equals(Entity.Null))
		{
			ctx.Reply("Player is not in a clan");
			return;
		}


		var members = Core.EntityManager.GetBuffer<ClanMemberStatus>(clanEntity);
		var userBuffer = Core.EntityManager.GetBuffer<SyncToUserBuffer>(clanEntity);
		bool foundLeader = false;
		FromCharacter fromCharacter = new();
		for (var i = 0; i < members.Length; ++i)
		{
			var member = members[i];
			if (member.ClanRole == ClanRoleEnum.Leader)
			{
				var userBufferEntry = userBuffer[i];
				fromCharacter = new FromCharacter()
				{
					Character = userBufferEntry.UserEntity.Read<User>().LocalCharacter.GetEntityOnServer(),
					User = userBufferEntry.UserEntity
				};
				foundLeader = true;
				break;
			}
		}

		if (!foundLeader)
		{
			ctx.Reply("No leader found in the clan");
			return;
		}

		for (var i = 0; i < members.Length; ++i)
		{
			var userBufferEntry = userBuffer[i];
			if (userBufferEntry.UserEntity.Equals(playerToRemove.Value.UserEntity))
			{
				var member = members[i];
				if (member.ClanRole == ClanRoleEnum.Leader)
				{
					ctx.Reply("Cannot remove a leader of a clan. Change their role first if you wish to kick.");
					return;
				}

				var archetype = Core.EntityManager.CreateArchetype(new ComponentType[]
				{
					ComponentType.ReadWrite<FromCharacter>(),
					ComponentType.ReadWrite<ClanEvents_Client.Kick_Request>()
				});

				var entity = Core.EntityManager.CreateEntity(archetype);
				entity.Write(fromCharacter);

				entity.Write(new ClanEvents_Client.Kick_Request()
				{
					TargetUserIndex = members[i].UserIndex
				});

				Core.Log.LogInfo($"Kicking {userBufferEntry.UserEntity.Read<User>().CharacterName}\n" +
							$"FromCharacter {fromCharacter.Character} {fromCharacter.User} TargetUserIndex: {members[i].UserIndex}");
				ctx.Reply($"{playerToRemove.Value.CharacterName} removed from clan {clanEntity.Read<ClanTeam>().Name}");
				return;
			}
		}
	}
	/*	//this was a fix for a brief issue that got patched out. Keeping it here for reference
	 *	[Command("findinvalid", "fi", description: "Finds people not in a clan but are shown in those clans", adminOnly: true)]
		public static void FindInvalidClanMembers(ChatCommandContext ctx)
		{
			var found = false;
			foreach (var userEntity in Helper.GetEntitiesByComponentType<User>())
			{
				var user = userEntity.Read<User>();
				if (!user.ClanEntity.Equals(NetworkedEntity.Empty)) continue;

				var teamReference = userEntity.Read<TeamReference>();
				if (teamReference.Value.Equals(Entity.Null)) continue;

				var teamAllies = Core.EntityManager.GetBuffer<TeamAllies>(teamReference.Value);
				var removing = new List<int>();
				for (var i = 0; i < teamAllies.Length; ++i)
				{
					var allyEntity = teamAllies[i].Value;
					if (allyEntity.Equals(Entity.Null)) continue;

					var prefabGuid = allyEntity.Read<PrefabGUID>();
					if (prefabGuid != Data.Prefabs.ClanTeam) continue;

					found = true;
					ctx.Reply($"{user.CharacterName} is in and not in \"{allyEntity.Read<ClanTeam>().Name}\"");
				}
			}

			if(!found)
				ctx.Reply("No invalid clan members found");
		}

		[Command("fix", "f", description: "Fixes person not in clan but shows in those clans", adminOnly: true)]
		public static void Fix(ChatCommandContext ctx, OnlinePlayer playerToFix)
		{
			var userEntity = ctx.Event.SenderUserEntity;
			var user = userEntity.Read<User>();
			if (!user.ClanEntity.Equals(NetworkedEntity.Empty))
			{
				ctx.Reply($"Player is already in a clan");
				return;
			}

			var teamReference = userEntity.Read<TeamReference>();
			if (teamReference.Value.Equals(Entity.Null))
			{
				ctx.Reply($"Player is not in a team");
				return;
			}

			var teamEntity = teamReference.Value;

			var teamAllies = Core.EntityManager.GetBuffer<TeamAllies>(teamEntity);
			var removing = new List<int>();
			for(var i=0; i<teamAllies.Length; ++i)
			{
				var allyEntity = teamAllies[i].Value;
				var prefabGuid = allyEntity.Read<PrefabGUID>();
				if (prefabGuid != Data.Prefabs.ClanTeam) continue;

				ctx.Reply($"Removing from clan \"{allyEntity.Read<ClanTeam>().Name}\"");
				removing.Add(i);

				var members = Core.EntityManager.GetBuffer<ClanMemberStatus>(allyEntity);
				var snapshotMembers = Core.EntityManager.GetBuffer<Snapshot_ClanMemberStatus>(allyEntity);
				var userBuffer = Core.EntityManager.GetBuffer<OnlySyncToUserBuffer>(allyEntity);
				var clanTeamAllies = Core.EntityManager.GetBuffer<TeamAllies>(allyEntity);
				for (var j = 0; j < members.Length; ++j)
				{
					var userBufferEntry = userBuffer[j];
					if (userBufferEntry.UserEntity.Equals(userEntity))
					{
						members.RemoveAt(j);
						snapshotMembers.RemoveAt(j);
						userBuffer.RemoveAt(j);
						clanTeamAllies.RemoveAt(j);
						break;
					}
				}

				var users = userBuffer.AsNativeArray().ToArray().Select(x => x.UserEntity).ToArray();
				foreach (var castle in Helper.GetEntitiesByComponentType<CastleHeart>())
				{
					var owner = castle.Read<UserOwner>().Owner.GetEntityOnServer();
					if (users.Contains(owner))
					{
						// Remove the kicked person from the other clan member's castles
						var castleMemberNames = Core.EntityManager.GetBuffer<CastleMemberNames>(castle);
						var snapshotCastleMemberNames = Core.EntityManager.GetBuffer<Snapshot_CastleMemberNames>(castle);
						for (var j = 0; i < castleMemberNames.Length; ++j)
						{
							if (castleMemberNames[j].Name.Equals(user.CharacterName))
							{
								Debug.Log($"Removing {user.CharacterName} from {owner.Read<User>().CharacterName}'s castle");
								castleMemberNames.RemoveAt(j);
								snapshotCastleMemberNames.RemoveAt(j);
								break;
							}
						}
					}
				}
			}

			removing.Reverse();
			foreach (var i in removing)
			{
				teamAllies.RemoveAt(i);
			}

			// Need to change team of char entity
			var newTeamValue = Helper.GetEntitiesByComponentType<TeamData>().ToArray().Select(x => x.Read<TeamData>().TeamValue).Aggregate((x, y) => x > y ? x : y) + 1;

			var team = userEntity.Read<Team>();
			var oldTeamValue = team.Value;
			team.Value = newTeamValue;
			userEntity.Write<Team>(team);

			var userTeam = (Entity)userEntity.Read<TeamReference>().Value;
			var td = userTeam.Read<TeamData>();
			td.TeamValue = newTeamValue;
			userTeam.Write<TeamData>(td);
			var userTeamAllies = Core.EntityManager.GetBuffer<TeamAllies>(userTeam);
			for (var i = userTeamAllies.Length - 1; i > 0; --i)
			{
				if (userTeamAllies[i].Value.Has<CastleTeamData>())
				{
					td = userTeamAllies[i].Value.Read<TeamData>();
					td.TeamValue = newTeamValue;
					userTeamAllies[i].Value.Write<TeamData>(td);

					var castleHeart = userTeamAllies[i].Value.Read<CastleTeamData>().CastleHeart;
					if (castleHeart.Has<TeamData>())
					{
						var ctd = castleHeart.Read<TeamData>();
						ctd.TeamValue = newTeamValue;
						castleHeart.Write<TeamData>(ctd);
					}
					if (castleHeart.Has<Team>())
					{
						var ctd = castleHeart.Read<Team>();
						ctd.Value = newTeamValue;
						castleHeart.Write<Team>(ctd);
					}
				}
			}

			foreach (var userOwned in Helper.GetEntitiesByComponentType<UserOwner>(true))
			{
				if (userOwned.Read<UserOwner>().Owner.Equals(userEntity) && userOwned.Has<Team>())
				{
					var t = userOwned.Read<Team>();
					t.Value = newTeamValue;
					userOwned.Write<Team>(t);
				}
			}

			// Remove the player from the castle hearts
			ctx.Reply($"Fixed {playerToFix.Value.CharacterName}");
		}*/


	[Command("list", "l", description: "List clans on the server")]
    public static void ListClans(ChatCommandContext ctx, int page = 1)
    {
        var clanList = new List<string>();
        foreach (var clan in Helper.GetEntitiesByComponentType<ClanTeam>())
        {
            var members = Core.EntityManager.GetBuffer<ClanMemberStatus>(clan);
            if (members.Length == 0) continue;

            var clanTeam = clan.Read<ClanTeam>();
            clanList.Add($"{clanTeam.Name} - {clanTeam.Motto}");
        }

        // Set newest clans first
        clanList.Reverse();

        const int clanBatchSize = 8;
        // Group the clans into batches
        var groupedClans = clanList
            .Select((name, index) => new { Index = index, Value = name })
            .GroupBy(x => x.Index / clanBatchSize)
            .Select(group => group.Select(x => x.Value)).ToList();

        var totalPages = groupedClans.Count;
        if (totalPages == 0)
        {
            ctx.Reply("No Clans");
            return;
        }

        page = Mathf.Clamp(page, 1, totalPages);

        ctx.Reply($"Clans (Page {page}/{totalPages})\n" + String.Join("\n", groupedClans[page - 1]));
    }


    [Command("members", "m", description: "List members")]
    public static void ListClanMembers(ChatCommandContext ctx, string clanName)
    {
        if (!FindClan(clanName, out var clanEntity))
        {
            ctx.Reply($"No clan found matching name '{clanName}'");
            return;
        }

        var members = Core.EntityManager.GetBuffer<ClanMemberStatus>(clanEntity);
        var memberList = new List<string>();
        var userBuffer = Core.EntityManager.GetBuffer<SyncToUserBuffer>(clanEntity);

        for (var i = 0; i < members.Length; ++i)
        {
            var member = members[i];
            var userBufferEntry =	userBuffer[i];
            var user = userBufferEntry.UserEntity.Read<User>();
			memberList.Add($"{user.CharacterName} - {member.ClanRole}");
        }

        ctx.Reply($"Members in Clan '{clanEntity.Read<ClanTeam>().Name}'\n" + string.Join("\n", memberList));
    }


    [Command("changerole", "cr", description: "Change clan role of a player", adminOnly: true)]
    public static void ChangeClanRole(ChatCommandContext ctx, OnlinePlayer player, ClanRoleEnum newRole)
    {
        var user = player.Value.UserEntity.Read<User>();
        if (user.ClanEntity.Equals(NetworkedEntity.Empty))
        {
            ctx.Reply($"{player.Value.CharacterName} isn't in a clan");
            return;
        }

        var clanRole = player.Value.UserEntity.Read<ClanRole>();
        var oldRole = clanRole.Value;
        clanRole.Value = newRole;
        player.Value.UserEntity.Write<ClanRole>(clanRole);
		ctx.Reply($"Changed {player.Value.CharacterName} role from {oldRole} to {newRole}");
	}

	[Command("rename", "rn", description: "Rename a clan", adminOnly: true)]
	public static void RenameClan(ChatCommandContext ctx, string oldClanName, string newClanName, string leaderName = null)
	{
		var matchingClans = FindClans(oldClanName);

		if (matchingClans.Count == 0)
		{
			ctx.Reply($"No clan found matching name '{oldClanName}'");
			return;
		}

		if (matchingClans.Count == 1)
		{
			RenameClanEntity(matchingClans[0], newClanName);
			ctx.Reply($"Clan '{oldClanName}' renamed to '{newClanName}'");
			return;
		}

		if (leaderName != null)
		{
			Entity targetClan = new Entity();
			bool foundClan = false;

			foreach (var clan in matchingClans)
			{
				var members = Core.EntityManager.GetBuffer<ClanMemberStatus>(clan);
				var userBuffer = Core.EntityManager.GetBuffer<SyncToUserBuffer>(clan);

				// Find the leader
				for (var j = 0; j < members.Length; ++j)
				{
					if (members[j].ClanRole == ClanRoleEnum.Leader)
					{
						var userBufferEntry = userBuffer[j];
						var user = userBufferEntry.UserEntity.Read<User>();
						var currentLeaderName = user.CharacterName.ToString();

						// Case-insensitive comparison
						if (currentLeaderName.ToLower() == leaderName.ToLower())
						{
							targetClan = clan;
							foundClan = true;
							break;
						}
					}
				}

				if (foundClan) break;
			}

			if (foundClan)
			{
				var oldName = targetClan.Read<ClanTeam>().Name;
				RenameClanEntity(targetClan, newClanName);
				ctx.Reply($"Clan '{oldName}' with leader '{leaderName}' renamed to '{newClanName}'");
				return;
			}

			ctx.Reply($"No clan found with name '{oldClanName}' and leader '{leaderName}'");
		}

		ctx.Reply($"Found {matchingClans.Count} clans matching '{oldClanName}'. Please specify which one by including the leader's name:");

		for (int i = 0; i < matchingClans.Count; i++)
		{
			var clan = matchingClans[i];
			var members = Core.EntityManager.GetBuffer<ClanMemberStatus>(clan);
			var userBuffer = Core.EntityManager.GetBuffer<SyncToUserBuffer>(clan);

			// Find the leader
			string leaderNameFound = "Unknown";
			for (var j = 0; j < members.Length; ++j)
			{
				if (members[j].ClanRole == ClanRoleEnum.Leader)
				{
					var userBufferEntry = userBuffer[j];
					var user = userBufferEntry.UserEntity.Read<User>();
					leaderNameFound = user.CharacterName.ToString();
					break;
				}
			}

			ctx.Reply($"{i + 1}. '{oldClanName}' - Leader: {leaderNameFound}");
		}

		ctx.Reply($"Usage: .clan rename \"{oldClanName}\" \"{newClanName}\" \"LeaderName\"");
	}

	private static void RenameClanEntity(Entity clanEntity, string newClanName)
	{
		var clanTeam = clanEntity.Read<ClanTeam>();
		clanTeam.Name = newClanName;
		clanEntity.Write(clanTeam);
		var members = Core.EntityManager.GetBuffer<ClanMemberStatus>(clanEntity);
		var userBuffer = Core.EntityManager.GetBuffer<SyncToUserBuffer>(clanEntity);

		// Update the Clan Tag on all Members
		for (var i = 0; i < members.Length; ++i)
		{
			var member = members[i];
			var userBufferEntry = userBuffer[i];
			var user = userBufferEntry.UserEntity.Read<User>();
			var playerCharacter = user.LocalCharacter.GetEntityOnServer().Read<PlayerCharacter>();
			playerCharacter.SmartClanName = ClanUtility.GetSmartClanName(newClanName);
			user.LocalCharacter.GetEntityOnServer().Write(playerCharacter);
		}
	}

	[Command("castles", "c", description: "List castles owned by a clan", adminOnly: true)]
    public static void ListClanCastles(ChatCommandContext ctx, string clanName)
    {
        if (!FindClan(clanName, out var clanEntity))
        {
            ctx.Reply($"No clan found matching name '{clanName}'");
            return;
        }

        var teamValue = clanEntity.Read<ClanTeam>().TeamValue;
        var castleHearts = Helper.GetEntitiesByComponentType<CastleHeart>();
        var castleList = new List<string>();
        int castleCount = 0; // Initialize castle count

        foreach (var castle in castleHearts)
        {
            var heartTeam = castle.Read<Team>().Value;
            if (heartTeam != teamValue) continue;
            var ownerEntity = castle.Read<UserOwner>().Owner.GetEntityOnServer();
            var owner = ownerEntity.Read<User>();
            var castleData = castle.Read<CastleHeart>();
            var castleTerritoryEntity = castleData.CastleTerritoryEntity;
            var region = castleTerritoryEntity.Read<TerritoryWorldRegion>().Region;

            castleList.Add($"{owner.CharacterName} - {castleData.CastleTerritoryEntity.Read<CastleTerritory>().CastleTerritoryIndex} in {region} ");
            castleCount++; 
        }

        ctx.Reply($"Castles owned by Clan '{clanEntity.Read<ClanTeam>().Name}' (Total: {castleCount})\n" + string.Join("\n", castleList));
	}

	public static bool FindClan(string clanName, out Entity clanEntity)
	{
		var clans = Helper.GetEntitiesByComponentType<ClanTeam>().ToArray();
		var matchedClans = clans.Where(x => x.Read<ClanTeam>().Name.ToString().ToLower() == clanName.ToLower());

		foreach (var clan in matchedClans)
		{
			var members = Core.EntityManager.GetBuffer<ClanMemberStatus>(clan);
			if (members.Length == 0) continue;
			clanEntity = clan;
			return true;
		}
		clanEntity = new Entity();
		return false;
	}
	public static List<Entity> FindClans(string clanName)
	{
		var matchingClans = new List<Entity>();
		var clans = Helper.GetEntitiesByComponentType<ClanTeam>().ToArray();

		var matchedClans = clans.Where(x => x.Read<ClanTeam>().Name.ToString().ToLower() == clanName.ToLower());
		foreach (var clan in matchedClans)
		{
			var members = Core.EntityManager.GetBuffer<ClanMemberStatus>(clan);
			if (members.Length == 0) continue; // Skip empty clans
			matchingClans.Add(clan);
		}

		return matchingClans;
	}
}


