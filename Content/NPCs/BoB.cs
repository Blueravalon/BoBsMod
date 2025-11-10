using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Effects;
using broilinghell.Content.Projectiles;
using Luminance.Core.Graphics;
using Luminance.Common.Utilities;

namespace broilinghell.Content.NPCs
{
    [AutoloadBossHead]
    public class BoB : ModNPC, IPixelatedPrimitiveRenderer
    {
        private int projectileTimer = 0;
        private int attackPhaseTimer = 0;
        private bool useLasers = false;
        private bool hasEnteredPhase2 = false;
        private bool hasEnteredPhase3 = false;
        private int laserRate = 3;
        private int spiteRate = 9;

        private bool shaderActive = false;

        private int attackCycleCounter = 0;
        private int laser360Timer = 0;
        private bool isShooting360 = false;
        private int currentLaserAngle = 0;

        // Death phase variables
        private bool isInDeathPhase = false;
        private int deathPhaseTimer = 0;
        private int deathLaserTimer = 0;
        private bool isLaserWallActive = false;
        private int laserWallTimer = 0;
        private int laserWallCooldown = 0;
        private bool laserWallFromLeft = true;

        // Monologue system variables
        private bool isInMonologue = false;
        private int monologueTimer = 0;
        private int currentMonologueLine = 0;
        private string[] monologueLines = new string[]
        {
            "<BoB> ...so...you did it.",
            "<BoB> ...",
            "<BoB> no.",
            "<BoB> i'm not going to become a cosmic joke."
        };

        // New attack patterns
        private bool isTrackingPlayer = false;
        private int trackingTimer = 0;
        private bool isTeleporting = false;
        private int teleportTimer = 0;
        private Vector2 teleportTarget;
        private bool isSpinAttacking = false;
        private int spinTimer = 0;
        private float spinSpeed = 0f;
        private bool isLaserStorm = false;
        private int laserStormTimer = 0;
        private int homingLaserTimer = 0;

        // DeathrayUnmoving attack variables
        private int deathrayUnmovingTimer = 0;
        private int deathrayUnmovingCooldown = 0;

        // Luminance visual effects
        private float afterimageIntensity = 0f;
        private float energyPulseTimer = 0f;

        // Primitive rendering settings
        public PixelationPrimitiveLayer LayerToRenderTo => PixelationPrimitiveLayer.AfterNPCs;

        // Multiplayer sync indices
        private const int ShaderActiveIndex = 0;
        private const int IsInDeathPhaseIndex = 1;
        private const int IsInMonologueIndex = 2;
        private const int HasEnteredPhase2Index = 3;
        private const int HasEnteredPhase3Index = 4;

        public override void SetDefaults()
        {
            NPC.width = 64;
            NPC.height = 64;
            NPC.damage = 120;
            NPC.defense = 15;
            NPC.lifeMax = 1500000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath60;
            NPC.value = 800000f;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = 0;
            NPC.boss = true;
            NPC.npcSlots = 15f;

            if (!Main.dedServ)
            {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/grand");
            }
        }

        public override void SendExtraAI(System.IO.BinaryWriter writer)
        {
            // Send important state variables for multiplayer sync
            writer.Write(shaderActive);
            writer.Write(isInDeathPhase);
            writer.Write(isInMonologue);
            writer.Write(hasEnteredPhase2);
            writer.Write(hasEnteredPhase3);
            writer.Write(isShooting360);
            writer.Write(isSpinAttacking);
            writer.Write(isTeleporting);
            writer.Write(deathPhaseTimer);
            writer.Write(monologueTimer);
            writer.Write(currentMonologueLine);
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader)
        {
            bool oldShaderActive = shaderActive;

            // Receive state variables from server
            shaderActive = reader.ReadBoolean();
            isInDeathPhase = reader.ReadBoolean();
            isInMonologue = reader.ReadBoolean();
            hasEnteredPhase2 = reader.ReadBoolean();
            hasEnteredPhase3 = reader.ReadBoolean();
            isShooting360 = reader.ReadBoolean();
            isSpinAttacking = reader.ReadBoolean();
            isTeleporting = reader.ReadBoolean();
            deathPhaseTimer = reader.ReadInt32();
            monologueTimer = reader.ReadInt32();
            currentMonologueLine = reader.ReadInt32();

            // Handle shader activation/deactivation on clients
            if (!Main.dedServ)
            {
                if (shaderActive && !oldShaderActive)
                {
                    ActivateShader();
                }
                else if (!shaderActive && oldShaderActive)
                {
                    DeactivateShader();
                }
            }
        }

        private void ActivateShader()
        {
            if (Main.dedServ)
                return;

            var filter = Terraria.Graphics.Effects.Filters.Scene["GreenTintSystem"];
            if (filter != null && !filter.IsActive())
            {
                Terraria.Graphics.Effects.Filters.Scene.Activate("GreenTintSystem");
                var shader = filter.GetShader();
                if (shader != null)
                {
                    shader.UseTargetPosition(Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) / 2f);
                }
            }
        }

        private void DeactivateShader()
        {
            if (Main.dedServ)
                return;

            var filter = Terraria.Graphics.Effects.Filters.Scene["GreenTintSystem"];
            if (filter != null && filter.IsActive())
            {
                Terraria.Graphics.Effects.Filters.Scene.Deactivate("GreenTintSystem");
                Main.SceneMetrics.ShimmerTileCount = 0;
                Main.bgStyle = 1;
            }
        }

        private void CreateWarningEffect(Vector2 start, Vector2 end)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = Vector2.Normalize(end - start);
            float distance = Vector2.Distance(start, end);

            for (int i = 0; i < (int)(distance / 20f); i++)
            {
                Vector2 position = start + direction * (i * 20f);

                // Create warning dust particles
                for (int j = 0; j < 2; j++)
                {
                    Dust dust = Dust.NewDustDirect(position, 1, 1, DustID.FireworkFountain_Red);
                    dust.velocity = Main.rand.NextVector2Circular(2f, 2f);
                    dust.noGravity = true;
                    dust.scale = 1.5f;
                }
            }
        }

        private void FireDeathrayUnmoving()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[NPC.target];

                // Visual warning effect before firing
                CreateWarningEffect(NPC.Center, target.Center);

                int deathrayCount = hasEnteredPhase3 ? 5 : hasEnteredPhase2 ? 3 : 2;

                for (int i = 0; i < deathrayCount; i++)
                {
                    Vector2 targetPosition;

                    if (deathrayCount == 1)
                    {
                        targetPosition = target.Center;
                    }
                    else
                    {
                        float spreadAngle = MathHelper.TwoPi / deathrayCount;
                        float angle = spreadAngle * i;
                        float distance = hasEnteredPhase3 ? 400f : 300f;
                        targetPosition = target.Center + new Vector2(
                            (float)Math.Cos(angle) * distance,
                            (float)Math.Sin(angle) * distance
                        );
                    }

                    Vector2 direction = Vector2.Normalize(targetPosition - NPC.Center);
                    float rotation = direction.ToRotation();

                    int damage = hasEnteredPhase3 ? 80 : hasEnteredPhase2 ? 60 : 45;

                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<DeathrayUnmoving>(),
                        damage,
                        1f,
                        Main.myPlayer,
                        0f,
                        rotation
                    );
                }

                // Enhanced screen shake using Luminance
                if (!Main.dedServ)
                {
                    ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 12f);
                }

                deathrayUnmovingCooldown = hasEnteredPhase3 ? 300 : hasEnteredPhase2 ? 360 : 420;
                deathrayUnmovingTimer = 0;
            }
        }

        private void EnterDeathPhase()
        {
            isInDeathPhase = true;
            deathPhaseTimer = 0;
            deathLaserTimer = 0;

            if (!Main.dedServ)
            {
                ChromaticAberrationSystem.ActivateAberrationPulse(NPC.Center, strength: 3.0f);

                // Create dramatic explosion of particles
                for (int i = 0; i < 100; i++)
                {
                    Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Electric);
                    dust.velocity = Main.rand.NextVector2Circular(15f, 15f);
                    dust.color = Color.Lerp(Color.Green, Color.Lime, Main.rand.NextFloat());
                    dust.noGravity = true;
                    dust.scale = Main.rand.NextFloat(1.5f, 3f);
                }

                // Intense screen shake
                ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 20f);
            }

            NPC.life = 100;
            NPC.dontTakeDamage = true;
            NPC.velocity = Vector2.Zero;

            isShooting360 = false;
            isSpinAttacking = false;
            isLaserStorm = false;
            isTeleporting = false;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Main.NewText("<BoB> ok fuckin die dude", Color.Green);
                NPC.netUpdate = true;
            }
        }

        private void StartMonologue()
        {
            isInMonologue = true;
            monologueTimer = 0;
            currentMonologueLine = 0;

            NPC.velocity = Vector2.Zero;
            NPC.dontTakeDamage = true;

            if (!Main.dedServ)
            {
                ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 8f);

                // Particle burst effect
                for (int i = 0; i < 50; i++)
                {
                    Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Electric);
                    dust.velocity = Main.rand.NextVector2Circular(15f, 15f);
                    dust.scale = Main.rand.NextFloat(1.5f, 3f);
                    dust.noGravity = true;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true;
            }
        }

        private void HandleMonologue()
        {
            monologueTimer++;
            NPC.velocity = Vector2.Zero;

            int lineInterval = 180;
            if (monologueTimer % lineInterval == 0 && currentMonologueLine < monologueLines.Length)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Main.NewText(monologueLines[currentMonologueLine], Color.Green);
                }
                currentMonologueLine++;

                for (int i = 0; i < 20; i++)
                {
                    Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.MagicMirror);
                    dust.velocity = Main.rand.NextVector2Circular(8f, 8f);
                    dust.scale = Main.rand.NextFloat(1f, 2f);
                }

                if (!Main.dedServ)
                {
                    ChromaticAberrationSystem.ActivateAberrationRadial(NPC.Center, strength: 2.0f);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }

            if (currentMonologueLine >= monologueLines.Length && monologueTimer >= (monologueLines.Length * lineInterval) + 300)
            {
                NPC.life = 1;
                NPC.dontTakeDamage = false;

                // Epic final explosion with enhanced effects
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 150; i++)
                    {
                        Dust dust = Dust.NewDustDirect(NPC.Center, 0, 0, DustID.Electric);
                        dust.velocity = Main.rand.NextVector2Circular(30f, 30f);
                        dust.scale = Main.rand.NextFloat(2f, 5f);
                        dust.noGravity = true;
                    }

                    ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 25f);
                }
            }
        }

        private void HandleDeathPhase()
        {
            deathPhaseTimer++;
            deathLaserTimer++;

            NPC.velocity = Vector2.Zero;

            int laserFreq = Math.Max(90, 150 - (deathPhaseTimer / 120));

            if (deathLaserTimer >= laserFreq)
            {
                FireDeathrayLaser();
                deathLaserTimer = 0;
            }

            if (deathPhaseTimer % 240 == 0)
            {
                FireDeathrayUnmoving();
            }

            if (deathPhaseTimer >= 1350)
            {
                StartMonologue();
            }
        }

        private void FireDeathrayLaser()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[NPC.target];
                Vector2 direction = Vector2.Normalize(target.Center - NPC.Center);
                float rotation = direction.ToRotation();

                Vector2 predictedPos = target.Center + target.velocity * 30f;
                Vector2 predictedDirection = Vector2.Normalize(predictedPos - NPC.Center);
                float predictedRotation = predictedDirection.ToRotation();

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<DeathrayLaser>(),
                    400,
                    1f,
                    Main.myPlayer,
                    0f,
                    predictedRotation
                );

                if (!Main.dedServ)
                {
                    ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 15f);
                }
            }
        }

        private void FireLaserWall()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[NPC.target];
                laserWallFromLeft = Main.rand.NextBool();

                int screenWidth = Main.screenWidth;
                int screenHeight = Main.screenHeight;

                Vector2 spawnSide;
                Vector2 direction;

                if (laserWallFromLeft)
                {
                    spawnSide = new Vector2(target.Center.X - screenWidth / 2 - 100, target.Center.Y - screenHeight / 2);
                    direction = Vector2.UnitX;
                }
                else
                {
                    spawnSide = new Vector2(target.Center.X + screenWidth / 2 + 100, target.Center.Y - screenHeight / 2);
                    direction = -Vector2.UnitX;
                }

                int laserCount = 15;
                float spacing = screenHeight / laserCount;

                for (int i = 0; i < laserCount; i++)
                {
                    Vector2 laserSpawnPos = spawnSide + new Vector2(0, i * spacing);
                    Vector2 laserVelocity = direction * 12f;

                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        laserSpawnPos,
                        laserVelocity,
                        ModContent.ProjectileType<Laser>(),
                        40,
                        1f,
                        Main.myPlayer
                    );
                }
            }
        }

        private void StartTeleportAttack()
        {
            isTeleporting = true;
            teleportTimer = 0;
            Player target = Main.player[NPC.target];

            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            float distance = Main.rand.NextFloat(200f, 400f);
            teleportTarget = target.Center + new Vector2((float)Math.Cos(angle) * distance, (float)Math.Sin(angle) * distance - 150f);

            NPC.alpha = 255;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true;
            }
        }

        private void HandleTeleportAttack()
        {
            teleportTimer++;

            if (teleportTimer == 30)
            {
                NPC.Center = teleportTarget;

                for (int i = 0; i < 30; i++)
                {
                    Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.MagicMirror);
                    dust.velocity = Main.rand.NextVector2Circular(10f, 10f);
                    dust.scale = Main.rand.NextFloat(1f, 2f);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int burstCount = 8;
                    float baseAngle = MathHelper.TwoPi / burstCount;

                    for (int i = 0; i < burstCount; i++)
                    {
                        float angle = baseAngle * i;
                        Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 12f;

                        Projectile.NewProjectile(
                            NPC.GetSource_FromAI(),
                            NPC.Center,
                            velocity,
                            ModContent.ProjectileType<Laser>(),
                            35,
                            1f,
                            Main.myPlayer
                        );
                    }

                    NPC.netUpdate = true;
                }
            }

            if (teleportTimer >= 30)
            {
                NPC.alpha = Math.Max(0, NPC.alpha - 15);
            }

            if (teleportTimer >= 60)
            {
                isTeleporting = false;
                attackPhaseTimer = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }
        }

        private void StartSpinAttack()
        {
            isSpinAttacking = true;
            spinTimer = 0;
            spinSpeed = 0f;
            NPC.velocity = Vector2.Zero;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true;
            }
        }

        private void HandleSpinAttack()
        {
            spinTimer++;

            if (spinTimer < 120)
            {
                spinSpeed += 0.05f;
            }
            else if (spinTimer > 300)
            {
                spinSpeed *= 0.95f;
            }

            NPC.rotation += spinSpeed;

            if (spinTimer % 8 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 4; i++)
                {
                    float angle = NPC.rotation + (MathHelper.PiOver2 * i);
                    Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 15f;

                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center,
                        velocity,
                        ModContent.ProjectileType<Laser>(),
                        hasEnteredPhase3 ? 50 : 35,
                        1f,
                        Main.myPlayer
                    );
                }
            }

            if (spinTimer >= 400 || spinSpeed < 0.01f)
            {
                isSpinAttacking = false;
                NPC.rotation = 0f;
                attackPhaseTimer = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }
        }

        private void FireHomingLasers()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Player target = Main.player[NPC.target];

                for (int i = 0; i < (hasEnteredPhase3 ? 5 : 3); i++)
                {
                    Vector2 spawnOffset = Main.rand.NextVector2Circular(100f, 100f);
                    Vector2 initialVelocity = Vector2.Normalize(target.Center - (NPC.Center + spawnOffset)) * 8f;

                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center + spawnOffset,
                        initialVelocity,
                        ModContent.ProjectileType<Laser>(),
                        40,
                        1f,
                        Main.myPlayer
                    );
                }
            }
        }

        private void SpawnOrbitalSatellites()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int satelliteCount = hasEnteredPhase3 ? 4 : 3;
                float angleStep = MathHelper.TwoPi / satelliteCount;

                for (int i = 0; i < satelliteCount; i++)
                {
                    int proj = Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<OrbitalSatellite>(),
                        hasEnteredPhase3 ? 50 : 35,
                        1f,
                        Main.myPlayer,
                        NPC.whoAmI,
                        0f
                    );

                    if (proj >= 0 && proj < Main.maxProjectiles)
                    {
                        (Main.projectile[proj].ModProjectile as OrbitalSatellite).Projectile.localAI[0] = angleStep * i;
                    }
                }
            }
        }

        private void TriggerPhaseTransition(int phase)
        {
            if (Main.dedServ)
                return;

            Color phaseColor = phase == 3 ? Color.Red : Color.Orange;

            for (int i = 0; i < 150; i++)
            {
                Dust dust = Dust.NewDustDirect(NPC.Center, 0, 0, DustID.Electric);
                dust.velocity = Main.rand.NextVector2Circular(20f, 20f);
                dust.color = Color.Lerp(phaseColor, Color.White, Main.rand.NextFloat(0.3f));
                dust.scale = Main.rand.NextFloat(1.5f, 3f);
                dust.noGravity = true;
            }

            ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 15f);
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch)
        {
            if (NPC.IsABestiaryIconDummy)
                return;

            // Draw energy trail behind boss
            if (NPC.velocity.Length() > 0.5f && NPC.oldPos != null)
            {
                Color trailColor = hasEnteredPhase3 ? Color.Red :
                                   hasEnteredPhase2 ? Color.Orange : Color.Lime;

                PrimitiveSettings settings = new(
                    progress => 20f * (1f - progress),  // Width function
                    progress => trailColor * (1f - progress) * 0.6f,  // Color function
                    Smoothen: true,
                    Pixelate: true
                );

                PrimitiveRenderer.RenderTrail(NPC.oldPos, settings, 30);
            }

            // Draw energy aura during special attacks
            if (isShooting360 || isSpinAttacking || isInDeathPhase)
            {
                float pulseIntensity = (float)Math.Sin(energyPulseTimer * 0.1f) * 0.5f + 0.5f;
                Color auraColor = isInDeathPhase ? Color.Red : Color.Lime;

                PrimitiveSettingsCircle circleSettings = new(
                    _ => 80f + pulseIntensity * 20f,  // Radius function
                    _ => auraColor * 0.3f * pulseIntensity,  // Color function
                    Pixelate: true
                );

                PrimitiveRenderer.RenderCircle(NPC.Center, circleSettings, 64);
            }
        }

        public override void AI()
        {
            // Update visual timers
            energyPulseTimer++;

            if (!Main.dedServ)
            {
                // Update afterimage intensity based on speed
                afterimageIntensity = Math.Min(NPC.velocity.Length() / 15f, 1f);

                // Create ambient particles occasionally
                if (Main.rand.NextBool(10))
                {
                    Vector2 particlePos = NPC.Center + Main.rand.NextVector2Circular(40f, 40f);
                    Color particleColor = hasEnteredPhase3 ? Color.Red :
                                         hasEnteredPhase2 ? Color.Orange : Color.Lime;

                    Dust dust = Dust.NewDustDirect(particlePos, 1, 1, DustID.Electric);
                    dust.velocity = Main.rand.NextVector2Circular(2f, 2f);
                    dust.color = particleColor;
                    dust.noGravity = true;
                    dust.scale = 1.2f;
                }
            }

            NPC.TargetClosest(true);
            if (!NPC.HasValidTarget) return;

            // Handle monologue (takes priority over everything)
            if (isInMonologue)
            {
                HandleMonologue();
                return;
            }

            // Handle death phase
            if (isInDeathPhase)
            {
                HandleDeathPhase();
                return;
            }

            // Check if should enter death phase
            if (NPC.life <= 50000 && !isInDeathPhase)
            {
                EnterDeathPhase();
                return;
            }

            // Activate shader (only on server/singleplayer)
            if (!shaderActive && Main.netMode != NetmodeID.MultiplayerClient)
            {
                shaderActive = true;
                NPC.netUpdate = true;
            }

            // Handle shader activation on clients
            if (!Main.dedServ && shaderActive)
            {
                ActivateShader();
            }

            Player target = Main.player[NPC.target];

            // Handle special attacks
            if (isTeleporting)
            {
                HandleTeleportAttack();
                return;
            }

            if (isSpinAttacking)
            {
                HandleSpinAttack();
                return;
            }

            if (isShooting360)
            {
                Handle360LaserAttack();
                return;
            }

            // Enhanced movement based on phase
            Vector2 desiredPosition;
            float speed = hasEnteredPhase3 ? 15f : hasEnteredPhase2 ? 12f : 10f;

            if (hasEnteredPhase3)
            {
                float angle = (float)(Main.GameUpdateCount * 0.02f);
                desiredPosition = target.Center + new Vector2((float)Math.Cos(angle) * 300f, (float)Math.Sin(angle * 0.7f) * 200f - 150f);
            }
            else
            {
                desiredPosition = target.Center + new Vector2(0, -200f);
            }

            Vector2 toDesired = desiredPosition - NPC.Center;
            float inertia = hasEnteredPhase3 ? 8f : 10f;
            Vector2 velocity = toDesired.SafeNormalize(Vector2.Zero) * speed;
            NPC.velocity = (NPC.velocity * (inertia - 1) + velocity) / inertia;

            NPC.spriteDirection = (NPC.Center.X < target.Center.X) ? 1 : -1;

            // Phase transitions with enhanced effects
            if (!hasEnteredPhase2 && NPC.life <= NPC.lifeMax * 0.66f)
            {
                hasEnteredPhase2 = true;
                laserRate = 1;
                spiteRate = 3;
                attackPhaseTimer = -60;

                TriggerPhaseTransition(2);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Main.NewText("<BoB> startin' to peev me off, dude", Color.Green);
                    NPC.netUpdate = true;
                }
            }

            if (!hasEnteredPhase3 && NPC.life <= NPC.lifeMax * 0.33f)
            {
                hasEnteredPhase3 = true;
                laserRate = 1;
                spiteRate = 2;
                attackPhaseTimer = -60;

                TriggerPhaseTransition(3);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Main.NewText("<BoB> okay that's it, fuck you.", Color.Green);
                    NPC.netUpdate = true;
                }
            }

            // Handle DeathrayUnmoving timer and cooldown
            deathrayUnmovingTimer++;
            if (deathrayUnmovingCooldown > 0)
            {
                deathrayUnmovingCooldown--;
            }

            if (deathrayUnmovingCooldown <= 0)
            {
                int triggerChance = hasEnteredPhase3 ? 360 : hasEnteredPhase2 ? 480 : 600;
                if (deathrayUnmovingTimer >= triggerChance)
                {
                    for (int i = 0; i < 40; i++)
                    {
                        Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.FireworkFountain_Red, 0, 0, 150, default, 1.5f);
                    }
                    FireDeathrayUnmoving();
                }
            }

            // Attack cycle
            attackPhaseTimer++;
            int cycleDuration = hasEnteredPhase3 ? 120 : hasEnteredPhase2 ? 140 : 160;

            if (attackPhaseTimer >= cycleDuration)
            {
                attackCycleCounter++;
                int specialAttackFreq = hasEnteredPhase3 ? 2 : hasEnteredPhase2 ? 3 : 4;

                if (attackCycleCounter >= specialAttackFreq)
                {
                    int attackChoice = Main.rand.Next(5);
                    switch (attackChoice)
                    {
                        case 0:
                            FireLaserWall();
                            break;
                        case 1:
                            StartTeleportAttack();
                            break;
                        case 2:
                            StartSpinAttack();
                            break;
                        case 3:
                            FireDeathrayUnmoving();
                            break;
                        case 4:
                            SpawnOrbitalSatellites();
                            break;
                    }
                    attackCycleCounter = 0;
                    attackPhaseTimer = 0;
                    projectileTimer = 0;
                    return;
                }

                useLasers = !useLasers;
                attackPhaseTimer = 0;
                projectileTimer = 0;
            }

            // Fire homing lasers in phase 3
            if (hasEnteredPhase3)
            {
                homingLaserTimer++;
                if (homingLaserTimer >= 45)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 spawnOffset = Main.rand.NextVector2Circular(100f, 100f);
                            Vector2 initialVelocity = Vector2.Normalize(target.Center - (NPC.Center + spawnOffset)) * 6f;
                            Projectile.NewProjectile(
                                NPC.GetSource_FromAI(),
                                NPC.Center + spawnOffset,
                                initialVelocity,
                                ModContent.ProjectileType<Laser>(),
                                40,
                                1f,
                                Main.myPlayer
                            );
                        }
                    }
                    homingLaserTimer = 0;
                }
            }

            // Projectile shooting
            int currentRate = useLasers ? laserRate : spiteRate;
            projectileTimer++;
            if (projectileTimer >= currentRate)
            {
                projectileTimer = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (useLasers)
                    {
                        int laserCount = hasEnteredPhase3 ? 2 : hasEnteredPhase2 ? 2 : 1;
                        for (int i = 0; i < laserCount; i++)
                        {
                            Vector2 direction = Vector2.Normalize(target.Center - NPC.Center);
                            if (!direction.HasNaNs())
                            {
                                if (laserCount > 1)
                                {
                                    float spread = 0.3f;
                                    float angle = direction.ToRotation() + Main.rand.NextFloat(-spread, spread);
                                    direction = angle.ToRotationVector2();
                                }
                                Vector2 laserVelocity = direction * (hasEnteredPhase3 ? 30f : hasEnteredPhase2 ? 25f : 20f);
                                Projectile.NewProjectile(
                                    NPC.GetSource_FromAI(),
                                    NPC.Center,
                                    laserVelocity,
                                    ModContent.ProjectileType<Laser>(),
                                    hasEnteredPhase3 ? 40 : hasEnteredPhase2 ? 30 : 25,
                                    1f,
                                    Main.myPlayer
                                );
                            }
                        }
                    }
                    else
                    {
                        int spiteCount = hasEnteredPhase3 ? 3 : hasEnteredPhase2 ? 3 : 2;
                        for (int i = 0; i < spiteCount; i++)
                        {
                            Vector2 spawnPos = NPC.Center + new Vector2(Main.rand.Next(-60, 60), 20f);
                            Vector2 projVelocity = new Vector2(Main.rand.NextFloat(-2f, 2f), -Main.rand.NextFloat(3f, 7f));
                            Projectile.NewProjectile(
                                NPC.GetSource_FromAI(),
                                spawnPos,
                                projVelocity,
                                ModContent.ProjectileType<FallingSpiteProjectile>(),
                                hasEnteredPhase3 ? 45 : hasEnteredPhase2 ? 35 : 30,
                                1f,
                                Main.myPlayer
                            );
                        }
                    }
                }
            }
        }

        private void Handle360LaserAttack()
        {
            laser360Timer++;

            int fireRate = hasEnteredPhase3 ? 1 : hasEnteredPhase2 ? 1 : 2;

            if (laser360Timer % fireRate == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float angleRad = MathHelper.ToRadians(currentLaserAngle);
                Vector2 laserDirection = new Vector2((float)Math.Cos(angleRad), (float)Math.Sin(angleRad));
                Vector2 laserVelocity = laserDirection * 18f;

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    laserVelocity,
                    ModContent.ProjectileType<Laser>(),
                    hasEnteredPhase3 ? 50 : hasEnteredPhase2 ? 40 : 35,
                    1f,
                    Main.myPlayer
                );

                int angleStep = hasEnteredPhase3 ? 3 : hasEnteredPhase2 ? 4 : 5;
                currentLaserAngle += angleStep;
            }

            if (currentLaserAngle >= 360)
            {
                isShooting360 = false;
                attackPhaseTimer = 0;
                projectileTimer = 0;
                useLasers = false;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ)
                return true;

            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Vector2 origin = texture.Size() / 2f;

            // Draw afterimages
            if (afterimageIntensity > 0.1f && NPC.oldPos != null)
            {
                Color afterimageColor = hasEnteredPhase3 ? new Color(255, 0, 0, 0) :
                                        hasEnteredPhase2 ? new Color(255, 165, 0, 0) :
                                        new Color(0, 255, 0, 0);

                // Draw up to 7 afterimages
                for (int i = 1; i < Math.Min(NPC.oldPos.Length, 7); i++)
                {
                    Vector2 drawPos = NPC.oldPos[i] + NPC.Size / 2f - Main.screenPosition;
                    float opacity = (1f - (i / 7f)) * 0.7f * afterimageIntensity;
                    Color color = afterimageColor * opacity;

                    spriteBatch.Draw(
                        texture,
                        drawPos,
                        NPC.frame,
                        color,
                        NPC.rotation,
                        origin,
                        NPC.scale,
                        NPC.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
                        0f
                    );
                }
            }

            // Draw glow effect during special attacks
            if (isShooting360 || isSpinAttacking || isInDeathPhase)
            {
                Color glowColor = isInDeathPhase ? new Color(255, 0, 0, 0) : new Color(0, 255, 0, 0);
                float glowIntensity = (float)Math.Sin(energyPulseTimer * 0.1f) * 0.5f + 0.5f;

                // Draw multiple expanding circles for glow effect
                for (int i = 0; i < 3; i++)
                {
                    float scale = 0.5f + (i * 0.15f) + (glowIntensity * 0.2f);
                    Main.EntitySpriteDraw(
                        TextureAssets.Extra[98].Value,
                        NPC.Center - Main.screenPosition,
                        null,
                        glowColor * (0.3f - i * 0.08f) * glowIntensity,
                        0f,
                        TextureAssets.Extra[98].Value.Size() * 0.5f,
                        scale,
                        SpriteEffects.None,
                        0
                    );
                }
            }

            return true;
        }

        public override void OnKill()
        {
            // Deactivate shader BEFORE checking netMode
            if (!Main.dedServ)
            {
                DeactivateShader();
            }

            // Set shader inactive (this will sync to clients via netUpdate)
            shaderActive = false;

            if (!Main.dedServ)
            {
                // Death explosion particles with enhanced effects
                for (int i = 0; i < 200; i++)
                {
                    Dust dust = Dust.NewDustDirect(NPC.Center, 0, 0, DustID.Electric);
                    dust.velocity = Main.rand.NextVector2Circular(25f, 25f);
                    dust.color = Color.Lerp(Color.Red, Color.Orange, Main.rand.NextFloat());
                    dust.scale = Main.rand.NextFloat(2f, 4f);
                    dust.noGravity = true;
                }

                // Final screen shake
                ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 20f, MathHelper.TwoPi);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int handRight = NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)NPC.Center.X + 160,
                    (int)NPC.Center.Y,
                    ModContent.NPCType<handright>()
                );

                int handLeft = NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)NPC.Center.X - 160,
                    (int)NPC.Center.Y,
                    ModContent.NPCType<handleft>()
                );

                int ultima = NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)NPC.Center.X,
                    (int)NPC.Center.Y - 200,
                    ModContent.NPCType<bobultima>()
                );

                if (handRight >= 0) NetMessage.SendData(MessageID.SyncNPC, number: handRight);
                if (handLeft >= 0) NetMessage.SendData(MessageID.SyncNPC, number: handLeft);
                if (ultima >= 0) NetMessage.SendData(MessageID.SyncNPC, number: ultima);

                // Force sync to ensure shader deactivation is sent to clients
                NPC.netUpdate = true;
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.LunarBar, 1, 15, 40));
            npcLoot.Add(ItemDropRule.Common(ItemID.GroxTheGreatHelm, 5));
            npcLoot.Add(ItemDropRule.Common(ItemID.GroxTheGreatArmor, 5));
            npcLoot.Add(ItemDropRule.Common(ItemID.GroxTheGreatGreaves, 5));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.BoBTreasureBag>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.InfernalChains>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.bobsword>(), 1));
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                new FlavorTextBestiaryInfoElement("Fueled by the power of raw spite (and a healthy dose of Auric souls), BoB is ready for one last duel."),
            });
        }
    }
}