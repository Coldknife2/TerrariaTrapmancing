using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TrapmancingMod.Content.TileEntities;

namespace TrapmancingMod;

public enum PacketType : byte
{
	SyncPlacer,
	SyncItem,
}

public class TrapmancingMod : Mod
{
	public override void HandlePacket(BinaryReader reader, int whoAmI)
	{
		PacketType type = (PacketType)reader.ReadByte();
		switch (type) {
			case PacketType.SyncPlacer:
				HandleSyncPlacer(reader, whoAmI);
				break;
			case PacketType.SyncItem:
				HandleSyncItem(reader, whoAmI);
				break;
		}
	}

	// Client → Server: "I placed a trap at (x,y) and my name is placerName"
	private void HandleSyncPlacer(BinaryReader reader, int whoAmI)
	{
		short x = reader.ReadInt16();
		short y = reader.ReadInt16();
		string name = reader.ReadString();

		if (TileEntity.ByPosition.TryGetValue(new Point16(x, y), out TileEntity te)
			&& te is TrapTileEntity trap) {
			trap.PlacerName = name;
			// Broadcast updated entity to all clients
			NetMessage.SendData(MessageID.TileEntitySharing, number: trap.ID);
		}
		// If the entity hasn't been created yet the placer info is lost, which is rare
		// since TileEntityPlacement and SyncPlacer arrive on the same TCP stream in order
	}

	// Client → Server: "the item in trap at (x,y) is now <item>"
	private void HandleSyncItem(BinaryReader reader, int whoAmI)
	{
		short x = reader.ReadInt16();
		short y = reader.ReadInt16();
		Item item = Terraria.ModLoader.IO.ItemIO.Receive(reader, readStack: true);

		if (TileEntity.ByPosition.TryGetValue(new Point16(x, y), out TileEntity te)
			&& te is TrapTileEntity trap) {
			trap.StoredItem = item;
			NetMessage.SendData(MessageID.TileEntitySharing, number: trap.ID);
		}
	}
}
