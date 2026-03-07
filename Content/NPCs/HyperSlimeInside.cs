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
    public class HyperSlimeInside : ModNPC
    {
        // 可调参数
        protected int explosionDamage = 20;
        protected int explosionRadius = 100;  // 伤害范围
        protected float explodeRange = 80f;    // 自爆触发距离

        // 使用NPC.ai数组存储自定义状态，标记是否已爆炸
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

            NPC.aiStyle = -1;                         // 使用自定义AI
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.npcSlots = 0.5f;

            // 初始化史莱姆AI参数
            aiParams = new SlimeAIParameters
            {
                useAdvancedPathfinding = true,
                immuneToLava = false,
                canSwim = true,
                jumpSpeed = 4.0f,
                jumpChaseSpeed = 5.5f,
                jumpCooldownMin = 3,
                jumpCooldownMax = 8,
                moveSpeed = 4f,
                splitOnDeath = false,                 // 内层史莱姆不再分裂
                respectRoyalGel = true,                // 实际由Buff控制
                maxJumpHeight = 5f,
                aggroRange = 800f,
                chaseRange = 1000f,
                canJumpOverGaps = true,
                idleJumpChance = 0.5f,
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
            // 调用史莱姆基础AI（移动、跳跃、目标选择等）
            SlimeAI.UpdateAI(NPC, aiParams);

            // ---------- 自爆检测（仅服务器执行）----------
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return; // 客户端不处理自爆逻辑

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
                return;

            // 玩家拥有感染Buff则不会触发自爆
            if (target.HasBuff(ModContent.BuffType<BrilliantInfection>()))
                return;

            float distance = Vector2.Distance(NPC.Center, target.Center);
            if (distance < explodeRange && HasExploded == 0f)
            {
                // 触发爆炸效果，然后让NPC死亡
                DoExplode();
                NPC.life = 0; // 生命归零，游戏将自动调用CheckDead并同步死亡
            }
        }

        /// <summary>
        /// 执行爆炸效果（伤害、分裂、掉落、特效）。此方法应保证只执行一次，并处理好网络同步。
        /// </summary>
        private void DoExplode()
        {
            if (HasExploded == 1f) return;
            HasExploded = 1f;

            // ---------- 服务器执行伤害与生成 ----------
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
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

                // 分裂与掉落（内部已检查网络模式）
                OnDeathEffects();
            }

            // ---------- 特效播放（客户端与单机）----------
            if (Main.netMode != NetmodeID.Server) // 纯服务器不播放特效
            {
                SoundEngine.PlaySound(SoundID.Item14, NPC.Center);
                for (int i = 0; i < 20; i++)
                    Dust.NewDust(NPC.Center + Main.rand.NextVector2Circular(80, 80), 10, 10, DustID.Smoke, Scale: 1.5f);
                for (int i = 0; i < 10; i++)
                    Dust.NewDust(NPC.Center + Main.rand.NextVector2Circular(80, 80), 10, 10, DustID.Torch, Scale: 1.2f);
            }
        }

        /// <summary>
        /// 当NPC被玩家击杀时触发，此处统一调用爆炸效果（避免重复爆炸）。
        /// </summary>
        public override bool CheckDead()
        {
            // 如果尚未爆炸，则触发爆炸效果（例如被玩家击杀时）
            if (HasExploded == 0f)
            {
                DoExplode();
            }
            // 返回true让游戏自然处理NPC死亡（不再手动杀死）
            return true;
        }

        /// <summary>
        /// 死亡时的额外效果（掉落等）。仅服务器执行。
        /// </summary>
        public virtual void OnDeathEffects()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // 10% 概率放置纯净辉石物块
            if (Main.rand.NextFloat() <= 0.1f)
            {
                // 找到正下方的地面（第一个实心物块）
                Point origin = (NPC.Bottom / 16).ToPoint();
                int groundY = origin.Y;
                bool foundGround = false;

                while (groundY < Main.maxTilesY - 10)
                {
                    Tile tile = Main.tile[origin.X, groundY];
                    if (tile.HasTile && Main.tileSolid[tile.TileType])
                    {
                        foundGround = true;
                        break;
                    }
                    groundY++;
                }

                if (foundGround)
                {
                    int placeX = origin.X;
                    int placeY = groundY - 1; // 地面块的上方一格

                    if (WorldGen.InWorld(placeX, placeY, 5))
                    {
                        Tile targetTile = Main.tile[placeX, placeY];
                        if (!targetTile.HasTile || !Main.tileSolid[targetTile.TileType] || TileID.Sets.CanBeClearedDuringGeneration[targetTile.TileType])
                        {
                            WorldGen.PlaceTile(placeX, placeY, ModContent.TileType<Tiles.PureBrilliantStoneTile>(), forced: true);
                        }
                    }
                }
            }

            // 掉落辉石
            int itemType = ModContent.ItemType<Items.BrilliantStone>();
            int amount = Main.rand.Next(1, 4);
            Item.NewItem(NPC.GetSource_Death(), NPC.getRect(), itemType, amount);
        }

        internal static void ApplyInfection(Player target, int duration)
        {
            target.GetModPlayer<BrilliantPlayer>().AddInfectionStack(duration);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.InModBiome<Biomes.BrilliantBiome>())
                return 0.1f;
            return 0f;
        }
    }
}