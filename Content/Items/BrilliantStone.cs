using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Items
{
    public class BrilliantStone : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 30;
            Item.maxStack = 9999;
            Item.value = 0;                     // 不可出售
            Item.rare = ItemRarityID.Yellow;
            Item.useStyle = ItemUseStyleID.Swing; // 放置动画
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.autoReuse = true;               // 自动重复放置
            Item.consumable = true;               // 放置时消耗
            Item.createTile = ModContent.TileType<Tiles.BrilliantStoneTile>();
            Item.placeStyle = 0;                  // 可选样式
            Item.material = true;                  // 可作为合成材料

            // 新增：标记为弹药类型，用于支持武器消耗
            Item.ammo = Item.type;
        }
    }
}