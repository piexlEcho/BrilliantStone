using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Items
{
    public class PureBrrilliantStone : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 32;
            Item.maxStack = 9999;          // 可大量堆叠用于放置
            Item.value = Item.sellPrice(copper: 90);
            Item.rare = ItemRarityID.LightPurple;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.autoReuse = true;
            Item.consumable = true;         // 放置时消耗
            Item.material = true;
            Item.createTile = ModContent.TileType<Tiles.PureBrilliantStoneTile>(); // 关联新物块
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ModContent.ItemType<BrilliantStone>(), 15)
                .AddTile(ModContent.TileType<Content.Tiles.EXPressureFurnaceTile>())
                .Register();
        }
    }
}