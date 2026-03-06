using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using BrilliantStone.Content.Buffs;
using BrilliantStone.Content.Players;

namespace BrilliantStone.Content.NPCs
{
    public class QueenGuard : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 5; // 蝙蝠有5帧动画
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 28;
            NPC.height = 24;
            NPC.damage = 5;
            NPC.defense = 1;
            NPC.lifeMax = 80;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath4;
            NPC.value = 100f; // 掉落金币数量
            NPC.knockBackResist = 0.3f;
            NPC.aiStyle = NPCAIStyleID.Bat; // 蝙蝠AI (14)
            AIType = NPCID.CaveBat;          // 继承蝙蝠行为
            AnimationType = NPCID.CaveBat;    // 使用蝙蝠动画
            NPC.noGravity = true;
            NPC.noTileCollide = true;         // 穿墙
            NPC.npcSlots = 0.5f;
        }

        // 重写 AI 以增加速度
        public override void AI()
        {
            // 先执行原版蝙蝠 AI
            base.AI();

            // 提高最大速度限制（原版蝙蝠最大速度约 4-5，这里设为 8）
            float maxSpeed = 10f;
            if (NPC.velocity.Length() > maxSpeed)
            {
                NPC.velocity = Vector2.Normalize(NPC.velocity) * maxSpeed;
            }


            if (NPC.velocity.Length() < maxSpeed * 0.8f)
            {
                NPC.velocity *= 1.02f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            // 叠加辉石感染（120帧 = 2秒）
            target.GetModPlayer<BrilliantPlayer>().AddInfectionStack(120);

            // 60%概率施加辉石震慑，持续300帧（5秒）
            if (Main.rand.NextFloat() <= 0.6f)
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