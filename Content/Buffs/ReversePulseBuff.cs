using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;

namespace BrilliantStone.Content.Buffs
{
    public class ReversePulseBuff : ModBuff
    {
        public override LocalizedText DisplayName => Language.GetText("Mods.BrilliantStone.Buffs.ReversePulseBuff.DisplayName");
        public override LocalizedText Description => Language.GetText("Mods.BrilliantStone.Buffs.ReversePulseBuff.Description");

        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;       // 退出世界不保存
            Main.buffNoTimeDisplay[Type] = true; // 不显示时间（无限）
            Main.debuff[Type] = false;           // 不是减益效果
        }
    }
}