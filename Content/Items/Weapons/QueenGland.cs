using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using BrilliantStone.Content.Buffs;
using BrilliantStone.Content.Projectiles.Minion;

namespace BrilliantStone.Content.Items.Weapons
{
    public class QueenGland : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 中文名已在 .hjson 或本地化文件中定义
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Orange;          // 稀有度橙色，比蓝色高
            Item.value = Item.buyPrice(gold: 2, silver: 50); // 2金50银

            Item.damage = 12;                          // 基础伤害（随从伤害）
            Item.knockBack = 2f;
            Item.mana = 10;                             // 耗蓝
            Item.useStyle = ItemUseStyleID.Swing;       // 挥动使用
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;       // 召唤伤害类型

            // 召唤物设置
            Item.shoot = ModContent.ProjectileType<QueenGuardMinion>(); // 随从投射物
            Item.buffType = ModContent.BuffType<QueenGuardBuff>();      // 召唤buff
            Item.buffTime = 3600;                       // buff持续时间（秒）

            Item.UseSound = SoundID.Item44;             // 召唤音效
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 召唤时直接生成随从，同时给予buff（原版机制会自动处理）
            // 但我们需要确保随从的初始位置合理
            position = Main.MouseWorld; // 召唤在鼠标位置
            Projectile.NewProjectile(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI);
            player.AddBuff(Item.buffType, Item.buffTime);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SlimeStaff)        // 史莱姆法杖
                .AddIngredient<CystQueenCore>(1)         // 女皇核心（BOSS掉落）
                .AddIngredient<PureBrrilliantStone>(10)  // 纯净辉石（注意拼写）
                .AddTile(ModContent.TileType<Content.Tiles.EXPressureFurnaceTile>())
                .Register();
        }
    }
}