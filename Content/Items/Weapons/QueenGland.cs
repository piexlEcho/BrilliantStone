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
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.buyPrice(gold: 2, silver: 50);

            Item.damage = 12;
            Item.knockBack = 2f;
            Item.mana = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Summon;

            Item.shoot = ModContent.ProjectileType<QueenGuardMinion>();
            Item.buffType = ModContent.BuffType<QueenGuardBuff>();
            Item.buffTime = 3600;

            Item.UseSound = SoundID.Item44;
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 只在服务器或单机生成随从，避免联机重复生成
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                position = Main.MouseWorld;
                // 将基础伤害通过 ai1 传递给随从
                Projectile.NewProjectile(source, position, Vector2.Zero, type, damage, knockback, player.whoAmI, ai1: damage);
            }
            player.AddBuff(Item.buffType, Item.buffTime);
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SlimeStaff)
                .AddIngredient<CystQueenCore>(1)
                .AddIngredient<PureBrrilliantStone>(10)
                .AddTile(ModContent.TileType<Content.Tiles.EXPressureFurnaceTile>())
                .Register();
        }
    }
}