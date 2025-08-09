using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Stunlock.Core;

namespace KindredCommands.Services;
internal class ConfigSettingsService
{
	private static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
	private static readonly string SETTINGS_PATH = Path.Combine(CONFIG_PATH, "settings.json");

	public bool RevealMapToAll {
		get {
			return config.RevealMapToAll;

		}
		set { 
			config.RevealMapToAll = value; 
			SaveConfig();
		}
	}

	public bool HeadgearBloodbound
	{
		get
		{
			return config.HeadgearBloodbound;
		}
		set
		{
			config.HeadgearBloodbound = value;
			SaveConfig();
		}
	}

	public IReadOnlyDictionary<string, bool> BloodBound => config.BloodBound;
	public IReadOnlyDictionary<int, PrisonerFeed> PrisonerFeeds => config.PrisonerFeeds;

	public bool SoulshardsFlightRestricted
	{
		get
		{
			return config.SoulshardsRestricted;
		}
		set
		{
			config.SoulshardsRestricted = value;
			SaveConfig();
		}
	}

	public int ItemDropLifetime
	{
		get
		{
			return config.ItemDropLifetime;
		}
		set
		{
			config.ItemDropLifetime = value;
			SaveConfig();
		}
	}

	public int ItemDropLifetimeWhenDisabled
	{
		get
		{
			return config.ItemDropLifetimeWhenDisabled;
		}
		set
		{
			config.ItemDropLifetimeWhenDisabled = value;
			SaveConfig();
		}
	}

	public int ShardDropLifetime
	{
		get
		{
			return config.ShardDropLifetime;
		}
		set
		{
			config.ShardDropLifetime = value;
			SaveConfig();
		}
	}

	public int? ShardDurabilityTime
	{
		get
		{
			return config.ShardDurabilityTime;
		}
		set
		{
			config.ShardDurabilityTime = value;
			SaveConfig();
		}
	}

	public bool ShardDropManagementEnabled
	{
		get
		{
			return config.ShardDropManagementEnabled;
		}
		set
		{
			config.ShardDropManagementEnabled = value;
			SaveConfig();
		}
	}

	public int ShardDraculaDropLimit
	{
		get
		{
			return config.ShardDraculaDropLimit ?? (config.ShardDropLimit ?? 1);
		}
		set
		{
			config.ShardDraculaDropLimit = value;
			SaveConfig();
		}
	}

	public int ShardWingedHorrorDropLimit
	{
		get
		{
			return config.ShardWingedHorrorDropLimit ?? (config.ShardDropLimit ?? 1);
		}
		set
		{
			config.ShardWingedHorrorDropLimit = value;
			SaveConfig();
		}
	}

	public int ShardMonsterDropLimit
	{
		get
		{
			return config.ShardMonsterDropLimit ?? (config.ShardDropLimit ?? 1);
		}
		set
		{
			config.ShardMonsterDropLimit = value;
			SaveConfig();
		}
	}

	public int ShardMorganaDropLimit
	{
		get
		{
			return config.ShardMorganaDropLimit ?? (config.ShardDropLimit ?? 1);
		}
		set
		{
			config.ShardMorganaDropLimit = value;
			SaveConfig();
		}
	}

	public int ShardSolarusDropLimit
	{
		get
		{
			return config.ShardSolarusDropLimit ?? (config.ShardDropLimit ?? 1);
		}
		set
		{
			config.ShardSolarusDropLimit = value;
			SaveConfig();
		}
	}

	public bool EveryoneDaywalker
	{
		get
		{
			return config.EveryoneDaywalker ?? false;
		}
		set
		{
			config.EveryoneDaywalker = value;
			SaveConfig();
		}
	}

	public float GruelMutantChance
	{
		get
		{
			return config.GruelMutantChance ?? 0.35f;
		}
		set
		{
			config.GruelMutantChance = value;
			SaveConfig();
		}
	}

	public float GruelBloodMin
	{
		get
		{
			return config.GruelBloodMin ?? 0.01f;
		}
		set
		{
			config.GruelBloodMin = value;
			SaveConfig();
		}
	}
	public float GruelBloodMax
	{
		get
		{
			return config.GruelBloodMax ?? 0.02f;
		}
		set
		{
			config.GruelBloodMax = value;
			SaveConfig();
		}
	} 
	public PrefabGUID GruelTransform
	{
		get
		{
			return config.GruelTransformPrefabInt.HasValue ? new PrefabGUID(config.GruelTransformPrefabInt.Value) 
				                                  : new PrefabGUID(-1025552087);
		}
		set
		{
			config.GruelTransformPrefabInt = value.GuidHash;
			SaveConfig();
		}
	}

	public bool BatVision
	{
		get
		{
			return config.BatVision;
		}
		set
		{
			config.BatVision = value;
			SaveConfig();
		}
	}

	public bool KitHabilitado
	{
		get
		{
			return config.KitHabilitado;
		}
		set
		{
			config.KitHabilitado = value;
			SaveConfig();
		}
	}


	public void SetBloodBound(string key, bool value)
	{
		config.BloodBound[key] = value;
		SaveConfig();
	}

	public void ClearBloodBound(IEnumerable<string> keys)
	{
		foreach (var key in keys)
		{
			config.BloodBound.Remove(key);
		}

		SaveConfig();
	}

	public void SetPrisonerFeed(int prefabGuid, PrisonerFeed value)
	{
		config.PrisonerFeeds[prefabGuid] = value;
		SaveConfig();
	}

	public void ClearPrisonerFeed(int prefabGuid)
	{
		config.PrisonerFeeds.Remove(prefabGuid);
		SaveConfig();
	}

	
	public struct PrisonerFeed
	{
		public float HealthChangeMin { get; set; }
		public float HealthChangeMax { get; set; }
		public float MiseryChangeMin { get; set; }
		public float MiseryChangeMax { get; set; }
		public float BloodQualityChangeMin { get; set; }
		public float BloodQualityChangeMax { get; set; }
	}

	struct Config
	{
		public Config()
		{
			BloodBound = [];
			SoulshardsRestricted = true;
			ItemDropLifetimeWhenDisabled = 300;
			ShardDropLimit = 1;
			ShardDropManagementEnabled = true;
			KitHabilitado = false;
		}

		public bool RevealMapToAll { get; set; }

		/// <summary>
		/// Entities whenever added or removed from blood-bound category.
		/// Key: prefab guid, Value: indicates if entity belongs to blood-bound category.
		/// </summary>
		public Dictionary<string, bool> BloodBound { get; set; }
		public bool HeadgearBloodbound { get; set; }
		public bool SoulshardsRestricted { get; set; }
		public int ItemDropLifetime { get; set; }
		public int ItemDropLifetimeWhenDisabled { get; set; }
		public int ShardDropLifetime { get; set; }
		public bool ShardDropManagementEnabled { get; set; }
		public int? ShardDropLimit { get; set; }
		public int? ShardDurabilityTime { get; set; }
		public int? ShardDraculaDropLimit { get; set; }
		public int? ShardWingedHorrorDropLimit { get; set; }
		public int? ShardMonsterDropLimit { get; set; }
		public int? ShardMorganaDropLimit { get; set; }
		public int? ShardSolarusDropLimit { get; set; }
		public bool? EveryoneDaywalker { get; set; }
		public float? GruelMutantChance { get; set; }
		public float? GruelBloodMin { get; set; }
		public float? GruelBloodMax { get; set; }
		public int? GruelTransformPrefabInt { get; set; }
		public bool BatVision { get; set; }
		public Dictionary<int, PrisonerFeed> PrisonerFeeds { get; set; } = [];
		public bool KitHabilitado { get; set; } = false;		
	}

	Config config;

	public ConfigSettingsService()
	{
		LoadConfig();

		// Log out current settings
		Core.Log.LogInfo("Current settings");
		Core.Log.LogInfo($"RevealMapToAll: {RevealMapToAll}");
		Core.Log.LogInfo($"HeadgearBloodbound: {HeadgearBloodbound}");
		Core.Log.LogInfo($"BloodBound set for: {string.Join(", ", BloodBound.Where(p => p.Value).Select(p => p.Key))}");
		Core.Log.LogInfo($"BloodBound unset for: {string.Join(", ", BloodBound.Where(p => !p.Value).Select(p => p.Key))}");
		Core.Log.LogInfo($"SoulshardsRestricted: {SoulshardsFlightRestricted}");
		Core.Log.LogInfo($"ItemDropLifetime: {ItemDropLifetime}");
		Core.Log.LogInfo($"ItemDropLifetimeWhenDisabled: {ItemDropLifetimeWhenDisabled}");
		Core.Log.LogInfo($"ShardDropLifetimeWhenDisabled: {ShardDropLifetime}");
		Core.Log.LogInfo($"ShardDraculaDropLimit: {ShardDraculaDropLimit}");
		Core.Log.LogInfo($"ShardManticoreDropLimit: {ShardWingedHorrorDropLimit}");
		Core.Log.LogInfo($"ShardMonsterDropLimit: {ShardMonsterDropLimit}");
		Core.Log.LogInfo($"ShardSolarusDropLimit: {ShardSolarusDropLimit}");
		Core.Log.LogInfo($"EveryoneDaywalker: {EveryoneDaywalker}");
		Core.Log.LogInfo($"GruelMutantChance: {GruelMutantChance}");
		Core.Log.LogInfo($"GruelBloodMin: {GruelBloodMin}");
		Core.Log.LogInfo($"GruelBloodMax: {GruelBloodMax}");
		Core.Log.LogInfo($"GruelTransformPrefabInt: {GruelTransform}");
		Core.Log.LogInfo($"BatVision: {BatVision}");
	}

	void LoadConfig()
	{
		if (!File.Exists(SETTINGS_PATH))
		{
			config = new Config();
			SaveConfig();
			return;
		}

		var json = File.ReadAllText(SETTINGS_PATH);
		config = JsonSerializer.Deserialize<Config>(json);
	}

	void SaveConfig()
	{
		if(!Directory.Exists(CONFIG_PATH))
			Directory.CreateDirectory(CONFIG_PATH);
		var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull });
		File.WriteAllText(SETTINGS_PATH, json);
	}
}
