using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader.Utilities;
using Terraria.Graphics.Shaders;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Terraria.Graphics.Effects; // Required for Filters
using broilinghell.Content.Projectiles;

namespace broilinghell.Content.NPCs
{
    [AutoloadBossHead]
    public class Bobling : ModNPC
    {
        private int attackTimer;
        private bool isPhase2;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.Zombie];

            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Velocity = 1f
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 64;
            NPC.height = 64;
            NPC.damage = 100;
            NPC.defense = 70;
            NPC.lifeMax = 10000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 60000f;
            NPC.knockBackResist = 0.4f;
            NPC.aiStyle = 3;
            NPC.boss = true;
            NPC.npcSlots = 10f;

            AIType = NPCID.Zombie;
            AnimationType = NPCID.Zombie;
        }

        public override void AI()
        {
            // PHASE 2 TRIGGER
            if (!isPhase2 && NPC.life < NPC.lifeMax / 2)
            {
                isPhase2 = true;
                ActivatePhase2Effects();
            }

            // Basic projectile attack
            attackTimer++;

            if (attackTimer >= (isPhase2 ? 60 : 90)) // Faster in Phase 2
            {
                attackTimer = 0;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.TargetClosest(true);

                    if (NPC.HasValidTarget)
                    {
                        Player target = Main.player[NPC.target];
                        Vector2 direction = Vector2.Normalize(target.Center - NPC.Center);

                        if (float.IsNaN(direction.X) || float.IsNaN(direction.Y))
                            direction = -Vector2.UnitY;

                        Vector2 velocity = direction * (isPhase2 ? 9f : 6f);
                        int damage = isPhase2 ? 40 : 25;
                        int type = ModContent.ProjectileType<BoblingProjectile>();

                        var source = NPC.GetSource_FromAI();
                        Projectile.NewProjectile(source, NPC.Center, velocity, type, damage, 0f, Main.myPlayer);
                    }
                }
            }
        }

        private void ActivatePhase2Effects()
        {

            // Shader effect
            if (Main.netMode != NetmodeID.Server)
            {
                Terraria.Graphics.Effects.Filters.Scene.Activate("BoblingMoonlordEffect", NPC.Center).GetShader().UseIntensity(1.5f);
            }

            // Text message (optional)
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Main.NewText("BoBling becomes enraged!", 255, 100, 180);
            }
        }

    public override void OnKill()
    {
        // Only do this on the client
        if (Main.netMode != NetmodeID.Server)
        {
            if (Terraria.Graphics.Effects.Filters.Scene["BoblingMoonlordEffect"].IsActive())
            {
                Terraria.Graphics.Effects.Filters.Scene.Deactivate("BoblingMoonlordEffect");
            }
        }
    }

    public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.LunarBar, 1, 3, 22));
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[] {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Times.NightTime,
                new FlavorTextBestiaryInfoElement("BoBling moment"),
            });
        }
    }
}