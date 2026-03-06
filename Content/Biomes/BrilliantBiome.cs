using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Biomes
{
    public class BrilliantBiome : ModBiome
    {
        public override LocalizedText DisplayName => Language.GetText("Mods.BrilliantStone.BiomeName");

        // 取消注释下面两行，关联背景样式
        public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle => ModContent.GetInstance<BrilliantSurfaceBackgroundStyle>();
        public override ModUndergroundBackgroundStyle UndergroundBackgroundStyle => ModContent.GetInstance<BrilliantUndergroundBackgroundStyle>();

        public override int Music => MusicID.Jungle; // 音乐暂时不变
        public override SceneEffectPriority Priority => SceneEffectPriority.BiomeHigh;

        public override bool IsBiomeActive(Player player)
        {
            // ... 你的判定逻辑保持不变 ...
            int range = 40;
            int tileX = (int)(player.Center.X / 16);
            int tileY = (int)(player.Center.Y / 16);

            int minX = Utils.Clamp(tileX - range, 5, Main.maxTilesX - 5);
            int maxX = Utils.Clamp(tileX + range, 5, Main.maxTilesX - 5);
            int minY = Utils.Clamp(tileY - range, 5, Main.maxTilesY - 5);
            int maxY = Utils.Clamp(tileY + range, 5, Main.maxTilesY - 5);

            int countBrilliant = 0;
            int countPure = 0;
            int countUltra = 0;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile != null && tile.HasTile)
                    {
                        if (tile.TileType == ModContent.TileType<Tiles.BrilliantStoneTile>())
                            countBrilliant++;
                        else if (tile.TileType == ModContent.TileType<Tiles.PureBrilliantStoneTile>())
                            countPure++;
                        else if (tile.TileType == ModContent.TileType<Tiles.UltrapureBrilliantStoneTile>())
                            countUltra++;
                    }
                }
            }

            return countBrilliant + countPure + countUltra > 150;
        }

        public override void OnEnter(Player player) { }
        public override void OnLeave(Player player) { }
    }
}