using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Tiles
{
    public class PureBrilliantStoneTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileMergeDirt[Type] = false;

            AddMapEntry(new Color(150, 200, 255), CreateMapEntryName());

            HitSound = SoundID.Tink;
            DustType = DustID.Stone;

            MineResist = 2.5f;
            MinPick = 65; // 需要银镐/金镐左右
        }


        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.8f;
            g = 0.2f;
            b = 0f;
        }

        // 感染逻辑：有条件感染5x5区域，生成普通辉石矿
        public override void RandomUpdate(int i, int j)
        {
            if (Main.rand.NextBool(2))
            {
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        if (x == 0 && y == 0) continue;

                        int targetX = i + x;
                        int targetY = j + y;
                        if (!WorldGen.InWorld(targetX, targetY)) continue;

                        Tile targetTile = Main.tile[targetX, targetY];
                        if (!targetTile.HasTile) continue;

                        // 跳过所有辉石系列物块
                        int tileType = targetTile.TileType;
                        if (tileType == ModContent.TileType<UltrapureBrilliantStoneTile>() ||
                            tileType == ModContent.TileType<PureBrilliantStoneTile>() ||
                            tileType == ModContent.TileType<BrilliantStoneTile>())
                        {
                            continue;
                        }

                        // 仅感染指定基础物块
                        bool canInfect = tileType == TileID.Dirt ||
                                         tileType == TileID.Mud ||
                                         tileType == TileID.Sand ||
                                         tileType == TileID.SnowBlock ||
                                         tileType == TileID.Stone;

                        if (canInfect && Main.rand.NextFloat() < 0.9f)
                        {
                            WorldGen.KillTile(targetX, targetY, noItem: true, effectOnly: false);
                            WorldGen.PlaceTile(targetX, targetY, ModContent.TileType<BrilliantStoneTile>(), forced: true);

                            if (Main.netMode != NetmodeID.Server)
                            {
                                for (int d = 0; d < 3; d++)
                                    Dust.NewDust(new Vector2(targetX * 16, targetY * 16), 16, 16, DustID.Stone);
                            }
                        }
                    }
                }
            }
        }

        // 玩家站在上面无效果（可自行添加）
        // public override void FloorVisuals(Player player) { }

        // 掉落对应物品
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (fail) return;
            //int itemType = ModContent.ItemType<Content.Items.PureBrrilliantStone>(); // 注意物品名拼写
            //int amount = 1;
            //Item.NewItem(new Terraria.DataStructures.EntitySource_TileBreak(i, j), i * 16, j * 16, 16, 16, itemType, amount);
        }
    }
}