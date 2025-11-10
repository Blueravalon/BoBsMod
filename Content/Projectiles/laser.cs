using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace broilinghell.Content.Projectiles
{
    public class Laser : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.aiStyle = 0; // No built-in AI, we're handling it manually
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Default;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 300; // Lives for 5 seconds max
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // Add a little glow trail
            Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Terra, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f);

            // Optional: small rotation effect
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Apply BoBBurning debuff
            target.AddBuff(ModContent.BuffType<Buffs.BoBBurn>(), 120); // 1 second of burning
        }
    }
}