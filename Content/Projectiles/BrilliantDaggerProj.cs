using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Projectiles
{
    public class BrilliantDaggerProj : ModProjectile
    {
        // 重力加速度（每帧增加的垂直速度）
        private const float Gravity = 0.08f;

        public override void SetStaticDefaults()
        {
            // 可选：设置类型名称等
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;            // 无限穿透
            Projectile.tileCollide = true;       // 碰撞物块
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;       // 考虑近战冷却
            Projectile.timeLeft = 50;              // 增加存在时间，让抛物线更完整（原 30）
            Projectile.extraUpdates = 1;            // 增加一次额外更新，使运动更平滑
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            // ----- 重力影响 -----
            // 对 Y 轴速度施加恒定向下的重力
            Projectile.velocity.Y += Gravity;

            // ----- 旋转效果 -----
            // 根据速度方向设置旋转角度（使匕首尖端指向前进方向）
            // Atan2 返回的是弧度，直接赋给 rotation
            if (Projectile.velocity != Vector2.Zero)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                // 可选：为视觉效果添加一点偏移，比如 + MathHelper.PiOver2 让刀片垂直于运动方向
                Projectile.rotation += MathHelper.PiOver2;
            }

            // ----- 粒子效果（可选）-----
            if (Main.rand.NextBool(3))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.GemTopaz, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 命中时添加感染效果（可选）
            // 例如有概率给目标附加感染 debuff
        }
    }
}