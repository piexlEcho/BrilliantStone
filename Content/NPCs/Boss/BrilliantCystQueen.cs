using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.DataStructures;
using BrilliantStone.Content.Buffs;
using BrilliantStone.Content.Items;
using BrilliantStone.Content.Projectiles;
using BrilliantStone.Content.Biomes;
using BrilliantStone.Content.NPCs;

namespace BrilliantStone.Content.NPCs.Boss
{
    [AutoloadBossHead]
    public class BrilliantCystQueen : ModNPC
    {
        // ----- 狂暴数值乘数（可调） -----
        private const float Phase2DamageMult = 1.5f;
        private const float EnragedDamageMult = 3.5f;
        private const float Phase2SpeedMult = 1.3f;
        private const float EnragedSpeedMult = 2f;
        private const int Phase2Defense = 5;
        private const int EnragedDefense = -5;
        private const float Phase2ProjectileDamageMult = 1.3f;
        private const float EnragedProjectileDamageMult = 1.5f;
        private const int Phase2ProjectileCountBonus = 1;
        private const int EnragedProjectileCountBonus = 5;

        // ----- 状态标志 -----
        private bool enraged = false;
        private bool phase2 = false;
        private int originalDamage;
        private int originalDefense;
        private float currentDamageMultiplier = 1f;

        // ----- 阶段定义 -----
        private enum BossPhase
        {
            Hover,   // 核心移动状态，穿插在所有攻击阶段之间
            Shoot,   // 射击阶段
            Spawn,   // 召唤阶段
            Dash     // 冲刺阶段
        }
        private BossPhase currentPhase = BossPhase.Hover;
        private int phaseTimer = 0;

        // 阶段持续时间（帧）
        private const int HoverDuration = 60;       // 悬浮过渡时间
        private const int ShootDuration = 240;       // 射击阶段持续4秒
        private const int SpawnDuration = 300;       // 召唤阶段持续5秒
        private const int DashDuration = 160;        // 冲刺阶段总时长（含预备和多次冲刺）

        // 攻击阶段顺序循环（Hover后依次执行Shoot、Spawn、Dash，然后回到Hover）
        private int attackIndex = 0;                  // 0=Shoot, 1=Spawn, 2=Dash

        // 阶段持续时间受狂暴影响
        private int GetPhaseDuration(BossPhase phase)
        {
            int duration = phase switch
            {
                BossPhase.Hover => HoverDuration,
                BossPhase.Shoot => ShootDuration,
                BossPhase.Spawn => SpawnDuration,
                BossPhase.Dash => DashDuration,
                _ => 60
            };
            if (phase2) duration = (int)(duration * 0.7f);
            if (enraged) duration = (int)(duration * 0.6f);
            return Math.Max(duration, 20);
        }

        // ----- Dash 子状态 -----
        private enum DashSubState
        {
            Prepare,   // 预备阶段（30帧）
            Dashing,   // 冲刺中
            Pause      // 冲刺间停顿
        }
        private DashSubState dashSubState = DashSubState.Prepare;
        private int dashSubTimer = 0;
        private int dashCount = 0;                    // 已完成冲刺次数
        private const int PrepareTime = 30;            // 预备帧数
        private const int DashTime = 20;                // 每次冲刺持续时间
        private const int PauseTime = 40;               // 冲刺间停顿
        private const int MaxDashes = 3;                // 最大冲刺次数

        // ----- 攻击阶段内计时器（用于Shoot/Spawn的周期动作）-----
        private int actionTimer = 0;
        private const int ShootInterval = 40;           // 射击间隔40帧
        private const int SpawnInterval = 60;           // 召唤间隔60帧

        // ----- 离场计时器 -----
        private int leaveTimer = 0;
        private const int LeaveDelay = 180;
        private const float MaxPlayerDistance = 2000f;

        // ----- 动画状态 -----
        private int attackAnimationTimer = 0;
        private const int AttackAnimationDuration = 20;

        // ----- 小怪数量上限（动态）-----
        private int GetCurrentGuardLimit()
        {
            if (phase2 && enraged) return 15;
            if (phase2) return 13;
            if (enraged) return 15;
            return 9;
        }

        private int GetCurrentWormLimit()
        {
            if (phase2 && enraged) return 4;
            if (phase2) return 2;
            if (enraged) return 4;
            return 0;
        }

        private int CountNPCs(int type)
        {
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
                if (Main.npc[i].active && Main.npc[i].type == type)
                    count++;
            return count;
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 12;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
        }

        public override void SetDefaults()
        {
            NPC.width = 86;
            NPC.height = 76;
            NPC.damage = 15;                           // 降低本体伤害
            NPC.defense = 10;
            NPC.lifeMax = 4000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = Item.buyPrice(0, 5, 0, 0);
            NPC.boss = true;
            NPC.npcSlots = 10f;

            NPC.aiStyle = -1;
            originalDamage = NPC.damage;
            originalDefense = NPC.defense;
        }

        public override void AI()
        {
            Player player = Main.player[NPC.target];
            if (!player.active || player.dead)
            {
                NPC.TargetClosest(false);
                player = Main.player[NPC.target];
                if (!player.active || player.dead)
                {
                    HandleLeaveCondition();
                    return;
                }
            }

            if (AllPlayersDeadOrFar())
            {
                HandleLeaveCondition();
                return;
            }
            else
            {
                leaveTimer = 0;
            }

            // 状态激活检测
            if (!phase2 && NPC.life <= NPC.lifeMax / 2)
                EnterPhase2();

            bool inBiome = player.InModBiome<BrilliantBiome>();
            if (!inBiome && !enraged)
                EnterEnraged();
            else if (inBiome && enraged)
                ExitEnraged();

            UpdateDamageMultiplier();

            // ----- 阶段轮换逻辑 -----
            phaseTimer--;
            if (phaseTimer <= 0)
            {
                // 当前阶段结束，切换到下一个阶段
                if (currentPhase == BossPhase.Hover)
                {
                    // Hover结束后进入攻击阶段，按顺序：Shoot -> Spawn -> Dash
                    switch (attackIndex)
                    {
                        case 0: currentPhase = BossPhase.Shoot; break;
                        case 1: currentPhase = BossPhase.Spawn; break;
                        case 2: currentPhase = BossPhase.Dash; break;
                    }
                    attackIndex = (attackIndex + 1) % 3; // 0,1,2循环
                }
                else
                {
                    // 攻击阶段结束后回到Hover
                    currentPhase = BossPhase.Hover;
                }
                phaseTimer = GetPhaseDuration(currentPhase);

                // 进入新阶段的初始化
                if (currentPhase == BossPhase.Dash)
                {
                    // 初始化冲刺子状态
                    dashSubState = DashSubState.Prepare;
                    dashSubTimer = PrepareTime;
                    dashCount = 0;
                }
                else if (currentPhase == BossPhase.Shoot || currentPhase == BossPhase.Spawn)
                {
                    actionTimer = 0; // 重置周期动作计时器
                }
            }

            // ----- 执行当前阶段行为 -----
            float baseSpeed = GetBaseSpeed();

            switch (currentPhase)
            {
                case BossPhase.Hover:
                    HoverBehavior(player, baseSpeed);
                    break;
                case BossPhase.Shoot:
                    ShootBehavior(player, baseSpeed);
                    break;
                case BossPhase.Spawn:
                    SpawnBehavior(player, baseSpeed);
                    break;
                case BossPhase.Dash:
                    DashBehavior(player, baseSpeed);
                    break;
            }

            // 更新攻击动画计时器
            if (attackAnimationTimer > 0)
                attackAnimationTimer--;

            NPC.spriteDirection = (NPC.velocity.X > 0) ? 1 : -1;
            NPC.rotation = NPC.velocity.X * 0.05f;
        }

        // ----- Hover：保持在玩家上方约250px，水平偏移350px -----
        private void HoverBehavior(Player player, float baseSpeed)
        {
            // 目标位置：玩家中心 + (水平偏移, 垂直偏移)
            int horizontalDir = (player.Center.X > NPC.Center.X) ? 1 : -1; // Boss在玩家左侧则向右偏移，右侧则向左偏移，以保持在玩家一侧
            Vector2 targetPos = player.Center + new Vector2(horizontalDir * 350, -250);

            Vector2 direction = targetPos - NPC.Center;
            float distance = direction.Length();
            if (distance > 10f)
            {
                direction.Normalize();
                float speed = Math.Min(baseSpeed, distance / 10f); // 减速靠近
                NPC.velocity = (NPC.velocity * 20f + direction * speed) / 21f;
            }
            else
            {
                NPC.velocity *= 0.95f; // 缓慢停止
            }
        }

        // ----- Shoot：每40帧发射一波扇形弹幕 -----
        private void ShootBehavior(Player player, float baseSpeed)
        {
            // 移动：简单保持Hover状态（复用Hover逻辑，但可适当调整）
            HoverBehavior(player, baseSpeed * 0.8f); // 射击时稍微减速

            actionTimer++;
            if (actionTimer >= ShootInterval)
            {
                actionTimer = 0;
                FireProjectiles(player);
            }
        }

        // ----- Spawn：每60帧召唤一批小怪 -----
        private void SpawnBehavior(Player player, float baseSpeed)
        {
            // 移动：保持Hover
            HoverBehavior(player, baseSpeed * 0.7f);

            actionTimer++;
            if (actionTimer >= SpawnInterval)
            {
                actionTimer = 0;
                SpawnMinions(player);
            }
        }

        // ----- Dash：预备30帧 → 连续冲刺2~3次（每次冲刺20帧，停顿10帧）-----
        private void DashBehavior(Player player, float baseSpeed)
        {
            dashSubTimer--;
            switch (dashSubState)
            {
                case DashSubState.Prepare:
                    // 预备阶段：停止移动，可播放特效
                    NPC.velocity *= 0.9f;
                    if (dashSubTimer <= 0)
                    {
                        dashSubState = DashSubState.Dashing;
                        dashSubTimer = DashTime;
                        dashCount++;
                    }
                    break;

                case DashSubState.Dashing:
                    // 冲刺：直线冲向玩家
                    Vector2 dashDir = player.Center - NPC.Center;
                    if (dashDir.LengthSquared() > 1f)
                        dashDir.Normalize();
                    NPC.velocity = dashDir * baseSpeed * 3f; // 冲刺速度
                    if (dashSubTimer <= 0)
                    {
                        if (dashCount < MaxDashes)
                        {
                            dashSubState = DashSubState.Pause;
                            dashSubTimer = PauseTime;
                        }
                        else
                        {
                            // 冲刺完成，强制结束Dash阶段（回到Hover）
                            phaseTimer = 0;
                        }
                    }
                    break;

                case DashSubState.Pause:
                    // 停顿：减速
                    NPC.velocity *= 0.8f;
                    if (dashSubTimer <= 0)
                    {
                        dashSubState = DashSubState.Dashing;
                        dashSubTimer = DashTime;
                        dashCount++;
                    }
                    break;
            }
        }

        // ----- 射击逻辑（保持原样，仅微调弹幕速度）-----
        private void FireProjectiles(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            int dir = (player.Center.X > NPC.Center.X) ? 1 : -1;
            float baseSpeedProj = GetProjectileBaseSpeed();
            int damage = (int)(20 * currentDamageMultiplier);
            float knockback = 10f;

            int shotCount = GetProjectileCount();
            float spread = MathHelper.ToRadians(GetProjectileSpread());

            for (int i = 0; i < shotCount; i++)
            {
                float angle = MathHelper.Lerp(-spread, spread, i / (float)(shotCount - 1));
                Vector2 velocity = new Vector2(baseSpeedProj, 0).RotatedBy(angle) * dir;
                Vector2 spawnPos = NPC.Center + new Vector2(dir * 30, -20);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, velocity,
                    ModContent.ProjectileType<QueensDecree>(), damage, knockback, Main.myPlayer);
            }
        }

        // ----- 召唤逻辑（使用动态上限）-----
        private void SpawnMinions(Player player)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // 女王守卫
            int guardType = ModContent.NPCType<QueenGuard>();
            int currentGuards = CountNPCs(guardType);
            int guardLimit = GetCurrentGuardLimit();
            if (currentGuards < guardLimit)
            {
                int desiredSpawn = phase2 ? 2 : Main.rand.Next(1, 3); // 适当减少单次生成数量，防止瞬间爆满
                int canSpawn = Math.Min(desiredSpawn, guardLimit - currentGuards);
                for (int i = 0; i < canSpawn; i++)
                {
                    Vector2 spawnPos = GetEmptyAirPosition(NPC.Center, 100, 300);
                    int guard = NPC.NewNPC(NPC.GetSource_FromAI(),
                        (int)spawnPos.X, (int)spawnPos.Y, guardType);
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, guard);
                }
            }

            // 超能蠕虫（仅在允许上限>0时尝试）
            int wormLimit = GetCurrentWormLimit();
            if (wormLimit > 0)
            {
                int wormType = ModContent.NPCType<HyperWormHead>();
                int currentWorms = CountNPCs(wormType);
                if (currentWorms < wormLimit && Main.rand.NextBool(3)) // 33%概率
                {
                    Vector2 spawnPos = GetEmptyAirPosition(NPC.Center, 100, 300);
                    int worm = NPC.NewNPC(NPC.GetSource_FromAI(),
                        (int)spawnPos.X, (int)spawnPos.Y, wormType);
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, worm);
                }
            }
        }

        // ----- 以下为原有辅助方法（未改动，仅保留）-----
        private void EnterPhase2()
        {
            phase2 = true;
            ApplyVisualEffects();
            if (Main.netMode != NetmodeID.Server)
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            for (int i = 0; i < 20; i++)
            {
                Vector2 dustPos = NPC.Center + Main.rand.NextVector2Circular(100, 100);
                Dust.NewDust(dustPos, 0, 0, DustID.Torch, 0f, -2f, 0, default, 1.5f);
            }
        }

        private void EnterEnraged()
        {
            enraged = true;
            ApplyVisualEffects();
            if (Main.netMode != NetmodeID.Server)
                SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, NPC.Center);

            // 狂暴时立即尝试生成蠕虫（不超过当前上限）
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int wormType = ModContent.NPCType<HyperWormHead>();
                int currentWorms = CountNPCs(wormType);
                int wormLimit = GetCurrentWormLimit();
                int toSpawn = Math.Min(2, wormLimit - currentWorms);
                for (int i = 0; i < toSpawn; i++)
                {
                    Vector2 spawnPos = GetEmptyAirPosition(NPC.Center, 100, 300);
                    int worm = NPC.NewNPC(NPC.GetSource_FromAI(),
                        (int)spawnPos.X, (int)spawnPos.Y, wormType);
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, worm);
                }
            }
        }

        private void ExitEnraged()
        {
            enraged = false;
            ApplyVisualEffects();
        }

        private void ApplyVisualEffects()
        {
            if (enraged && phase2)
            {
                NPC.color = Color.DarkRed;
                NPC.damage = (int)(originalDamage * Phase2DamageMult * EnragedDamageMult);
                NPC.defense = EnragedDefense + Phase2Defense;
            }
            else if (enraged)
            {
                NPC.color = Color.Red;
                NPC.damage = (int)(originalDamage * EnragedDamageMult);
                NPC.defense = EnragedDefense;
            }
            else if (phase2)
            {
                NPC.color = Color.OrangeRed;
                NPC.damage = (int)(originalDamage * Phase2DamageMult);
                NPC.defense = Phase2Defense;
            }
            else
            {
                NPC.color = Color.White;
                NPC.damage = originalDamage;
                NPC.defense = originalDefense;
            }
        }

        private void UpdateDamageMultiplier()
        {
            float mult = 1f;
            if (phase2) mult *= Phase2ProjectileDamageMult;
            if (enraged) mult *= EnragedProjectileDamageMult;
            currentDamageMultiplier = mult;
        }

        private float GetBaseSpeed()
        {
            float speed = 8f;
            if (phase2) speed *= Phase2SpeedMult;
            if (enraged) speed *= EnragedSpeedMult;
            return speed;
        }

        private float GetProjectileBaseSpeed()
        {
            float speed = 10f;
            if (enraged) speed *= 1.2f;
            if (phase2) speed *= 1.1f;
            return speed;
        }

        private int GetProjectileCount()
        {
            int baseCount = phase2 ? 7 : 5;
            if (enraged) baseCount += EnragedProjectileCountBonus;
            if (phase2) baseCount += Phase2ProjectileCountBonus;
            return baseCount;
        }

        private float GetProjectileSpread()
        {
            float baseSpread = phase2 ? 45f : 30f;
            if (enraged) baseSpread *= 1.2f;
            return baseSpread;
        }

        private bool AllPlayersDeadOrFar()
        {
            bool anyValid = false;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    anyValid = true;
                    float dist = Vector2.Distance(NPC.Center, player.Center);
                    if (dist <= MaxPlayerDistance)
                        return false;
                }
            }
            return !anyValid;
        }

        private void HandleLeaveCondition()
        {
            leaveTimer++;
            if (leaveTimer % 10 == 0 && Main.netMode != NetmodeID.Server)
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, -1f);
            if (leaveTimer >= LeaveDelay)
            {
                if (Main.netMode != NetmodeID.Server)
                    SoundEngine.PlaySound(SoundID.NPCDeath6, NPC.Center);
                NPC.active = false;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }
        }

        private Vector2 GetEmptyAirPosition(Vector2 center, int minHeight, int maxHeight)
        {
            int attempts = 20;
            for (int i = 0; i < attempts; i++)
            {
                int offsetY = -Main.rand.Next(minHeight, maxHeight);
                int x = (int)center.X;
                int y = (int)(center.Y + offsetY);
                Point tilePos = new Point(x / 16, y / 16);
                if (WorldGen.InWorld(tilePos.X, tilePos.Y, 10))
                {
                    Tile tile = Main.tile[tilePos.X, tilePos.Y];
                    if (tile == null || !tile.HasTile || !Main.tileSolid[tile.TileType])
                        return new Vector2(x, y);
                }
            }
            return new Vector2(center.X, center.Y - 200);
        }

        public override void FindFrame(int frameHeight)
        {
            int frameY;
            if (attackAnimationTimer > 0)
            {
                int attackFrame = (int)((Main.GameUpdateCount / 5) % 8);
                frameY = (4 + attackFrame) * frameHeight;
            }
            else
            {
                int moveFrame = (int)((Main.GameUpdateCount / 8) % 4);
                frameY = moveFrame * frameHeight;
            }
            NPC.frame.Y = frameY;
        }

        public override void OnKill()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath6, NPC.Center);
                for (int i = 0; i < 30; i++)
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Torch,
                        Main.rand.NextFloat(-5f, 5f), -2f, 0, default, 1.5f);
            }
            OnDeathEffects();
        }

        public virtual void OnDeathEffects()
        {
            if (Main.rand.NextFloat() <= 0.5f)
            {
                int itemType = ModContent.ItemType<Items.BrilliantCarapace>(); // 50%概率掉落辉石甲壳
                int amount = Main.rand.Next(1, 3);
                Item.NewItem(NPC.GetSource_Death(), NPC.getRect(), itemType, amount);
            }
            int itemTypehaveto = ModContent.ItemType<Items.CystQueenCore>();
            int amounthaveto = Main.rand.Next(1, 2);
            Item.NewItem(NPC.GetSource_Death(), NPC.getRect(), itemTypehaveto, amounthaveto);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot) { }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            scale = 1.2f;
            return null;
        }
    }
}