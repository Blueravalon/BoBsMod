using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace broilinghell.Content.Projectiles
{
    public class BoblingProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.aiStyle = 0; // Custom AI
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 10;
            Projectile.timeLeft = 720;
            Projectile.light = 0.1f;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false; // ❌ No block collision
            Projectile.extraUpdates = 0;
        }

        public override void AI()
        {
            // Rotate while flying
            Projectile.rotation += 0.2f * Projectile.direction;

            // Home toward the closest player
            float maxDetectRadius = 400f; // How far the projectile can "see"
            float homingSpeed = 8f;        // How fast it homes in

            Player closestPlayer = null;
            float closestDist = maxDetectRadius;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    float distance = Vector2.Distance(player.Center, Projectile.Center);
                    if (distance < closestDist)
                    {
                        closestDist = distance;
                        closestPlayer = player;
                    }
                }
            }

            if (closestPlayer != null)
            {
                Vector2 desiredVelocity = Projectile.DirectionTo(closestPlayer.Center) * homingSpeed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.1f); // Smooth homing
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 600); // 10 seconds of Frostburn
        }
    }
}