using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.Localization;

namespace BrilliantStone.Content.Buffs
{
    public class BrilliantStun : ModBuff
    {
        public override LocalizedText DisplayName => Language.GetText("Mods.BrilliantStone.Buffs.BrilliantStun.DisplayName");
        public override LocalizedText Description => Language.GetText("Mods.BrilliantStone.Buffs.BrilliantStun.Description");
        public override void SetStaticDefaults()
        {

            Main.debuff[Type] = true;               // 这是一个debuff
            Main.buffNoSave[Type] = true;            // 退出世界时不保存
            Main.persistentBuff[Type] = false;       // 死亡后消失
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true; // 护士无法移除（可选）
        }
    }
}