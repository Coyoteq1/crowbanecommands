using CrowbaneCommands.Models;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using UnityEngine;
using VampireCommandFramework;

namespace CrowbaneCommands.Commands;

internal static class BloodCommands
{
	
	[Command("bloodpotion", "bp", description: "Creates a Potion with specified Blood Type, Quality, and amount", adminOnly: true)]
	public static void GiveBloodPotionCommand(ChatCommandContext ctx, BloodType type = BloodType.Frailed, float quality = 100f, int quantity = 1)
	{
		quality = Mathf.Clamp(quality, 0, 100);
		for (var i = 0; i < quantity; i++)
		{
			Entity entity = Helper.AddItemToInventory(ctx.Event.SenderCharacterEntity, new PrefabGUID(1223264867), 1);

			if(entity == Entity.Null)
			{
				ctx.Reply($"Received <color=#ff0>{i}</color> Blood Merlot of <color=#ff0>{type}</color> type of <color=#ff0>{quality}</color>% quality");
				ctx.Reply($"Inventory is full, could not add the last <color=#ff0>{quantity - i}</color> Blood Merlot");
				return;
			}

			var blood = new StoredBlood()
			{
				BloodQuality = quality,
				PrimaryBloodType = new PrefabGUID((int)type)
			};

			Core.EntityManager.SetComponentData(entity, blood);
		}
		ctx.Reply($"Received <color=#ff0>{quantity}</color> Blood Merlot of <color=#ff0>{type}</color> type of <color=#ff0>{quality}</color>% quality");
	}

	[Command("bloodpotionmix", "bpm", description: "Creates a Potion with two specified Blood Types, Qualities, secondary trait option and amount", adminOnly: true)]
	public static void GiveBloodMerlotCommand(ChatCommandContext ctx, BloodType primaryType = BloodType.Frailed, float primaryQuality = 100f, BloodType secondaryType = BloodType.Frailed, float secondaryQuality=100f, int secondaryTrait=1, int quantity = 1)
	{
		primaryQuality = Mathf.Clamp(primaryQuality, 0, 100);
		secondaryQuality = Mathf.Clamp(secondaryQuality, 0, 100);
		secondaryTrait = Mathf.Clamp(secondaryTrait, 1, 3);
		for (var i = 0; i < quantity; i++)
		{
			Entity entity = Helper.AddItemToInventory(ctx.Event.SenderCharacterEntity, new PrefabGUID(1223264867), 1);

			if (entity == Entity.Null)
			{
				ctx.Reply($"Received <color=#ff0>{i}</color> Blood Merlot of <color=#ff0>{primaryType}</color> type of <color=#ff0>{primaryQuality}</color>% quality "+
					      $"with secondary <color=#ff0>{secondaryType}</color> type of <color=#ff0>{secondaryQuality}</color>% quality and trait option {secondaryTrait}");
				ctx.Reply($"Inventory is full, could not add the last <color=#ff0>{quantity - i}</color> Blood Merlot");
				return;
			}

			var blood = new StoredBlood()
			{
				BloodQuality = primaryQuality,
				PrimaryBloodType = new PrefabGUID((int)primaryType),
				SecondaryBlood = new()
				{
					Quality = secondaryQuality,
					Type = new PrefabGUID((int)secondaryType),
					BuffIndex = (byte)(secondaryTrait - 1)
				}
			};

			Core.EntityManager.SetComponentData(entity, blood);
		}

		ctx.Reply($"Received <color=#ff0>{quantity}</color> Blood Merlot of <color=#ff0>{primaryType}</color> type of <color=#ff0>{primaryQuality}</color>% quality " +
				  $"with secondary <color=#ff0>{secondaryType}</color> type of <color=#ff0>{secondaryQuality}</color>% quality and trait option {secondaryTrait}");
	}
}

