using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrowbaneCommands.Commands.Converters;
using VampireCommandFramework;

namespace CrowbaneCommands.Commands;

[CommandGroup("region")]
internal class RegionCommands
{
	[Command("lock", "l", description: "Locks the specified region.", adminOnly: true)]
	public static void LockRegionCommand(ChatCommandContext ctx, FoundRegion region)
	{
		if (Core.Regions.LockRegion(region.Value))
		{
			ctx.Reply($"Locked region {region.Name}");
		}
		else
		{
			ctx.Reply($"Region {region.Name} is already locked.");
		}
	}

	[Command("unlock", "ul", description: "Unlocks the specified region.", adminOnly: true)]
	public static void UnlockRegionCommand(ChatCommandContext ctx, FoundRegion region)
	{
		if (Core.Regions.UnlockRegion(region.Value))
		{
			ctx.Reply($"Unlocked region {region.Name}");
		}
		else
		{
			ctx.Reply($"Region {region.Name} is already unlocked.");
		}
	}

	[Command("gate", "g", description: "Gates the specified region.", adminOnly: true)]
	public static void GateRegionCommand(ChatCommandContext ctx, FoundRegion region, int level)
	{
		Core.Regions.GateRegion(region.Value, level);
		ctx.Reply($"Gated region {region.Name} at level {level}");
	}

	[Command("ungate", "ug", description: "Ungates the specified region.", adminOnly: true)]
	public static void UngateRegionCommand(ChatCommandContext ctx, FoundRegion region)
	{
		if(Core.Regions.UngateRegion(region.Value))
			ctx.Reply($"Ungated region {region.Name}");
		else
			ctx.Reply($"Region {region.Name} is not gated.");
	}

	[Command("list", "l", description: "Lists all locked and gated regions.", adminOnly: false)]
	public static void ListRegionsCommand(ChatCommandContext ctx)
	{
		var lockedRegions = Core.Regions.LockedRegions.Select(x => x.ToString());
		var gatedRegions = Core.Regions.GatedRegions.Select(x => $"{x.Key} at level {x.Value}");

		var sb = new StringBuilder();
		sb.AppendLine("Locked Regions:");
		foreach(var region in lockedRegions)
		{
			if (sb.Length + region.Length > Core.MAX_REPLY_LENGTH)
			{
				ctx.Reply(sb.ToString());
				sb.Clear();
			}

			sb.AppendLine(region);
		}

		if(!lockedRegions.Any())
			sb.AppendLine("None");

		ctx.Reply(sb.ToString());
		sb.Clear();

		sb.AppendLine("Gated Regions:");
		foreach(var region in gatedRegions)
		{
			if (sb.Length + region.Length > Core.MAX_REPLY_LENGTH)
			{
				ctx.Reply(sb.ToString());
				sb.Clear();
			}

			sb.AppendLine(region);
		}

		if (!gatedRegions.Any())
			sb.AppendLine("None");

		ctx.Reply(sb.ToString());
	}

	[Command("allow", "a", description: "Allows the specified player to enter gated regions.", adminOnly: true)]
	public static void AllowPlayerCommand(ChatCommandContext ctx, FoundPlayer player)
	{
		Core.Regions.AllowPlayer(player.Value.CharacterName.ToString());
		ctx.Reply($"Allowed player {player.Value.CharacterName} to enter disallowed regions");
	}

	[Command("ban", "b", description: "Bans the specified player from entering a region.", adminOnly: true)]
	public static void BanPlayerCommand(ChatCommandContext ctx, FoundPlayer player, FoundRegion region)
	{
		Core.Regions.BanPlayerFromRegion(player.Value.CharacterName.ToString(), region.Value);

		ctx.Reply($"Banned player {player.Value.CharacterName} from entering {region.Name}");
	}

	[Command("unban", "ub", description: "Unbans the specified player from entering a region.", adminOnly: true)]
	public static void UnbanPlayerCommand(ChatCommandContext ctx, FoundPlayer player, FoundRegion region)
	{
		Core.Regions.UnbanPlayerFromRegion(player.Value.CharacterName.ToString(), region.Value);

		ctx.Reply($"Unbanned player {player.Value.CharacterName} from entering {region.Name}");
	}

	[Command("listbans", "lb", description: "Lists all players banned from entering a region.", adminOnly: true)]
    public static void ListBansCommand(ChatCommandContext ctx, FoundRegion region)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<color=red>Banned Players</color> for <color=green>{region.Name}</color>:");
        foreach (var ban in Core.Regions.BannedPlayers)
        {
            if (ban.Value.Contains(region.Name))
            {
                if (sb.Length + ban.Key.Length > Core.MAX_REPLY_LENGTH)
                {
                    ctx.Reply(sb.ToString());
                    sb.Clear();
                }

                sb.AppendLine($"<color=white>{ban.Key}</color>");
            }
        }
        ctx.Reply(sb.ToString());
    }

	[Command("remove", "r", description: "Removes the specified player from the allowed list.", adminOnly: true)]
	public static void RemovePlayerCommand(ChatCommandContext ctx, FoundPlayer player)
	{
		Core.Regions.RemovePlayer(player.Value.CharacterName.ToString());
		ctx.Reply($"Removed player {player.Value.CharacterName} from being able to enter disallowed regions");
	}

	[Command("listplayers", "lp", description: "Lists all players allowed to enter disallowed regions.", adminOnly: true)]
	public static void ListPlayersCommand(ChatCommandContext ctx)
	{;
		var sb = new StringBuilder();
		sb.AppendLine("Allowed Players:");
		foreach(var player in Core.Regions.AllowedPlayers)
		{
			if (sb.Length + player.Length > Core.MAX_REPLY_LENGTH)
			{
				ctx.Reply(sb.ToString());
				sb.Clear();
			}

			sb.AppendLine(player);
		}

		if (!Core.Regions.AllowedPlayers.Any())
			sb.AppendLine("None");

		ctx.Reply(sb.ToString());
	}
}

