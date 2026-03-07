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
        // 攻击冷却计时器（使用 localAI[0]）
        private ref float AttackCooldown => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5; // 蝙蝠有5帧动画
            ProjectileID.Sets.MinionTargettingFeature[Type] = true; // 允许鞭子标记目标
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 24;
            Projectile.tileCollide = false;     // 不碰撞物块
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;      // 网络同步重要
            Projectile.friendly = true;          // 对敌人造成伤害
            Projectile.hostile = false;
            Projectile.minion = true;            // 标记为随从
            Projectile.minionSlots = 1f;          // 占用1个召唤栏
            Projectile.penetrate = -1;            // 无限穿透
            Projectile.timeLeft = 36000;          // 存在时间（足够长，由主人管理）
            Projectile.aiStyle = -1;               // 不使用原版AI
            Projectile.usesLocalNPCImmunity = true; // 使用本地无敌帧，防止多段命中
            Projectile.localNPCHitCooldown = 20;   // 对同一NPC的伤害间隔（帧）
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.active = false;
                return;
            }

            // 确保buff存在
            if (player.HasBuff(ModContent.BuffType<QueenGuardBuff>()))
            {
                Projectile.timeLeft = 2;
            }

            // ----- 目标选择（优先鞭子标记，其次自动索敌）-----
            float maxDetectDistance = 800f;       // 自动索敌范围
            float returnDistance = 1500f;          // 玩家与目标距离超过此值则强制返回
            NPC target = null;

            // 先检查鞭子标记的目标（原版自动赋值到 OwnerMinionAttackTargetNPC）
            NPC markedTarget = Projectile.OwnerMinionAttackTargetNPC;
            if (markedTarget != null && markedTarget.active && !markedTarget.friendly && markedTarget.CanBeChasedBy())
            {
                // 检查标记目标是否在射程内（也可以不考虑范围，直接追击）
                target = markedTarget;
            }
            else
            {
                // 自动搜索最近敌人
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

            // 如果存在目标，检查玩家与目标的距离是否过大
            if (target != null)
            {
                float playerToTargetDist = Vector2.Distance(player.Center, target.Center);
                if (playerToTargetDist > returnDistance)
                {
                    target = null; // 距离过远，强制丢失仇恨
                }
            }

            // ----- 移动逻辑 -----
            Vector2 moveDirection = Vector2.Zero;
            float speed = 0f;
            float inertia = 20f; // 惯性系数，越大转向越慢

            if (target != null)
            {
                // 有目标：直接飞向目标
                moveDirection = target.Center - Projectile.Center;
                moveDirection.Normalize();
                speed = 10f; // 追击速度
            }
            else
            {
                // 无目标：围绕玩家飞行（画圆）
                // 计算理想位置：以玩家为中心，半径 80 像素的圆上，随时间移动
                float radius = 80f;
                float angle = Projectile.ai[0]; // 使用 ai[0] 存储当前角度
                // 每帧增加角度，产生绕圈效果
                angle += 0.05f;
                if (angle > MathHelper.TwoPi) angle -= MathHelper.TwoPi;
                Projectile.ai[0] = angle;

                Vector2 desiredPos = player.Center + new Vector2((float)System.Math.Cos(angle) * radius, (float)System.Math.Sin(angle) * radius * 0.5f); // 垂直压缩使圈更扁，更像飞行
                moveDirection = desiredPos - Projectile.Center;
                if (moveDirection.Length() > 10f) // 距离较远时直接飞过去
                {
                    moveDirection.Normalize();
                    speed = 8f;
                }
                else
                {
                    // 距离很近时减速，避免抖动
                    moveDirection = Vector2.Zero;
                    Projectile.velocity *= 0.9f;
                }
            }

            // 应用移动
            if (moveDirection != Vector2.Zero)
            {
                // 平滑转向
                Projectile.velocity = (Projectile.velocity * (inertia - 1) + moveDirection * speed) / inertia;
            }

            // 限制最大速度
            float maxSpeed = 12f;
            if (Projectile.velocity.Length() > maxSpeed)
            {
                Projectile.velocity = Vector2.Normalize(Projectile.velocity) * maxSpeed;
            }

            // ----- 动画和旋转 -----
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                    Projectile.frame = 0;
            }

            // 根据速度方向旋转（模仿蝙蝠）
            if (Projectile.velocity != Vector2.Zero)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public override bool MinionContactDamage()
        {
            return true; // 允许接触伤害
        }
    }
}