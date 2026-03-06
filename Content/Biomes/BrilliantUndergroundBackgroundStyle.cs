using Terraria.ModLoader;

namespace BrilliantStone.Content.Biomes
{
    public class BrilliantUndergroundBackgroundStyle : ModUndergroundBackgroundStyle
    {
        // 这个方法用来填充地下背景的四个图层
        public override void FillTextureArray(int[] textureSlots)
        {
            // 按顺序填入你的背景图片ID：最远、中间、较近、最近[citation:2]
            textureSlots[0] = BackgroundTextureLoader.GetBackgroundSlot(Mod, "Backgrounds/Background_195"); // 最远层
            textureSlots[1] = BackgroundTextureLoader.GetBackgroundSlot(Mod, "Backgrounds/Background_195");
            textureSlots[2] = BackgroundTextureLoader.GetBackgroundSlot(Mod, "Backgrounds/Background_195");
            textureSlots[3] = BackgroundTextureLoader.GetBackgroundSlot(Mod, "Backgrounds/Background_195"); // 最近层
        }
    }
}