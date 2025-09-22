using System.Collections.Generic;
using System.Linq;
using CrowbaneCommands.Data;
using Stunlock.Core;
using VampireCommandFramework;

namespace CrowbaneCommands.Commands.Converters;
public record struct FoundPrisonerFeed(PrefabGUID Value, string Name);

public class FoundPrisonerFeedConverter : CommandArgumentConverter<FoundPrisonerFeed>
{
	public readonly static Dictionary<string, PrefabGUID> NameToPrisonerFeedPrefab = new()
	{
		{"Blood Snapper", Prefabs.FakeItem_FeedPrisoner_BloodSnapper },
		{"Corrupted Fish", Prefabs.FakeItem_FeedPrisoner_Corrupted },
		{"Fat Goby", Prefabs.FakeItem_FeedPrisoner_FatGoby },
		{"Fierce Stinger", Prefabs.FakeItem_FeedPrisoner_FierceStinger },
		{"Golden River Bass", Prefabs.FakeItem_FeedPrisoner_GoldenRiverBass },
		{"Rainbow Trout", Prefabs.FakeItem_FeedPrisoner_RainbowTrout },
		{"Rat", Prefabs.FakeItem_FeedPrisoner_Rat },
		{"Sage Fish", Prefabs.FakeItem_FeedPrisoner_SageFish },
		{"Swamp Dweller", Prefabs.FakeItem_FeedPrisoner_SwampDweller },
		{"Twilight Snapper", Prefabs.FakeItem_FeedPrisoner_TwilightSnapper },
	};

	public override FoundPrisonerFeed Parse(ICommandContext ctx, string input)
	{
		var matches = NameToPrisonerFeedPrefab.Where(kvp => kvp.Key.ToLower().Replace(" ", "").Contains(input.Replace(" ", "").ToLower()));

		if (matches.Count() == 1)
		{
			var theMatch = matches.First();
			return new FoundPrisonerFeed(theMatch.Value, theMatch.Key);
		}

		if (matches.Count() > 1)
		{
			throw ctx.Error($"Multiple feeds found matching '{input}'. Please be more specific.\n" + string.Join("\n", matches.Select(x => x.Key)));
		}

		throw ctx.Error($"Could not find prisoner feed matching '{input}'.");
	}

	static public bool Parse(string input, out FoundPrisonerFeed foundPrisonerFeed)
	{
		var matches = NameToPrisonerFeedPrefab.Where(kvp => kvp.Key.ToLower().Replace(" ", "").Contains(input.Replace(" ", "").ToLower()));

		if (matches.Count() == 1)
		{
			var theMatch = matches.First();
			foundPrisonerFeed = new FoundPrisonerFeed(theMatch.Value, theMatch.Key);
			return true;
		}

		foundPrisonerFeed = new FoundPrisonerFeed();
		return false;
	}
}

