using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using broilinghell.Content.Projectiles;
using System;

namespace broilinghell.Content.NPCs
{
    public class handright : ModNPC
    {
        public int targetWhoAmI = -1;
        private int attackTimer = 0;
        private int attackCooldown = 200; // ~6.7 seconds at 60fps (faster attacks)
        private int attackCounter = 0; // tracks which attack to use

        // Charge attack state
        private enum ChargeState
        {
            Normal,
            Charging,
            Launching
        }

        private ChargeState chargeState = ChargeState.Normal;
        private int chargeTimer = 0;
        private Vector2 launchTarget;
        private float launchTimer = 0;

        // Spiral laser state
        private bool isDoingSpiralLasers = false;
        private int spiralTimer = 0;
        private float spiralAngle = 0f;

        // Orbital projectiles state
        private bool isDoingOrbital = false;
        private int orbitalTimer = 0;

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.damage = 20;
            NPC.defense = 10;
            NPC.lifeMax = 1000000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 60f;
            NPC.knockBackResist = 1f;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
        }

        public override void AI()
        {
            // Acquire target
            if (targetWhoAmI == -1 || !Main.npc[targetWhoAmI].active)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<bobultima>())
                    {
                        targetWhoAmI = i;
                        break;
                    }
                }
            }

            // Handle spiral laser state
            if (isDoingSpiralLasers)
            {
                HandleSpiralLasers();
                return;
            }

            // Handle orbital projectiles state
            if (isDoingOrbital)
            {
                HandleOrbitalProjectiles();
                return;
            }

            // Handle charge attack states
            if (chargeState == ChargeState.Charging)
            {
                HandleChargingState();
                return; // Skip normal movement during charge
            }
            else if (chargeState == ChargeState.Launching)
            {
                HandleLaunchingState();
                return; // Skip normal movement during launch
            }

            // Normal hover behavior
            if (targetWhoAmI != -1 && Main.npc[targetWhoAmI].active)
            {
                NPC target = Main.npc[targetWhoAmI];
                Vector2 desiredPosition = target.Center + new Vector2(160f, 0f);

                // If about to attack, shift up by 160px
                if (attackTimer >= attackCooldown - 60)
                {
                    desiredPosition.Y -= 160f;
                }

                float speed = 100f;
                float inertia = 1f;
                Vector2 move = desiredPosition - NPC.Center;
                if (move.Length() > speed)
                {
                    move.Normalize();
                    move *= speed;
                }
                NPC.velocity = (NPC.velocity * (inertia - 1) + move) / inertia;
            }

            // Handle attack logic
            attackTimer++;
            if (attackTimer >= attackCooldown)
            {
                // Cycle through 4 different attacks
                int attackType = attackCounter % 4;

                switch (attackType)
                {
                    case 0:
                        DoSpiteAttack();
                        break;
                    case 1:
                        StartChargeAttack();
                        break;
                    case 2:
                        StartSpiralLasers();
                        break;
                    case 3:
                        StartOrbitalProjectiles();
                        break;
                }

                attackCounter++;
                attackTimer = 0;
            }
        }

        private void StartSpiralLasers()
        {
            isDoingSpiralLasers = true;
            spiralTimer = 0;
            spiralAngle = 0f;
            NPC.velocity = Vector2.Zero; // Stop moving
        }

        private void HandleSpiralLasers()
        {
            spiralTimer++;
            NPC.velocity *= 0.95f; // Slow down

            // Fire lasers in a double spiral pattern
            if (spiralTimer % 4 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // First spiral arm
                float angle1 = MathHelper.ToRadians(spiralAngle);
                Vector2 direction1 = new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1));

                // Second spiral arm (180 degrees offset)
                float angle2 = MathHelper.ToRadians(spiralAngle + 180f);
                Vector2 direction2 = new Vector2((float)Math.Cos(angle2), (float)Math.Sin(angle2));

                // Fire both arms
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    direction1 * 15f,
                    ModContent.ProjectileType<Laser>(),
                    35,
                    1f,
                    Main.myPlayer
                );

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    direction2 * 15f,
                    ModContent.ProjectileType<Laser>(),
                    35,
                    1f,
                    Main.myPlayer
                );

                // Rotate for spiral effect
                spiralAngle += 15f;
            }

            // End after 3 full rotations (1080 degrees)
            if (spiralAngle >= 1080f)
            {
                isDoingSpiralLasers = false;
                spiralTimer = 0;
            }
        }

        private void StartOrbitalProjectiles()
        {
            isDoingOrbital = true;
            orbitalTimer = 0;
            NPC.velocity = Vector2.Zero;
        }

        private void HandleOrbitalProjectiles()
        {
            orbitalTimer++;
            NPC.velocity *= 0.95f;

            // Spawn orbiting projectiles that slowly expand outward
            if (orbitalTimer % 15 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.TargetClosest();
                Player player = Main.player[NPC.target];

                // Create a ring of projectiles
                int count = 8;
                for (int i = 0; i < count; i++)
                {
                    float angle = MathHelper.TwoPi * i / count;
                    Vector2 position = NPC.Center + new Vector2(
                        (float)Math.Cos(angle) * 80f,
                        (float)Math.Sin(angle) * 80f
                    );

                    // Calculate velocity to spiral outward and toward player
                    Vector2 toPlayer = Vector2.Normalize(player.Center - position);
                    Vector2 tangent = new Vector2(-toPlayer.Y, toPlayer.X); // Perpendicular
                    Vector2 velocity = (toPlayer * 3f) + (tangent * 4f); // Spiral motion

                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        position,
                        velocity,
                        ModContent.ProjectileType<FallingSpiteProjectile>(),
                        35,
                        1f,
                        255
                    );
                }
            }

            // End after spawning 6 waves
            if (orbitalTimer >= 90)
            {
                isDoingOrbital = false;
                orbitalTimer = 0;
            }
        }

        private void StartChargeAttack()
        {
            chargeState = ChargeState.Charging;
            chargeTimer = Main.rand.Next(60, 121); // 1-2 seconds
            NPC.velocity = Vector2.Zero; // Stop moving

            // Store player position
            NPC.TargetClosest();
            Player player = Main.player[NPC.target];
            launchTarget = player.Center;
        }

        private void HandleChargingState()
        {
            chargeTimer--;
            NPC.velocity *= 0.95f; // Slow to a stop

            // Visual telegraph - dust effect
            if (chargeTimer % 10 == 0)
            {
                Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Electric);
                dust.velocity = Main.rand.NextVector2Circular(3f, 3f);
            }

            if (chargeTimer <= 0)
            {
                // Launch!
                chargeState = ChargeState.Launching;
                launchTimer = 0;

                Vector2 direction = launchTarget - NPC.Center;
                direction.Normalize();
                NPC.velocity = direction * 30f; // High speed launch
            }
        }

        private void HandleLaunchingState()
        {
            launchTimer++;

            // Leave a trail of projectiles while charging
            if (launchTimer % 5 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPos = NPC.Center + Main.rand.NextVector2Circular(20f, 20f);
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    spawnPos,
                    Vector2.Zero, // Stationary
                    ModContent.ProjectileType<FallingSpiteProjectile>(),
                    25,
                    1f,
                    255
                );
            }

            // Check for tile collision
            Point tilePos = NPC.Center.ToTileCoordinates();
            Tile tile = Main.tile[tilePos.X, tilePos.Y];

            bool hitBlock = tile != null && tile.HasTile && Main.tileSolid[tile.TileType];

            // Return to normal after 4 seconds (240 ticks) or hitting a block
            if (launchTimer >= 240 || hitBlock)
            {
                chargeState = ChargeState.Normal;
                launchTimer = 0;
                NPC.velocity *= 0.5f; // Slow down when returning to normal
            }
        }

        private void DoSpiteAttack()
        {
            NPC.TargetClosest();
            Player player = Main.player[NPC.target];
            int projectileCount = 20; // Increased count

            for (int i = 0; i < projectileCount; i++)
            {
                // Pick a random X inside the visible screen
                float xPos = Main.screenPosition.X + Main.rand.Next(Main.screenWidth);
                // Spawn exactly at the top edge of the screen
                Vector2 spawnPos = new Vector2(xPos, Main.screenPosition.Y);
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(5f, 8f));

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    spawnPos,
                    velocity,
                    ModContent.ProjectileType<FallingSpiteProjectile>(),
                    30,
                    1f,
                    255
                );
            }
        }
    }
}