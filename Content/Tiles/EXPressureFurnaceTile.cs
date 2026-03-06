using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BrilliantStone.Content.Tiles
{
    public class EXPressureFurnaceTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            // 基础属性
            Main.tileSolid[Type] = false;                     // 非实心方块（家具通常不是实心）
            Main.tileSolidTop[Type] = false;                   // 不强制平台化
            Main.tileFrameImportant[Type] = true;               // 帧重要（防止被相邻物块覆盖）
            Main.tileNoAttach[Type] = true;                     // 不可附着在其他物块上
            Main.tileLavaDeath[Type] = true;                    // 遇岩浆销毁
            Main.tileWaterDeath[Type] = false;                  // 遇水不销毁
            Main.tileTable[Type] = false;                         // 可作为工作台（用于NPC房屋判定）
            Main.tileLighted[Type] = true;                       // 可发光

            // 添加地图显示名称（本地化）
            AddMapEntry(new Color(200, 120, 50), CreateMapEntryName());

            // 音效：挖掘使用金属音效
            HitSound = SoundID.Tink;
            DustType = DustID.Stone;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 }; // 三个格子各高16像素
            TileObjectData.newTile.CoordinateWidth = 16; // 每个格子宽32像素
            TileObjectData.newTile.CoordinatePadding = 2; // 每个格子之间的间距（根据实际贴图调整）
            TileObjectData.newTile.Origin = new Point16(0, 2);           // 放置时锚点在下格（0,1）
            TileObjectData.newTile.StyleHorizontal = false;               // 水平方向有样式变化
            TileObjectData.newTile.LavaDeath = true;                     // 遇岩浆销毁
            TileObjectData.newTile.DrawYOffset = 2;                      // 垂直偏移（避免与背景重叠）
            TileObjectData.addTile(Type);

            // 禁止与泥土融合
            Main.tileMergeDirt[Type] = false;

            // 挖掘所需镐力（设为0表示任意镐可挖）
            MineResist = 2f;        // 挖掘阻力（越大越难挖）
            MinPick = 0;             // 最小镐力
        }

        // 可选：鼠标悬停时显示名称（配合本地化）
        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<Content.Items.EXPressureFurnace>();
        }
    }
}