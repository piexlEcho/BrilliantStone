using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Items
{
    public class EXPressureFurnace : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 99;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;        // 消耗品（放置后物品减少）
            Item.createTile = ModContent.TileType<Tiles.EXPressureFurnaceTile>(); // 放置后生成的物块
            Item.placeStyle = 0;            // 样式索引（如果有多个样式）
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = ItemRarityID.Blue;
        }

        // 添加该物品的合成配方
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Furnace)                         // 熔炉
                .AddIngredient(ItemID.GlassKiln)                       // 玻璃窑
                .AddIngredient(ItemID.HeavyWorkBench)                  // 重型工作台
                .AddRecipeGroup(RecipeGroupID.IronBar, 20)             // 改为 IronBar，代表铁锭或铅锭
                .AddTile(TileID.HeavyWorkBench)                         // 需要重型工作台来合成自身
                .Register();
        }
    }
}