//using Il2CppInterop.Runtime;
//using KindredCommands.Data;
//using ProjectM;
//using ProjectM.Network;
//using ProjectM.Shared;
//using Stunlock.Core;
//using System;
//using System.Collections;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using Unity.Collections;
//using Unity.Entities;
//using UnityEngine;


//namespace KindredCommands.Services;
//internal class QuestService
//{
//    static EntityManager EntityManager => Core.EntityManager;
//    static SystemService SystemService => Core.SystemService;
//    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;
//    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

//	const float START_DELAY = 10f;
//    const float ROUTINE_DELAY = 60f;

//    static readonly WaitForSeconds _startDelay = new(START_DELAY);
//    static readonly WaitForSeconds _routineDelay = new(ROUTINE_DELAY);

//    //public static readonly DateTime _lastUpdate;

//    static readonly ComponentType[] _targetUnitAllComponents =
//    [
//        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
//        ComponentType.ReadOnly(Il2CppType.Of<Health>()),
//        ComponentType.ReadOnly(Il2CppType.Of<UnitStats>()),
//        ComponentType.ReadOnly(Il2CppType.Of<Movement>()),
//        ComponentType.ReadOnly(Il2CppType.Of<AbilityBar_Server>()),
//        ComponentType.ReadOnly(Il2CppType.Of<AbilityBar_Shared>()),
//        ComponentType.ReadOnly(Il2CppType.Of<AggroConsumer>())
//    ];

//    static readonly ComponentType[] _harvestableResourceAllComponents =
//    [
//        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
//        ComponentType.ReadOnly(Il2CppType.Of<Health>()),
//        ComponentType.ReadOnly(Il2CppType.Of<DurabilityTarget>()),
//        ComponentType.ReadOnly(Il2CppType.Of<UnitLevel>()),
//        ComponentType.ReadOnly(Il2CppType.Of<DropTable>()),
//        ComponentType.ReadOnly(Il2CppType.Of<DropTableBuffer>()),
//        ComponentType.ReadOnly(Il2CppType.Of<YieldResourcesOnDamageTaken>())
//    ];

//	public static IReadOnlyDictionary<PrefabGUID, HashSet<Entity>> TargetCache => _targetCache;
//    static readonly ConcurrentDictionary<PrefabGUID, HashSet<Entity>> _targetCache = [];

//    public static readonly List<PrefabGUID> ShardBearers =
//    [
//        Prefabs.CHAR_Manticore_VBlood,
//        Prefabs.CHAR_ChurchOfLight_Paladin_VBlood,
//        Prefabs.CHAR_Gloomrot_Monster_VBlood,
//        Prefabs.CHAR_Vampire_Dracula_VBlood,
//        Prefabs.CHAR_Blackfang_Morgana_VBlood
//    ];

//    public static readonly HashSet<string> FilteredTargetUnits =
//    [
//        "Trader",
//        "HostileVillager",
//        "TombSummon",
//        "StatueSpawn",
//        "SmiteOrb",
//        "CardinalAide",
//        "GateBoss",
//        "DraculaMinion",
//        "Summon",
//        "Minion",
//        "Chieftain",
//        "ConstrainingPole",
//        "Horse",
//        "EnchantedCross",
//        "DivineAngel",
//        "FallenAngel",
//        "FarbaneSuprise",
//        "Withered",
//        "Servant",
//        "Spider_Melee",
//        "Spider_Range",
//        "GroundSword",
//        "FloatingWeapon",
//        "Airborne",
//        "SpiritDouble",
//        "ValyrCauldron",
//        "EmeryGolem"
//    ];

//    static readonly HashSet<string> _filteredCraftableItems =
//    [
//        "Item_Cloak",
//        "BloodKey_T01",
//        "NewBag",
//        "Miners",
//        "WoodCutter",
//        "ShadowMatter",
//        "T0X",
//        "Heart_T",
//        "Water_T",
//        "FakeItem",
//        "PrisonPotion",
//        "Dracula",
//        "Consumable_Empty",
//        "Reaper_T02",
//        "Slashers_T02",
//        "FishingPole",
//        "Disguise",
//        "Canister",
//        "Trippy",
//        "Eat_Rat",
//        "Irradiant",
//        "Slashers_T01",
//        "Slashers_T03",
//        "Slashers_T04",
//        "Reaper_T03",
//        "Reaper_T04",
//        "Reaper_T01",
//        "GarlicResistance",
//        "T01_Bone"
//    ];

//    static readonly HashSet<string> _filteredHarvestableResources =
//    [
//        "Item_Ingredient_Crystal",
//        "Coal",
//        "Thistle"
//	];

//	public QuestService()
//    {
//        //_targetUnitQueryDesc = EntityManager.CreateQueryDesc(_targetUnitAllComponents, typeIndices: [0], options: EntityQueryOptions.IncludeDisabled);
//        //_harvestableResourceQueryDesc = EntityManager.CreateQueryDesc(allTypes: _harvestableResourceAllComponents, typeIndices: [0], options: EntityQueryOptions.IncludeSpawnTag);

//        //Configuration.GetQuestRewardItems();
//        //QuestServiceRoutine().Run();
//    }
//    static IEnumerator QuestServiceRoutine()
//    {
//        //if (_craftables) InitializeCraftables();
//        //if (_harvestables) InitializeHarvestables().Run();

//		//while (true)
//		//{
//		//    foreach (var playerInfoPair in SteamIdPlayerInfoCache)
//		//    {
//		//        ulong steamId = playerInfoPair.Key;
//		//        PlayerInfo playerInfo = playerInfoPair.Value;

//		//        Entity userEntity = playerInfo.CharEntity;
//		//        User user = playerInfo.User;

//		//        if (!_leveling)
//		//        {
//		//            RefreshQuests(user, steamId, Progression.GetSimulatedLevel(userEntity));
//		//        }
//		//        else if (_leveling && steamId.TryGetPlayerExperience(out var xpData))
//		//        {
//		//            RefreshQuests(user, steamId, xpData.Key);
//		//        }

//		//        yield return null;
//		//    }

//		//    _lastUpdate = DateTime.UtcNow;
//		//    yield return _routineDelay;
//		//}
//		yield return null;
//    }
//    static void InitializeCraftables()
//    {
//        var prefabGuidEntities = PrefabCollectionSystem._PrefabGuidToEntityMap;
//        var recipeDataMap = GameDataSystem.RecipeHashLookupMap;

//        var prefabGuids = recipeDataMap.GetKeyArray(Allocator.Temp);
//        var recipeDatas = recipeDataMap.GetValueArray(Allocator.Temp);

//        try
//        {
//            for (int i = 0; i < prefabGuids.Length; i++)
//            {
//                PrefabGUID prefabGuid = prefabGuids[i];
//                RecipeData recipeData = recipeDatas[i];
//                Entity recipeEntity = recipeData.Entity;

//                //if (!recipeEntity.TryGetBuffer<RecipeOutputBuffer>(out var buffer) || buffer.IsEmpty)
//                //    continue;

//                //if (!prefabGuidEntities.TryGetValue(buffer[0].Guid, out Entity prefabEntity))
//                //    continue;

//                //prefabGuid = Prefabs.Ability_MistStrike_Curve;
//                //string prefabName = prefabGuid.GetPrefabName();

//                //if (_filteredCraftableItems.Any(item => prefabName.Contains(item, StringComparison.CurrentCultureIgnoreCase)))
//                //    continue;

//                //if (prefabEntity.Has<Equippable>()
//                //    && prefabEntity.TryGetComponent(out Salvageable salvageable))
//                //{
//                //    if (salvageable.RecipeGUID.HasValue())
//                //    {
//                //        //CraftPrefabs.Add(prefabGuid);
//                //    }
//                //}
//                //else if (prefabEntity.Has<ConsumableCondition>())
//                //{
//                //    //CraftPrefabs.Add(prefabGuid);
//                //}
//            }
//        }
//        catch (Exception ex)
//        {
//            Core.Log.LogError($"[QuestService] InitializeCraftables() - {ex}");
//        }
//        finally
//        {
//            prefabGuids.Dispose();
//            recipeDatas.Dispose();
//        }

        
//    }
//    static IEnumerator InitializeHarvestables()
//    {
//        //yield return QueryResultStreamAsync(
//        //    _harvestableResourceQueryDesc,
//        //    stream =>
//        //    {
//        //        try
//        //        {
//        //            using (stream)
//        //            {
//        //                foreach (QueryResult result in stream.GetResults())
//        //                {
//        //                    Entity entity = result.Entity;
//        //                    PrefabGUID prefabGuid = result.ResolveComponentData<PrefabGUID>();
//        //                    string prefabName = prefabGuid.GetPrefabName();

//        //                    if (!entity.Has<DropTableBuffer>()) continue;
//        //                    else if (_filteredHarvestableResources.Any(resource => prefabName.Contains(resource, StringComparison.CurrentCultureIgnoreCase))) continue;
//        //                    else if (prefabGuid.HasValue()) ResourcePrefabs.Add(prefabGuid);
//        //                }
//        //            }
//        //        }
//        //        catch (Exception ex)
//        //        {
//        //            Core.Log.LogError($"[QuestService] InitializeHarvestables() - {ex}");
//        //        }
//        //    }
//        //);

//		yield return null;
//    }
//}
