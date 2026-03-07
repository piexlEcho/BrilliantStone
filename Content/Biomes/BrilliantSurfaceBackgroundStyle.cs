using Terraria;
using Terraria.ModLoader;

namespace BrilliantStone.Content.Biomes
{
    public class BrilliantSurfaceBackgroundStyle : ModSurfaceBackgroundStyle
    {
        // 必须实现此方法（用于背景淡入淡出效果），即使暂时留空
        public override void ModifyFarFades(float[] fades, float transitionSpeed)
        {
            // 可在此添加自定义背景过渡逻辑，目前先留空
        }

        public override int ChooseFarTexture()// 选择远景背景纹理
        {
            return BackgroundTextureLoader.GetBackgroundSlot(Mod, "Backgrounds/Background_342");
        }

        public override int ChooseMiddleTexture()// 选择中景背景纹理
        {
            return BackgroundTextureLoader.GetBackgroundSlot(Mod, "Backgrounds/Background_42");
        }

        //public override int ChooseCloseTexture(ref float scale, ref double parallax, ref float a, ref float b)
        //{
        // 可根据需要调整 scale 和 parallax
        // scale = 1f;
        // parallax = 0.5;
        //    return BackgroundTextureLoader.GetBackgroundSlot(Mod, "Backgrounds/Background_195");
        //}
    }
}