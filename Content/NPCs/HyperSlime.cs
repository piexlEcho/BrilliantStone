using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.DataStructures;
using BrilliantStone.Content.Players;
using BrilliantStone.Content.NPCs;
using BrilliantStone.Content.Buffs;

namespace BrilliantStone.Content.NPCs
{
    public class HyperSlime : ModNPC
    {
        // 可调参数
        protected int explosionDamage = 20;
        protected int explosionRadius = 100;
        protected float explodeRange = 80f;

        private ref float HasExploded => ref NPC.ai[3];
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
            NPC.damage = 0;
            NPC.defense = 5;
            NPC.lifeMax = 150;
            NPC.value = 100f;
            NPC.knockBackResist = 0.8f;

            NPC.aiStyle = -1;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.npcSlots = 0.5f;

            aiParams = new SlimeAIParameters
            {
                useAdvancedPathfinding = true,
                immuneToLava = false,
                canSwim = true,
                jumpSpeed = 3.5f,
                jumpChaseSpeed = 4.0f,
                jumpCooldownMin = 2,
                jumpCooldownMax = 7,
                moveSpeed = 4f,
                splitOnDeath = true,                     // 死亡时分裂
                splitCount = 1,
                splitType = ModContent.NPCType<HyperSlimeInside>(),
                splitChildLife = 10,
                respectRoyalGel = true,
                maxJumpHeight = 4f,
                aggroRange = 600f,
                chaseRange = 800f,
                canJumpOverGaps = true,
                idleJumpChance = 0.7f,
                tiltFactor = 0.05f,
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
            SlimeAI.UpdateAI(NPC, aiParams);

            // 自爆检测（仅服务器）
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
                return;

            if (target.HasBuff(ModContent.BuffType<BrilliantInfection>()))
                return;

            float distance = Vector2.Distance(NPC.Center, target.Center);
            if (distance < explodeRange && HasExploded == 0f)
            {
                DoExplode();
                NPC.life = 0;
            }
        }

        private void DoExplode()
        {
            if (HasExploded == 1f) return;
            HasExploded = 1f;

            // 服务器执行伤害与生成
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
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

                // 死亡分裂与掉落
                splitOnDeath();
                OnDeathEffects();
            }

            // 特效播放（客户端与单机）
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
                for (int i = 0; i < 20; i++)
                    Dust.NewDust(NPC.Center + Main.rand.NextVector2Circular(80, 80), 10, 10, DustID.Smoke, Scale: 1.5f);
                for (int i = 0; i < 10; i++)
                    Dust.NewDust(NPC.Center + Main.rand.NextVector2Circular(80, 80), 10, 10, DustID.Torch, Scale: 1.2f);
            }
        }

        public override bool CheckDead()
        {
            if (HasExploded == 0f)
            {
                DoExplode();
            }
            return true;
        }

        public void splitOnDeath()
        {
            if (aiParams.splitOnDeath && Main.netMode != NetmodeID.MultiplayerClient)
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
                return 0.8f;
            return 0f;
        }

        public virtual void OnDeathEffects()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (Main.rand.NextFloat() <= 0.5f)
            {
                int itemType = ModContent.ItemType<Items.BrilliantCarapace>();
                int amount = Main.rand.Next(1, 2);
                Item.NewItem(NPC.GetSource_Death(), NPC.getRect(), itemType, amount);
            }
        }
    }
}