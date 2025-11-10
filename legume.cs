using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace broilinghell
{
    public class legume : ModMenu
    {
        public override string DisplayName => "BoB's Mod: In Hell";
        public override Asset<Texture2D> Logo => ModContent.Request<Texture2D>("broilinghell/Assets/Logo");
        public override Asset<Texture2D> SunTexture => ModContent.Request<Texture2D>("broilinghell/Assets/invis");
        public override Asset<Texture2D> MoonTexture => ModContent.Request<Texture2D>("broilinghell/Assets/invis");
        public override int Music => MusicLoader.GetMusicSlot(Mod, "Assets/Music/hellfire");

        public override void OnSelected()
        {
            SoundEngine.PlaySound(SoundID.MenuOpen);
        }

        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
        {
            // Draw a black overlay over the entire screen BEFORE the logo
            Texture2D blackTexture = ModContent.Request<Texture2D>("broilinghell/bobground2").Value;
            spriteBatch.Draw(blackTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);

            // Return true to continue with normal logo drawing
            return true;
        }
    }
}
