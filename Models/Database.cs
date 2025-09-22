using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ProjectM.Network;
using Unity.Entities;

namespace CrowbaneCommands.Models;
public readonly struct Database
{
	private static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, "CrowbaneCommands");
	private static readonly string STAFF_PATH = Path.Combine(CONFIG_PATH, "staff.json");
	private static readonly string NOSPAWN_PATH = Path.Combine(CONFIG_PATH, "nospawn.json");
	private static readonly string CANWIPE_PATH = Path.Combine(CONFIG_PATH, "canwipe.txt");
	private static readonly string AUTOADMIN_PATH = Path.Combine(CONFIG_PATH, "autoadmin.txt");

	public static void InitConfig()
	{
		string json;
		Dictionary<string, string> dict;

		STAFF.Clear();
		NOSPAWN.Clear();
		CANWIPE.Clear();
		AUTOADMIN.Clear();

		if (File.Exists(STAFF_PATH))
		{
			json = File.ReadAllText(STAFF_PATH);
			dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

			foreach (var kvp in dict)
			{
				STAFF[kvp.Key] = kvp.Value;
			}
		}
		else {
			SaveStaff();
		}

		if (File.Exists(NOSPAWN_PATH))
		{
			json = File.ReadAllText(NOSPAWN_PATH);
			dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

			foreach (var kvp in dict)
			{
				NOSPAWN[kvp.Key] = kvp.Value;
			}
		}
		else
		{
			NOSPAWN["CHAR_VampireMale"] = "Enters a player with no string for name, causes some mods issues.";
			NOSPAWN["CHAR_Mount_Horse_Gloomrot"] = "it causes an instant server crash.";
			NOSPAWN["CHAR_Mount_Horse_Vampire"] = "it causes an instant server crash.";
			NOSPAWN["CHAR_Vampire_Ghost"] = "It is weird. Lets not.";
			SaveNoSpawn();
		}

		if (File.Exists(CANWIPE_PATH))
		{
			CANWIPE.AddRange(File.ReadAllText(CANWIPE_PATH).Split("\n").Select(x => x.Trim()));
		}
		else
		{
			SaveCanWipe();
		}

		if (File.Exists(AUTOADMIN_PATH))
		{
			AUTOADMIN.AddRange(File.ReadAllText(AUTOADMIN_PATH).Split("\n").Select(x => x.Trim()));
		}
		else
		{
			SaveAutoAdmin();
		}
	}

	static void WriteConfig(string path, Dictionary<string, string> dict)
	{
		if (!Directory.Exists(CONFIG_PATH)) Directory.CreateDirectory(CONFIG_PATH);
		var json = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(path, json);
	}

	static public void SaveStaff()
	{
		WriteConfig(STAFF_PATH, STAFF);
	}

	static public void SaveNoSpawn()
	{
		WriteConfig(NOSPAWN_PATH, NOSPAWN);
	}

	static public void SaveCanWipe()
	{
		if (!Directory.Exists(CONFIG_PATH)) Directory.CreateDirectory(CONFIG_PATH);
		File.WriteAllText(CANWIPE_PATH, string.Join("\n\r", CANWIPE));
	}

	static public void SaveAutoAdmin()
	{
		if (!Directory.Exists(CONFIG_PATH)) Directory.CreateDirectory(CONFIG_PATH);
		File.WriteAllText(AUTOADMIN_PATH, string.Join("\n\r", AUTOADMIN));
	}

	static public void SetStaff(Entity userEntity, string rank)
	{
		var user = userEntity.Read<User>();
		STAFF[user.PlatformId.ToString()] = rank;
		SaveStaff();
		Core.Log.LogWarning($"User {user.CharacterName} added to staff config as {rank}.");
	}

	static public void SetNoSpawn(string prefabName, string reason)
	{
		NOSPAWN[prefabName] = reason;
		SaveNoSpawn();
		Core.Log.LogWarning($"NPC {prefabName} is banned from spawning because {reason}.");
	}

	static public void SetAutoAdmin(Entity userEntity)
	{
		AUTOADMIN.Add(userEntity.Read<User>().PlatformId.ToString());
		SaveAutoAdmin();
		Core.Log.LogWarning($"User {userEntity.Read<User>().CharacterName} added to autoadmin list.");
	}

	static public bool IsSpawnBanned(string prefabName, out string reason)
	{
		return NOSPAWN.TryGetValue(prefabName, out reason);
	}

	private static readonly Dictionary<string, string> STAFF = new()
	{
		{ "SteamID1", "[Rank]" },
		{ "SteamID2", "[Rank]" }
	};

	private static readonly Dictionary<string, string> NOSPAWN = new()
	{
		{ "PrefabGUID", "Reason" }
	};

	private static readonly List<string> AUTOADMIN = new();

	public static Dictionary<string, string> GetStaff()
	{
		return STAFF;
	}

	public static List<string> GetAutoAdmin()
	{
		return AUTOADMIN;
	}

	public static bool RemoveStaff(Entity userEntity)
	{
		var removed = STAFF.Remove(userEntity.Read<User>().PlatformId.ToString());
		if (removed)
		{
			SaveStaff();
			Core.Log.LogWarning($"User {userEntity.Read<User>().CharacterName} removed from staff config.");
		}
		else
		{
			Core.Log.LogInfo($"User {userEntity.Read<User>().CharacterName} attempted to be removed from staff config but wasn't there.");
		}
		return removed;
	}

	public static bool RemoveAutoAdmin(Entity userEntity)
	{
		var removed = AUTOADMIN.Remove(userEntity.Read<User>().PlatformId.ToString());
		if (removed)
		{
			SaveAutoAdmin();
			Core.Log.LogWarning($"User {userEntity.Read<User>().CharacterName} removed from autoadmin list.");
		}
		else
		{
			Core.Log.LogInfo($"User {userEntity.Read<User>().CharacterName} attempted to be removed from autoadmin list but wasn't there.");
		}
		return removed;
	}
	private static List<string> CANWIPE = new();


	public static bool CanWipe(Entity userEntity)
	{
		return CANWIPE.Contains(userEntity.Read<User>().PlatformId.ToString());
	}
}

