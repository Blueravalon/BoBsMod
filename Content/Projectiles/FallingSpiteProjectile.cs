using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace broilinghell.Content.Projectiles
{
    public class FallingSpiteProjectile : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // Optional display name
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.aiStyle = 0; // We'll use custom movement
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Default;
            Projectile.penetrate = 1; // Break on hit
            Projectile.timeLeft = 600;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Simple gravity
            Projectile.velocity.Y += 0.3f;

            // Optional: spin
            Projectile.rotation += 0.2f * Projectile.direction;

            // Optional: dust for visual feedback
            if (Main.rand.NextBool(4))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Terra);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Optional debuff
            target.AddBuff(BuffID.OnFire, 180); // Burn for 3 seconds
        }
    }
}
