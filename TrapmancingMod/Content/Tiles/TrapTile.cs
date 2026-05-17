using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using TrapmancingMod.Common.Systems;
using TrapmancingMod.Content.TileEntities;

namespace TrapmancingMod.Content.Tiles;

public class TrapTile : ModTile
{
	// Hammer cycles: left(0) → right(1) → up(2) → down(3) → left…
	private static readonly int[] DirectionCycle = [1, 2, 3, 0];

	public override void SetStaticDefaults()
	{
		Main.tileSolid[Type] = true;
		Main.tileBlockLight[Type] = true;
		Main.tileFrameImportant[Type] = true;

		TileID.Sets.DisableSmartCursor[Type] = true;
		TileID.Sets.DontDrawTileSliced[Type] = true;
		TileID.Sets.IgnoresNearbyHalfbricksWhenDrawn[Type] = true;
		TileID.Sets.Wiring.IsAMechanism[Type] = true;

		TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
		TileObjectData.newTile.StyleHorizontal = true;
		TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(
			ModContent.GetInstance<TrapTileEntity>().HookAfterPlacement, -1, 0, true);
		TileObjectData.addTile(Type);

		AddMapEntry(new Color(160, 90, 40), CreateMapEntryName());
		DustType = DustID.Stone;
	}

	// Hammer rotates the trap direction (stored in TileFrameX)
	public override bool Slope(int i, int j)
	{
		Tile tile = Main.tile[i, j];
		tile.TileFrameX = (short)(DirectionCycle[tile.TileFrameX / 18] * 18);

		if (Main.netMode == NetmodeID.MultiplayerClient)
			NetMessage.SendTileSquare(-1, i, j, 1, 1);

		return false; // suppress vanilla slope behaviour
	}

	public override void HitWire(int i, int j)
	{
		if (!Wiring.CheckMech(i, j, 60))
			return;

		if (!TileEntity.TryGet(i, j, out TrapTileEntity te))
			return;

		if (te.StoredItem.IsAir || te.StoredItem.shoot <= ProjectileID.None)
			return;

		Tile tile = Main.tile[i, j];
		int dir = tile.TileFrameX / 18; // 0=left 1=right 2=up 3=down
		float speed = te.StoredItem.shootSpeed > 0f ? te.StoredItem.shootSpeed : 8f;

		Vector2 velocity = dir switch {
			0 => new Vector2(-speed, 0f),
			1 => new Vector2(speed, 0f),
			2 => new Vector2(0f, -speed),
			_ => new Vector2(0f, speed),
		};

		int owner = te.FindOwnerPlayer();

		Projectile.NewProjectile(
			Wiring.GetProjectileSource(i, j),
			new Vector2(i * 16 + 8, j * 16 + 8),
			velocity,
			te.StoredItem.shoot,
			te.StoredItem.damage,
			te.StoredItem.knockBack,
			owner
		);
	}

	public override bool RightClick(int i, int j)
	{
		if (!TileEntity.TryGet(i, j, out TrapTileEntity te))
			return true;

		TrapUISystem.Instance.OpenUI(te, i, j);
		return true;
	}

	public override void MouseOver(int i, int j)
	{
		Player player = Main.LocalPlayer;
		player.noThrow = 2;
		player.cursorItemIconEnabled = true;

		if (TileEntity.TryGet(i, j, out TrapTileEntity te) && !te.StoredItem.IsAir)
			player.cursorItemIconID = te.StoredItem.type;
		else
			player.cursorItemIconID = ModContent.ItemType<Items.TrapItem>();
	}

	public override bool IsTileDangerous(int i, int j, Player player)
		=> TileEntity.TryGet(i, j, out TrapTileEntity te) && !te.StoredItem.IsAir;

	// Called for tiles placed via TileObjectData when destroyed
	public override void KillMultiTile(int i, int j, int frameX, int frameY)
	{
		// Drop stored item server-side only (item spawning is server-authoritative)
		if (Main.netMode != NetmodeID.MultiplayerClient
			&& TileEntity.TryGet(i, j, out TrapTileEntity te)
			&& !te.StoredItem.IsAir) {
			Item.NewItem(new EntitySource_TileBreak(i, j),
				i * 16, j * 16, 16, 16,
				te.StoredItem.type, te.StoredItem.stack);
		}

		ModContent.GetInstance<TrapTileEntity>().Kill(i, j);
	}

	// Draw a temporary direction arrow until real sprites are added.
	// To replace: add Content/Tiles/TrapTile.png with 4×18px horizontal frames.
	public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
	{
		Tile tile = Main.tile[i, j];
		if (!tile.HasTile)
			return;

		int dir = tile.TileFrameX / 18;
		string arrow = dir switch { 0 => "◄", 1 => "►", 2 => "▲", _ => "▼" };

		Vector2 pos = new Vector2(
			i * 16 - (int)Main.screenPosition.X + 1,
			j * 16 - (int)Main.screenPosition.Y + 1);
		Utils.DrawBorderString(spriteBatch, arrow, pos, Color.OrangeRed, 0.7f);
	}
}
