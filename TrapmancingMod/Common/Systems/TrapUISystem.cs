using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using TrapmancingMod.Common.UI;
using TrapmancingMod.Content.TileEntities;

namespace TrapmancingMod.Common.Systems;

[Autoload(Side = ModSide.Client)]
public class TrapUISystem : ModSystem
{
	public static TrapUISystem Instance => ModContent.GetInstance<TrapUISystem>();

	private UserInterface _ui;
	private TrapUIState _state;

	public bool IsOpen => _ui?.CurrentState != null;

	public override void Load()
	{
		_ui = new UserInterface();
		_state = new TrapUIState();
		_state.Activate();
	}

	public void OpenUI(TrapTileEntity te, int tileX, int tileY)
	{
		_state.Load(te, tileX, tileY);
		_ui.SetState(_state);
		Main.playerInventory = true; // open inventory so the slot is interactable
	}

	public void CloseUI()
	{
		if (!IsOpen)
			return;

		_state.Flush(); // sync slot contents back to the entity + server
		_ui.SetState(null);
	}

	public override void UpdateUI(GameTime gameTime)
	{
		if (!IsOpen)
			return;

		Player player = Main.LocalPlayer;

		// Auto-close when the player closes the inventory or walks too far away
		if (!Main.playerInventory) {
			CloseUI();
			return;
		}

		const float MaxDistSq = 200f * 200f;
		Vector2 tileCenter = new Vector2(_state.TileX * 16 + 8, _state.TileY * 16 + 8);
		if (Vector2.DistanceSquared(player.Center, tileCenter) > MaxDistSq) {
			CloseUI();
			return;
		}

		_ui.Update(gameTime);
	}

	public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
	{
		int idx = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
		if (idx < 0)
			return;

		layers.Insert(idx, new LegacyGameInterfaceLayer(
			"TrapmancingMod: Trap UI",
			() => {
				if (IsOpen)
					_ui.Draw(Main.spriteBatch, new GameTime());
				return true;
			},
			InterfaceScaleType.UI
		));
	}
}
