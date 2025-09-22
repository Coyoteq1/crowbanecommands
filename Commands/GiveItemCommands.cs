using CrowbaneCommands.Commands.Converters;
using ProjectM;
using VampireCommandFramework;

namespace CrowbaneCommands.Commands;

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
}

