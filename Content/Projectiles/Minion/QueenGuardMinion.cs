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
            ProjectileID.Sets.MinionTargettingFeature[Type] = true; // 允许右键点击切换目标
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

            // 寻找目标（优先选择玩家右键点击的目标）
            NPC target = Projectile.OwnerMinionAttackTargetNPC;
            if (target == null || !target.active || target.friendly || target.dontTakeDamage)
            {
                // 自动寻找最近敌人
                float maxDist = 800f;
                target = null;
                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && npc.CanBeChasedBy() && Vector2.Distance(Projectile.Center, npc.Center) < maxDist)
                    {
                        maxDist = Vector2.Distance(Projectile.Center, npc.Center);
                        target = npc;
                    }
                }
            }

            // ----- 移动逻辑（模仿蝙蝠AI）-----
            float speed = 8f;
            float inertia = 20f;
            Vector2 moveDirection = Vector2.Zero;

            if (target != null)
            {
                // 有目标时向目标移动
                moveDirection = target.Center - Projectile.Center;
                moveDirection.Normalize();
                speed = 10f; // 追击速度更快
            }
            else
            {
                // 无目标时随机移动（类似蝙蝠）
                if (Projectile.ai[1] <= 0f)
                {
                    Projectile.ai[1] = Main.rand.Next(60, 180); // 随机停留时间
                    Projectile.ai[0] = Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi); // 随机方向
                }
                Projectile.ai[1]--;

                moveDirection = new Vector2((float)System.Math.Cos(Projectile.ai[0]), (float)System.Math.Sin(Projectile.ai[0]));
                speed = 6f;
            }

            // 应用移动
            if (moveDirection != Vector2.Zero)
            {
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

            // ----- 攻击冷却更新（自动攻击由碰撞触发）-----
            // 攻击间隔已在 SetDefaults 中用 localNPCHitCooldown 控制
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 命中时给玩家施加感染（同QueenGuard逻辑）
            Player player = Main.player[Projectile.owner];
            if (player.whoAmI == Main.myPlayer && player.active)
            {
                player.GetModPlayer<BrilliantPlayer>().AddInfectionStack(120); // 2秒感染
            }

            // 60%概率给敌人施加震慑效果（5秒）
            if (Main.rand.NextFloat() <= 0.6f)
            {
                target.AddBuff(ModContent.BuffType<BrilliantStun>(), 300);
            }
        }

        public override bool MinionContactDamage()
        {
            return true; // 允许接触伤害
        }
    }
}