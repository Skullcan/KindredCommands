using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using KindredCommands.Data;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Gameplay.Clan;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VampireCommandFramework;

namespace KindredCommands;

// This is an anti-pattern, move stuff away from Helper not into it
internal static partial class Helper
{
	public static AdminAuthSystem adminAuthSystem = Core.Server.GetExistingSystemManaged<AdminAuthSystem>();
	public static ClanSystem_Server clanSystem = Core.Server.GetExistingSystemManaged<ClanSystem_Server>();
	public static EntityCommandBufferSystem entityCommandBufferSystem = Core.Server.GetExistingSystemManaged<EntityCommandBufferSystem>();

	static readonly System.Random _random = new();
	public static PrefabGUID GetPrefabGUID(Entity entity)
	{
		var entityManager = Core.EntityManager;
		PrefabGUID guid;
		try
		{
			guid = entityManager.GetComponentData<PrefabGUID>(entity);
		}
		catch
		{
			guid = new PrefabGUID(0);
		}
		return guid;
	}

	public static bool TryGetClanEntityFromPlayer(Entity User, out Entity ClanEntity)
	{
		if (User.Read<TeamReference>().Value._Value.ReadBuffer<TeamAllies>().Length > 0)
		{
			ClanEntity = User.Read<TeamReference>().Value._Value.ReadBuffer<TeamAllies>()[0].Value;
			return true;
		}
		ClanEntity = new Entity();
		return false;
	}

	public static Entity AddItemToInventory(Entity recipient, PrefabGUID guid, int amount)
	{
		try
		{
			ServerGameManager serverGameManager = Core.Server.GetExistingSystemManaged<ServerScriptMapper>()._ServerGameManager;
			var inventoryResponse = serverGameManager.TryAddInventoryItem(recipient, guid, amount);

			return inventoryResponse.NewEntity;
		}
		catch (System.Exception e)
		{
			Core.LogException(e);
		}
		return new Entity();
	}

	public static NativeArray<Entity> GetEntitiesByComponentType<T1>(bool includeAll = false, bool includeDisabled = false, bool includeSpawn = false, bool includePrefab = false, bool includeDestroyed = false)
	{
		EntityQueryOptions options = EntityQueryOptions.Default;
		if (includeAll) options |= EntityQueryOptions.IncludeAll;
		if (includeDisabled) options |= EntityQueryOptions.IncludeDisabled;
		if (includeSpawn) options |= EntityQueryOptions.IncludeSpawnTag;
		if (includePrefab) options |= EntityQueryOptions.IncludePrefab;
		if (includeDestroyed) options |= EntityQueryOptions.IncludeDestroyTag;

		var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
			.AddAll(new(Il2CppType.Of<T1>(), ComponentType.AccessMode.ReadWrite))
			.WithOptions(options);

		var query = Core.EntityManager.CreateEntityQuery(ref entityQueryBuilder);

		var entities = query.ToEntityArray(Allocator.Temp);
		return entities;
	}

	public static NativeArray<Entity> GetEntitiesByComponentTypes<T1, T2>(bool includeAll = false, bool includeDisabled = false, bool includeSpawn = false, bool includePrefab = false, bool includeDestroyed = false)
	{
		EntityQueryOptions options = EntityQueryOptions.Default;
		if (includeAll) options |= EntityQueryOptions.IncludeAll;
		if (includeDisabled) options |= EntityQueryOptions.IncludeDisabled;
		if (includeSpawn) options |= EntityQueryOptions.IncludeSpawnTag;
		if (includePrefab) options |= EntityQueryOptions.IncludePrefab;
		if (includeDestroyed) options |= EntityQueryOptions.IncludeDestroyTag;

		var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
			.AddAll(new(Il2CppType.Of<T1>(), ComponentType.AccessMode.ReadWrite))
			.AddAll(new(Il2CppType.Of<T2>(), ComponentType.AccessMode.ReadWrite))
			.WithOptions(options);

		var query = Core.EntityManager.CreateEntityQuery(ref entityQueryBuilder);

		var entities = query.ToEntityArray(Allocator.Temp);
		return entities;
	}

	public static IEnumerable<Entity> GetAllEntitiesInRadius<T>(float2 center, float radius)
	{
		var spatialData = Core.GenerateCastle._TileModelLookupSystemData;
		var tileModelSpatialLookupRO = spatialData.GetSpatialLookupReadOnlyAndComplete(Core.GenerateCastle);

		var gridPos = ConvertPosToTileGrid(center);

		var gridPosMin = ConvertPosToTileGrid(center - radius);
		var gridPosMax = ConvertPosToTileGrid(center + radius);
		var bounds = new BoundsMinMax(Mathf.FloorToInt(gridPosMin.x), Mathf.FloorToInt(gridPosMin.y),
									  Mathf.CeilToInt(gridPosMax.x), Mathf.CeilToInt(gridPosMax.y));

		var entities = tileModelSpatialLookupRO.GetEntities(ref bounds, TileType.All);
		foreach (var entity in entities)
		{
			if (!entity.Has<T>()) continue;
			if (!entity.Has<Translation>()) continue;
			var pos = entity.Read<Translation>().Value;
			if (math.distance(center, pos.xz) <= radius)
			{
				yield return entity;
			}
		}
		entities.Dispose();
	}

	public static Entity FindClosestTilePosition(Vector3 pos, bool ignoreFloors = false)
	{
		var spatialData = Core.GenerateCastle._TileModelLookupSystemData;
		var tileModelSpatialLookupRO = spatialData.GetSpatialLookupReadOnlyAndComplete(Core.GenerateCastle);

		var gridPos = ConvertPosToTileGrid(pos);
		var bounds = new BoundsMinMax((int)(gridPos.x - 2.5), (int)(gridPos.z - 2.5),
									  (int)(gridPos.x + 2.5), (int)(gridPos.z + 2.5));

		var closestEntity = Entity.Null;
		var closestDistance = float.MaxValue;
		var entities = tileModelSpatialLookupRO.GetEntities(ref bounds, TileType.All);
		for (var i = 0; i < entities.Length; ++i)
		{
			var entity = entities[i];
			if (!entity.Has<TilePosition>()) continue;
			if (!entity.Has<Translation>()) continue;
			if (ignoreFloors && entity.Has<CastleFloor>()) continue;
			var entityPos = entity.Read<Translation>().Value;
			var distance = math.distancesq(pos, entityPos);
			if (distance < closestDistance)
			{
				var prefabName = GetPrefabGUID(entity).LookupName();
				if (!prefabName.StartsWith("TM_")) continue;

				closestDistance = distance;
				closestEntity = entity;
			}
		}
		entities.Dispose();

		return closestEntity;
	}

	public static float2 ConvertPosToTileGrid(float2 pos)
	{
		return new float2(Mathf.FloorToInt(pos.x * 2) + 6400, Mathf.FloorToInt(pos.y * 2) + 6400);
	}

	public static float3 ConvertPosToTileGrid(float3 pos)
	{
		return new float3(Mathf.FloorToInt(pos.x * 2) + 6400, pos.y, Mathf.FloorToInt(pos.z * 2) + 6400);
	}

	public static void RepairGear(Entity Character, bool repair = true)
	{
		Equipment equipment = Character.Read<Equipment>();
		NativeList<Entity> equippedItems = new(Allocator.Temp);
		equipment.GetAllEquipmentEntities(equippedItems);
		foreach (var equippedItem in equippedItems)
		{
			if (equippedItem.Has<Durability>())
			{
				var durability = equippedItem.Read<Durability>();
				if (repair)
				{
					durability.Value = durability.MaxDurability;
				}
				else
				{
					durability.Value = 0;
				}

				equippedItem.Write(durability);
			}
		}
		equippedItems.Dispose();

		for (int i = 0; i < 36; i++)
		{
			if (InventoryUtilities.TryGetItemAtSlot(Core.EntityManager, Character, i, out InventoryBuffer item))
			{
				var itemEntity = item.ItemEntity._Entity;
				if (itemEntity.Has<Durability>())
				{
					var durability = itemEntity.Read<Durability>();
					if (repair)
					{
						durability.Value = durability.MaxDurability;
					}
					else
					{
						durability.Value = 0;
					}

					itemEntity.Write(durability);
				}
			}
		}
	}

	public static void ReviveCharacter(Entity Character, Entity User, ChatCommandContext ctx = null)
	{
		var health = Character.Read<Health>();
		ctx?.Reply("TryGetbuff");
		if (BuffUtility.TryGetBuff(Core.EntityManager, Character, Prefabs.Buff_General_Vampire_Wounded_Buff, out var buffData))
		{
			ctx?.Reply("Destroy");
			DestroyUtility.Destroy(Core.EntityManager, buffData, DestroyDebugReason.TryRemoveBuff);

			ctx?.Reply("Health");
			health.Value = health.MaxHealth;
			health.MaxRecoveryHealth = health.MaxHealth;
			Character.Write(health);
		}
		if (health.IsDead)
		{
			ctx?.Reply("Respawn");
			var pos = Character.Read<LocalToWorld>().Position;

			Il2CppSystem.Nullable_Unboxed<float3> spawnLoc = new() { value = pos };

			ctx?.Reply("Respawn2");
			var sbs = Core.Server.GetExistingSystemManaged<ServerBootstrapSystem>();
			var bufferSystem = Core.Server.GetExistingSystemManaged<EntityCommandBufferSystem>();
			var buffer = bufferSystem.CreateCommandBuffer();
			ctx?.Reply("Respawn3");
			sbs.RespawnCharacter(buffer, User,
				customSpawnLocation: spawnLoc,
				previousCharacter: Character);
		}
    }

	public static void KickPlayer(Entity userEntity)
	{
		EntityManager entityManager = Core.Server.EntityManager;
		User user = userEntity.Read<User>();

		if (!user.IsConnected || user.PlatformId==0) return;

		Entity entity =  entityManager.CreateEntity(new ComponentType[3]
		{
			ComponentType.ReadOnly<NetworkEventType>(),
			ComponentType.ReadOnly<SendEventToUser>(),
			ComponentType.ReadOnly<KickEvent>()
		});

		entity.Write(new KickEvent()
		{
			PlatformId = user.PlatformId
		});
		entity.Write(new SendEventToUser()
		{
			UserIndex = user.Index
		});
		entity.Write(new NetworkEventType()
		{
			EventId = NetworkEvents.EventId_KickEvent,
			IsAdminEvent = false,
			IsDebugEvent = false
		});
	}

	public static void UnlockWaypoints(Entity userEntity)
	{
		DynamicBuffer<UnlockedWaypointElement> dynamicBuffer = Core.EntityManager.AddBuffer<UnlockedWaypointElement>(userEntity);
		dynamicBuffer.Clear();
		foreach (Entity waypoint in Helper.GetEntitiesByComponentType<ChunkWaypoint>())
			dynamicBuffer.Add(new UnlockedWaypointElement()
			{
				Waypoint = waypoint.Read<NetworkId>()
			});
	}

	public static void RevealMapForPlayer(Entity userEntity)
	{
		var mapZoneElements = Core.EntityManager.GetBuffer<UserMapZoneElement>(userEntity);
		foreach (var mapZone in mapZoneElements)
		{
			var userZoneEntity = mapZone.UserZoneEntity.GetEntityOnServer();
			var revealElements = Core.EntityManager.GetBuffer<UserMapZonePackedRevealElement>(userZoneEntity);
			revealElements.Clear();
			var revealElement = new UserMapZonePackedRevealElement
			{
				PackedPixel = 255
			};
			for (var i = 0; i < 8192; i++)
			{
				revealElements.Add(revealElement);
			}
		}
	}
	// add the component debugunlock
	public static void SetPosition(this Entity entity, float3 position)
	{
		if (entity.Has<Translation>())
		{
			entity.With((ref Translation translation) => translation.Value = position);
		}

		if (entity.Has<LastTranslation>())
		{
			entity.With((ref LastTranslation lastTranslation) => lastTranslation.Value = position);
		}
	}
	public delegate void WithRefHandler<T>(ref T item);
	public static void HasWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
	{
		if (entity.Has<T>())
		{
			entity.WithBloodCraft(action);
		}
	}
	public static void WithBloodCraft<T>(this Entity entity, WithRefHandler<T> action) where T : struct
	{
		T item = entity.Read<T>();
		action(ref item);

		Core.EntityManager.SetComponentData(entity, item);
	}
	public static void TryApplyBuffWithLifeTimeNone(this Entity entity, PrefabGUID buffPrefabGuid)
	{
		if (entity.TryApplyAndGetBuff(buffPrefabGuid, out Entity buffEntity))
		{
			buffEntity.AddWith((ref LifeTime lifeTime) =>
			{
				lifeTime.Duration = 0f;
				lifeTime.EndAction = LifeTimeEndAction.None;
			});
		}
	}
	public static bool TryApplyAndGetBuff(this Entity entity, PrefabGUID buffPrefabGuid, out Entity buffEntity)
	{
		buffEntity = Entity.Null;

		if (entity.TryApplyBuff(buffPrefabGuid) && entity.TryGetBuff(buffPrefabGuid, out buffEntity))
		{
			return true;
		}

		return false;
	}
	public static bool TryApplyBuff(this Entity entity, PrefabGUID prefabGuid)
	{
		bool hasBuff = entity.HasBuff(prefabGuid);

		if (hasBuff && ShouldApplyStack(entity, prefabGuid, out Entity buffEntity, out byte stacks))
		{
			// ServerGameManager.CreateStacksIncreaseEvent(buffEntity, stacks, ++stacks);
			Core.ServerGameManager.InstantiateBuffEntityImmediate(entity, entity, prefabGuid, null, stacks);
		}
		else if (!hasBuff)
		{
			ApplyBuffDebugEvent applyBuffDebugEvent = new()
			{
				BuffPrefabGUID = prefabGuid,
				Who = entity.GetNetworkId(),
			};

			FromCharacter fromCharacter = new()
			{
				Character = entity,
				User = entity.IsPlayer() ? entity.GetUserEntity() : entity
			};

            Core.Server.GetExistingSystemManaged<DebugEventsSystem>().ApplyBuff(fromCharacter, applyBuffDebugEvent);
			
			return true;
		}

		return false;
	}
	public static bool TryGetBuff(this Entity entity, PrefabGUID buffPrefabGUID, out Entity buffEntity)
	{
		if (Core.ServerGameManager.TryGetBuff(entity, buffPrefabGUID.ToIdentifier(), out buffEntity))
		{
			return true;
		}

		return false;
	}
	static readonly Dictionary<PrefabGUID, int> _buffMaxStacks = [];
	static bool ShouldApplyStack(Entity entity, PrefabGUID prefabGuid, out Entity buffEntity, out byte stacks)
	{
		buffEntity = Entity.Null;
		stacks = 0;

		if (_buffMaxStacks.TryGetValue(prefabGuid, out int maxStacks)
			&& entity.TryGetBuffStacks(prefabGuid, out buffEntity, out int buffStacks))
		{
			stacks = (byte)buffStacks;
			return stacks < maxStacks;
		}

		return false;
	}
	public static bool TryGetBuffStacks(this Entity entity, PrefabGUID buffPrefabGUID, out Entity buffEntity, out int stacks)
	{
		stacks = 0;

		if (Core.ServerGameManager.TryGetBuff(entity, buffPrefabGUID.ToIdentifier(), out buffEntity)
			&& buffEntity.TryGetComponent(out Buff buff))
		{
			stacks = buff.Stacks;
			return true;
		}

		return false;
	}
	public static bool HasBuff(this Entity entity, PrefabGUID buffPrefabGuid)
	{
		return Core.ServerGameManager.HasBuff(entity, buffPrefabGuid.ToIdentifier());
	}
	public static void AddWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
	{
		if (!entity.Has<T>())
		{
			entity.Add<T>();
		}

		entity.WithHelper(action);
	}
	public static void WithHelper<T>(this Entity entity, WithRefHandler<T> action) where T : struct
	{
		T item = entity.Read<T>();
		action(ref item);

		Core.EntityManager.SetComponentData(entity, item);
	}
	public static bool IsPlayer(this Entity entity)
	{
		if (entity.Has<PlayerCharacter>())
		{
			return true;
		}

		return false;
	}
	public static NetworkId GetNetworkId(this Entity entity)
	{
		if (entity.TryGetComponent(out NetworkId networkId))
		{
			return networkId;
		}

		return NetworkId.Empty;
	}
	public static Entity GetUserEntity(this Entity entity)
	{
		if (entity.TryGetComponent(out PlayerCharacter playerCharacter)) return playerCharacter.UserEntity;
		else if (entity.Has<User>()) return entity;

		return Entity.Null;
	}
}
