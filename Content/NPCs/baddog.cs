using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.Utilities;

namespace broilinghell.Content.NPCs
{
    public class baddog : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.width = 64;
            NPC.height = 47;
            NPC.damage = 20;
            NPC.defense = 7;
            NPC.lifeMax = 80;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 60f;
            NPC.knockBackResist = 1f;
            NPC.aiStyle = 26; // unicorn AI
            NPC.noGravity = false;
            NPC.noTileCollide = false;
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

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.FurGuard>(), 100));
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            return SpawnCondition.OverworldNightMonster.Chance * 0.1f;
        }

    }
}
