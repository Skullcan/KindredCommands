using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppSystem;
using KindredCommands.Models;
using ProjectM;
using ProjectM.Gameplay.WarEvents;
using ProjectM.Network;
using ProjectM.Shared;
using ProjectM.Shared.WarEvents;
using ProjectM.Shared.WorldEvents;
using ProjectM.Terrain;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static NetworkEvents_Serialize;

namespace KindredCommands.Commands
{
	internal class MiscCommands
	{
		[Command("forcerespawn", "fr", description: "Sets the chain transition time for nearby spawn chains to now to force them to respawn if they can", adminOnly: true)]
		public static void ChainTransition(ChatCommandContext ctx, float range = 10)
		{
			var charEntity = ctx.Event.SenderCharacterEntity;
			var time = Core.ServerTime;
			foreach (var chainEntity in Helper.GetAllEntitiesInRadius<AutoChainInstanceData>(charEntity.Read<Translation>().Value.xz, range))
			{
				chainEntity.Write(new AutoChainInstanceData() { NextTransitionAttempt = time });
			}
		}

		[Command("settime", "st", description: "Sets the game time to the day and hour", adminOnly: true)]
		public static void SetTime(ChatCommandContext _, int day, int hour)
		{
			var st = Core.EntityManager.CreateEntity(new ComponentType[1] { ComponentType.ReadOnly<SetTimeOfDayEvent>() });
			st.Write(new SetTimeOfDayEvent()
			{
				Day = day,
				Hour = hour,
				Type = SetTimeOfDayEvent.SetTimeType.Set
			});
		}

		[Command("cleancontainerlessshards", description: "Destroys all items that are not in a container", adminOnly: true)]
		public static void CleanContainerlessShards(ChatCommandContext ctx)
		{
			var destroyedPrefabs = new Dictionary<PrefabGUID, int>();
			foreach (var item in Helper.GetEntitiesByComponentTypes<InventoryItem, Relic>())
			{
				if (!item.Read<InventoryItem>().ContainerEntity.Equals(Entity.Null)) continue;
				if (item.Has<Equippable>() && !item.Read<Equippable>().EquipTarget.GetEntityOnServer().Equals(Entity.Null)) continue;

				DestroyEntityAndAttached(item, destroyedPrefabs);
			}

			foreach (var (guid, count) in destroyedPrefabs)
			{
				ctx.Reply($"Destroyed <color=white>{count}</color>x <color=yellow>{guid.LookupName()}</color>");
				Core.Log.LogInfo($"Destroyed {count}x {guid.LookupName()}");
			}
		}

		static void DestroyEntityAndAttached(Entity entity, Dictionary<PrefabGUID, int> destroyCount)
		{
			if (entity.Has<AttachedBuffer>())
			{
				var attachedBuffer = Core.EntityManager.GetBuffer<AttachedBuffer>(entity);
				foreach (var attached in attachedBuffer)
				{
					DestroyEntityAndAttached(attached.Entity, destroyCount);
				}
			}

			if (entity.Has<PrefabGUID>())
			{
				var guid = entity.Read<PrefabGUID>();
				if (!destroyCount.TryGetValue(guid, out var count))
					count = 0;
				destroyCount[guid] = count + 1;
			}
			DestroyUtility.Destroy(Core.EntityManager, entity);
		}

		[Command("everyonedaywalker", "ed", description: "Toggles daywalker for everyone", adminOnly: true)]
		public static void EveryoneDaywalker(ChatCommandContext ctx)
		{
			Core.ConfigSettings.EveryoneDaywalker = !Core.ConfigSettings.EveryoneDaywalker;
			var players = Helper.GetEntitiesByComponentType<PlayerCharacter>(includeDisabled: true);
			foreach (var player in players)
			{
				if (Core.ConfigSettings.EveryoneDaywalker ^ Core.BoostedPlayerService.IsDaywalker(player))
				{
					Core.BoostedPlayerService.ToggleDaywalker(player);
					Core.BoostedPlayerService.UpdateBoostedPlayer(player);
				}
			}
			players.Dispose();

			ctx.Reply($"Everyone is now {(Core.ConfigSettings.EveryoneDaywalker ? "a daywalker" : "a vampire")}");
		}

		[Command("globalbatvision", "gbv", description: "Allows players to see entities while in batform", adminOnly: true)]
		public static void BatVision(ChatCommandContext ctx)
		{
			if(Core.GlobalMisc.ToggleBatVision())
			{
				ctx.Reply("Bat Vision is now enabled");
			}
			else
			{
				ctx.Reply("Bat Vision is now disabled");
			}
		}

		[Command("primalrift", "prift", description: "Allows admins to start a primal rift", adminOnly: true)]
		public static void PrimalRift(ChatCommandContext ctx)
		{
			NetworkEventType networkEventType = new()
			{
				EventId = NetworkEvents.EventId_WarEvent_StartEvent,
				IsAdminEvent = true,
				IsDebugEvent = true
			};

			WarEvent_StartEvent warEvent = new()
			{
				EventType = WarEventType.Primal,
				EnableAllGates = true
			};

			FromCharacter fromCharacter = new()
			{
				Character = ctx.Event.SenderCharacterEntity,
				User = ctx.Event.SenderUserEntity
			};

			ComponentType[] _eventComponents =
			[
				ComponentType.ReadOnly(Il2CppType.Of<WarEvent_StartEvent>()),
				ComponentType.ReadOnly(Il2CppType.Of<FromCharacter>()),
				ComponentType.ReadOnly(Il2CppType.Of<NetworkEventType>())
			];

			Entity entity = Core.EntityManager.CreateEntity(_eventComponents);
			entity.Write(warEvent);
			entity.Write(fromCharacter);
			entity.Write(networkEventType);
		}
	}
}
