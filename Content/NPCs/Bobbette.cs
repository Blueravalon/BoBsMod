using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.Utilities;
using Terraria.Graphics.Shaders;
using Terraria.DataStructures;

namespace broilinghell.Content.NPCs
{
    public class Bobbette : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 3; // Total number of frames in the sprite sheet
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.damage = 15;
            NPC.defense = 5;
            NPC.lifeMax = 3000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 50f;
            NPC.knockBackResist = 0.5f;
            NPC.aiStyle = NPCAIStyleID.Fighter; // Basic enemy AI that walks around
        }

        public override void FindFrame(int frameHeight)
        {
            // Check if the NPC is moving horizontally
            bool isMoving = NPC.velocity.X != 0f;

            if (!isMoving)
            {
                // Use frame 0 (first frame) when standing still
                NPC.frame.Y = 0 * frameHeight;
            }
            else
            {
                // Animate through frames 1 and 2 (second and third frames) when moving
                NPC.frameCounter++;

                if (NPC.frameCounter >= 8) // Adjust this value to change animation speed
                {
                    NPC.frameCounter = 0;
                    NPC.frame.Y += frameHeight;

                    // Loop between frames 1 and 2
                    if (NPC.frame.Y >= 3 * frameHeight || NPC.frame.Y < 1 * frameHeight)
                    {
                        NPC.frame.Y = 1 * frameHeight;
                    }
                }
            }
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // Only spawn in Hardmode
            if (!Main.hardMode)
            {
                return 0f;
            }

            // Example spawn conditions - adjust as needed
            return SpawnCondition.OverworldDaySlime.Chance;
        }
    }
}