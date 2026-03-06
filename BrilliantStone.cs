using System.IO;
using Terraria.ModLoader;
using Terraria;

namespace BrilliantStone
{
	public class BrilliantStone : Mod
	{
		// 与BrilliantPlayer中的常量保持一致
		private const byte InfectionStackSyncID = 0;

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			byte msgType = reader.ReadByte();
			if (msgType == InfectionStackSyncID)
			{
				byte playerId = reader.ReadByte();
				int stacks = reader.ReadInt32();
				if (Main.player[playerId]?.active == true)
				{
					var brilliantPlayer = Main.player[playerId].GetModPlayer<Content.Players.BrilliantPlayer>();
					brilliantPlayer.infectionStacks = stacks;
				}
			}
		}
	}
}