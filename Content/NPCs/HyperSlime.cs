using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.DataStructures;
using BrilliantStone.Content.Players;
using BrilliantStone.Content.NPCs;  // 确保 SlimeAI 所在的命名空间
using BrilliantStone.Content.Buffs;  // 引入Buff命名空间以检测Buff

namespace BrilliantStone.Content.NPCs
{
    public class HyperSlime : ModNPC
    {
        // 可调参数
        protected int explosionDamage = 20;
        protected int explosionRadius = 100;  // 伤害范围
        protected float explodeRange = 80f;         // 自爆触发距离

        // 使用NPC.ai数组存储自定义状态
        private ref float HasExploded => ref NPC.ai[3];

        // 史莱姆AI参数实例
        private SlimeAIParameters aiParams;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 2;
            NPCID.Sets.SpawnsWithCustomName[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 32;
            NPC.height = 32;
            NPC.damage = 0;                          // 不自爆时无接触伤害
            NPC.defense = 5;
            NPC.lifeMax = 150;
            NPC.value = 100f;
            NPC.knockBackResist = 0.8f;

            // 不使用原版史莱姆AI，改用自定义AI
            NPC.aiStyle = -1;
            // AIType 不需要设置

            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.npcSlots = 0.5f;

            // 初始化史莱姆AI参数
            aiParams = new SlimeAIParameters
            {
                // 高级寻路
                useAdvancedPathfinding = true,

                // 不免疫岩浆
                immuneToLava = false,

                // 能游泳（水中上浮）
                canSwim = true,

                // 跳跃高度低
                jumpSpeed = 3.5f,
                jumpChaseSpeed = 4.0f,

                // 跳跃频率极高（冷却时间极短）
                jumpCooldownMin = 2,
                jumpCooldownMax = 7,

                // 水平速度中等（快速的矮距离蹦跶）
                moveSpeed = 4f,

                // 死亡分裂成蓝史莱姆
                splitOnDeath = true,
                splitCount = 1,
                splitType = ModContent.NPCType<HyperSlimeInside>(),
                splitChildLife = 10, // 分裂出的史莱姆生命值

                // 尊重自定义友好Buff（BrilliantInfection）
                respectRoyalGel = true,   // 实际逻辑改为检测Buff

                // 其他参数可按需调整
                maxJumpHeight = 4f,        // 最大跳跃高度6格（配合低跳）
                aggroRange = 600f, // 仇恨
                chaseRange = 800f, // 追击范围
                canJumpOverGaps = true, // 允许跳过小缝隙
                idleJumpChance = 0.7f,      // 空闲时偶尔蹦跶

                tiltFactor = 0.05f, // 轻微倾斜

                idleJumpHorizontalSpeed = 1.0f
            };
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;
            if (NPC.frameCounter < 8)
                NPC.frame.Y = 0 * frameHeight;
            else if (NPC.frameCounter < 16)
                NPC.frame.Y = 1 * frameHeight;
            else
                NPC.frameCounter = 0;
        }

        public override void AI()
        {
            // 先调用自定义史莱姆AI，处理移动、跳跃、目标选择等
            SlimeAI.UpdateAI(NPC, aiParams);

            // 获取当前目标（SlimeAI已经更新了NPC.target）
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
                return;

            // 拥有友好Buff（BrilliantInfection），则不会主动爆炸
            if (target.HasBuff(ModContent.BuffType<BrilliantInfection>()))
                return;

            float distance = Vector2.Distance(NPC.Center, target.Center);
            if (distance < explodeRange)
                Explode();
        }

        private void Explode()
        {
            if (HasExploded == 1f) return;
            HasExploded = 1f;

            // 伤害玩家
            foreach (Player player in Main.player)
            {
                if (player.active && !player.dead && Vector2.Distance(NPC.Center, player.Center) < explosionRadius)
                {
                    PlayerDeathReason deathReason = PlayerDeathReason.ByNPC(NPC.whoAmI);
                    int hitDirection = player.Center.X > NPC.Center.X ? 2 : -2;
                    player.Hurt(deathReason, explosionDamage, hitDirection);
                    ApplyInfection(player, 300);
                }
            }

            // 伤害其他敌对NPC
            foreach (NPC target in Main.npc)
            {
                if (target.active && !target.friendly && target.life > 0 &&
                    target.whoAmI != NPC.whoAmI &&
                    Vector2.Distance(NPC.Center, target.Center) < explosionRadius)
                {
                    NPC.HitInfo hitInfo = new NPC.HitInfo
                    {
                        Damage = explosionDamage,
                        Knockback = 0f,
                        HitDirection = target.Center.X > NPC.Center.X ? 2 : -2
                    };
                    target.StrikeNPC(hitInfo);
                }
            }

            // 特效
            SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 20; i++)
                    Dust.NewDust(NPC.Center + Main.rand.NextVector2Circular(80, 80), 10, 10, DustID.Smoke, Scale: 1.5f);
                for (int i = 0; i < 10; i++)
                    Dust.NewDust(NPC.Center + Main.rand.NextVector2Circular(80, 80), 10, 10, DustID.Torch, Scale: 1.2f);
            }

            splitOnDeath();
            OnDeathEffects();

            // 杀死自己
            NPC.life = 0;
            NPC.HitEffect();  // 触发死亡效果
            NPC.active = false;
        }

        public void splitOnDeath()
        {
            if (aiParams.splitOnDeath)
            {
                SlimeAI.DoSplit(NPC, aiParams);
            }
        }

        internal static void ApplyInfection(Player target, int duration)
        {
            target.GetModPlayer<BrilliantPlayer>().AddInfectionStack(duration);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.InModBiome<Biomes.BrilliantBiome>())
            {
                return 0.8f;
            }
            return 0f;
        }

        public override bool CheckDead()
        {
            Explode();
            return false;
        }

        public virtual void OnDeathEffects()
        {
            if (Main.rand.NextFloat() <= 0.5f)
            {
                int itemType = ModContent.ItemType<Items.BrilliantCarapace>();
                int amount = Main.rand.Next(1, 2);
                Item.NewItem(NPC.GetSource_Death(), NPC.getRect(), itemType, amount);
            }
        }
    }
}