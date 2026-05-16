using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TrapmancingMod.Content.Items;

public class TrapItem : ModItem
{
	public override void SetDefaults()
	{
		Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.TrapTile>());
		Item.width = 14;
		Item.height = 14;
		Item.mech = true; // show wires when held, like vanilla trap items
		Item.value = Item.buyPrice(0, 1, 0, 0);
	}

	public override void AddRecipes()
	{
		// 10 Souls of Might (from The Destroyer) + 1 Dart Trap → crafted at a Mythril/Orichalcum Anvil
		CreateRecipe()
			.AddIngredient(ItemID.SoulofMight, 10)
			.AddIngredient(ItemID.DartTrap)
			.AddTile(TileID.MythrilAnvil)
			.Register();
	}
}
