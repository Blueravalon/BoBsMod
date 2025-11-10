using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ReLogic.Content;

namespace broilinghell.Content.NPCs
{
    public class bobultima : ModNPC
    {

        public override void SetDefaults()
        {
            NPC.width = 48;
            NPC.height = 48;
            NPC.boss = true;
            NPC.BossBar = ModContent.GetInstance<BobUltimaBossBar>();

            // Harmless & invulnerable
            NPC.damage = 0;
            NPC.dontTakeDamage = true;
            NPC.chaseable = false;        // Homing/minions won't target it
            NPC.friendly = true;          // Not hostile

            // Hovering puppet vibe
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.knockBackResist = 0f;
            NPC.aiStyle = -1;
            NPC.lifeMax = 27534512;
            NPC.defense = 0;
            NPC.npcSlots = 100f;          // Prevent other mobs from spawning
            NPC.netAlways = true;
            NPC.lavaImmune = true;

            // This makes it draw behind tiles
            NPC.behindTiles = true;
        }

        public override void AI()
        {
            int leftType = ModContent.NPCType<handleft>();
            int rightType = ModContent.NPCType<handright>();

            bool leftAlive = NPC.AnyNPCs(leftType);
            bool rightAlive = NPC.AnyNPCs(rightType);

            // If both are dead, kill the body too
            if (!leftAlive && !rightAlive)
            {
                if (Main.netMode != Terraria.ID.NetmodeID.MultiplayerClient)
                {
                    NPC.StrikeInstantKill(); // handles effects + loot + network sync
                }
            }
            // Keep it from despawning naturally
            NPC.timeLeft = 10;

            // Track closest active player (or you can store a specific player in ai[0] when spawning)
            NPC.TargetClosest(false);
            Player player = Main.player[NPC.target];

            if (!player.active || player.dead)
            {
                NPC.velocity *= 0.9f;
                return;
            }

            // Offset directly "behind" the player's facing direction
            float xOffset = 0f;  // horizontal distance behind the player
            float yOffset = 0f;   // vertical offset (adjust if you want it higher/lower)
            Vector2 desiredPos = player.Center + new Vector2(-player.direction * xOffset, yOffset);

            // Smooth hover movement
            float speed = 100f;
            float inertia = 18f;
            Vector2 toDest = desiredPos - NPC.Center;

            if (toDest.Length() > speed)
            {
                toDest.Normalize();
                toDest *= speed;
            }

            NPC.velocity = (NPC.velocity * (inertia - 1f) + toDest) / inertia;

            // Optional: face the same way as player
            NPC.direction = player.direction;
            NPC.spriteDirection = NPC.direction;
        }


        // Ensure it never damages the player on contact
        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;
        public override bool? CanBeHitByItem(Player player, Item item) => false;
        public override bool? CanBeHitByProjectile(Projectile projectile) => false;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Don't draw the NPC normally
            return false;
        }

        // Draw it manually in PostDraw at a much lower layer
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2 drawPos = NPC.Center - screenPos;

            // Use ModContent instead of TextureAssets
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            spriteBatch.Draw(texture, drawPos, NPC.frame, drawColor,
                NPC.rotation, NPC.frame.Size() / 2f, NPC.scale, SpriteEffects.None, 0f);
        }
    }
}