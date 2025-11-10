using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using Luminance.Core.Cutscenes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.Utilities;
using Terraria.DataStructures;

namespace broilinghell.Content.NPCs
{
    public class ChaosWing : ModNPC
    {
        // AI fields
        private ref float AITimer => ref NPC.ai[0];
        private ref float AttackState => ref NPC.ai[1];
        private ref float AttackTimer => ref NPC.ai[2];
        private ref float Phase => ref NPC.ai[3];

        private Player Target => Main.player[NPC.target];

        // Phase thresholds
        private const float Phase2Threshold = 0.75f;
        private const float Phase3Threshold = 0.5f;
        private const float Phase4Threshold = 0.25f;

        // Attack patterns
        private enum AttackPattern
        {
            ErraticDash,
            SpiralBarrage,
            ChaoticBurst,
            DeathRain,
            VoidRift,
            ChaoticTeleport,
            OrbitalStrike,
            TimeStop
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 4;
            NPCID.Sets.TrailCacheLength[Type] = 60;
            NPCID.Sets.TrailingMode[Type] = 3;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 120;
            NPC.height = 120;
            NPC.damage = 80;
            NPC.defense = 40;
            NPC.lifeMax = 750000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.value = Item.buyPrice(gold: 500);
            NPC.boss = true;
            NPC.npcSlots = 50f;
        }

        public override void AI()
        {
            // Activate screen shader with intensity based on phase
            if (!Main.dedServ)
            {
                ActivateScreenShader();
            }

            // Target validation
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers || !Target.active || Target.dead)
            {
                NPC.TargetClosest();
                if (!Target.active || Target.dead)
                {
                    NPC.velocity.Y -= 0.4f;
                    if (NPC.timeLeft > 10)
                        NPC.timeLeft = 10;
                    return;
                }
            }

            // Check for phase transitions
            CheckPhaseTransition();

            AITimer++;

            // Get current phase attack speed multiplier
            float attackSpeedMultiplier = 1f + (Phase * 0.3f);
            int attackCycleTime = (int)(300 / attackSpeedMultiplier);

            // Change attack pattern periodically
            if (AITimer % attackCycleTime == 0)
            {
                AttackState = Main.rand.Next(GetAvailableAttacks());
                AttackTimer = 0;

                // Dramatic effect on attack change
                if (!Main.dedServ)
                {
                    ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 5f);
                    CameraPanSystem.PanTowards(NPC.Center, 0.2f);
                }
            }

            // Execute current attack pattern
            ExecuteAttack((AttackPattern)AttackState);

            // Constant visual effects
            SpawnVisualEffects();
            SpawnPhaseParticles();

            // Rotation based on velocity
            NPC.rotation = NPC.velocity.X * 0.05f + (float)Math.Sin(AITimer * 0.05f) * 0.2f;

            AttackTimer++;
        }

        private void CheckPhaseTransition()
        {
            float lifeRatio = NPC.life / (float)NPC.lifeMax;
            int newPhase = Phase switch
            {
                0 when lifeRatio <= Phase2Threshold => 1,
                1 when lifeRatio <= Phase3Threshold => 2,
                2 when lifeRatio <= Phase4Threshold => 3,
                _ => (int)Phase
            };

            if (newPhase != Phase)
            {
                Phase = newPhase;
                OnPhaseChange();
            }
        }

        private void OnPhaseChange()
        {
            // Dramatic phase transition
            if (!Main.dedServ)
            {
                ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 15f + Phase * 5f);
                CameraPanSystem.PanTowards(NPC.Center, 0.5f);
                CameraPanSystem.ZoomIn(0.3f * Phase);
            }

            // Heal a bit to make it even harder
            NPC.life += NPC.lifeMax / 20;
            if (NPC.life > NPC.lifeMax)
                NPC.life = NPC.lifeMax;

            // Explosion of projectiles
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int projectileCount = 30 + (int)Phase * 20;
                for (int i = 0; i < projectileCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / projectileCount;
                    Vector2 velocity = angle.ToRotationVector2() * (10f + Phase * 3f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity,
                        ProjectileID.PhantasmalDeathray, 40 + (int)Phase * 10, 5f);
                }
            }

            // Massive particle burst
            for (int i = 0; i < 100; i++)
            {
                Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height,
                    Phase % 2 == 0 ? DustID.PurpleTorch : DustID.BlueTorch,
                    Scale: Main.rand.NextFloat(3f, 5f));
                dust.velocity = Main.rand.NextVector2Circular(20f, 20f);
                dust.noGravity = true;
            }

            // Reset attack timer
            AttackTimer = 0;
        }

        private int GetAvailableAttacks()
        {
            return Phase switch
            {
                0 => 3,  // Only first 3 attacks
                1 => 5,  // First 5 attacks
                2 => 7,  // First 7 attacks
                _ => 8   // All attacks
            };
        }

        private void ExecuteAttack(AttackPattern pattern)
        {
            switch (pattern)
            {
                case AttackPattern.ErraticDash:
                    ErraticDashAttack();
                    break;
                case AttackPattern.SpiralBarrage:
                    SpiralBarrageAttack();
                    break;
                case AttackPattern.ChaoticBurst:
                    ChaoticBurstAttack();
                    break;
                case AttackPattern.DeathRain:
                    DeathRainAttack();
                    break;
                case AttackPattern.VoidRift:
                    VoidRiftAttack();
                    break;
                case AttackPattern.ChaoticTeleport:
                    ChaoticTeleportAttack();
                    break;
                case AttackPattern.OrbitalStrike:
                    OrbitalStrikeAttack();
                    break;
                case AttackPattern.TimeStop:
                    TimeStopAttack();
                    break;
            }
        }

        private void ActivateScreenShader()
        {
            if (ShaderManager.TryGetFilter("broilinghell.ChaosWingFilter", out ManagedScreenFilter filter))
            {
                filter.Activate();
                filter.SetFocusPosition(NPC.Center);

                // Increase intensity with phase
                filter.TrySetParameter("intensity", 1f + Phase * 0.3f);

                filter.SetTexture(ModContent.Request<Texture2D>("Luminance/Assets/Noise/TurbulentNoise"), 1, SamplerState.LinearWrap);
            }
        }

        // ATTACK PATTERNS

        private void ErraticDashAttack()
        {
            int dashFrequency = Math.Max(15, 30 - (int)Phase * 5);

            if (AttackTimer % dashFrequency == 0)
            {
                Vector2 dashDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
                float dashSpeed = Main.rand.NextFloat(15f, 25f) + Phase * 3f;
                NPC.velocity = dashDirection * dashSpeed;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int projectileCount = 5 + (int)Phase * 3;
                    for (int i = 0; i < projectileCount; i++)
                    {
                        Vector2 shootVel = Main.rand.NextVector2Circular(10f, 10f);
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shootVel,
                            Main.rand.NextBool() ? ProjectileID.CursedFlameHostile : ProjectileID.ShadowBeamHostile,
                            25 + (int)Phase * 5, 3f);
                    }
                }

                if (!Main.dedServ)
                    ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 4f + Phase);
            }

            NPC.velocity *= 0.97f;
        }

        private void SpiralBarrageAttack()
        {
            float orbitSpeed = 0.05f + Phase * 0.02f;
            Vector2 targetPosition = Target.Center + new Vector2(350 - Phase * 50, 0).RotatedBy(AttackTimer * orbitSpeed);
            NPC.SmoothFlyNear(targetPosition, 0.1f + Phase * 0.02f, 0.9f);

            int shootFrequency = Math.Max(3, 8 - (int)Phase * 2);
            if (AttackTimer % shootFrequency == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float rotation = AttackTimer * 0.15f;
                int spiralCount = 3 + (int)Phase;
                for (int i = 0; i < spiralCount; i++)
                {
                    Vector2 shootVel = (rotation + i * MathHelper.TwoPi / spiralCount).ToRotationVector2() * (7f + Phase * 2f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shootVel,
                        ProjectileID.DeathLaser, 30 + (int)Phase * 5, 3f);
                }
            }
        }

        private void ChaoticBurstAttack()
        {
            Vector2 hoverPosition = Target.Center - new Vector2(0, 350);
            NPC.SmoothFlyNear(hoverPosition, 0.2f, 0.92f);

            int chargeTime = Math.Max(40, 60 - (int)Phase * 5);
            int releaseTime = chargeTime + 20;

            if (AttackTimer == chargeTime)
            {
                if (!Main.dedServ)
                {
                    ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 10f + Phase * 3f);
                    CameraPanSystem.PanTowards(NPC.Center, 0.4f);
                    CameraPanSystem.ZoomIn(0.2f);
                }
            }

            if (AttackTimer == releaseTime && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int burstCount = 40 + (int)Phase * 20;
                for (int i = 0; i < burstCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / burstCount;
                    Vector2 shootVel = angle.ToRotationVector2() * Main.rand.NextFloat(6f, 15f + Phase * 3f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, shootVel,
                        ProjectileID.PhantasmalBolt, 35 + (int)Phase * 5, 3f);
                }
            }
        }

        private void DeathRainAttack()
        {
            // Unlocked in Phase 2
            Vector2 targetPosition = Target.Center + new Vector2(0, -400);
            NPC.SmoothFlyNear(targetPosition, 0.15f, 0.88f);

            if (AttackTimer % 10 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Rain down projectiles
                for (int i = 0; i < 3 + (int)Phase; i++)
                {
                    Vector2 spawnPos = Target.Center + new Vector2(Main.rand.NextFloat(-600f, 600f), -800);
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(8f, 12f + Phase * 2f));
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, velocity,
                        ProjectileID.MartianTurretBolt, 30 + (int)Phase * 5, 3f);
                }
            }
        }

        private void VoidRiftAttack()
        {
            // Unlocked in Phase 2
            if (AttackTimer % 80 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Create void rifts that spawn projectiles
                Vector2 riftPos = Target.Center + Main.rand.NextVector2Circular(400f, 400f);

                for (int i = 0; i < 16; i++)
                {
                    float angle = MathHelper.TwoPi * i / 16f;
                    Vector2 vel = angle.ToRotationVector2() * 8f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), riftPos, vel,
                        ProjectileID.PhantasmalEye, 35, 3f);
                }

                if (!Main.dedServ)
                    ScreenShakeSystem.StartShakeAtPoint(riftPos, 6f);
            }

            // Erratic movement
            if (AttackTimer % 40 == 0)
            {
                NPC.velocity = Main.rand.NextVector2Circular(12f, 12f);
            }
            NPC.velocity *= 0.96f;
        }

        private void ChaoticTeleportAttack()
        {
            // Unlocked in Phase 3
            if (AttackTimer % 60 == 0)
            {
                // Teleport effects
                if (!Main.dedServ)
                {
                    ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 8f);

                    // Teleport particles at old position
                    for (int i = 0; i < 30; i++)
                    {
                        Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch,
                            Scale: Main.rand.NextFloat(2f, 4f));
                        dust.velocity = Main.rand.NextVector2Circular(10f, 10f);
                        dust.noGravity = true;
                    }
                }

                // Teleport
                Vector2 teleportPos = Target.Center + Main.rand.NextVector2Circular(500f, 500f);
                NPC.Center = teleportPos;
                NPC.velocity = Vector2.Zero;

                // Explosion at new position
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        float angle = MathHelper.TwoPi * i / 24f;
                        Vector2 vel = angle.ToRotationVector2() * 10f;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, vel,
                            ProjectileID.CursedFlameHostile, 40, 5f);
                    }
                }

                if (!Main.dedServ)
                {
                    ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 8f);

                    // Teleport particles at new position
                    for (int i = 0; i < 30; i++)
                    {
                        Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.BlueTorch,
                            Scale: Main.rand.NextFloat(2f, 4f));
                        dust.velocity = Main.rand.NextVector2Circular(10f, 10f);
                        dust.noGravity = true;
                    }
                }
            }
        }

        private void OrbitalStrikeAttack()
        {
            // Unlocked in Phase 3
            Vector2 targetPosition = Target.Center;
            NPC.SmoothFlyNear(targetPosition, 0.05f, 0.95f);

            if (AttackTimer % 30 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Create orbital projectiles
                int orbitCount = 8 + (int)Phase * 2;
                for (int i = 0; i < orbitCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / orbitCount + AttackTimer * 0.05f;
                    Vector2 orbitPos = Target.Center + angle.ToRotationVector2() * 300f;
                    Vector2 velocity = NPC.SafeDirectionTo(orbitPos) * -12f;

                    Projectile.NewProjectile(NPC.GetSource_FromAI(), orbitPos, velocity,
                        ProjectileID.DeathLaser, 45, 5f);
                }
            }
        }

        private void TimeStopAttack()
        {
            // Unlocked in Phase 4 - most brutal attack
            if (AttackTimer == 1)
            {
                if (!Main.dedServ)
                {
                    // Extreme visual effects
                    ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 20f);
                    CameraPanSystem.PanTowards(NPC.Center, 0.6f);
                    CameraPanSystem.ZoomIn(0.5f);
                }
            }

            // Slow down player
            if (AttackTimer < 120)
            {
                Target.velocity *= 0.7f;
            }

            // Position above player
            Vector2 hoverPos = Target.Center - new Vector2(0, 400);
            NPC.SmoothFlyNear(hoverPos, 0.25f, 0.93f);

            // Massive spam of projectiles
            if (AttackTimer % 5 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 shootPos = NPC.Center + Main.rand.NextVector2Circular(100f, 100f);
                    Vector2 shootVel = NPC.SafeDirectionTo(Target.Center) * Main.rand.NextFloat(8f, 15f);

                    int projType = Main.rand.Next(new int[] {
                        ProjectileID.PhantasmalBolt,
                        ProjectileID.PhantasmalDeathray,
                        ProjectileID.CursedFlameHostile,
                        ProjectileID.ShadowBeamHostile
                    });

                    Projectile.NewProjectile(NPC.GetSource_FromAI(), shootPos, shootVel,
                        projType, 50, 5f);
                }
            }
        }

        private void SpawnVisualEffects()
        {
            if (Main.netMode != NetmodeID.Server && Main.rand.NextBool())
            {
                int dustType = Phase switch
                {
                    0 => DustID.PurpleTorch,
                    1 => DustID.BlueTorch,
                    2 => DustID.RedTorch,
                    _ => DustID.RainbowMk2
                };

                Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, dustType,
                    Scale: Main.rand.NextFloat(2f, 3.5f));
                dust.noGravity = true;
                dust.velocity = NPC.velocity * 0.4f + Main.rand.NextVector2Circular(2f, 2f);
                dust.fadeIn = 1.5f;
            }

            if (NPC.velocity.Length() > 8f && Main.netMode != NetmodeID.Server && Main.rand.NextBool(2))
            {
                Dust spark = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Electric);
                spark.noGravity = true;
                spark.velocity = Main.rand.NextVector2Circular(4f, 4f);
                spark.scale = 2f + Phase * 0.3f;
            }
        }

        private void SpawnPhaseParticles()
        {
            // Extra dramatic particles based on phase
            if (Phase >= 2 && Main.rand.NextBool(5))
            {
                Dust voidDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame);
                voidDust.noGravity = true;
                voidDust.velocity = Main.rand.NextVector2Circular(3f, 3f);
                voidDust.scale = 2.5f;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            // Faster animation with higher phase
            int frameSpeed = Math.Max(2, 6 - (int)Phase);
            NPC.frameCounter++;
            if (NPC.frameCounter >= frameSpeed)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[Type])
                    NPC.frame.Y = 0;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            // Draw longer afterimage trail based on phase
            int trailLength = Math.Min(NPCID.Sets.TrailCacheLength[Type], 15 + (int)Phase * 10);

            for (int i = trailLength - 1; i >= 0; i--)
            {
                if (NPC.oldPos[i] == Vector2.Zero)
                    continue;

                float progress = 1f - (i / (float)trailLength);

                Color afterimageColor = Phase switch
                {
                    0 => Color.Purple,
                    1 => Color.Blue,
                    2 => Color.Red,
                    _ => Color.Lerp(Color.Purple, Color.Cyan, progress)
                };

                afterimageColor.A = 0;
                afterimageColor *= progress * 0.6f;

                Vector2 drawPos = NPC.oldPos[i] + NPC.Size * 0.5f - screenPos;
                float oldRotation = NPC.oldRot[i];

                spriteBatch.Draw(texture, drawPos, NPC.frame, afterimageColor,
                    oldRotation, NPC.frame.Size() * 0.5f, NPC.scale * (0.7f + progress * 0.3f),
                    SpriteEffects.None, 0f);
            }

            // Enhanced glowing aura
            Color auraColor = Phase switch
            {
                0 => Color.Lerp(Color.Purple, Color.Blue, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f),
                1 => Color.Lerp(Color.Blue, Color.Cyan, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f),
                2 => Color.Lerp(Color.Red, Color.Orange, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.5f + 0.5f),
                _ => Color.Lerp(Color.Purple, Color.White, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 6f) * 0.5f + 0.5f)
            };
            auraColor.A = 0;

            int auraLayers = 4 + (int)Phase * 2;
            for (int i = 0; i < auraLayers; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / auraLayers).ToRotationVector2() * (4f + Phase * 2f);
                spriteBatch.Draw(texture, NPC.Center - screenPos + offset, NPC.frame, auraColor * 0.5f,
                    NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0f);
            }

            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("Luminance/Assets/GreyscaleTextures/BloomCircleSmall").Value;

            Color bloomColor = Phase switch
            {
                0 => Color.Lerp(Color.Purple, Color.Cyan, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2f) * 0.5f + 0.5f),
                1 => Color.Lerp(Color.Blue, Color.White, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f),
                2 => Color.Lerp(Color.Red, Color.Yellow, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f),
                _ => Color.White
            };
            bloomColor.A = 0;

            float bloomScale = (0.6f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.2f) * (1f + Phase * 0.3f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // Draw multiple bloom layers
            for (int i = 0; i < 2 + (int)Phase; i++)
            {
                float layerScale = bloomScale * (1f + i * 0.4f);
                spriteBatch.Draw(bloomTexture, NPC.Center - screenPos, null, bloomColor * (0.7f / (i + 1)),
                    0f, bloomTexture.Size() * 0.5f, layerScale * 2.5f, SpriteEffects.None, 0f);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            // Spawn extra particles on hit
            if (Main.netMode != NetmodeID.Server)
            {
                for (int i = 0; i < 5 + (int)Phase * 3; i++)
                {
                    int dustType = Phase switch
                    {
                        0 => DustID.PurpleTorch,
                        1 => DustID.BlueTorch,
                        2 => DustID.RedTorch,
                        _ => DustID.RainbowMk2
                    };

                    Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, dustType,
                        Scale: Main.rand.NextFloat(1.5f, 2.5f));
                    dust.velocity = Main.rand.NextVector2Circular(6f, 6f);
                    dust.noGravity = true;
                }
            }
        }

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                // Absolutely massive death explosion
                ScreenShakeSystem.StartShakeAtPoint(NPC.Center, 30f);
                CameraPanSystem.PanTowards(NPC.Center, 0.8f);
                CameraPanSystem.ZoomIn(0.8f);

                // Death particle explosion
                for (int i = 0; i < 200; i++)
                {
                    int dustType = i % 4 switch
                    {
                        0 => DustID.PurpleTorch,
                        1 => DustID.BlueTorch,
                        2 => DustID.RedTorch,
                        _ => DustID.RainbowMk2
                    };

                    Dust dust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, dustType,
                        Scale: Main.rand.NextFloat(3f, 6f));
                    dust.velocity = Main.rand.NextVector2Circular(25f, 25f);
                    dust.noGravity = true;
                    dust.fadeIn = 2f;
                }
            }

            // Final explosion of projectiles
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < 60; i++)
                {
                    float angle = MathHelper.TwoPi * i / 60f;
                    Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(5f, 20f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity,
                        ProjectileID.PhantasmalDeathray, 0, 0f);
                }
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }
    }
}