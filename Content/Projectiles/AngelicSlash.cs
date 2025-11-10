using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Microsoft.Xna.Framework;

namespace broilinghell.Content.Projectiles
{
    public class AngelicSlash : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 32; // hitbox width
            Projectile.height = 32; // hitbox height
            Projectile.friendly = true; // damages enemies
            Projectile.hostile = false; // doesn't damage player
            Projectile.DamageType = DamageClass.Melee; // or Ranged/Melee depending on your weapon
            Projectile.penetrate = 1; // how many enemies it can hit before disappearing
            Projectile.timeLeft = 180; // lifetime in ticks (60 = 1 second)
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true; // can hit tiles
        }

        public override void AI()
        {
            // Simple light effect
            Lighting.AddLight(Projectile.Center, Color.Pink.ToVector3() * 0.6f);

            // Fade out as it nears the end of its life
            if (Projectile.timeLeft < 60) // last second of life
            {
                Projectile.alpha += 5; // increase transparency
                if (Projectile.alpha > 255)
                    Projectile.alpha = 255;
            }

            // Rotate projectile to face velocity
            if (Projectile.velocity.Length() > 0.1f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Optional: Add effects on hit (like debuffs)
            target.AddBuff(BuffID.OnFire, 120); // burns enemy for 2 seconds
        }
    }
}
