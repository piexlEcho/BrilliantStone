using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Audio;

namespace BrilliantStone.Content.Players
{
    public class BrilliantPlayer : ModPlayer
    {
        private const byte InfectionStackSyncID = 0;

        public int infectionStacks = 0;
        private bool hadInfectionLastFrame = false;
        public bool pulseEmitter;

        public void AddInfectionStack(int durationInTicks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int buffType = ModContent.BuffType<Buffs.BrilliantInfection>();

            if (Player.HasBuff(buffType))
            {
                infectionStacks++;
            }
            else
            {
                infectionStacks = 1;
            }
            Player.AddBuff(buffType, durationInTicks);

            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write(InfectionStackSyncID);
                packet.Write((byte)Player.whoAmI);
                packet.Write(infectionStacks);
                packet.Send();
            }
        }

        public override void PostUpdate()
        {
            bool hasInfection = Player.HasBuff(ModContent.BuffType<Buffs.BrilliantInfection>());

            if (!hasInfection)
                infectionStacks = 0;

            // 音效：只在新增时播放（通过 hadInfectionLastFrame 判断）
            if (hasInfection && !hadInfectionLastFrame && Player.whoAmI == Main.myPlayer)
            {
                SoundEngine.PlaySound(SoundID.PlayerHit, Player.Center);
            }
            hadInfectionLastFrame = hasInfection;

            // 生成黄色/橙色尘埃（非服务器）
            if (hasInfection && Main.netMode != NetmodeID.Server && Main.rand.NextBool(3))
            {
                // 使用黄色火炬尘埃，你也可以换为 DustID.OrangeTorch
                Dust.NewDust(Player.position, Player.width, Player.height, DustID.YellowTorch);
            }
        }

        public override void ResetEffects()
        {
            pulseEmitter = false;
        }

        public override void UpdateBadLifeRegen()
        {
            if (infectionStacks > 0)
            {
                bool shouldTakeDamage = true;
                // 检查是否拥有反向脉冲buff，且处于辉石生物群落，且感染层数 ≤ 5
                if (Player.HasBuff(ModContent.BuffType<Buffs.ReversePulseBuff>()) && Player.InModBiome<Biomes.BrilliantBiome>() && infectionStacks <= 5)
                {
                    shouldTakeDamage = false;
                }

                if (shouldTakeDamage)
                {
                    int baseDamagePerStack = 0; // 基础每层伤害
                    if (Player.HasBuff(ModContent.BuffType<Buffs.BrilliantStun>()))
                    {
                        baseDamagePerStack += 1; // 每层额外+1
                    }
                    int baseRegen = Player.InModBiome<Biomes.BrilliantBiome>() ? 10 : 2;
                    Player.lifeRegen -= (baseRegen + baseDamagePerStack) * infectionStacks;
                }
            }
        }

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write(InfectionStackSyncID);
                packet.Write((byte)Player.whoAmI);
                packet.Write(infectionStacks);
                packet.Send(toWho, fromWho);
            }
        }

        public void ReceiveSync(BinaryReader reader)
        {
            infectionStacks = reader.ReadInt32();
        }
    }
}