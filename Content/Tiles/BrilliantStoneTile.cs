using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Tiles
{
    public class BrilliantStoneTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = true;
            Main.tileBlockLight[Type] = true; // 阻挡光线
            Main.tileLighted[Type] = false; // 允许发光
            Main.tileMergeDirt[Type] = true; // 可以与泥土合并

            AddMapEntry(new Color(255, 128, 0), CreateMapEntryName());

            HitSound = SoundID.Tink;
            DustType = DustID.Stone;

            MineResist = 1f;
            MinPick = 0; // 任意镐可挖
        }

        // 无感染逻辑

        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            //if (fail) return;
            //int itemType = ModContent.ItemType<Content.Items.BrilliantStone>();
            //int amount = 1;
            //Item.NewItem(new Terraria.DataStructures.EntitySource_TileBreak(i, j), i * 16, j * 16, 16, 16, itemType, amount);
        }
    }
}