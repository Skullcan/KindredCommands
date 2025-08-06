using System.Collections.Generic;
using System.Linq;
using KindredCommands.Commands.Converters;
using KindredCommands.Models;
using Newtonsoft.Json.Linq;
using ProjectM;
using Stunlock.Core;
using VampireCommandFramework;
using static RootMotion.FinalIK.Grounding;

namespace KindredCommands.Commands;

internal class GiveItemCommands
{
	[Command("give", "g", "<Prefab GUID or name> [quantity=1]", "Gives the specified item to the player", adminOnly: true)]
	public static void GiveItem(ChatCommandContext ctx, ItemParameter item, int quantity = 1)
	{
		Helper.AddItemToInventory(ctx.Event.SenderCharacterEntity, item.Value, quantity);
		var prefabSys = Core.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
		var name = prefabSys._PrefabLookupMap.GetName(item.Value);
		ctx.Reply($"Gave {quantity} {name}");
	}

	#region Comandos Parabellum

	[Command("starterkit", "kit", description: "Entrega um kit de iniciante para o player.")]
	public static void StarterKit(ChatCommandContext ctx)
	{
		if (DBKits.EnabledKitCommand)
		{
			var PlatformId = ctx.User.PlatformId;
			var kitAtual = DBKits.StartKits.First().Key;

			if (DBKits.StartKits.TryGetValue(kitAtual, out var Kit))
			{
				if (DBKits.UsedKits.TryGetValue(PlatformId, out var Used))
				{
					Core.Log.LogInfo($"Kit used status for {ctx.User.CharacterName} ({PlatformId}): {Used}");
					if (!Used)
					{
						GiveStartKit(ctx, Kit);
						DBKits.UsedKits[PlatformId] = true;
						ctx.Reply($"Você recebeu um kit <color=#ffffffff>{kitAtual}</color>. Divirta-se!");
						DBKits.SaveData();
						Core.Log.LogInfo($"Kit {kitAtual} given to {ctx.User.CharacterName} ({PlatformId})");
					}
					else
					{
						ctx.Reply($"Você já recebeu este kit.");
					}
				}
			}
			else
			{
				ctx.Reply($"Kit {kitAtual} não encontrado.");
			}
		}
		else
		{
			ctx.Reply($"Kits ainda não estão disponíveis.");
		}				
	}

	private static void GiveStartKit(ChatCommandContext ctx, List<RecordKit> kit)
	{
		var prefabSys = Core.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
		foreach (var item in kit)
		{
			prefabSys._PrefabLookupMap.TryGetPrefabGuidWithName(item.Name, out PrefabGUID gUID);
			Helper.AddItemToInventory(ctx.Event.SenderCharacterEntity, gUID, item.Amount);
		}
	}

	#endregion
}
