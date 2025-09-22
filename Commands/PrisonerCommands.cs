using CrowbaneCommands.Services;
using static CrowbaneCommands.Commands.SpawnCommands;
using VampireCommandFramework;
using Stunlock.Core;
using CrowbaneCommands.Data;
using CrowbaneCommands.Commands.Converters;

namespace CrowbaneCommands.Commands;

[CommandGroup("prisoner")]
internal class PrisonerCommands
{
	[Command("gruel", description: "adjust gruel details", adminOnly: true)]
	public static void AdjustGruel(ChatCommandContext ctx, float chance, float min, float max)
	{
		max /= 100;
		min /= 100;
		chance /= 100;
		PrisonerService.GruelChange(chance, min, max);
		Core.ConfigSettings.GruelMutantChance = chance;
		Core.ConfigSettings.GruelBloodMin = min;
		Core.ConfigSettings.GruelBloodMax = max;
		ctx.Reply($"Transform chance set to {chance * 100}%. Blood Quality increase range set to {min * 100}% - {max * 100}%");
	}

	[Command("grueltransform", description: "set the transform for gruel", adminOnly: true)]
	public static void GruelTransform(ChatCommandContext ctx, CharacterUnit prefab)
	{
		PrisonerService.GruelTransform(prefab.Prefab);
		Core.ConfigSettings.GruelTransform = prefab.Prefab;
		ctx.Reply($"Gruel transform set to {prefab.Name}");
	}
	[Command("gruelsettings", description: "show current gruel settings", adminOnly: true)]
    public static void ShowGruelSettings(ChatCommandContext ctx)
    {
        var prefabInt = Core.ConfigSettings.GruelTransform;
		var prefabInt2 = Character.NameFromPrefab[prefabInt.GuidHash];
		var prefabName = Character.NameFromPrefab.TryGetValue(prefabInt.GuidHash, out var name) ? name : prefabInt2;

		ctx.Reply($"Chance: {Core.ConfigSettings.GruelMutantChance * 100}%. Quality increase: {Core.ConfigSettings.GruelBloodMin * 100}% - {Core.ConfigSettings.GruelBloodMax * 100}%. Transform: {prefabName}");
    }

	[Command("feed", description: "Change one of the feed items that isn't gruel", adminOnly: true)]
	public static void ChangeFeed(ChatCommandContext ctx, FoundPrisonerFeed feed,
		                          float healthChangeMin, float healthChangeMax,
								  float miseryChangeMin, float miseryChangeMax,
								  float bloodQualityChangeMin, float bloodQualityChangeMax)
	{
		Core.Prisoners.ChangeFeed(feed.Value, healthChangeMin, healthChangeMax, miseryChangeMin, miseryChangeMax, bloodQualityChangeMin, bloodQualityChangeMax);
		ctx.Reply($"Changed settings for {feed.Name}");
	}

	[Command("feeddefault", description: "Restores a feed prisoner to default settings", adminOnly: true)]
	public static void DefaultFeed(ChatCommandContext ctx, FoundPrisonerFeed feed)
	{
		Core.Prisoners.ResetToDefault(feed.Value);
		ctx.Reply($"Feeding {feed.Name} is reset to default");
	}

	[Command("feedsettings", description: "Show settings of a feed prisoner", adminOnly: true)]
	public static void FeedSettings(ChatCommandContext ctx, FoundPrisonerFeed feed)
	{
		if (Core.ConfigSettings.PrisonerFeeds.TryGetValue(feed.Value._Value, out var prisonerFeed))
		{
			ctx.Reply($"Prisoner Feed Settings for {feed.Name}\n" +
					  $"Health Change: {prisonerFeed.HealthChangeMin} - {prisonerFeed.HealthChangeMax}\n" +
					  $"Misery Change: {prisonerFeed.MiseryChangeMin} - {prisonerFeed.MiseryChangeMax}\n" +
					  $"Blood Quality Change: {prisonerFeed.BloodQualityChangeMin} - {prisonerFeed.BloodQualityChangeMax}");
		}
		else if (Core.Prisoners.defaultPrisonerFeeds.TryGetValue(feed.Value, out prisonerFeed))
		{

			ctx.Reply($"Prisoner Feed Settings are Default for {feed.Name}\n" +
					  $"Health Change: {prisonerFeed.HealthChangeMin} - {prisonerFeed.HealthChangeMax}\n" +
					  $"Misery Change: {prisonerFeed.MiseryChangeMin} - {prisonerFeed.MiseryChangeMax}\n" +
					  $"Blood Quality Change: {prisonerFeed.BloodQualityChangeMin} - {prisonerFeed.BloodQualityChangeMax}");
		}
		else
		{
			ctx.Reply($"Prisoner Feed Settings unknown for {feed.Name}");
		}
	}
}

