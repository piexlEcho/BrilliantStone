using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Projectiles
{
    public class BrilliantDaggerProj2 : ModProjectile
    {
        private const float Gravity = 0.06f;          // 重力加速度
        private const float HomingStrength = 0.05f;   // 追踪强度（每帧转向比例）
        private const float MaxTurnAngle = 0.1f;       // 每帧最大转向角（弧度）

        public override void SetStaticDefaults()
        {
            // 可选设置
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;            // 无限穿透
            Projectile.tileCollide = true;         // 碰撞物块
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;        // 近战命中检查
            Projectile.timeLeft = 120;               // 存在时间（帧）
            Projectile.extraUpdates = 1;             // 平滑运动
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            // ----- 追踪逻辑（仅在前中期有效）-----
            if (Projectile.timeLeft > 30) // 最后10帧停止追踪，避免乱飞
            {
                float maxDetectDistance = 400f; // 检测范围
                NPC target = null;
                float sqrMaxDist = maxDetectDistance * maxDetectDistance;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && !npc.friendly && npc.CanBeChasedBy())
                    {
                        float sqrDist = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                        if (sqrDist < sqrMaxDist)
                        {
                            sqrMaxDist = sqrDist;
                            target = npc;
                        }
                    }
                }

                if (target != null)
                {
                    Vector2 directionToTarget = target.Center - Projectile.Center;
                    directionToTarget.Normalize();

                    if (Projectile.velocity.LengthSquared() > 0.01f)
                    {
                        // 当前速度方向角度
                        float currentAngle = Projectile.velocity.ToRotation();
                        float targetAngle = directionToTarget.ToRotation();
                        float angleDiff = MathHelper.WrapAngle(targetAngle - currentAngle);

                        // 限制最大转向幅度
                        angleDiff = MathHelper.Clamp(angleDiff, -MaxTurnAngle, MaxTurnAngle);

                        // 应用转向（HomingStrength 控制灵敏度）
                        float newAngle = currentAngle + angleDiff * HomingStrength;
                        Vector2 newDirection = new Vector2((float)System.Math.Cos(newAngle), (float)System.Math.Sin(newAngle));
                        float speed = Projectile.velocity.Length();
                        Projectile.velocity = newDirection * speed;
                    }
                }
            }

            // ----- 重力影响 -----
            Projectile.velocity.Y += Gravity;

            // ----- 旋转效果（刀片指向运动方向，加 Pi/2 使刀面垂直于运动）-----
            if (Projectile.velocity != Vector2.Zero)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }

            // ----- 增强粒子效果 -----
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    DustID.GemTopaz, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f);
                dust.noGravity = true;
                dust.scale = 1.2f;
            }
            if (Main.rand.NextBool(5))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.GoldFlame, Projectile.velocity.X * 0.1f, Projectile.velocity.Y * 0.1f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
        }
    }
}