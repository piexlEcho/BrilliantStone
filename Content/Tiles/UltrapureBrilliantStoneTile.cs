using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using BrilliantStone.Content.Buffs;
using BrilliantStone.Content.Players;

namespace BrilliantStone.Content.Tiles
{
    public class UltrapureBrilliantStoneTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileMergeDirt[Type] = false;

            AddMapEntry(new Color(220, 120, 220), CreateMapEntryName());

            HitSound = SoundID.Tink;
            DustType = DustID.Stone;

            MineResist = 4f;
            MinPick = 100; // 需要金镐或以上
        }

        // 发光：暖黄色
        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 1.0f;
            g = 0.8f;
            b = 0.2f;
        }

        // 感染逻辑：无条件感染周围一圈（3x3），生成纯净辉石矿
        public override void RandomUpdate(int i, int j)
        {
            if (Main.rand.NextBool(2))
            {
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
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

                        // 感染非辉石物块
                        if (Main.rand.NextFloat() < 0.9f)
                        {
                            WorldGen.KillTile(targetX, targetY, noItem: true, effectOnly: false);
                            WorldGen.PlaceTile(targetX, targetY, ModContent.TileType<PureBrilliantStoneTile>(), forced: true);

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

        // 附近玩家获得辉石感染
        public override void NearbyEffects(int i, int j, bool closer)
        {
            if (closer)
            {
                foreach (Player player in Main.player)
                {
                    if (player.active && !player.dead &&
                        System.Math.Abs(player.Center.X / 16 - i) <= 2 &&
                        System.Math.Abs(player.Center.Y / 16 - j) <= 2)
                    {
                        // 每60帧（1秒）调用一次，避免每帧叠加
                        if (Main.GameUpdateCount % 30 == 0)
                        {
                            player.GetModPlayer<BrilliantPlayer>().AddInfectionStack(180); // 持续1秒
                        }
                    }
                }
            }
        }

        // 掉落对应物品
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (fail) return; // 如果挖掘失败（如被保护），不掉落物品
            //int itemType = ModContent.ItemType<Content.Items.UltrapureBrilliantStone>();
            //int amount = 1;
            //Item.NewItem(new Terraria.DataStructures.EntitySource_TileBreak(i, j), i * 16, j * 16, 16, 16, itemType, amount);
        }
    }
}