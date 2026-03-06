using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;
using BrilliantStone.Content.Players; // 引入BrilliantPlayer

namespace BrilliantStone.Content.Buffs
{
    public class BrilliantInfection : ModBuff
    {
        public override LocalizedText DisplayName => Language.GetText("Mods.BrilliantStone.Buffs.BrilliantInfection.DisplayName");
        public override LocalizedText Description => Language.GetText("Mods.BrilliantStone.Buffs.BrilliantInfection.Description");

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.persistentBuff[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = false;
        }

        // 在buff文本上显示层数
        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            // 获取本地玩家（因为buff图标只显示在当前玩家的buff栏）
            Player player = Main.LocalPlayer;
            if (player != null && player.active && player.HasBuff(Type))
            {
                var brilliantPlayer = player.GetModPlayer<BrilliantPlayer>();
                // 当层数大于1时显示数字（1层时不显示，更简洁）
                if (brilliantPlayer.infectionStacks > 1)
                {
                    buffName = $"{buffName} ({brilliantPlayer.infectionStacks})";
                }
            }
        }
    }
}