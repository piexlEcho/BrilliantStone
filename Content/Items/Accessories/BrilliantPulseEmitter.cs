using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using BrilliantStone.Content.Players;
using BrilliantStone.Content.Items;
using BrilliantStone.Content.Tiles;
using BrilliantStone.Content.Buffs;

namespace BrilliantStone.Content.Items.Accessories
{
    public class BrilliantPulseEmitter : ModItem
    {
        public override LocalizedText DisplayName => Language.GetText("Mods.BrilliantStone.Items.BrilliantPulseEmitter.DisplayName");
        public override LocalizedText Tooltip => Language.GetText("Mods.BrilliantStone.Items.BrilliantPulseEmitter.Tooltip");

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightRed; // 可调整，建议与材料匹配
            Item.value = Item.sellPrice(gold: 2);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            // 每帧添加 buff，持续时间 2 帧（足够保持 buff 常驻）
            player.AddBuff(ModContent.BuffType<ReversePulseBuff>(), 2);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Hive, 1)          // 蜂窝
                .AddIngredient<PureBrrilliantStone>(20) // 纯净辉石
                .AddIngredient(ItemID.Bezoar, 1)        // 牛黄
                .AddTile<EXPressureFurnaceTile>()
                .Register();
        }
    }
}