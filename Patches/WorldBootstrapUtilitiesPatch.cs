using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using KindredCommands.Systems;
//using KindredCommands.Systems.Quests;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace KindredCommands.Patches;
[HarmonyPatch]
public static class WorldBootstrapPatch
{
	static bool ElitePrimalRifts { get; } = true;
	static readonly List<Type> _registerSystems =
	[
		//typeof(QuestTargetSystem),
		typeof(PrimalWarEventSystem)
	];

	static readonly MethodInfo _getOrCreate = typeof(World)
		.GetMethods(BindingFlags.Instance | BindingFlags.Public)
		.First(m =>
			m.Name == nameof(World.GetOrCreateSystemManaged) &&
			m.IsGenericMethodDefinition &&
			m.GetParameters().Length == 0
		);

	[HarmonyPatch(typeof(WorldBootstrapUtilities), nameof(WorldBootstrapUtilities.AddSystemsToWorld))]
	[HarmonyPrefix]
	public static void Prefix(World world, WorldBootstrap worldConfig, WorldSystemConfig worldSystemConfig)
	{
		try
		{
			if (world == null)
			{
				Plugin.LogInstance.LogError("[WorldBootstrap_Server.AddSystemsToWorld] World is null.");
				return;
			}
			if (typeof(PrimalWarEventSystem) == null)
			{
				Plugin.LogInstance.LogError("[WorldBootstrap_Server.AddSystemsToWorld] PrimalWarEventSystem is null.");
				return;
			}
			if (world.Name.Equals("Server"))
			{
				var updateGroup = Core.Server.GetOrCreateSystemManaged<UpdateGroup>();
				
				foreach (Type type in _registerSystems)
				{
					if (!ElitePrimalRifts && type == typeof(PrimalWarEventSystem))
						continue;

					RegisterAndAddSystem(world, updateGroup, type);
				}

				updateGroup.SortSystems();
			}
		}
		catch (Exception e)
		{
			Plugin.LogInstance.LogError($"[WorldBootstrap_Server.AddSystemsToWorld] Exception: {e}");
		}
	}
	static void RegisterAndAddSystem(this World world, UpdateGroup group, Type systemType)
	{
		ClassInjector.RegisterTypeInIl2Cpp(systemType);
		
		var getOrCreate = _getOrCreate.MakeGenericMethod(systemType);
		var systemInstance = (ComponentSystemBase)getOrCreate.Invoke(world, null);

		group.AddSystemToUpdateList(systemInstance);
	}
}

