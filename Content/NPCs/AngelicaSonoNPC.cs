using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using System.Collections.Generic; // ADD THIS LINE
using Terraria.GameContent.Biomes;
using Terraria.WorldBuilding;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.Default;
using Terraria.GameContent.Personalities;

namespace broilinghell.Content.NPCs
{
    public class AngelicaSonoNPC : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // Display name of the NPC
            // DisplayName.SetDefault("Angel");

            // Automatically group with other town NPCs
            Main.npcFrameCount[Type] = 1; // Number of frames in the sprite sheet
            NPCID.Sets.ExtraFramesCount[Type] = 9;
            NPCID.Sets.AttackFrameCount[Type] = 4;
            NPCID.Sets.DangerDetectRange[Type] = 700;
            NPCID.Sets.AttackType[Type] = 0;
            NPCID.Sets.AttackTime[Type] = 90;
            NPCID.Sets.AttackAverageChance[Type] = 30;
            NPCID.Sets.HatOffsetY[Type] = 4;

            // ADD HAPPINESS SYSTEM HERE (after the existing code in SetStaticDefaults)
            NPC.Happiness
                .SetBiomeAffection<Terraria.GameContent.Personalities.ForestBiome>(AffectionLevel.Love)
                .SetBiomeAffection<Terraria.GameContent.Personalities.HallowBiome>(AffectionLevel.Hate)
                .SetBiomeAffection<Terraria.GameContent.Personalities.DesertBiome>(AffectionLevel.Dislike)
                .SetBiomeAffection<Terraria.GameContent.Personalities.UndergroundBiome>(AffectionLevel.Like)
                .SetNPCAffection(NPCID.Wizard, AffectionLevel.Love)
                .SetNPCAffection(NPCID.Princess, AffectionLevel.Like)
                .SetNPCAffection(NPCID.Demolitionist, AffectionLevel.Like)
                .SetNPCAffection(NPCID.GoblinTinkerer, AffectionLevel.Hate);
        }

        public override void SetDefaults()
        {
            // Basic NPC stats (KEEP THIS EXACTLY AS YOU HAVE IT)
            NPC.townNPC = true; // Set to false for hostile NPCs
            NPC.friendly = true; // Set to false for hostile NPCs
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = 7; // Town NPC AI style (use different values for other behaviors)
            NPC.damage = 10;
            NPC.defense = 15;
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)
        {
            // Conditions for the NPC to spawn
            // Example: spawn after defeating Eye of Cthulhu
            return NPC.downedBoss1;
        }
        public override List<string> SetNPCNameList()
        {
            return new List<string>()
            {
                "Angelica"
            };
        }

        // REPLACE YOUR EXISTING GetChat() METHOD WITH THIS:
        public override string GetChat()
        {
            {
                switch (Main.rand.Next(8))
                {
                    case 0:
                        return "What's up?";
                    case 1:
                        return "I've never really had the chance to look up at the stars...I was always looking down on them, before. It's...amazing.";
                    case 2:
                        return "You seen any interesting creatures lately?";
                    case 3:
                        return "This world is more diverse than the angels ever told me it was...";
                    case 4:
                        return "Bastards. Every last angel, a bastard... Oh, hey. Didn't notice you.";
                    case 5:
                        return "I'm quite content here. The company is... tolerable.";
                    case 6:
                        return "This place has a nice feel to it. Much better than the celestial realm.";
                    default:
                        return "Sometimes I wonder if my words are shaped by some tweenage brat...";
                }
            }
        }

        // KEEP ALL THE REST OF YOUR METHODS AS THEY ARE...
        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = "Shop";
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shop)
        {
            if (firstButton)
            {
                shop = "Shop"; // Open shop
            }
        }
        public override void AddShops()
        {
            var npcShop = new NPCShop(Type)
                .Add(ItemID.HealingPotion)
                .Add(ItemID.ManaPotion)
                .Add(ItemID.Torch)
                .Add(ItemID.GreaterHealingPotion, Condition.Hardmode); // Only in hardmode

            npcShop.Register();
        }
    }
}