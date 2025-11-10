using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Luminance.Common.Utilities;
using Luminance.Core.Graphics;
using static Luminance.Common.Utilities.Utilities;
using static System.MathF;

namespace broilinghell.Content.NPCs
{
    [AutoloadBossHead]
    public class babybob : ModNPC
    {
        // AI states
        private enum AIState
        {
            Spawn,
            DankMemeBarrage,
            MLGTeleport,
            AirHornSpin,
            ShreksWrath,
            RickRollRain,
            NyanCatDash,
            DoritusTornado,
            MountainDewFlood,
            Exhausted
        }

        private AIState State
        {
            get => (AIState)NPC.ai[0];
            set => NPC.ai[0] = (float)value;
        }

        private ref float AttackTimer => ref NPC.ai[1];
        private ref float AttackPhase => ref NPC.ai[2];
        private ref float TeleportCounter => ref NPC.ai[3];

        private Player Target => Main.player[NPC.target];

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;

            NPCID.Sets.TrailCacheLength[Type] = 20;
            NPCID.Sets.TrailingMode[Type] = 3;

            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
        }

        public override void SetDefaults()
        {
            NPC.width = 120;
            NPC.height = 120;
            NPC.damage = 150;
            NPC.defense = 80;
            NPC.lifeMax = 200000000; // Over 2 billion HP
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = Item.buyPrice(platinum: 50);
            NPC.SpawnWithHigherTime(30);
            NPC.boss = true;
            NPC.npcSlots = 15f;
            NPC.aiStyle = -1;

            if (!Main.dedServ)
            {
                Music = MusicLoader.GetMusicSlot(Mod, "Assets/Music/BabyBob");
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(AttackTimer);
            writer.Write(AttackPhase);
            writer.Write(TeleportCounter);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            AttackTimer = reader.ReadSingle();
            AttackPhase = reader.ReadSingle();
            TeleportCounter = reader.ReadSingle();
        }

        public override void AI()
        {
            // Target selection
            if (NPC.target < 0 || NPC.target == 255 || Target.dead || !Target.active)
                NPC.TargetClosest();

            // Despawn if no valid target
            if (Target.dead || !Target.active)
            {
                NPC.velocity.Y -= 0.5f;
                if (NPC.timeLeft > 10)
                    NPC.timeLeft = 10;
                return;
            }

            AttackTimer++;

            switch (State)
            {
                case AIState.Spawn:
                    DoSpawnAnimation();
                    break;
                case AIState.DankMemeBarrage:
                    DoDankMemeBarrage();
                    break;
                case AIState.MLGTeleport:
                    DoMLGTeleport();
                    break;
                case AIState.AirHornSpin:
                    DoAirHornSpin();
                    break;
                case AIState.ShreksWrath:
                    DoShreksWrath();
                    break;
                case AIState.RickRollRain:
                    DoRickRollRain();
                    break;
                case AIState.NyanCatDash:
                    DoNyanCatDash();
                    break;
                case AIState.DoritusTornado:
                    DoDoritusTornado();
                    break;
                case AIState.MountainDewFlood:
                    DoMountainDewFlood();
                    break;
                case AIState.Exhausted:
                    DoExhausted();
                    break;
            }

            // Screen effects
            if (!Main.dedServ)
            {
                float intensity = Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5f + 0.5f;
                ScreenShakeSystem.StartShake(intensity * 2f);
            }
        }

        private void DoSpawnAnimation()
        {
            NPC.velocity *= 0.95f;

            if (AttackTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
            }

            if (AttackTimer > 120)
            {
                SelectNextAttack();
            }
        }

        private void DoDankMemeBarrage()
        {
            // Hover around player
            Vector2 targetPos = Target.Center + new Vector2(0, -300);
            NPC.velocity = (targetPos - NPC.Center) * 0.05f;

            if (AttackTimer % 30 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item1, NPC.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 velocity = (MathF.PI * i / 8f).ToRotationVector2() * 8f;
                        int proj = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity,
                            ProjectileID.CursedFlameHostile, 40, 0f);
                        Main.projectile[proj].tileCollide = false;
                    }
                }
            }

            if (AttackTimer > 300)
            {
                SelectNextAttack();
            }
        }

        private void DoMLGTeleport()
        {
            if (AttackTimer % 60 == 0)
            {
                // Teleport to random position near player
                Vector2 teleportPos = Target.Center + Main.rand.NextVector2Circular(400, 400);
                NPC.Center = teleportPos;
                NPC.velocity = Vector2.Zero;

                SoundEngine.PlaySound(SoundID.Item8, NPC.Center);

                // Screen flash
                if (!Main.dedServ)
                {
                    ScreenShakeSystem.StartShake(8f);
                }

                // Spawn projectiles on teleport
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 velocity = (MathF.PI * i / 12f).ToRotationVector2() * 10f;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity,
                            ProjectileID.ShadowBeamHostile, 50, 0f);
                    }
                }

                TeleportCounter++;
            }

            if (TeleportCounter >= 6)
            {
                TeleportCounter = 0;
                SelectNextAttack();
            }
        }

        private void DoAirHornSpin()
        {
            NPC.velocity = NPC.velocity.RotatedBy(0.1f);

            if (AttackTimer % 5 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item62, NPC.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 velocity = (AttackTimer * 0.1f).ToRotationVector2() * 12f;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity,
                        ProjectileID.PhantasmalBolt, 45, 0f);
                }
            }

            if (AttackTimer > 240)
            {
                SelectNextAttack();
            }
        }

        private void DoShreksWrath()
        {
            // Move toward player aggressively
            Vector2 direction = NPC.SafeDirectionTo(Target.Center);
            NPC.velocity = Vector2.Lerp(NPC.velocity, direction * 15f, 0.08f);

            if (AttackTimer % 20 == 0)
            {
                SoundEngine.PlaySound(SoundID.Roar, NPC.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // Spiral of doom
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = MathF.PI * i / 5f + AttackTimer * 0.1f;
                        Vector2 velocity = angle.ToRotationVector2() * 6f;
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity,
                            ProjectileID.CursedFlameHostile, 55, 0f);
                    }
                }
            }

            if (AttackTimer > 360)
            {
                SelectNextAttack();
            }
        }

        private void DoRickRollRain()
        {
            // Float above player
            Vector2 targetPos = Target.Center + new Vector2(0, -400);
            NPC.SmoothFlyNear(targetPos, 0.1f, 0.85f);

            if (AttackTimer % 10 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item9, NPC.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPos = NPC.Center + new Vector2(Main.rand.NextFloat(-300, 300), -50);
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-2f, 2f), 12f);
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, velocity,
                        ProjectileID.LostSoulHostile, 35, 0f);
                }
            }

            if (AttackTimer > 400)
            {
                SelectNextAttack();
            }
        }

        private void DoNyanCatDash()
        {
            if (AttackTimer % 90 == 1)
            {
                // Charge at player
                NPC.velocity = NPC.SafeDirectionTo(Target.Center) * 25f;
                SoundEngine.PlaySound(SoundID.Item74, NPC.Center);

                if (!Main.dedServ)
                {
                    ScreenShakeSystem.StartShake(10f);
                }
            }
            else if (AttackTimer % 90 > 1 && AttackTimer % 90 < 40)
            {
                // Leave trail
                if (Main.netMode != NetmodeID.MultiplayerClient && AttackTimer % 3 == 0)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
                        ProjectileID.RainbowCrystalExplosion, 30, 0f);
                }
            }
            else
            {
                NPC.velocity *= 0.95f;
            }

            if (AttackTimer > 450)
            {
                SelectNextAttack();
            }
        }

        private void DoDoritusTornado()
        {
            NPC.velocity *= 0.98f;

            if (AttackTimer % 3 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item8, NPC.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float spiralAngle = AttackTimer * 0.3f;
                    float spiralRadius = AttackTimer * 2f % 400f;

                    Vector2 offset = spiralAngle.ToRotationVector2() * spiralRadius;
                    Vector2 spawnPos = NPC.Center + offset;
                    Vector2 velocity = (spawnPos - NPC.Center).SafeNormalize(Vector2.Zero).RotatedBy(MathF.PI * 0.5f) * 8f;

                    Projectile.NewProjectile(NPC.GetSource_FromAI(), spawnPos, velocity,
                        ProjectileID.MartianTurretBolt, 40, 0f);
                }
            }

            if (AttackTimer > 300)
            {
                SelectNextAttack();
            }
        }

        private void DoMountainDewFlood()
        {
            // Descend toward player
            Vector2 targetPos = Target.Center + new Vector2(0, -200);
            NPC.SmoothFlyNear(targetPos, 0.08f, 0.9f);

            if (AttackTimer % 2 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(5f, 10f));
                    int proj = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, velocity,
                        ProjectileID.CursedDartFlame, 25, 0f);
                    Main.projectile[proj].tileCollide = true;
                }
            }

            if (AttackTimer > 350)
            {
                SelectNextAttack();
            }
        }

        private void DoExhausted()
        {
            NPC.velocity *= 0.95f;

            if (AttackTimer > 120)
            {
                SelectNextAttack();
            }
        }

        private void SelectNextAttack()
        {
            AttackTimer = 0;

            // Cycle through attacks
            int nextState = (int)State + 1;
            if (nextState > (int)AIState.Exhausted)
                nextState = (int)AIState.DankMemeBarrage;

            State = (AIState)nextState;

            NPC.netUpdate = true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Draw afterimages
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            for (int i = 0; i < NPC.oldPos.Length; i++)
            {
                float progress = 1f - (i / (float)NPC.oldPos.Length);
                Vector2 drawPos = NPC.oldPos[i] + NPC.Size * 0.5f - screenPos;
                Color color = Color.Purple * progress * 0.5f;

                spriteBatch.Draw(texture, drawPos, NPC.frame, color, NPC.rotation, NPC.frame.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0f);
            }

            return true;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Add your loot here
        }
    }
}