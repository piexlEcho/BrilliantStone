using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using BrilliantStone.Content.Players;

namespace BrilliantStone.Content.Projectiles
{
    public class QueensDecree : ModProjectile
    {
        private const float MaxHomingDistance = 500f;   // 最大追踪距离
        private const float HomingStrength = 0.05f;      // 追踪强度（已提高至0.2，转弯更灵活）

        // 弹幕基础速度（可在此调整）
        private float projectileSpeed = 6f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;                   // 单帧动画，如需多帧请修改
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = -1;
            Projectile.friendly = false;
            Projectile.hostile = true; // 伤害玩家
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1; // 只能穿透一个目标
            Projectile.timeLeft = 150;                    // 2.5秒
            Projectile.alpha = 0;
            Projectile.light = 0.5f; // 发光效果

            // 如需改变速度，直接修改此值
            projectileSpeed = 4.25f;
        }

        public override void AI()
        {
            // 生命周期结束前的视觉效果（最后30帧时闪烁）
            if (Projectile.timeLeft < 30 && Projectile.timeLeft % 10 < 5)
            {
                Projectile.alpha = 200;  // 半透明闪烁
            }
            else
            {
                Projectile.alpha = 0;    // 正常
            }

            // 弱追踪玩家
            Player target = FindClosestPlayer();
            if (target != null)
            {
                Vector2 toTarget = target.Center - Projectile.Center;
                float distance = toTarget.Length();
                if (distance < MaxHomingDistance)
                {
                    toTarget.Normalize();
                    // 使用可调的速度值
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, toTarget * projectileSpeed, HomingStrength);
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        private Player FindClosestPlayer()
        {
            Player closest = null;
            float closestDist = MaxHomingDistance;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    float dist = Vector2.Distance(Projectile.Center, player.Center);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = player;
                    }
                }
            }
            return closest;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            // 给玩家添加感染效果
            target.GetModPlayer<BrilliantPlayer>().AddInfectionStack(240); // 4秒

            // 播放击中音效
            if (Main.netMode != NetmodeID.Server)
                SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            // 只在客户端播放音效和粒子
            if (Main.netMode != NetmodeID.Server)
            {
                // 播放爆炸声
                SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

                // 产生火焰/烟雾粒子
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, 0f, 0, default, 1f);
                }
            }
        }
    }
}