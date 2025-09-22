using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectM;
using ProjectM.Network;
using ProjectM.Terrain;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace CrowbaneCommands.Services;
internal class RegionService
{
	static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
	static readonly string REGIONS_PATH = Path.Combine(CONFIG_PATH, "regions.json");
	static readonly string MAXLEVELS_PATH = Path.Combine(CONFIG_PATH, "maxLevels.json");

	List<WorldRegionType> lockedRegions = [];
	Dictionary<string, int> gatedRegions = [];
	Dictionary<Entity, (WorldRegionType, Vector3)> lastValidPos = [];
	Dictionary<Entity, float> lastSentMessage = [];
	Dictionary<string, int> maxPlayerLevels = [];
	List<string> allowPlayers = [];
	Dictionary<string, List<string>> banPlayers = [];

	public IEnumerable<WorldRegionType> LockedRegions => lockedRegions;
	public IEnumerable<KeyValuePair<string, int>> GatedRegions => gatedRegions;
	public IEnumerable<string> AllowedPlayers => allowPlayers;
	public IEnumerable<KeyValuePair<string, List<string>>> BannedPlayers => banPlayers;

	struct RegionPolygon
	{
		public WorldRegionType Region;
		public Aabb Aabb;
		public float2[] Vertices;
	};

	List<RegionPolygon> regionPolygons = new();

	struct RegionFile
	{
		public WorldRegionType[] LockedRegions { get; set; }
		public Dictionary<string, int> GatedRegions { get; set; }
		public Dictionary<string, int> MaxPlayerLevels { get; set; }
		public string[] AllowPlayers { get; set; }
		public Dictionary<string, string[]> BanPlayers { get; set; }
	}

	public RegionService()
	{
		LoadRegions();

		foreach (var worldRegionPolygonEntity in Helper.GetEntitiesByComponentType<WorldRegionPolygon>(true))
		{
			var wrp = worldRegionPolygonEntity.Read<WorldRegionPolygon>();
			var vertices = Core.EntityManager.GetBuffer<WorldRegionPolygonVertex>(worldRegionPolygonEntity);

			regionPolygons.Add(
				new RegionPolygon
				{
					Region = wrp.WorldRegion,
					Aabb = wrp.PolygonBounds,
					Vertices = vertices.ToNativeArray(allocator: Allocator.Temp).ToArray().Select(x => x.VertexPos).ToArray()
				});
		}

		Core.StartCoroutine(CheckPlayerRegions());
	}

	void LoadRegions()
	{
		if (!File.Exists(REGIONS_PATH))
		{
			return;
		}

		var options = new JsonSerializerOptions
		{
			Converters = { new RegionConverter() },
			WriteIndented = true,
		};

		var json = File.ReadAllText(REGIONS_PATH);
		var regionFile = JsonSerializer.Deserialize<RegionFile>(json, options);

		lockedRegions.Clear();
		if(regionFile.LockedRegions != null)
			lockedRegions.AddRange(regionFile.LockedRegions);
		gatedRegions = regionFile.GatedRegions ?? gatedRegions;
		maxPlayerLevels = regionFile.MaxPlayerLevels ?? maxPlayerLevels;
		allowPlayers.Clear();
		if(regionFile.AllowPlayers != null)
			allowPlayers.AddRange(regionFile.AllowPlayers);

		banPlayers = new();
		if (regionFile.BanPlayers != null)
		{
			foreach (var (key, value) in regionFile.BanPlayers)
				banPlayers.Add(key, new List<string>(value));
		}
	}

	void SaveRegions()
	{
		if (!Directory.Exists(CONFIG_PATH)) Directory.CreateDirectory(CONFIG_PATH);


		var banPlayersExport = new Dictionary<string, string[]>();
		foreach (var (key, value) in banPlayers)
			banPlayersExport.Add(key, value.ToArray());

		var regionFile = new RegionFile
		{
			LockedRegions = lockedRegions.ToArray(),
			GatedRegions = gatedRegions,
			MaxPlayerLevels = maxPlayerLevels,
			AllowPlayers = allowPlayers.ToArray(),
			BanPlayers = banPlayersExport
		};

		var options = new JsonSerializerOptions
		{
			Converters = { new RegionConverter() },
			WriteIndented = true,
		};

		var json = JsonSerializer.Serialize(regionFile, options);
		File.WriteAllText(REGIONS_PATH, json);
	}

	public bool LockRegion(WorldRegionType region)
	{
		if (lockedRegions.Contains(region))
		{
			return false;
		}

		lockedRegions.Add(region);
		SaveRegions();
		return true;
	}

	public bool UnlockRegion(WorldRegionType region)
	{
		var result = lockedRegions.Remove(region);
		SaveRegions();
		return result;
	}

	public void GateRegion(WorldRegionType region, int level)
	{
		gatedRegions[region.ToString()] = level;
		SaveRegions();
	}

	public bool UngateRegion(WorldRegionType region)
	{
		var result = gatedRegions.Remove(region.ToString());
		SaveRegions();
		return result;
	}

	public void AllowPlayer(string playerName)
	{
		if(allowPlayers.Contains(playerName))
			return;
		allowPlayers.Add(playerName);
		SaveRegions();
	}

	public void RemovePlayer(string playerName)
	{
		allowPlayers.Remove(playerName);
		SaveRegions();
	}

	public void BanPlayerFromRegion(string playerName, WorldRegionType region)
	{
		if (!banPlayers.TryGetValue(playerName, out var regions))
		{
			regions = new List<string>();
			banPlayers[playerName] = regions;
		}
		regions.Add(region.ToString());
		SaveRegions();
	}

	public void UnbanPlayerFromRegion(string playerName, WorldRegionType region)
	{
		if (banPlayers.TryGetValue(playerName, out var regions))
		{
			regions.Remove(region.ToString());
			if (regions.Count == 0)
				banPlayers.Remove(playerName);
		}
		SaveRegions();
	}

	

	public int GetPlayerMaxLevel(string playerName)
	{
		if (maxPlayerLevels.TryGetValue(playerName, out var level))
			return Mathf.FloorToInt(level);
		return 0;
	}

	IEnumerator CheckPlayerRegions()
	{
		while(true)
		{
			foreach(var userEntity in Core.Players.GetCachedUsersOnline())
			{
				if(!userEntity.Has<User>()) continue;

				var charName = userEntity.Read<User>().CharacterName.ToString();

				if(String.IsNullOrEmpty(charName)) continue;

				var charEntity = userEntity.Read<User>().LocalCharacter.GetEntityOnServer();
				if(!charEntity.Has<Equipment>()) continue;

				var pos = charEntity.Read<Translation>().Value;
				var currentWorldRegion = GetRegion(pos);
				var equipment = charEntity.Read<Equipment>();
				var maxLevel = Mathf.Max(Mathf.RoundToInt(equipment.ArmorLevel+equipment.SpellLevel+equipment.WeaponLevel),
										 maxPlayerLevels.TryGetValue(charName, out var cachedLevel) ? cachedLevel : 0);
				
				if (maxLevel > cachedLevel)
				{
					maxPlayerLevels[charName] = maxLevel;
					SaveRegions();
				}

				var returnReason = DisallowedFromRegion(userEntity, currentWorldRegion);
				if (returnReason != null)
				{
					ReturnPlayer(userEntity, returnReason);
				}
				else if(charEntity.Has<Dead>())
				{
					lastValidPos.Remove(userEntity);
				}
				else
				{
					lastValidPos[userEntity] = (currentWorldRegion, charEntity.Read<Translation>().Value);
				}
				yield return null;
			}
			yield return null;
		}
	}

	string DisallowedFromRegion(Entity userEntity, WorldRegionType region)
	{
		var charName = userEntity.Read<User>().CharacterName.ToString();
		if (allowPlayers.Contains(charName))
			return null;

		if (banPlayers.TryGetValue(charName, out var regions))
		{
			if (regions.Contains(region.ToString()))
				return $"You are banned from region {region.ToString()}";
		}

		if (!maxPlayerLevels.TryGetValue(charName, out var maxLevel))
			maxLevel = 0;

		if (lockedRegions.Contains(region))
		{
			return $"Can't enter region {region.ToString()} as it's locked";
		}
		else if(gatedRegions.TryGetValue(region.ToString(), out var level) && maxLevel < level)
		{
			return $"Can't enter region {region.ToString()} as it's gated to level {level} while your max reached level is only {Mathf.FloorToInt(maxLevel)}";
		}

		return null;
	}

	void ReturnPlayer(Entity userEntity, string returnReason)
	{
		var returnPos = Vector3.zero;
		if (lastValidPos.TryGetValue(userEntity, out var lastValid) && DisallowedFromRegion(userEntity, lastValid.Item1) == null)
		{
			returnPos = lastValid.Item2;
		}
		else
		{
			// Alright if they aren't in a valid region then need to find the closest waypoint that is in a valid region
			// Note not checking what is unlocked so they can return to a waypoint they haven't unlocked yet
			var waypoints = Helper.GetEntitiesByComponentType<ChunkWaypoint>();
			var waypointArray = waypoints.ToArray();
			waypoints.Dispose();

			var charPos = userEntity.Read<User>().LocalCharacter.GetEntityOnServer().Read<Translation>().Value;
			returnPos = waypointArray.Where(x =>
			{
				if (!x.Has<UserOwner>())
					return true;
				var owner = x.Read<UserOwner>().Owner.GetEntityOnServer();
				return owner == Entity.Null || owner == userEntity;
			}).
			Select(x => x.Read<Translation>().Value).
			OrderBy(waypointPos =>
			{
				var charPos = userEntity.Read<User>().LocalCharacter.GetEntityOnServer().Read<Translation>().Value;
				return Vector3.Distance(waypointPos, charPos);
			}).
			Where(waypointPos =>
			{
				var region = GetRegion(waypointPos);
				return DisallowedFromRegion(userEntity, region) == null;
			}).
			FirstOrDefault();
		}

		if (!lastSentMessage.TryGetValue(userEntity, out var lastSent) ||
						lastSent + 10 < Time.time)
		{
			FixedString512Bytes message = returnReason;
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, userEntity.Read<User>(), ref message);
			lastSentMessage[userEntity] = Time.time;
		}

		var charEntity = userEntity.Read<User>().LocalCharacter.GetEntityOnServer();
		charEntity.Write(new Translation { Value = returnPos });
		charEntity.Write(new LastTranslation { Value = returnPos });
	}

	public WorldRegionType GetRegion(float3 pos)
	{
		foreach(var worldRegionPolygon in regionPolygons)
		{
			if (worldRegionPolygon.Aabb.Contains(pos))
			{
				if (IsPointInPolygon(worldRegionPolygon.Vertices, pos.xz))
				{
					return worldRegionPolygon.Region;
				}
			}
		}
		return WorldRegionType.None;
	}

	static bool IsPointInPolygon(float2[] polygon, Vector2 point)
	{
		int intersections = 0;
		int vertexCount = polygon.Length;

		for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++)
		{
			if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
				(point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x))
			{
				intersections++;
			}
		}

		return intersections % 2 != 0;
	}


	internal class RegionConverter : JsonConverter<WorldRegionType>
	{
		public override WorldRegionType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.String)
			{
				throw new JsonException();
			}

			reader.GetString();

			foreach(var value in Enum.GetValues<WorldRegionType>())
			{
				if (value.ToString() == reader.GetString())
				{
					return value;
				}
			}

			return WorldRegionType.None;
		}

		public override void Write(Utf8JsonWriter writer, WorldRegionType value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString());
		}
	}
}

