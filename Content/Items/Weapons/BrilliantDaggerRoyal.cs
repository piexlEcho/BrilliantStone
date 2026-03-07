using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using BrilliantStone.Content.Players;
using BrilliantStone.Content.Buffs;
using BrilliantStone.Content.Projectiles;

namespace BrilliantStone.Content.Items.Weapons
{
    public class BrilliantDaggerRoyal : ModItem
    {
        private const int BaseUseTime = 15; // 基础使用时间（更快）

        public override void SetStaticDefaults()
        {
            // 可设置显示名称和工具提示
        }

        public override void SetDefaults()
        {
            // 基础属性
            Item.width = 40;
            Item.height = 40;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(gold: 30); // 30金币

            // 战斗属性 - 显著提升
            Item.damage = 30;
            Item.knockBack = 5f;
            Item.DamageType = DamageClass.MeleeNoSpeed;

            // 使用方式
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = BaseUseTime;
            Item.useAnimation = BaseUseTime;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            // 射弹 - 使用升级版投射物
            Item.shoot = ModContent.ProjectileType<BrilliantDaggerProj2>();
            Item.shootSpeed = 8f; // 更快的基础速度

            // 音效
            Item.UseSound = SoundID.Item1;
        }

        // 感染层数增加攻速
        public override float UseTimeMultiplier(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
            {
                var brilliantPlayer = player.GetModPlayer<BrilliantPlayer>();
                int stacks = brilliantPlayer.infectionStacks;
                if (stacks > 0)
                {
                    int targetUseTime = BaseUseTime - stacks;
                    if (targetUseTime < 3) targetUseTime = 3; // 最快攻速限制
                    return (float)targetUseTime / BaseUseTime;
                }
            }
            return 1f;
        }

        // 感染层数增加伤害
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
            {
                var brilliantPlayer = player.GetModPlayer<BrilliantPlayer>();
                int stacks = brilliantPlayer.infectionStacks;
                if (stacks > 0)
                {
                    int extra = stacks * 3; // 每层 +3，最多 +15
                    if (extra > 15) extra = 15;
                    damage.Flat += extra;
                }
            }
        }

        // 感染层数增加射弹速度（攻击距离）并生成投射物
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            float speedMultiplier = 1f;
            if (player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
            {
                var brilliantPlayer = player.GetModPlayer<BrilliantPlayer>();
                int stacks = brilliantPlayer.infectionStacks;
                if (stacks > 0)
                {
                    speedMultiplier += stacks * 0.12f; // 每层 +12%，最多 +60%
                    if (speedMultiplier > 1.6f) speedMultiplier = 1.6f;
                }
            }

            Vector2 newVelocity = velocity * speedMultiplier;
            Projectile.NewProjectile(source, position, newVelocity, type, damage, knockback, player.whoAmI);
            return false;
        }

        // 合成配方：原匕首 + BOSS核心 + 材料
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<BrilliantDagger>()               // 原版光辉匕首
                .AddIngredient<CystQueenCore>(1)                // BOSS掉落核心
                .AddTile(ModContent.TileType<Content.Tiles.EXPressureFurnaceTile>())
                .AddCondition(Language.GetText("Mods.BrilliantStone.Conditions.InBrilliantBiome"), () => Main.LocalPlayer.InModBiome<Biomes.BrilliantBiome>())
                .Register();
        }
    }
}