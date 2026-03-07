using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using BrilliantStone.Content.Buffs;
using BrilliantStone.Content.Players;

namespace BrilliantStone.Content.Projectiles.Minion
{
    public class QueenGuardMinion : ModProjectile
    {
        // 武器传入的基础伤害，存储在 ai[1]
        private float BaseDamage => Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 24;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.minion = true;
            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 36000;
            Projectile.aiStyle = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.active = false;
                return;
            }

            // 确保召唤 buff 存在
            if (player.HasBuff(ModContent.BuffType<QueenGuardBuff>()))
                Projectile.timeLeft = 2;

            // ----- 获取感染层数，计算强化数值（服务器和客户端同步执行）-----
            int infectionStacks = 0;
            if (player.HasBuff(ModContent.BuffType<BrilliantInfection>()))
                infectionStacks = player.GetModPlayer<BrilliantPlayer>().infectionStacks;
            infectionStacks = (int)MathHelper.Clamp(infectionStacks, 0, 5); // 最多5层

            // 伤害加成（每层 +2，上限 +10）
            int bonusDamage = infectionStacks * 2;
            if (bonusDamage > 10) bonusDamage = 10;
            Projectile.damage = (int)BaseDamage + bonusDamage;

            // 攻击间隔缩短（每层 -2 帧，下限 5）
            int baseCooldown = 20;
            int minCooldown = 5;
            int newCooldown = baseCooldown - infectionStacks * 2;
            if (newCooldown < minCooldown) newCooldown = minCooldown;
            Projectile.localNPCHitCooldown = newCooldown;

            // 移动速度加成（每层 +10%，上限 +50%）
            float speedMultiplier = 1f + infectionStacks * 0.1f;
            if (speedMultiplier > 1.5f) speedMultiplier = 1.5f;

            // ----- 目标选择 -----
            float maxDetectDistance = 800f;
            float returnDistance = 1500f;
            NPC target = null;

            NPC markedTarget = Projectile.OwnerMinionAttackTargetNPC;
            if (markedTarget != null && markedTarget.active && !markedTarget.friendly && markedTarget.CanBeChasedBy())
                target = markedTarget;
            else
            {
                float sqrMaxDist = maxDetectDistance * maxDetectDistance;
                foreach (NPC npc in Main.npc)
                {
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
            }

            if (target != null)
            {
                float playerToTargetDist = Vector2.Distance(player.Center, target.Center);
                if (playerToTargetDist > returnDistance)
                    target = null;
            }

            // ----- 移动逻辑 -----
            Vector2 moveDirection = Vector2.Zero;
            float speed = 0f;
            float inertia = 20f;

            if (target != null)
            {
                moveDirection = target.Center - Projectile.Center;
                moveDirection.Normalize();
                speed = 10f * speedMultiplier;
            }
            else
            {
                // 围绕玩家飞行
                float radius = 80f;
                float angle = Projectile.ai[0];
                angle += 0.05f;
                if (angle > MathHelper.TwoPi) angle -= MathHelper.TwoPi;
                Projectile.ai[0] = angle;

                Vector2 desiredPos = player.Center + new Vector2((float)System.Math.Cos(angle) * radius, (float)System.Math.Sin(angle) * radius * 0.5f);
                moveDirection = desiredPos - Projectile.Center;
                if (moveDirection.Length() > 10f)
                {
                    moveDirection.Normalize();
                    speed = 8f * speedMultiplier;
                }
                else
                {
                    moveDirection = Vector2.Zero;
                    Projectile.velocity *= 0.9f;
                }
            }

            if (moveDirection != Vector2.Zero)
            {
                Projectile.velocity = (Projectile.velocity * (inertia - 1) + moveDirection * speed) / inertia;
            }

            float maxSpeed = 12f * speedMultiplier;
            if (Projectile.velocity.Length() > maxSpeed)
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * maxSpeed;

            // ----- 动画和旋转 -----
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                    Projectile.frame = 0;
            }

            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 可根据需要添加命中效果，例如施加感染，但原版 QueenGuard 已处理，这里留空
        }

        public override bool MinionContactDamage() => true;
    }
}