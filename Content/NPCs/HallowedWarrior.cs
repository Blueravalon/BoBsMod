using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.Utilities;
using Terraria.DataStructures;
using broilinghell.Systems;
using Microsoft.Xna.Framework;
using System;

namespace broilinghell.Content.NPCs
{
    public class HallowedWarrior : ModNPC
    {
        private bool tintActivated = false;
        private int projectileTimer = 0;
        private int aiTimer = 0;
        private int aiState = 0; // 0 = circling, 1 = moving to cardinal direction, 2 = firing
        private Vector2 targetPosition;
        private float circleRadius = 200f;
        private float circleAngle = 0f;
        private int cardinalDirection = 0; // 0=North, 1=East, 2=South, 3=West
        private int fireTimer = 0;
        private int burstCount = 0;
        private const int maxBursts = 10;
        private int excaliburTimer = 0; // New timer for constant Excalibur spawning

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Angelica Sono");
            Main.npcFrameCount[Type] = 4;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            { // Influences how the NPC looks in the Bestiary
                Velocity = 1f // Draws the NPC in the bestiary as if its walking +1 tiles in the x direction
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 18;
            NPC.height = 40;
            NPC.damage = 600;
            NPC.defense = 6;
            NPC.lifeMax = 82603536;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 60000f;
            NPC.knockBackResist = 1f; // Reduced knockback resistance for flying boss
            NPC.aiStyle = -1; // Custom AI
            NPC.noGravity = true; // Make it fly
            NPC.noTileCollide = true; // Can pass through tiles
        }

        public override void FindFrame(int frameHeight)
        {
            // Simple frame animation - cycles through frames over time
            NPC.frameCounter += 1.0;

            // Change frame every 10 ticks (adjust speed as needed)
            if (NPC.frameCounter >= 10.0)
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y += frameHeight;

                // Reset to first frame after reaching the last frame
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[Type])
                {
                    NPC.frame.Y = 0;
                }
            }
        }

        public override void AI()
        {
            // Activate pink tint when NPC is alive and active
            if (!tintActivated && NPC.active)
            {
                PinkTintSystem.ActivatePinkTint(0.8f);
                tintActivated = true;
            }

            //  Multiplayer-safe targeting
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(true);
                target = Main.player[NPC.target];
            }

            aiTimer++;
            excaliburTimer++;

            // Constantly spawn Excalibur projectiles
            SpawnExcaliburProjectiles(target);

            // Face the player
            NPC.spriteDirection = NPC.Center.X < target.Center.X ? 1 : -1;

            switch (aiState)
            {
                case 0: // Circling around player
                    CircleAroundPlayer(target);

                    //  Use NPC.ai[] for synced random decisions
                    if (aiTimer > NPC.ai[0])
                    {
                        aiState = 1;
                        aiTimer = 0;

                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.ai[0] = Main.rand.Next(180, 300); // next timer duration
                            NPC.ai[1] = Main.rand.Next(4);        // cardinal direction
                            NPC.netUpdate = true;
                        }

                        cardinalDirection = (int)NPC.ai[1];
                        SetCardinalTarget(target);
                    }
                    break;

                case 1: // Moving to cardinal direction
                    MoveToCardinalPosition(target);

                    if (Vector2.Distance(NPC.Center, targetPosition) < 50f || aiTimer > 120)
                    {
                        aiState = 2;
                        aiTimer = 0;
                        fireTimer = 0;
                        burstCount = 0;
                    }
                    break;

                case 2: // Firing projectiles
                    FireProjectiles(target);

                    if (burstCount >= maxBursts)
                    {
                        aiState = 0;
                        aiTimer = 0;
                        circleAngle = (float)Math.Atan2(NPC.Center.Y - target.Center.Y, NPC.Center.X - target.Center.X);
                    }
                    break;
            }
        }

        private void SpawnExcaliburProjectiles(Player target)
        {
            if (excaliburTimer % 3 == 0)
            {
                Vector2 direction = target.Center - NPC.Center;
                Vector2 velocity = direction.SafeNormalize(Vector2.UnitX) * 8f;

                //  Proper multiplayer-safe projectile spawning
                int projID = Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    velocity,
                    ProjectileID.PrincessWeapon,
                    400,
                    3f,
                    Main.netMode == NetmodeID.Server ? -1 : Main.myPlayer
                );

                if (projID >= 0)
                {
                    Main.projectile[projID].hostile = true;
                    Main.projectile[projID].friendly = false;
                    Main.projectile[projID].tileCollide = false;
                    Main.projectile[projID].timeLeft = 300;
                }
            }
        }

        private void FireProjectiles(Player target)
        {
            NPC.velocity *= 0.95f;
            fireTimer++;

            if (fireTimer % 5 == 0)
            {
                Vector2 direction = target.Center - NPC.Center;
                Vector2 velocity = direction.SafeNormalize(Vector2.UnitX) * 12f;

                //  Multiplayer-safe spawn
                int projID = Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center,
                    velocity,
                    ProjectileID.BlackBolt,
                    300,
                    2f,
                    Main.netMode == NetmodeID.Server ? -1 : Main.myPlayer
                );

                if (projID >= 0)
                {
                    Main.projectile[projID].hostile = true;
                    Main.projectile[projID].friendly = false;
                }

                burstCount++;
            }
        }


        private void MoveToCardinalPosition(Player target)
        {
            // Move towards cardinal position
            Vector2 direction = targetPosition - NPC.Center;
            float distance = direction.Length();

            if (distance > 10f)
            {
                direction = direction.SafeNormalize(Vector2.UnitX) * Math.Min(distance * 0.08f, 12f);
                NPC.velocity = direction;
            }
            else
            {
                NPC.velocity *= 0.8f;
            }
        }

        private void CircleAroundPlayer(Player target)
        {
            // Increase circle angle for smooth rotation
            circleAngle += 0.04f; // Adjust speed of circling

            // Calculate desired position around player
            Vector2 desiredPosition = target.Center + new Vector2(
                (float)Math.Cos(circleAngle) * circleRadius,
                (float)Math.Sin(circleAngle) * circleRadius
            );

            // Move towards desired position
            Vector2 direction = desiredPosition - NPC.Center;
            float distance = direction.Length();

            if (distance > 10f)
            {
                direction = direction.SafeNormalize(Vector2.UnitX) * Math.Min(distance * 0.1f, 8f);
                NPC.velocity = direction;
            }
            else
            {
                NPC.velocity *= 0.9f;
            }
        }

        private void SetCardinalTarget(Player target)
        {
            float offset = 250f; // Distance from player in cardinal direction

            switch (cardinalDirection)
            {
                case 0: // North
                    targetPosition = target.Center + new Vector2(0, -offset);
                    break;
                case 1: // East
                    targetPosition = target.Center + new Vector2(offset, 0);
                    break;
                case 2: // South
                    targetPosition = target.Center + new Vector2(0, offset);
                    break;
                case 3: // West
                    targetPosition = target.Center + new Vector2(-offset, 0);
                    break;
            }
        }

        public override void OnKill()
        {
            // Deactivate pink tint when NPC is killed
            if (tintActivated)
            {
                PinkTintSystem.DeactivatePinkTint();
                tintActivated = false;
            }
        }

        public override bool CheckDead()
        {
            if (Main.netMode != NetmodeID.Server && tintActivated)
            {
                PinkTintSystem.DeactivatePinkTint();
                tintActivated = false;
            }
            return true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // Activate pink tint when NPC spawns
            PinkTintSystem.ActivatePinkTint(0.8f); // Adjust intensity as needed
            tintActivated = true;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            // We can use AddRange instead of calling Add multiple times in order to add multiple items at once
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                // Sets the spawning conditions of this NPC that is listed in the bestiary.
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                // Sets the description of this NPC that is listed in the bestiary.
                new FlavorTextBestiaryInfoElement("An angel, banished from Heaven's eternal rave party for being 'too much of a prude'. She now lay in the earthly planes, defending all that is holy and destroying all that is dark."),
            });
        }
    }
}