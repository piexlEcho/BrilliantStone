using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using BrilliantStone.Content.Players;

namespace BrilliantStone.Content.Projectiles
{
    public class HyperWormLaser : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // 如果需要自定义显示名称，可以在这里设置
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.ShadowBeamHostile);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;          // 无限穿透（只反弹不消失）
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 5;         // 不需要额外更新，速度已经很快
            Projectile.aiStyle = -1;              // 完全自定义AI
            Projectile.width = 6;                  // 窄一点更像激光
            Projectile.height = 6;
            Projectile.timeLeft = 600;             // 默认存活时间，会被NPC的赋值覆盖
        }

        private int bounceCount = 0;
        private const int maxBounces = 20;         // 最多反弹20次，避免无限

        public override void AI()
        {
            // 根据发射者设置伤害属性
            if (Projectile.ai[0] == 1) // 玩家发射
            {
                Projectile.friendly = true;
                Projectile.hostile = false;
            }
            else // 敌人发射
            {
                Projectile.friendly = false;
                Projectile.hostile = true;
            }

            // 现有 AI 逻辑（旋转、发光等）
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Vector3(1f, 0.3f, 0.8f));
            if (bounceCount >= maxBounces) Projectile.Kill();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bounceCount++;

            // 标准反弹
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y;

            // 可选：添加碰撞音效
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            return false; // 继续存活
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.GetModPlayer<BrilliantPlayer>().AddInfectionStack(240); // 4s
        }
    }
}