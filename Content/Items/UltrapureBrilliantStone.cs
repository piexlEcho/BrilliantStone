using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Items
{
    public class UltrapureBrilliantStone : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 36;
            Item.maxStack = 9999;
            Item.value = 0;                     // 不可出售
            Item.rare = ItemRarityID.Yellow;
            Item.useStyle = ItemUseStyleID.Swing; // 使用动画（挥动）
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.autoReuse = true; // 自动重复使用
            Item.consumable = true;              // 消耗品（放置后物品减少）
            Item.createTile = ModContent.TileType<Tiles.UltrapureBrilliantStoneTile>();
            Item.placeStyle = 0;  // 0-2分别对应不同的外观
            Item.material = true;                 // 可作为合成材料
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<PureBrrilliantStone>(), 60)  // 60个纯净辉石
                .AddIngredient(ItemID.LifeCrystal, 5)                           // 5个生命水晶
                .AddTile(ModContent.TileType<Tiles.EXPressureFurnaceTile>())    // 高压超炉
                .Register();
        }
    }
}