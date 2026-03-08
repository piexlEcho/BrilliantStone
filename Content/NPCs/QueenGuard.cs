using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using BrilliantStone.Content.Buffs;
using BrilliantStone.Content.Players;
using System;

namespace BrilliantStone.Content.NPCs
{
    public class QueenGuard : ModNPC
    {
        // AI 参数
        private float maxSpeed = 7f;           // 最大移动速度
        private float turnSpeed = 0.04f;        // 转向速度（0-1，越大转向越快）
        // 无目标时的随机移动参数
        private ref float RandomTimer => ref NPC.ai[0];
        private ref float RandomAngle => ref NPC.ai[1];

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 5; // 蝙蝠有5帧动画
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 28;
            NPC.height = 24;
            NPC.damage = 10;
            NPC.defense = 1;
            NPC.lifeMax = 180;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath4;
            NPC.value = 100f;
            NPC.knockBackResist = 0.3f;
            NPC.aiStyle = -1;                     // 禁用原版AI，使用自定义
            NPC.noGravity = true;
            NPC.noTileCollide = true;              // 穿墙
            NPC.npcSlots = 0.5f;
        }

        public override void AI()
        {
            // 寻找最近的目标（玩家）
            Player target = null;
            float targetDistSq = float.MaxValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    float distSq = Vector2.DistanceSquared(NPC.Center, player.Center);
                    if (distSq < targetDistSq)
                    {
                        targetDistSq = distSq;
                        target = player;
                    }
                }
            }

            Vector2 desiredVelocity = Vector2.Zero;

            if (target != null)
            {
                // 有目标：计算朝向目标的单位向量，并乘以最大速度
                Vector2 toTarget = target.Center - NPC.Center;
                if (toTarget != Vector2.Zero)
                {
                    toTarget.Normalize();
                    desiredVelocity = toTarget * maxSpeed;
                }
            }
            else
            {
                // 无目标：随机飘荡
                if (RandomTimer <= 0)
                {
                    RandomTimer = Main.rand.Next(60, 180); // 随机移动持续时间
                    RandomAngle = Main.rand.NextFloat(-MathHelper.Pi, MathHelper.Pi); // 随机方向
                }
                RandomTimer--;

                float angle = RandomAngle;
                desiredVelocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * maxSpeed * 0.5f;
            }

            // 平滑转向：将当前速度向 desiredVelocity 插值
            // 公式：新速度 = 当前速度 + (期望速度 - 当前速度) * 转向速度
            Vector2 newVelocity = NPC.velocity + (desiredVelocity - NPC.velocity) * turnSpeed;

            // 限制速度不超过最大速度
            if (newVelocity.Length() > maxSpeed)
                newVelocity = Vector2.Normalize(newVelocity) * maxSpeed;

            NPC.velocity = newVelocity;

            // 动画控制（使用帧高度）
            int frameHeight = NPC.frame.Height; // 单帧高度
            NPC.frameCounter++;
            if (NPC.frameCounter >= 6)
            {
                NPC.frameCounter = 0;
                int currentFrame = NPC.frame.Y / frameHeight;
                currentFrame++;
                if (currentFrame >= Main.npcFrameCount[Type])
                    currentFrame = 0;
                NPC.frame.Y = currentFrame * frameHeight;
            }

            // 根据水平速度方向设置 sprite 方向
            if (NPC.velocity.X != 0)
                NPC.spriteDirection = NPC.velocity.X > 0 ? 1 : -1;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            // 叠加辉石感染（120帧 = 2秒）
            target.GetModPlayer<BrilliantPlayer>().AddInfectionStack(300);

            // 40%概率施加二形感染，持续300帧（5秒）
            if (Main.rand.NextFloat() <= 0.4f)
            {
                target.AddBuff(ModContent.BuffType<BrilliantStun>(), 300);
            }
        }

        public override void OnKill()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, 0f, 0f);
                }
            }
        }
    }
}