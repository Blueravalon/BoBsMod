using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace broilinghell.Content
{
    public class BlackBackgroundSystem : ModSystem
    {
        public override void Load()
        {
            On_Main.DrawBackground += BlockBackground;
        }

        private void BlockBackground(On_Main.orig_DrawBackground orig, Main self)
        {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.BoB>()))
            {
                // Draw a full-screen black rectangle over the background
                Main.spriteBatch.Draw(
                    Terraria.GameContent.TextureAssets.MagicPixel.Value, // 1x1 white pixel texture
                    new Rectangle(0, 0, Main.screenWidth + 200, Main.screenHeight + 200),
                    Color.Black
                );
                return; // skip vanilla background drawing
            }

            // Otherwise draw vanilla background
            orig(self);
        }
    }
}