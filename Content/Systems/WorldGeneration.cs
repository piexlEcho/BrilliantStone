using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace BrilliantStone
{
    public class WorldGeneration : ModSystem
    {
        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            int finalIndex = tasks.FindIndex(genpass => genpass.Name.Equals("Final Cleanup"));
            if (finalIndex != -1)
                tasks.Insert(finalIndex + 1, new BrilliantCrystalGenPass("Brilliant Stone Crystals", 100.0f));
            else
                tasks.Add(new BrilliantCrystalGenPass("Brilliant Stone Crystals", 100.0f));
        }
    }

    public class BrilliantCrystalGenPass : GenPass
    {
        public BrilliantCrystalGenPass(string name, float loadWeight) : base(name, loadWeight) { }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = "Growing Brilliant Stone Crystals";

            int jungleMinX = GenVars.jungleMinX;
            int jungleMaxX = GenVars.jungleMaxX;
            if (jungleMinX == 0 && jungleMaxX == 0)
            {
                jungleMinX = Main.maxTilesX / 3;
                jungleMaxX = Main.maxTilesX * 2 / 3;
            }

            int spacing = 150;
            int attempts = (jungleMaxX - jungleMinX) / spacing * 2;

            for (int i = 0; i < attempts; i++)
            {
                int x = WorldGen.genRand.Next(jungleMinX, jungleMaxX);
                int groundY = FindJungleSurface(x);
                if (groundY == -1) continue;

                PlaceCrystalCluster(x, groundY, WorldGen.genRand.Next(2, 4));
            }
        }

        private int FindJungleSurface(int x)
        {
            for (int y = 10; y < Main.maxTilesY - 200; y++)
            {
                Tile tile = Main.tile[x, y];
                if (tile == null || !tile.HasTile) continue;

                ushort type = tile.TileType;
                if (type == TileID.Mud || type == TileID.JungleGrass)
                {
                    bool hasSupport = false;
                    for (int checkY = y + 1; checkY < y + 5 && checkY < Main.maxTilesY; checkY++)
                    {
                        Tile below = Main.tile[x, checkY];
                        if (below != null && below.HasTile &&
                            (below.TileType == TileID.Mud || below.TileType == TileID.JungleGrass || below.TileType == TileID.Stone))
                        {
                            hasSupport = true;
                            break;
                        }
                    }
                    if (hasSupport) return y;
                }
            }
            return -1;
        }

        /// <summary>
        /// 放置一个完整的晶体簇（包含主刺和小刺）
        /// </summary>
        private void PlaceCrystalCluster(int baseX, int surfaceY, int mainSpikeCount)
        {
            // ===== 原有晶体生成逻辑继续 =====
            List<Point> allUltrapurePositions = new List<Point>();

            // 生成主刺
            for (int s = 0; s < mainSpikeCount; s++)
            {
                int offsetX = WorldGen.genRand.Next(-10, 10);  // 范围稍大，使刺分散
                int spikeX = baseX + offsetX;
                if (spikeX < GenVars.jungleMinX || spikeX > GenVars.jungleMaxX) continue;

                int groundY = FindJungleSurface(spikeX);
                if (groundY == -1) continue;

                // 随机倾斜因子 (-0.3 ~ 0.3)
                float tiltFactor = (float)(WorldGen.genRand.NextDouble() * 0.6 - 0.3);

                // 总长度、地上/地下部分
                int totalLength = WorldGen.genRand.Next(25, 40);
                int aboveGround = WorldGen.genRand.Next(10, 18);
                int belowGround = totalLength - aboveGround;

                int topY = groundY - aboveGround;
                int bottomY = groundY + belowGround;
                if (topY < 5) topY = 5;
                if (bottomY > Main.maxTilesY - 50) bottomY = Main.maxTilesY - 50;

                // 调用单个刺生成函数
                var spikePositions = PlaceSpike(spikeX, groundY, bottomY, topY, tiltFactor, isMain: true);
                allUltrapurePositions.AddRange(spikePositions);
            }

            // 生成小刺（在大簇周围随机点缀）
            int smallSpikeCount = WorldGen.genRand.Next(1, 3); // 1~2个小刺
            for (int i = 0; i < smallSpikeCount; i++)
            {
                // 在大簇中心附近随机偏移
                int smallX = baseX + WorldGen.genRand.Next(-20, 20); // 范围稍大，使小刺分散
                if (smallX < GenVars.jungleMinX || smallX > GenVars.jungleMaxX) continue;

                int groundY = FindJungleSurface(smallX);
                if (groundY == -1) continue;

                // 小刺参数：较短，更细，倾斜更大
                float tiltFactor = (float)(WorldGen.genRand.NextDouble() * 0.8 - 0.4); // -0.4 ~ 0.4
                int totalLength = WorldGen.genRand.Next(10, 14); // 小刺更短
                int aboveGround = WorldGen.genRand.Next(3, 5); // 小刺地上部分更短
                int belowGround = totalLength - aboveGround;

                int topY = groundY - aboveGround;
                int bottomY = groundY + belowGround;
                if (topY < 5) topY = 5;
                if (bottomY > Main.maxTilesY - 50) bottomY = Main.maxTilesY - 50;

                var spikePositions = PlaceSpike(smallX, groundY, bottomY, topY, tiltFactor, isMain: false);
                allUltrapurePositions.AddRange(spikePositions);
            }

            // ========== 添加双层隔离层（同之前逻辑） ==========
            HashSet<Point> firstLayer = new HashSet<Point>();
            foreach (Point pos in allUltrapurePositions)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = pos.X + dx;
                        int ny = pos.Y + dy;
                        if (nx < GenVars.jungleMinX || nx > GenVars.jungleMaxX || ny < 5 || ny >= Main.maxTilesY - 50) continue;

                        Tile tile = Main.tile[nx, ny];
                        if (tile == null) continue;

                        int tileType = tile.TileType;
                        if (tileType == ModContent.TileType<Content.Tiles.UltrapureBrilliantStoneTile>() ||
                            tileType == ModContent.TileType<Content.Tiles.PureBrilliantStoneTile>() ||
                            tileType == ModContent.TileType<Content.Tiles.BrilliantStoneTile>())
                        {
                            continue;
                        }
                        firstLayer.Add(new Point(nx, ny));
                    }
                }
            }

            foreach (Point pos in firstLayer)
            {
                WorldGen.PlaceTile(pos.X, pos.Y, ModContent.TileType<Content.Tiles.PureBrilliantStoneTile>(), true, true);
            }

            HashSet<Point> secondLayer = new HashSet<Point>();
            foreach (Point pos in firstLayer)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = pos.X + dx;
                        int ny = pos.Y + dy;
                        if (nx < GenVars.jungleMinX || nx > GenVars.jungleMaxX || ny < 5 || ny >= Main.maxTilesY - 50) continue;

                        if (firstLayer.Contains(new Point(nx, ny))) continue;

                        Tile tile = Main.tile[nx, ny];
                        if (tile == null) continue;

                        int tileType = tile.TileType;
                        if (tileType == ModContent.TileType<Content.Tiles.UltrapureBrilliantStoneTile>() ||
                            tileType == ModContent.TileType<Content.Tiles.PureBrilliantStoneTile>() ||
                            tileType == ModContent.TileType<Content.Tiles.BrilliantStoneTile>())
                        {
                            continue;
                        }
                        secondLayer.Add(new Point(nx, ny));
                    }
                }
            }

            foreach (Point pos in secondLayer)
            {
                WorldGen.PlaceTile(pos.X, pos.Y, ModContent.TileType<Content.Tiles.BrilliantStoneTile>(), true, true);
            }
        }

        /// <summary>
        /// 生成单个刺（倾斜），返回所有放置的至纯辉石坐标
        /// </summary>
        private List<Point> PlaceSpike(int baseX, int groundY, int bottomY, int topY, float tiltFactor, bool isMain)
        {
            List<Point> placed = new List<Point>();
            int totalLength = bottomY - topY; // 总长度（格数）

            // 从底部向顶部生成
            for (int y = bottomY; y >= topY; y--)
            {
                // 当前层在整根刺中的进度（0=底部，1=顶部）
                float progress = (float)(bottomY - y) / totalLength;

                // 计算中心X坐标：线性插值，底部偏移为0，顶部偏移为 tiltFactor * totalLength
                // 底部中心 = baseX，顶部中心 = baseX + tiltFactor * totalLength
                int centerX = baseX + (int)((bottomY - y) * tiltFactor);

                // 半径控制：底部稍粗，顶部尖细
                int radius;
                if (progress < 0.25f)          // 底部1/4
                    radius = WorldGen.genRand.Next(2, 4); // 主刺可稍粗，小刺较细
                else if (progress < 0.6f)       // 中部
                    radius = WorldGen.genRand.Next(1, 2);
                else                             // 顶部
                    radius = 1;

                // 如果是小刺，整体半径略小
                if (!isMain) radius = Math.Max(1, radius - 1);

                // 强制顶部5格只放中心（尖化）
                if (y <= topY + 4) // 顶部5格
                {
                    radius = 1; // 只放中心
                }

                // 放置这一层的方块
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int placeX = centerX + dx;
                    if (placeX < GenVars.jungleMinX || placeX > GenVars.jungleMaxX) continue;

                    // 边缘随机跳过，让形状更自然
                    if (Math.Abs(dx) == radius && WorldGen.genRand.NextBool(3)) continue;

                    Tile tile = Main.tile[placeX, y];
                    if (tile == null) continue;

                    // 允许覆盖：泥块、丛林草、空位，但不要覆盖已有的辉石（避免重叠混乱）
                    if (tile.HasTile)
                    {
                        int existingType = tile.TileType;
                        if (existingType == ModContent.TileType<Content.Tiles.UltrapureBrilliantStoneTile>() ||
                            existingType == ModContent.TileType<Content.Tiles.PureBrilliantStoneTile>() ||
                            existingType == ModContent.TileType<Content.Tiles.BrilliantStoneTile>())
                        {
                            // 如果已经是辉石，只记录坐标但不重复放置（防止覆盖不同类型的辉石）
                            if (existingType == ModContent.TileType<Content.Tiles.UltrapureBrilliantStoneTile>())
                                placed.Add(new Point(placeX, y));
                            continue;
                        }
                        if (existingType != TileID.Mud && existingType != TileID.JungleGrass)
                            continue; // 只允许替换泥块/丛林草
                    }

                    // 放置至纯辉石
                    WorldGen.PlaceTile(placeX, y, ModContent.TileType<Content.Tiles.UltrapureBrilliantStoneTile>(), true, true);
                    placed.Add(new Point(placeX, y));
                }

                // 如果是顶部5格，额外保证中心有一个普通辉石（尖顶）
                if (y <= topY + 4)
                {
                    Tile centerTile = Main.tile[centerX, y];
                    if (centerTile != null && centerTile.HasTile && centerTile.TileType == ModContent.TileType<Content.Tiles.UltrapureBrilliantStoneTile>())
                    {
                        // 将中心替换为普通辉石
                        WorldGen.KillTile(centerX, y, noItem: true, effectOnly: false);
                        WorldGen.PlaceTile(centerX, y, ModContent.TileType<Content.Tiles.BrilliantStoneTile>(), true, true);
                        // 注意：此位置不再是至纯，所以要从 placed 中移除？不，至纯列表用于隔离层，这些位置不再是至纯，需要剔除
                        // 简便起见，我们在循环结束后再统一处理尖顶替换。
                    }
                }
            }

            // 额外处理：将所有顶部5格内的至纯辉石替换为普通辉石（尖顶）
            // 由于上面生成时可能已经放置了普通辉石，但我们需要确保顶部几层只有普通辉石且是单格
            // 最简单：再次扫描从 topY 到 topY+4，将每个Y处离中心最近的一个方块（中心X附近）改为普通辉石，清除其他方块
            for (int y = topY; y <= topY + 4 && y <= bottomY; y++)
            {
                // 重新计算这一层的中心X
                int centerX = baseX + (int)((bottomY - y) * tiltFactor);

                // 清除这一层所有至纯辉石
                for (int dx = -2; dx <= 2; dx++)
                {
                    int checkX = centerX + dx;
                    if (checkX < GenVars.jungleMinX || checkX > GenVars.jungleMaxX) continue;
                    Tile tile = Main.tile[checkX, y];
                    if (tile != null && tile.HasTile && tile.TileType == ModContent.TileType<Content.Tiles.UltrapureBrilliantStoneTile>())
                    {
                        WorldGen.KillTile(checkX, y, noItem: true, effectOnly: false);
                    }
                }
                // 在中心放置普通辉石
                WorldGen.PlaceTile(centerX, y, ModContent.TileType<Content.Tiles.BrilliantStoneTile>(), true, true);
            }

            // 重新收集至纯辉石坐标（可能因顶部替换而减少）
            placed.Clear();
            for (int y = bottomY; y >= topY; y--)
            {
                int centerX = baseX + (int)((bottomY - y) * tiltFactor);
                for (int dx = -3; dx <= 3; dx++)
                {
                    int checkX = centerX + dx;
                    if (checkX < GenVars.jungleMinX || checkX > GenVars.jungleMaxX) continue;
                    Tile tile = Main.tile[checkX, y];
                    if (tile != null && tile.HasTile && tile.TileType == ModContent.TileType<Content.Tiles.UltrapureBrilliantStoneTile>())
                    {
                        placed.Add(new Point(checkX, y));
                    }
                }
            }
            return placed;
        }
    }
}