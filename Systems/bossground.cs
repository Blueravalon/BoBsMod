using Terraria;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using broilinghell.Content.NPCs;

public class bossground : ModSystem
{
    private static Texture2D customBackground;

    public override void PostSetupContent()
    {
        // Load your custom background texture
        customBackground = ModContent.Request<Texture2D>("broilinghell/blackbackground").Value;
    }

    public override void ModifyScreenPosition()
    {
        // Check if either BoB or bobultima is alive
        bool bobActive = NPC.AnyNPCs(ModContent.NPCType<BoB>());
        bool bobUltimaActive = NPC.AnyNPCs(ModContent.NPCType<bobultima>());

        if (bobActive || bobUltimaActive)
        {
            // Draw custom background
            DrawCustomBackground();
        }
    }

    private void DrawCustomBackground()
    {
        Main.spriteBatch.Begin();
        // Draw the background scaled to screen size
        Rectangle screenRect = new Rectangle(1, 1, Main.screenWidth, Main.screenHeight);
        Main.spriteBatch.Draw(customBackground, screenRect, Color.White);
        Main.spriteBatch.End();
    }
}
