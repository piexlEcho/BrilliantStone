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
    public class BrilliantDagger : ModItem
    {
        // 基础使用时间（帧），用于动态计算攻速
        private const int BaseUseTime = 20;

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            // 基础属性
            Item.width = 32;
            Item.height = 32;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(silver: 50); // 50银币，可调整

            // 战斗属性
            Item.damage = 18;                // 基础伤害
            Item.knockBack = 4f;              // 击退
            Item.DamageType = DamageClass.MeleeNoSpeed; // 近战，但速度由我们手动控制

            // 使用方式
            Item.useStyle = ItemUseStyleID.Shoot;    // 使用射弹风格（模拟匕首挥舞）
            Item.useTime = BaseUseTime;
            Item.useAnimation = BaseUseTime;
            Item.autoReuse = true;                   // 按住连续挥舞
            Item.noMelee = true;                      // 物品本身不造成近战伤害，全部由射弹负责
            Item.noUseGraphic = true;                  // 使用时隐藏物品本身，仅显示射弹

            // 射弹相关（匕首挥出的“剑尖”）
            Item.shoot = ModContent.ProjectileType<BrilliantDaggerProj>();
            Item.shootSpeed = 6.5f;                     // 基础射速，影响攻击距离

            // 音效
            Item.UseSound = SoundID.Item1;               // 近战挥动音效
        }

        // 根据感染层数动态调整使用时间（攻速）
        public override float UseTimeMultiplier(Player player)
        {
            if (player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
            {
                var brilliantPlayer = player.GetModPlayer<BrilliantPlayer>();
                int stacks = brilliantPlayer.infectionStacks;
                if (stacks > 0)
                {
                    // 每层减少 1 帧使用时间，最低 5 帧
                    int targetUseTime = BaseUseTime - stacks;
                    if (targetUseTime < 5) targetUseTime = 5;
                    return (float)targetUseTime / BaseUseTime; // 返回乘数（<1 表示更快）
                }
            }
            return 1f;
        }

        // 根据感染层数增加基础伤害（小幅度）
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
            {
                var brilliantPlayer = player.GetModPlayer<BrilliantPlayer>();
                int stacks = brilliantPlayer.infectionStacks;
                if (stacks > 0)
                {
                    // 每层 +2 基础伤害，最多 +10（从 18 提升到 28）
                    int extra = stacks * 2;
                    if (extra > 10) extra = 10;
                    damage.Flat += extra;
                }
            }
        }

        // 重写 Shoot 方法，以根据感染层数增加射弹速度（提升攻击距离）
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 计算修正后的速度
            float speedMultiplier = 1f;
            if (player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
            {
                var brilliantPlayer = player.GetModPlayer<BrilliantPlayer>();
                int stacks = brilliantPlayer.infectionStacks;
                if (stacks > 0)
                {
                    // 每层增加 10% 速度，最多 50%（层数上限 5 时）
                    speedMultiplier += stacks * 0.1f;
                    if (speedMultiplier > 1.5f) speedMultiplier = 1.5f;
                }
            }

            Vector2 newVelocity = velocity * speedMultiplier;

            // 生成自定义射弹（匕首尖）
            Projectile.NewProjectile(source, position, newVelocity, type, damage, knockback, player.whoAmI);

            return false; // 阻止原版生成射弹（因为我们自己生成了）
        }

        // 添加配方（简单示例：5个辉石在炉子合成）
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<PureBrrilliantStone>(5)      // 消耗 5 个纯净辉石
                .AddIngredient<BrilliantCarapace>(5)    // 消耗 5 个辉石甲壳
                .AddTile(ModContent.TileType<Content.Tiles.EXPressureFurnaceTile>())
                .Register();
        }
    }
}