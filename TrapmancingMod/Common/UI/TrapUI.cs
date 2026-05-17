using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using TrapmancingMod.Content.TileEntities;

namespace TrapmancingMod.Common.UI;

/// <summary>
/// A single item-slot UIElement, using the vanilla ItemSlot Draw/Handle API (ref Item form).
/// </summary>
internal class SingleItemSlot : UIElement
{
	public Item Item = new();
	private readonly int _context;
	private readonly float _scale;

	public SingleItemSlot(int context = ItemSlot.Context.ChestItem, float scale = 1f)
	{
		_context = context;
		_scale = scale;

		// Slot background texture drives the element's size
		float size = TextureAssets.InventoryBack9.Value.Width * scale;
		Width.Set(size, 0f);
		Height.Set(size, 0f);
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		float oldScale = Main.inventoryScale;
		Main.inventoryScale = _scale;

		Vector2 pos = GetDimensions().Position();
		if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface) {
			Main.LocalPlayer.mouseInterface = true;
			ItemSlot.Handle(ref Item, _context);
		}
		ItemSlot.Draw(spriteBatch, ref Item, _context, pos);

		Main.inventoryScale = oldScale;
	}
}

/// <summary>
/// UI panel shown when the player right-clicks a TrapTile.
/// </summary>
public class TrapUIState : UIState
{
	private UIPanel _panel;
	private SingleItemSlot _slot;
	private TrapTileEntity _currentTE;

	public int TileX { get; private set; }
	public int TileY { get; private set; }

	public override void OnInitialize()
	{
		_panel = new UIPanel {
			HAlign = 0.5f,
			VAlign = 0.4f,
		};
		_panel.Width.Set(110f, 0f);
		_panel.Height.Set(95f, 0f);
		Append(_panel);

		_slot = new SingleItemSlot(ItemSlot.Context.ChestItem, 1f);
		_slot.HAlign = 0.5f;
		_slot.VAlign = 0.55f;
		_panel.Append(_slot);
	}

	public void Load(TrapTileEntity te, int tileX, int tileY)
	{
		_currentTE = te;
		TileX = tileX;
		TileY = tileY;
		_slot.Item = te.StoredItem.Clone();
	}

	/// <summary>
	/// Pushes the slot's current item back to the tile entity and to the server (MP).
	/// </summary>
	public void Flush()
	{
		if (_currentTE == null)
			return;

		_currentTE.StoredItem = _slot.Item.Clone();

		if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient) {
			ModPacket packet = ModContent.GetInstance<TrapmancingMod>().GetPacket();
			packet.Write((byte)PacketType.SyncItem);
			packet.Write((short)TileX);
			packet.Write((short)TileY);
			Terraria.ModLoader.IO.ItemIO.Send(_slot.Item, packet, writeStack: true);
			packet.Send();
		}

		_currentTE = null;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		CalculatedStyle panelDims = _panel.GetDimensions();
		Utils.DrawBorderString(spriteBatch, "Trap Item",
			new Vector2(panelDims.X + 12f, panelDims.Y + 8f),
			Color.White, 0.8f);
	}
}
