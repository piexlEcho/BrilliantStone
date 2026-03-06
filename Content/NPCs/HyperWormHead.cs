using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using BrilliantStone.Content.Players;
using BrilliantStone.Content.Projectiles;
using BrilliantStone.Content.Items;

namespace BrilliantStone.Content.NPCs
{
    internal class HyperWormHead : WormHead
    {
        public override int BodyType => ModContent.NPCType<HyperWormBody>();
        public override int TailType => ModContent.NPCType<HyperWormTail>();

        public override void SetStaticDefaults()
        {
            var drawModifier = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                CustomTexturePath = "BrilliantStone/Content/NPCs/HyperWorm_Bestiary", // 图片路径改为你的Mod和文件名
                Position = new Vector2(40f, 24f),
                PortraitPositionXOverride = 0f,
                PortraitPositionYOverride = 12f
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, drawModifier);
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.DiggerHead);
            NPC.aiStyle = -1;

            // 自定义数值
            NPC.lifeMax = 100;
            NPC.life = NPC.lifeMax;
            NPC.damage = 15;      // 可根据需要调整
            NPC.defense = 7;      // 防御也可自定义

            Banner = Type;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.InModBiome<Biomes.BrilliantBiome>())
                return 0.04f; // 稀有
            return 0f;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange([
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Underground,
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Caverns,
                new FlavorTextBestiaryInfoElement("Mods.BrilliantStone.Bestiary.HyperWormHead") // 本地化键名
            ]);
        }

        public override void Init()
        {
            MinSegmentLength = 8;
            MaxSegmentLength = 10;
            CommonWormInit(this);
        }

        internal static void CommonWormInit(Worm worm)
        {
            worm.MoveSpeed = 4.5f;
            worm.Acceleration = 0.045f; // 加速更快，减少转向惯性
        }

        public override void OnKill()
        {
            // 20% 概率掉落高能核心
            if (Main.rand.NextFloat() <= 0.2f)
            {
                int itemType = ModContent.ItemType<HyperCore>();
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), itemType);
            }
        }

        private int attackCounter;
        public override void SendExtraAI(BinaryWriter writer) => writer.Write(attackCounter);
        public override void ReceiveExtraAI(BinaryReader reader) => attackCounter = reader.ReadInt32();

        // 在 AI 中修改投射物伤害
        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (attackCounter > 0) attackCounter--;

                Player target = Main.player[NPC.target];
                if (attackCounter <= 0 && Vector2.Distance(NPC.Center, target.Center) < 200 && Collision.CanHit(NPC.Center, 1, 1, target.Center, 1, 1))
                {
                    Vector2 direction = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX);
                    direction = direction.RotatedByRandom(MathHelper.ToRadians(10));

                    int projectile = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, direction * 2f, ModContent.ProjectileType<HyperWormLaser>(), NPC.damage, 0, Main.myPlayer);
                    // 设置激光的时间，确保它不会无限存在
                    Main.projectile[projectile].timeLeft = 3600;
                    attackCounter = 500;
                    NPC.netUpdate = true;
                }
            }
        }

        // 添加击中玩家效果
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            ApplyInfection(target, 240);
        }

        internal static void ApplyInfection(Player target, int duration)
        {
            target.GetModPlayer<BrilliantPlayer>().AddInfectionStack(duration);
        }
    }

    internal class HyperWormBody : WormBody
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
            NPCID.Sets.RespawnEnemyID[Type] = ModContent.NPCType<HyperWormHead>();
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.DiggerBody);
            NPC.aiStyle = -1;
            Banner = ModContent.NPCType<HyperWormHead>(); // 身体使用头部的旗帜类型
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            HyperWormHead.ApplyInfection(target, 120);
        }

        public override void Init() => HyperWormHead.CommonWormInit(this);
    }

    internal class HyperWormTail : WormTail
    {
        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
            NPCID.Sets.RespawnEnemyID[Type] = ModContent.NPCType<HyperWormHead>();
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.DiggerTail);
            NPC.aiStyle = -1;
            Banner = ModContent.NPCType<HyperWormHead>(); // 尾部也使用头部的旗帜类型
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            HyperWormHead.ApplyInfection(target, 120);
        }

        public override void Init() => HyperWormHead.CommonWormInit(this);
    }
}