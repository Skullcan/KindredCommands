using System.Collections;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using KindredCommands.Models;
using KindredCommands.Services;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using UnityEngine;

namespace KindredCommands;

internal static class Core
{

	public static World Server { get; } = GetWorld("Server") ?? throw new System.Exception("There is no Server world (yet). Did you install a server mod on the client?");
	
	public static EntityManager EntityManager { get; } = Server.EntityManager;
	public static GameDataSystem GameDataSystem { get; } = Server.GetExistingSystemManaged<GameDataSystem>();
	public static GenerateCastleSystem GenerateCastle { get; private set; }
	public static PrefabCollectionSystem PrefabCollectionSystem { get; internal set; }
	public static PrefabCollectionSystem PrefabCollection { get; } = Server.GetExistingSystemManaged<PrefabCollectionSystem>();
	public static RelicDestroySystem RelicDestroySystem { get; internal set; }
	public static ServerScriptMapper ServerScriptMapper { get; internal set; }
	public static double ServerTime => ServerGameManager.ServerTime;
	public static ServerGameManager ServerGameManager => ServerScriptMapper.GetServerGameManager();
	public static ServerGameSettingsSystem ServerGameSettingsSystem { get; internal set; }
	public static ManualLogSource Log { get; } = Plugin.PluginLog;
	public static AnnouncementsService AnnouncementsService { get; internal set; }
	public static AuditService AuditService { get; } = new();
	public static BloodBoundService BloodBoundService { get; private set; }
	public static BoostedPlayerService BoostedPlayerService { get; internal set; }
	public static BossService Boss { get; internal set; }
	public static CastleTerritoryService CastleTerritory { get; private set; }
	public static ConfigSettingsService ConfigSettings { get; internal set; }
	public static DropItemService DropItem { get; internal set; }
	public static GearService GearService { get; internal set; }
	public static GlobalMiscService GlobalMisc { get; internal set; }
	public static LocalizationService Localization { get; } = new();
	public static PlayerService Players { get; internal set; }
	public static PrefabService Prefabs { get; internal set; }
	public static PrisonerService Prisoners { get; internal set; }
	public static RegionService Regions { get; internal set; }
	public static SoulshardService SoulshardService { get; internal set; }
	public static TerritoryLocationService TerritoryLocation { get; internal set; }
	public static TrackPlayerEquipmentService TrackPlayerEquipment { get; internal set; }
	public static UnitSpawnerService UnitSpawner { get; internal set; }	

	static MonoBehaviour monoBehaviour;

	public const int MAX_REPLY_LENGTH = 509;

	public static void LogException(System.Exception e, [CallerMemberName] string caller = null)
	{
		Core.Log.LogError($"Failure in {caller}\nMessage: {e.Message} Inner:{e.InnerException?.Message}\n\nStack: {e.StackTrace}\nInner Stack: {e.InnerException?.StackTrace}");
	}

	internal static void InitializeAfterLoaded()
	{
		if (_hasInitialized) return;

		GenerateCastle = Server.GetOrCreateSystemManaged<GenerateCastleSystem>();
		PrefabCollectionSystem = Server.GetExistingSystemManaged<PrefabCollectionSystem>();
		RelicDestroySystem = Server.GetExistingSystemManaged<RelicDestroySystem>();
		ServerGameSettingsSystem = Server.GetExistingSystemManaged<ServerGameSettingsSystem>();
		ServerScriptMapper = Server.GetExistingSystemManaged<ServerScriptMapper>();

		Prefabs = new();
		ConfigSettings = new();
		BoostedPlayerService = new();
		Players = new();

		AnnouncementsService = new();
		BloodBoundService = new();
		Boss = new();
		CastleTerritory = new();
		DropItem = new();
		GearService = new();
		GlobalMisc = new();
		Prisoners = new();
		Regions = new();
		SoulshardService = new();
		TerritoryLocation = new();
		TrackPlayerEquipment = new();
		UnitSpawner = new();		

		Data.Character.Populate();

		_hasInitialized = true;		
		Log.LogInfo($"{nameof(InitializeAfterLoaded)} completed");
		
		#region Personalizado Parabellum
		// Limita o tamanho do stack da blood essence para ser possivel somente 5 dias de castelo full.
		var scriptMapper = Server.GetExistingSystemManaged<ServerScriptMapper>();
		var itemLookupMap = scriptMapper.GetServerGameManager().ItemLookupMap;
		var bloodEssence = new PrefabGUID(862477668);
		var demonFragmen = new PrefabGUID(-77477508);

		if (itemLookupMap.TryGetValue(bloodEssence, out var bloodData))
		{
			bloodData.MaxAmount = 540;
			itemLookupMap[bloodEssence] = bloodData;
		}

		if (itemLookupMap.TryGetValue(demonFragmen, out var demonData))
		{
			demonData.MaxAmount = 1000;
			itemLookupMap[demonFragmen] = demonData;
		}

		//Parabellum Brutal Spoofing - Thanks to Rendy from V-Arena.
		UpdateServerSettings();
		//Parabellum Kits
		DBKits.LoadKitsData();		
		#endregion

	}
	private static bool _hasInitialized = false;

	private static World GetWorld(string name)
	{
		foreach (var world in World.s_AllWorlds)
		{
			if (world.Name == name)
			{
				return world;
			}
		}

		return null;
	}

	public static Coroutine StartCoroutine(IEnumerator routine)
	{
		if (monoBehaviour == null)
		{
			var go = new GameObject("KindredCommands");
			monoBehaviour = go.AddComponent<IgnorePhysicsDebugSystem>();
			Object.DontDestroyOnLoad(go);
		}

		return monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
	}

	public static void StopCoroutine(Coroutine coroutine)
	{
		if (monoBehaviour == null)
		{
			return;
		}

		monoBehaviour.StopCoroutine(coroutine);
	}

	private static readonly GameDifficulty GameDifficulty = GameDifficulty.Hard;

	private static void UpdateServerSettings()
	{
		var entityGameBalanceSettings = Helper.GetEntitiesByComponentType<ServerGameBalanceSettings>(includeAll: true).ToArray();
		
		ServerGameBalanceSettings serverGameBalanceSettings = Core.Server.GetExistingSystemManaged<ServerGameSettingsSystem>()._Settings.ToStruct();
		serverGameBalanceSettings.GameDifficulty = GameDifficulty;
		Core.Server.EntityManager.SetComponentData(entityGameBalanceSettings[0], serverGameBalanceSettings);
	}

	public static void RunDelayed(System.Action action, float delay = 0.25f)
	{
		StartCoroutine(RunDelayedRoutine(delay, action));
	}
	static IEnumerator RunDelayedRoutine(float delay, System.Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
	public static bool TryGetComponent<T>(this Entity entity, out T componentData) where T : struct
	{
		componentData = default;

		if (entity.Has<T>())
		{
			componentData = entity.Read<T>();
			return true;
		}

		return false;
	}
	public static User GetUser(this Entity entity)
	{
		if (entity.TryGetComponent(out User user)) return user;
		else if (entity.TryGetComponent(out PlayerCharacter playerCharacter) && playerCharacter.UserEntity.TryGetComponent(out user)) return user;

		return User.Empty;
	}
}
