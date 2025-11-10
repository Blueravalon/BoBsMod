using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ReLogic.Content;

namespace broilinghell.Content.NPCs
{
    public class GodBoB : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.width = 48;
            NPC.height = 48;
            NPC.boss = true;
            NPC.BossBar = ModContent.GetInstance<BobUltimaBossBar>();
            NPC.chaseable = true;
            NPC.friendly = false;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
            NPC.lifeMax = 8603536;
            NPC.defense = 0;
            NPC.npcSlots = 100f;
            NPC.netAlways = true;
            NPC.lavaImmune = true;
            NPC.behindTiles = true;
        }

        public override void AI()
        {
            // Find the center point of all active players
            Vector2 centerPoint = Vector2.Zero;
            int activePlayerCount = 0;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p.active && !p.dead)
                {
                    centerPoint += p.Center;
                    activePlayerCount++;
                }
            }

            // If no active players, despawn
            if (activePlayerCount == 0)
            {
                NPC.velocity *= 0.9f;
                return;
            }

            // Calculate average position (center of all players)
            centerPoint /= activePlayerCount;

            // Positioning logic
            float yOffset = -200f; // Hover above the center point
            Vector2 desiredPos = centerPoint + new Vector2(0f, yOffset);

            float speed = 100f;
            float inertia = 1f;

            Vector2 toDest = desiredPos - NPC.Center;
            if (toDest.Length() > speed)
            {
                toDest.Normalize();
                toDest *= speed;
            }

            NPC.velocity = (NPC.velocity * (inertia - 1f) + toDest) / inertia;
        }
    }
}