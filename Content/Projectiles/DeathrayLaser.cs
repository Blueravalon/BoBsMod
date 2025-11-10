using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace broilinghell.Content.Projectiles
{
    public class DeathrayLaser : ModProjectile
    {
        private const float MAX_CHARGE_TIME = 90f; // Longer charge time for more tension
        private const float LASER_LENGTH = 3200f; // Longer reach
        private const int LASER_WIDTH = 48; // Wider laser

        public ref float ChargeTime => ref Projectile.ai[0];
        public ref float LaserRotation => ref Projectile.ai[1];

        private bool hasPlayedWarningSound = false;
        private bool hasPlayedFireSound = false;

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.alpha = 0;
            Projectile.timeLeft = 420; // 7 seconds total lifetime
            Projectile.hide = false;
            Projectile.light = 1.5f; // Brighter light
        }

        public override void AI()
        {
            // Find the BoB boss
            NPC owner = null;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<NPCs.BoB>())
                {
                    owner = Main.npc[i];
                    break;
                }
            }

            if (owner == null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            ChargeTime++;

            // Enhanced charging phase
            if (ChargeTime <= MAX_CHARGE_TIME)
            {
                // Warning sound at start of charge
                if (ChargeTime == 1 && !hasPlayedWarningSound)
                {
                    SoundEngine.PlaySound(SoundID.Zombie104, Projectile.Center);
                    hasPlayedWarningSound = true;
                }

                // Additional warning sound halfway through charge
                if (ChargeTime == MAX_CHARGE_TIME / 2)
                {
                    SoundEngine.PlaySound(SoundID.Zombie103, Projectile.Center);
                }

                // Pulsing scale during charge for more dramatic effect
                float pulseIntensity = (float)Math.Sin(ChargeTime * 0.3f) * 0.2f + 0.8f;
                Projectile.scale = (ChargeTime / MAX_CHARGE_TIME) * pulseIntensity;

                // Set initial rotation towards nearest player with slight prediction
                if (ChargeTime == 1)
                {
                    Player target = Main.player[Player.FindClosest(Projectile.Center, Projectile.width, Projectile.height)];
                    Vector2 predictedPos = target.Center + target.velocity * 45f; // More prediction
                    LaserRotation = (predictedPos - Projectile.Center).ToRotation();
                }

                // Create more intense charging effects
                if (ChargeTime % 3 == 0)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Electric);
                        dust.velocity = Main.rand.NextVector2Circular(5f, 5f);
                        dust.scale = Main.rand.NextFloat(1.5f, 2.5f);
                        dust.noGravity = true;
                    }
                }

                // Screen flash warning near end of charge
                if (ChargeTime > MAX_CHARGE_TIME - 30 && ChargeTime % 6 == 0)
                {
                    // Create warning dust at laser path
                    Vector2 direction = LaserRotation.ToRotationVector2();
                    for (int i = 0; i < LASER_LENGTH; i += 80)
                    {
                        Vector2 pos = Projectile.Center + direction * i;
                        Dust warning = Dust.NewDustDirect(pos, 4, 4, DustID.Terra);
                        warning.velocity = Vector2.Zero;
                        warning.scale = 1.5f;
                        warning.noGravity = true;
                    }
                }
            }
            else
            {
                // Firing phase
                Projectile.scale = 1f;

                // Play firing sound
                if (!hasPlayedFireSound)
                {
                    SoundEngine.PlaySound(SoundID.Item158, Projectile.Center);
                    hasPlayedFireSound = true;

                    // Screen shake on fire
                    Main.screenPosition += Main.rand.NextVector2Circular(15f, 15f);
                }

                // Faster rotation during firing for more challenge
                float rotationSpeed = 0.025f; // Increased from 0.015f
                LaserRotation += rotationSpeed;

                // Damage players during firing phase
                DamagePlayersInLaser();

                // Create more intense firing effects
                CreateEnhancedDustEffects();
            }
        }

        private void DamagePlayersInLaser()
        {
            if (ChargeTime <= MAX_CHARGE_TIME) return;

            Vector2 laserEnd = GetLaserEnd();
            Vector2 laserStart = Projectile.Center;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead) continue;

                if (IsPlayerInLaser(player, laserStart, laserEnd))
                {
                    // Increased damage and effects
                    player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByProjectile(-1, Projectile.whoAmI), 120, 0);

                    // Stronger knockback and debuffs
                    Vector2 knockbackDirection = Vector2.Normalize(player.Center - Projectile.Center);
                    player.velocity += knockbackDirection * 15f;

                    // Apply multiple debuffs
                    player.AddBuff(BuffID.OnFire, 300); // 5 seconds of fire
                    player.AddBuff(BuffID.Electrified, 180); // 3 seconds of electrified
                    player.AddBuff(ModContent.BuffType<Buffs.BoBBurn>(), 240); // 4 seconds of custom burn
                }
            }
        }

        private bool IsPlayerInLaser(Player player, Vector2 laserStart, Vector2 laserEnd)
        {
            Vector2 laserDirection = Vector2.Normalize(laserEnd - laserStart);
            Vector2 toPlayer = player.Center - laserStart;

            float projectionLength = Vector2.Dot(toPlayer, laserDirection);

            if (projectionLength < 0 || projectionLength > Vector2.Distance(laserStart, laserEnd))
                return false;

            Vector2 projectionPoint = laserStart + laserDirection * projectionLength;
            float distance = Vector2.Distance(player.Center, projectionPoint);

            return distance <= LASER_WIDTH / 2f;
        }

        private Vector2 GetLaserEnd()
        {
            Vector2 direction = LaserRotation.ToRotationVector2();
            Vector2 potentialEnd = Projectile.Center + direction * LASER_LENGTH;

            // Check for tile collision to make laser stop at walls
            Vector2 currentPos = Projectile.Center;
            while (Vector2.Distance(currentPos, Projectile.Center) < LASER_LENGTH)
            {
                currentPos += direction * 16f;
                int tileX = (int)(currentPos.X / 16f);
                int tileY = (int)(currentPos.Y / 16f);

                if (tileX >= 0 && tileX < Main.maxTilesX && tileY >= 0 && tileY < Main.maxTilesY)
                {
                    Tile tile = Main.tile[tileX, tileY];
                    if (tile.HasTile && Main.tileSolid[tile.TileType])
                    {
                        return currentPos;
                    }
                }
            }

            return potentialEnd;
        }

        private void CreateEnhancedDustEffects()
        {
            Vector2 laserEnd = GetLaserEnd();
            float laserLength = Vector2.Distance(Projectile.Center, laserEnd);
            Vector2 direction = LaserRotation.ToRotationVector2();

            // More frequent and varied dust effects
            if (Main.rand.NextBool(1))
            {
                for (int i = 0; i < laserLength; i += 40)
                {
                    Vector2 dustPos = Projectile.Center + direction * i;
                    dustPos += Main.rand.NextVector2Circular(LASER_WIDTH / 3f, LASER_WIDTH / 3f);

                    // Multiple dust types for variety
                    int dustType = Main.rand.Next(new int[] { DustID.Electric, DustID.GreenTorch, DustID.Torch, DustID.RedTorch });
                    Dust dust = Dust.NewDustDirect(dustPos, 0, 0, dustType);
                    dust.velocity = Main.rand.NextVector2Circular(3f, 3f);
                    dust.scale = Main.rand.NextFloat(1.5f, 3f);
                    dust.noGravity = true;
                }
            }

            // Add sparks at the laser end
            if (Main.rand.NextBool(2))
            {
                for (int i = 0; i < 5; i++)
                {
                    Dust spark = Dust.NewDustDirect(laserEnd, 0, 0, DustID.Electric);
                    spark.velocity = Main.rand.NextVector2Circular(8f, 8f);
                    spark.scale = Main.rand.NextFloat(2f, 4f);
                    spark.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 laserEnd = GetLaserEnd();
            Vector2 laserStart = Projectile.Center;

            // Enhanced flickering during charge
            if (ChargeTime <= MAX_CHARGE_TIME)
            {
                float flickerChance = 1f - (ChargeTime / MAX_CHARGE_TIME);
                if (Main.rand.NextFloat() < flickerChance * 0.7f)
                    return false;
            }

            DrawEnhancedLaser(laserStart, laserEnd);
            return false;
        }

        private void DrawEnhancedLaser(Vector2 start, Vector2 end)
        {
            Vector2 direction = Vector2.Normalize(end - start);
            float length = Vector2.Distance(start, end);
            float rotation = direction.ToRotation();

            SpriteBatch spriteBatch = Main.spriteBatch;

            Color laserColor = Color.Lime;
            if (ChargeTime <= MAX_CHARGE_TIME)
            {
                // More dramatic color transition during charge
                float chargeProgress = ChargeTime / MAX_CHARGE_TIME;
                laserColor = Color.Lerp(Color.Red * 0.3f, Color.Lime, chargeProgress);
                laserColor *= (float)Math.Sin(ChargeTime * 0.4f) * 0.3f + 0.7f; // Pulsing effect
            }

            Texture2D laserTexture = ModContent.Request<Texture2D>("broilinghell/Content/Projectiles/DeathrayLaser").Value;

            if (laserTexture != null)
            {
                Rectangle sourceRect = new Rectangle(0, 0, laserTexture.Width, laserTexture.Height);
                Vector2 origin = new Vector2(0, laserTexture.Height / 2f);
                Vector2 scale = new Vector2(length / laserTexture.Width, LASER_WIDTH / (float)laserTexture.Height);

                // Draw outer glow
                Vector2 glowScale = scale * 1.3f;
                spriteBatch.Draw(
                    laserTexture,
                    start - Main.screenPosition,
                    sourceRect,
                    laserColor * 0.3f,
                    rotation,
                    origin,
                    glowScale,
                    SpriteEffects.None,
                    0f
                );

                // Draw main beam
                spriteBatch.Draw(
                    laserTexture,
                    start - Main.screenPosition,
                    sourceRect,
                    laserColor,
                    rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0f
                );

                // Draw bright inner core
                Vector2 coreScale = new Vector2(length / laserTexture.Width, (LASER_WIDTH * 0.4f) / laserTexture.Height);
                spriteBatch.Draw(
                    laserTexture,
                    start - Main.screenPosition,
                    sourceRect,
                    Color.White,
                    rotation,
                    origin,
                    coreScale,
                    SpriteEffects.None,
                    0f
                );
            }
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 laserEnd = GetLaserEnd();

            // Massive explosion effect
            for (int i = 0; i < 50; i++)
            {
                Dust dust = Dust.NewDustDirect(laserEnd, 0, 0, DustID.Electric);
                dust.velocity = Main.rand.NextVector2Circular(15f, 15f);
                dust.scale = Main.rand.NextFloat(2f, 5f);
                dust.noGravity = true;
            }

            // Additional explosion at start
            for (int i = 0; i < 30; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.GreenTorch);
                dust.velocity = Main.rand.NextVector2Circular(12f, 12f);
                dust.scale = Main.rand.NextFloat(1.5f, 3f);
                dust.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item62, laserEnd);
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center); // Explosion sound
        }
    }
}