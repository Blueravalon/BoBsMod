using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using broilinghell.Content.Projectiles;
using System;

namespace broilinghell.Content.NPCs
{
    public class handleft : ModNPC
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

        // Laser sweep state
        private bool isDoingLaserSweep = false;
        private int laserSweepTimer = 0;
        private float laserAngle = 0f;

        // Ground slam state
        private bool isDoingGroundSlam = false;
        private int groundSlamTimer = 0;
        private Vector2 slamTarget;
        private bool slamStarted = false;

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

            // Handle laser sweep state
            if (isDoingLaserSweep)
            {
                HandleLaserSweep();
                return;
            }

            // Handle ground slam state
            if (isDoingGroundSlam)
            {
                HandleGroundSlam();
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
                Vector2 desiredPosition = target.Center + new Vector2(-160f, 0f);

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
                        StartLaserSweep();
                        break;
                    case 3:
                        StartGroundSlam();
                        break;
                }

                attackCounter++;
                attackTimer = 0;
            }
        }

        private void StartLaserSweep()
        {
            isDoingLaserSweep = true;
            laserSweepTimer = 0;
            laserAngle = -90f; // Start pointing down
            NPC.velocity = Vector2.Zero; // Stop moving
        }

        private void HandleLaserSweep()
        {
            laserSweepTimer++;
            NPC.velocity *= 0.95f; // Slow down

            // Fire lasers in a sweeping arc
            if (laserSweepTimer % 3 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float angleRad = MathHelper.ToRadians(laserAngle);
                Vector2 direction = new Vector2((float)Math.Cos(angleRad), (float)Math.Sin(angleRad));
                Vector2 velocity = direction * 20f;

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    velocity,
                    ModContent.ProjectileType<Laser>(),
                    35,
                    1f,
                    Main.myPlayer
                );

                // Sweep from -90 to +90 degrees (180 degree arc)
                laserAngle += 6f;
            }

            // End after sweeping 180 degrees
            if (laserAngle >= 90f)
            {
                isDoingLaserSweep = false;
                laserSweepTimer = 0;
            }
        }

        private void StartGroundSlam()
        {
            isDoingGroundSlam = true;
            groundSlamTimer = 0;
            slamStarted = false;

            // Target position above player
            NPC.TargetClosest();
            Player player = Main.player[NPC.target];
            slamTarget = new Vector2(player.Center.X, player.Center.Y - 300f);
        }

        private void HandleGroundSlam()
        {
            groundSlamTimer++;

            if (!slamStarted)
            {
                // Phase 1: Move to position above player (60 ticks = 1 second)
                if (groundSlamTimer < 60)
                {
                    Vector2 toTarget = slamTarget - NPC.Center;
                    if (toTarget.Length() > 10f)
                    {
                        toTarget.Normalize();
                        NPC.velocity = toTarget * 15f;
                    }
                    else
                    {
                        NPC.velocity *= 0.9f;
                    }
                }
                // Phase 2: Brief pause
                else if (groundSlamTimer < 90)
                {
                    NPC.velocity = Vector2.Zero;
                }
                // Phase 3: SLAM DOWN!
                else
                {
                    slamStarted = true;
                    NPC.velocity = new Vector2(0, 40f); // Fast downward
                }
            }
            else
            {
                // Continue slamming down
                NPC.velocity.Y = 40f;

                // Check if we hit the ground or a tile
                Point tilePos = NPC.Center.ToTileCoordinates();
                Tile tile = Main.tile[tilePos.X, tilePos.Y];
                bool hitGround = tile != null && tile.HasTile && Main.tileSolid[tile.TileType];

                if (hitGround || groundSlamTimer > 300) // Safety timeout
                {
                    // Create shockwave effect
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Spawn spite projectiles in a circle around impact point
                        for (int i = 0; i < 12; i++)
                        {
                            float angle = MathHelper.TwoPi * i / 12f;
                            Vector2 velocity = new Vector2(
                                (float)Math.Cos(angle) * 8f,
                                (float)Math.Sin(angle) * 8f
                            );

                            Projectile.NewProjectile(
                                NPC.GetSource_FromAI(),
                                NPC.Center,
                                velocity,
                                ModContent.ProjectileType<FallingSpiteProjectile>(),
                                40,
                                1f,
                                255
                            );
                        }
                    }

                    // Dust effect
                    for (int i = 0; i < 30; i++)
                    {
                        Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke);
                        dust.velocity = Main.rand.NextVector2Circular(8f, 8f);
                        dust.scale = 1.5f;
                    }

                    isDoingGroundSlam = false;
                    groundSlamTimer = 0;
                    NPC.velocity *= 0.5f;
                }
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