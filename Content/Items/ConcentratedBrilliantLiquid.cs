using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using BrilliantStone.Content.Buffs;
using BrilliantStone.Content.Tiles;
using BrilliantStone.Content.Items;
using BrilliantStone.Content.Players;

namespace BrilliantStone.Content.Items
{
    public class ConcentratedBrilliantLiquid : ModItem
    {
        public override LocalizedText DisplayName => Language.GetText("Mods.BrilliantStone.Items.ConcentratedBrilliantLiquid.DisplayName");
        public override LocalizedText Tooltip => Language.GetText("Mods.BrilliantStone.Items.ConcentratedBrilliantLiquid.Tooltip");

        public override void SetDefaults()
        {
            Item.width = 12;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 999;
            Item.consumable = true;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = 0; // 不可售卖
            Item.noMelee = true;
            // 不设置 createTile，表示不可放置
        }

        public override bool? UseItem(Player player)
        {
            if (player.itemAnimation > 0 && player.itemTime == 0)
            {
                ApplyInfection(player, 1200);
            }
            return true;
        }

        internal static void ApplyInfection(Player target, int duration)
        {
            target.GetModPlayer<BrilliantPlayer>().AddInfectionStack(duration);
        }

        public override void AddRecipes()
        {
            CreateRecipe(5) // 产出5个
                .AddIngredient<PureBrrilliantStone>(20) // 20个纯净辉石
                .AddIngredient(ItemID.Bottle, 5)       // 5个玻璃瓶
                .AddTile(ModContent.TileType<EXPressureFurnaceTile>()) // 高压超炉
                .Register();
        }
    }
}