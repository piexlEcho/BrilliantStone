using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Items
{
    public class HyperCore : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 名称和描述将在本地化文件中设置
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 34;
            Item.maxStack = 9999;      // 可堆叠
            Item.value = 0;             // 无售卖价值
            Item.rare = ItemRarityID.Blue; // 蓝色品质
            Item.useStyle = ItemUseStyleID.None; // 无法使用
            Item.useTime = 0;
            Item.useAnimation = 0;
            Item.noUseGraphic = false;
            Item.consumable = true;    // 不是消耗品（但作为材料可堆叠）
        }
    }
}