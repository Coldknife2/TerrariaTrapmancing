using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace TrapmancingMod.Content.TileEntities;

public class TrapTileEntity : ModTileEntity
{
	public Item StoredItem = new();
	public string PlacerName = "";

	public override bool IsTileValidForEntity(int x, int y)
	{
		Tile tile = Main.tile[x, y];
		return tile.HasTile && tile.TileType == ModContent.TileType<Tiles.TrapTile>();
	}

	public override void SaveData(TagCompound tag)
	{
		if (!StoredItem.IsAir)
			tag["item"] = ItemIO.Save(StoredItem);
		if (PlacerName != "")
			tag["placer"] = PlacerName;
	}

	public override void LoadData(TagCompound tag)
	{
		StoredItem = tag.ContainsKey("item") ? ItemIO.Load(tag.GetCompound("item")) : new Item();
		PlacerName = tag.GetString("placer");
	}

	public override void NetSend(BinaryWriter writer)
	{
		ItemIO.Send(StoredItem, writer, writeStack: true);
		writer.Write(PlacerName);
	}

	public override void NetReceive(BinaryReader reader)
	{
		StoredItem = ItemIO.Receive(reader, readStack: true);
		PlacerName = reader.ReadString();
	}

	// Custom placement hook: replicates Generic_Hook_AfterPlacement and additionally
	// records who placed the trap so that fired projectiles are attributed to them.
	public int HookAfterPlacement(int i, int j, int type, int style, int direction, int alternate)
	{
		if (Main.netMode == NetmodeID.MultiplayerClient) {
			// Tell the server to create the tile entity (standard TModLoader protocol)
			NetMessage.SendTileSquare(Main.myPlayer, i, j, 1, 1);
			NetMessage.SendData(MessageID.TileEntityPlacement, number: i, number2: j, number3: Type);

			// Immediately follow with our placer-name packet. Because both packets travel
			// over the same TCP stream, SyncPlacer is guaranteed to arrive after the server
			// has processed TileEntityPlacement and created the entity.
			var packet = Mod.GetPacket();
			packet.Write((byte)PacketType.SyncPlacer);
			packet.Write((short)i);
			packet.Write((short)j);
			packet.Write(Main.LocalPlayer.name);
			packet.Send();
			return -1;
		}

		// Single-player or listen-server: create the entity directly
		int id = Place(i, j);
		if (TileEntity.ByID.TryGetValue(id, out TileEntity te) && te is TrapTileEntity trap)
			trap.PlacerName = Main.LocalPlayer.name;
		return id;
	}

	/// <summary>
	/// Returns the player index to use as projectile owner when the trap fires.
	/// Falls back to 255 (no owner) if the placer is offline, meaning anyone can be hit.
	/// </summary>
	public int FindOwnerPlayer()
	{
		if (PlacerName == "")
			return Main.netMode == NetmodeID.SinglePlayer ? Main.myPlayer : 255;

		for (int i = 0; i < Main.maxPlayers; i++) {
			Player p = Main.player[i];
			if (p.active && p.name == PlacerName)
				return i;
		}

		// Placer is offline: revert to ownerless so the trap is still dangerous
		return Main.netMode == NetmodeID.SinglePlayer ? Main.myPlayer : 255;
	}
}
