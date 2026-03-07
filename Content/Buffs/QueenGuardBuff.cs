using Terraria;
using Terraria.ModLoader;
using BrilliantStone.Content.Projectiles.Minion;

namespace BrilliantStone.Content.Buffs
{
    public class QueenGuardBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;       // 退出世界时不保存
            Main.buffNoTimeDisplay[Type] = true; // 不显示剩余时间
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 如果玩家没有对应的随从，则重新召唤
            if (player.ownedProjectileCounts[ModContent.ProjectileType<QueenGuardMinion>()] > 0)
            {
                player.buffTime[buffIndex] = 18000; // 保持buff存在
            }
            else
            {
                player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }
}