using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Projectiles
{
    public class BrilliantDaggerProj : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // 可选：设置类型名称等
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;               // 碰撞箱宽度
            Projectile.height = 18;               // 碰撞箱高度
            Projectile.friendly = true;           // 对敌人造成伤害
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee; // 近战伤害
            Projectile.penetrate = -1;            // 无限穿透（但通常短剑只命中一次）
            Projectile.tileCollide = false;       // 不碰撞物块（穿过墙壁）
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;       // 确保命中时考虑近战冷却
            Projectile.timeLeft = 30;              // 存在时间（约 0.5 秒）
            Projectile.extraUpdates = 0;
            Projectile.aiStyle = -1;                // 自定义 AI
        }

        public override void AI()
        {
            // 简单直线运动，不做特殊处理
            // 可以添加一点粒子效果增加视觉反馈
            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GemTopaz, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 可选：命中时添加感染效果？此处留空
            // 例如有概率给目标附加感染 debuff
        }
    }
}