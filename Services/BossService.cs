using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrowbaneCommands.Commands.Converters;
using CrowbaneCommands.Data;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace CrowbaneCommands.Services;
internal class BossService
{
	static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
	static readonly string BOSS_PATH = Path.Combine(CONFIG_PATH, "boss.json");
	
	List<FoundVBlood> lockedBosses = [];
	public IEnumerable<PrefabGUID> LockedBosses => lockedBosses.Select(x => x.Value);

	public IEnumerable<string> LockedBossNames => lockedBosses.Select(boss => boss.Name);

	struct BossFile
	{
		public FoundVBlood[] LockedBosses { get; set; }
	}

	public BossService()
	{
		LoadBosses();
	}

	public void LoadBosses()
	{
		if (!File.Exists(BOSS_PATH))
		{
			return;
		}

		var options = new JsonSerializerOptions
		{
			Converters = { new FoundVBloodJsonConverter() },
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		};

		var bossFile = JsonSerializer.Deserialize<BossFile>(File.ReadAllText(BOSS_PATH), options);
		lockedBosses.AddRange(bossFile.LockedBosses);

		foreach(var boss in lockedBosses)
		{
			RemoveBoss(boss);
		}
	}

	public void SaveBosses()
	{
		var options = new JsonSerializerOptions
		{
			Converters = { new FoundVBloodJsonConverter() },
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		};

		var bossFile = new BossFile
		{
			LockedBosses = lockedBosses.ToArray()
		};

		File.WriteAllText(BOSS_PATH, JsonSerializer.Serialize(bossFile, options));
	}

	public bool LockBoss(FoundVBlood boss)
	{
		if (!lockedBosses.Contains(boss))
		{
			lockedBosses.Add(boss);

			RemoveBoss(boss);

			SaveBosses();
			return true;
		}
		return false;
	}

	private static void RemoveBoss(FoundVBlood boss)
	{
		foreach (var entity in Helper.GetEntitiesByComponentType<VBloodUnit>(includeDisabled: true).ToArray().Where(x => x.Read<PrefabGUID>().Equals(boss.Value)))
		{
			DestroyUtility.Destroy(Core.EntityManager, entity);
		}

		foreach (var entity in Helper.GetEntitiesByComponentType<VBloodUnit>(includePrefab: true).ToArray().Where(x => x.Read<PrefabGUID>().Equals(boss.Value)))
		{
			entity.Add<DestroyOnSpawn>();
		}
	}

	public bool UnlockBoss(FoundVBlood boss)
	{
		if(lockedBosses.Remove(boss))
		{
			foreach (var entity in Helper.GetEntitiesByComponentType<VBloodUnit>(includePrefab: true).ToArray().Where(x => x.Read<PrefabGUID>().Equals(boss.Value)))
			{
				if(entity.Has<Script_ApplyBuffUnderHealthThreshold_DataServer>())
				{
					entity.Write<Script_ApplyBuffUnderHealthThreshold_DataServer>(new Script_ApplyBuffUnderHealthThreshold_DataServer()
					{
						NewBuffEntity = Prefabs.Buff_General_VBlood_Downed,
						HealthFactor = 0.0f,
						ThresholdMet = false
					});
				}

				entity.Remove<DestroyOnSpawn>();
			}

			SaveBosses();
			return true;
		}
		return false;
	}

	public bool IsBossLocked(PrefabGUID boss)
	{
		return lockedBosses.Any(x => x.Value.Equals(boss));
	}
 
}

internal class FoundVBloodJsonConverter : JsonConverter<FoundVBlood>
{
	public override FoundVBlood Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.String)
		{
			throw new JsonException();
		}

		var name = reader.GetString();

		if (name.StartsWith("Primal "))
		{
			if (FoundPrimalConverter.Parse(name.Substring(7), out var foundPrimal))
			{
				return new FoundVBlood(foundPrimal.Value, name);
			}
		}

		if(FoundVBloodConverter.Parse(name, out var foundVBlood))
		{
			return foundVBlood;
		}

		throw new JsonException();
	}

	public override void Write(Utf8JsonWriter writer, FoundVBlood value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.Name);
	}
}

