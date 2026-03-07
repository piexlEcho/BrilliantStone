using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using BrilliantStone.Content.Projectiles;
using BrilliantStone.Content.Players;
using BrilliantStone.Content.Buffs;

namespace BrilliantStone.Content.Items.Weapons
{
    public class BrilliantWelder : ModItem
    {
        // 记录基础使用时间，用于动态调整射速
        private int baseUseTime = 15;

        public override void SetDefaults()
        {
            Item.width = 70;
            Item.height = 32;
            Item.rare = ItemRarityID.Blue;
            Item.value = 0;

            Item.damage = 20;
            Item.knockBack = 5f;
            Item.DamageType = DamageClass.Ranged;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = baseUseTime;
            Item.useAnimation = 30;
            Item.autoReuse = false;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<HyperWormLaser>();
            Item.shootSpeed = 2f;
            Item.UseSound = SoundID.Item15;

            Item.useAmmo = ModContent.ItemType<BrilliantStone>();
        }

        public override bool CanUseItem(Player player)
        {
            // 检查背包中是否有弹药（至少一个辉石）
            return player.HasItem(ModContent.ItemType<BrilliantStone>());
        }

        // 使用 UseTimeMultiplier 来动态调整使用时间
        public override float UseTimeMultiplier(Player player)
        {
            // 当玩家拥有辉石感染 buff 时，根据层数减少使用时间
            if (player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
            {
                var brilliantPlayer = player.GetModPlayer<BrilliantPlayer>();
                int stacks = brilliantPlayer.infectionStacks;
                if (stacks > 0)
                {
                    int targetUseTime = baseUseTime - stacks;
                    if (targetUseTime < 5) targetUseTime = 5;
                    return (float)targetUseTime / baseUseTime; // 乘数小于1表示更快
                }
            }
            return 1f; // 默认无加成
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            // 根据感染层数增加伤害，每层 +4，最多 +20（总伤害 ≤ 40）
            if (player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
            {
                var brilliantPlayer = player.GetModPlayer<BrilliantPlayer>();
                int stacks = brilliantPlayer.infectionStacks;
                if (stacks > 0)
                {
                    int extraDamage = stacks * 4;
                    if (extraDamage > 20)
                    {
                        extraDamage = 20; // 基础伤害20，最多到40
                    }
                    damage.Flat += extraDamage;
                }
            }
        }

        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            // 拥有 buff 时，30% 概率不消耗弹药
            if (player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
            {
                // 返回 false 表示不消耗弹药
                return Main.rand.NextFloat() >= 0.3f;
            }
            return true; // 默认消耗
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 生成投射物（ai0 = 1 为自定义参数，可在投射物 AI 中使用）
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, ai0: 1);

            // 后座力：只有没有 buff 时才施加
            if (!player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
            {
                Vector2 recoil = -velocity.SafeNormalize(Vector2.Zero) * 4f;
                player.velocity += recoil;
            }

            return false; // 阻止原版生成投射物
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<HyperCore>(3)
                .AddIngredient<BrilliantStone>(20)
                .AddTile(ModContent.TileType<Content.Tiles.EXPressureFurnaceTile>())
                .AddCondition(Language.GetText("Mods.BrilliantStone.Conditions.InBrilliantBiome"), () => Main.LocalPlayer.InModBiome<Biomes.BrilliantBiome>())
                .Register();
        }
    }
}