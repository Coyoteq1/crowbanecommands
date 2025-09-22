using System.Collections.Generic;
using CrowbaneCommands.Commands.Converters;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using UnityEngine;

namespace CrowbaneCommands.Services;
internal class PrisonerService
{
	public Dictionary<PrefabGUID, ConfigSettingsService.PrisonerFeed> defaultPrisonerFeeds = [];

	public PrisonerService()
	{
		foreach(var prefabGuid in FoundPrisonerFeedConverter.NameToPrisonerFeedPrefab.Values)
		{
			if(Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out var prefab))
			{
				var feedPrisoner = prefab.Read<FeedPrisoner>();
				defaultPrisonerFeeds.Add(prefabGuid, new ConfigSettingsService.PrisonerFeed()
				{
					HealthChangeMin = feedPrisoner.RecoverHealth_Min,
					HealthChangeMax = feedPrisoner.RecoverHealth_Max,
					MiseryChangeMin = feedPrisoner.RecoverMisery_Min,
					MiseryChangeMax = feedPrisoner.RecoverMisery_Max,
					BloodQualityChangeMin = feedPrisoner.AlterBloodQuality_Min,
					BloodQualityChangeMax = feedPrisoner.AlterBloodQuality_Max
				});
			}
		}

		GruelChange(Core.ConfigSettings.GruelMutantChance, Core.ConfigSettings.GruelBloodMin, Core.ConfigSettings.GruelBloodMax);
		GruelTransform(Core.ConfigSettings.GruelTransform);

		foreach (var entry in Core.ConfigSettings.PrisonerFeeds)
		{
			var feedPrefabGuid = new PrefabGUID(entry.Key);
			if (Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(feedPrefabGuid, out var prefab))
			{
				ChangeFeedPrisoner(prefab, entry.Value);
			}
		}
	}

	public static void GruelChange(float chance, float min, float max)
    {
		max = Mathf.Clamp(max, min, 1);
		min = Mathf.Clamp(min, 0, max);
		if (max < min) max = min;
		var gruelChance = Helper.GetEntitiesByComponentType<AffectPrisonerWithToxic>(true);
        foreach (var entity in gruelChance)
        {
            var affect = entity.Read<AffectPrisonerWithToxic>();
			affect.ChanceToBecomeMutant = chance;			
			affect.IncreaseBloodQuality_Max = max;
            affect.IncreaseBloodQuality_Min = min;
            entity.Write(affect);
        }
        gruelChance.Dispose();
    }

	public static void GruelTransform(PrefabGUID prefab)
	{
		var gruelTransform = Helper.GetEntitiesByComponentType<AffectPrisonerWithToxic>(true);
		foreach (var entity in gruelTransform)
		{
			var affect = entity.Read<AffectPrisonerWithToxic>();
			affect.MutantType = prefab;
			entity.Write(affect);
		}
		gruelTransform.Dispose();
	}

	public void ChangeFeed(PrefabGUID feedPrefabGuid,
								  float healthChangeMin, float healthChangeMax,
								  float miseryChangeMin, float miseryChangeMax,
								  float alterBloodQualityMin, float alterBloodQualityMax)
	{
		if (Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(feedPrefabGuid, out var prefab))
		{
			var newFeed = new ConfigSettingsService.PrisonerFeed()
			{
				HealthChangeMin = healthChangeMin,
				HealthChangeMax = healthChangeMax,
				MiseryChangeMin = miseryChangeMin,
				MiseryChangeMax = miseryChangeMax,
				BloodQualityChangeMin = alterBloodQualityMin,
				BloodQualityChangeMax = alterBloodQualityMax
			};

			Core.ConfigSettings.SetPrisonerFeed(feedPrefabGuid.GuidHash, newFeed);
			ChangeFeedPrisoner(prefab, newFeed);
		}
	}

	public void ResetToDefault(PrefabGUID prefabGuid)
	{
		if (!defaultPrisonerFeeds.TryGetValue(prefabGuid, out var prisonerFeed)) return;
		if (!Core.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out var prefab)) return;
		Core.ConfigSettings.ClearPrisonerFeed(prefabGuid.GuidHash);
		ChangeFeedPrisoner(prefab, prisonerFeed);
	}

	void ChangeFeedPrisoner(Entity prefab, ConfigSettingsService.PrisonerFeed prisonerFeed)
	{
		var feedPrisoner = new FeedPrisoner()
		{
			RecoverHealth_Min = prisonerFeed.HealthChangeMin,
			RecoverHealth_Max = prisonerFeed.HealthChangeMax,
			RecoverMisery_Min = prisonerFeed.MiseryChangeMin,
			RecoverMisery_Max = prisonerFeed.MiseryChangeMax,
			AlterBloodQuality_Min = prisonerFeed.BloodQualityChangeMin,
			AlterBloodQuality_Max = prisonerFeed.BloodQualityChangeMax,
		};

		if (prisonerFeed.BloodQualityChangeMin!=0 || prisonerFeed.BloodQualityChangeMax!=0)
		{
			feedPrisoner.BuffIncresaeBloodQualityFail = Data.Prefabs.PrisonerBloodQualityChangeFailBuff;
			feedPrisoner.BuffIncresaeBloodQualitySuccess = Data.Prefabs.PrisonerBloodQualityChangeSuccessBuff;
		}

		prefab.Write(feedPrisoner);
	}

}

