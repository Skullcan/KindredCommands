using System.Collections.Generic;
using System.Linq;
using KindredCommands.Commands.Converters;
using KindredCommands.Models;
using Newtonsoft.Json.Linq;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
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
			var isAdmin = ctx.IsAdmin;

			if (DBKits.StartKits.TryGetValue(kitAtual, out var Kit))
			{
				if (DBKits.UsedKits.TryGetValue(PlatformId, out var Used))
				{
					if (!Used || isAdmin)
					{
						if (GiveStartKit(ctx, Kit)){
							ctx.Reply($"Você recebeu um kit <color=#ffffffff>{kitAtual}</color>. Divirta-se!");
							Core.Log.LogInfo($"Kit {kitAtual} given to {ctx.User.CharacterName} ({PlatformId})");
						}
						else
						{
							Core.Log.LogInfo($"Error giving {kitAtual} to {ctx.User.CharacterName} ({PlatformId}). Inventory is full.");
						}

						DBKits.UsedKits[PlatformId] = true;
						DBKits.SaveData();
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

	private static bool GiveStartKit(ChatCommandContext ctx, List<RecordKit> kit)
	{
		var prefabSys = Core.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
		foreach (var item in kit)
		{
			prefabSys._PrefabLookupMap.TryGetPrefabGuidWithName(item.Name, out PrefabGUID gUID);
			var entity = Helper.AddItemToInventory(ctx.Event.SenderCharacterEntity, gUID, item.Amount);
			if (entity == Entity.Null && !item.Name.Equals("Item_Consumable_HealingPotion_T01"))
			{		
				ctx.Reply($"Couldn't add all the items to your inventory. Message an Admin so he can give you full set again.");
				return false;
			}
		}
		return true;
		
	}

	#endregion
}
