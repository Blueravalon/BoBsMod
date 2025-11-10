using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.Utilities;
using Terraria.Graphics.Shaders;
using Terraria.DataStructures;
using broilinghell.Content.Projectiles;

namespace broilinghell.Content.NPCs
{
    public class HolyButterfly : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // Display name for the NPC
            // DisplayName.SetDefault("Holy Butterfly");

            // Main determines the NPC's behavior type
            Main.npcFrameCount[NPC.type] = 2; // Number of animation frames
        }

        public override void SetDefaults()
        {
            // NPC dimensions
            NPC.width = 70;
            NPC.height = 64;

            // Health and damage
            NPC.damage = 20;
            NPC.defense = 5;
            NPC.lifeMax = 300;

            // Movement properties
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 50f; // Coin value when killed

            // AI and behavior
            NPC.knockBackResist = 0.3f;
            NPC.aiStyle = -1; // Custom AI (we'll define our own)

            // Flying properties
            NPC.noGravity = true; // Allows flying
            NPC.noTileCollide = true; // Can pass through tiles if set to true
            NPC.damage = 20;
        }

        public override void FindFrame(int frameHeight)
        {
            // Simple 2-frame animation that swaps every 10 frames
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 10.0)
            {
                NPC.frameCounter = 0.0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * 2)
                    NPC.frame.Y = 0;
            }
        }

        public override void AI()
        {
            // Find the closest player
            Player target = Main.player[NPC.target];

            // Check if target is valid and active
            if (!target.active || target.dead)
            {
                NPC.TargetClosest(false);
                target = Main.player[NPC.target];

                // If no valid target, float in place
                if (!target.active || target.dead)
                {
                    NPC.velocity.Y -= 0.1f; // Slowly float upward
                    if (NPC.velocity.Y < -2f)
                        NPC.velocity.Y = -2f;
                    return;
                }
            }

            // Calculate direction to player
            Vector2 direction = target.Center - NPC.Center;
            direction.Normalize();

            // Movement speed
            float speed = 2f;

            // Move towards player
            NPC.velocity = direction * speed;

            // Face the player
            if (direction.X > 0)
                NPC.spriteDirection = 1;
            else
                NPC.spriteDirection = -1;

            // Optional: Add some floating behavior
            NPC.ai[0] += 1f;
            if (NPC.ai[0] >= 60f) // Every 60 frames (1 second)
            {
                NPC.ai[0] = 0f;
                // Add slight random movement for more natural flying
                NPC.velocity.Y += Main.rand.NextFloat(-0.5f, 0.5f);
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // Define where this NPC can spawn
            // This example allows spawning on surface during day/night
            if (spawnInfo.Player.ZoneOverworldHeight)
                return 0.1f; // 10% of normal spawn rate

            return 0f; // Don't spawn elsewhere
        }
    }
}