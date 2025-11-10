using Luminance;
using Luminance.Common.Easings;
using Luminance.Core.Cutscenes;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using static Terraria.Utils;

namespace broilinghell.Content.Cutscenes
{
    // After beating all 3 mechanical bosses, draedons ambience theme will start playing. Then, the screen will slowly zoom out,
    // revealing that it is on a large monitor in a lab with draedon watching. Then, with a quick black flash, it will go back to normal.
    public class DraedonPostMechsCutscene : Cutscene
    {
        #region Instance Fields/Properties
        public ManagedRenderTarget ScreenTarget
        {
            get;
            private set;
        }

        public Texture2D LabTexture
        {
            get;
            private set;
        }

        public Texture2D pixel
        {
            get;
            private set;
        }

        private int FrameCounter;

        private Rectangle Frame = new(0, 0, 100, 120);
        #endregion

        #region Static Properties
        public static int InitialWait => 150;

        public static int CRTFadeInLength => 120;

        public static int ZoomOutLength => 380;

        public static int ZoomHoldLength => 240;

        public static int ScreenFlashLength => 20;

        public static int ScreenFlashHoldLength => ScreenFlashLength / 3;
        #endregion

        #region Overrides
        public override int CutsceneLength => InitialWait + ZoomOutLength + ZoomHoldLength;

        public override BlockerSystem.BlockCondition GetBlockCondition => new(false, true, () => IsActive && Timer < CutsceneLength - ScreenFlashLength / 2);

        public override void Load()
        {
            ScreenTarget = new(true, ManagedRenderTarget.CreateScreenSizedTarget);
            LabTexture = ModContent.Request<Texture2D>("broilinghell/Content/Cutscenes/DraedonLab", AssetRequestMode.ImmediateLoad).Value;
            pixel = ModContent.Request<Texture2D>("broilinghell/pixel").Value;
        }

        public override void Unload()
        {
            LabTexture = null;
        }

    public override void Update()
        {

        }

        public override void DrawWorld(SpriteBatch spriteBatch, RenderTarget2D screen)
        {
            // Store the screen before swapping targets.
            ScreenTarget.SwapToRenderTarget();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.Draw(screen, Vector2.Zero, Color.White);
            spriteBatch.End();

            Texture2D draedon = ModContent.Request<Texture2D>("broilinghell/Content/NPCs/BoB").Value;
            Texture2D draedonGlowmask = ModContent.Request<Texture2D>("broilinghell/Content/NPCs/BoBGlow").Value;

            Vector2 screenSize = new(Main.screenWidth, Main.screenHeight);
            Vector2 labSize = LabTexture.Size();
            Vector2 labSizeCorrection = screenSize / labSize;

            Vector2 labScreenSize = new(582f, 277f);

            // The middle of the screen on the lab texture. The lab needs to shove this into the center of the screen, and zoom into it.
            Vector2 originScalar = new Vector2(1274f, 585f) / new Vector2(2559f, 1374f);

            // The ratio for the zooms. Eased out for smoothness later on.
            float zoomoutRatio = Utils.GetLerpValue(InitialWait, ZoomOutLength + InitialWait, Timer, true);

            // The scale of the screen.
            float screenScale = (float)Lerp(1f, 0.75f, EasingCurves.Sine.OutFunction(zoomoutRatio));

            // The scale of the lab.
            Vector2 initialLabScale = labSizeCorrection * (labSize / labScreenSize);
            Vector2 labScale = initialLabScale * (float)Lerp(1f, 0.745f, EasingCurves.Sine.OutFunction(zoomoutRatio));

            // Bit hacky, but restore the screen scale, make the lab really zoomed out so the window is more than visible and stop drawing
            // draedon if after the flash.
            bool drawDraedon = true;
            if (Timer >= CutsceneLength - ScreenFlashLength / 2)
            {
                drawDraedon = false;
                screenScale = 1f;
                labScale = Vector2.One * 100f;
            }

            // Swap to the screen target and begin drawing.
            spriteBatch.GraphicsDevice.SetRenderTarget(screen);

            // Restart the spritebatch and draw THE CAPTURED SCREEN FIRST
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.DepthRead, null);

            // Draw the game screen that was captured in ScreenTarget, scaled down to look like it's on the monitor
            spriteBatch.Draw(ScreenTarget, screenSize * 0.5f, null, Color.White, 0f, screenSize * 0.5f, screenScale, SpriteEffects.None, 0f);

            // THEN draw the lab background
            spriteBatch.Draw(LabTexture, screenSize * 0.5f, null, Color.White, 0f, originScalar * LabTexture.Size(), labScale, SpriteEffects.None, 0f);

            // Draw Draedon if he should be.
            if (drawDraedon)
            {
                // Make him slide into view at around the same time as the zoom starts happening, to give a weird 3D depth illusion thing that looks like hes been there the entire
                // time, just closer to the screen than his monitor.
                Vector2 draedonPosition = new(Main.screenWidth * 1.2f, Main.screenHeight * 0.875f);
                draedonPosition += -Vector2.UnitX * Main.screenWidth * 0.3f * EasingCurves.Sine.OutFunction(Utils.GetLerpValue(InitialWait, ZoomOutLength + 110, Timer, true));

                // Draw a dropshadow because I think it looks pretty.
                Vector2 dropshadowPosition = draedonPosition + Vector2.One * 20f;
                spriteBatch.Draw(draedon, dropshadowPosition, Frame, Color.Black * 0.7f, 0f, Frame.Size() * 0.5f, 5.5f, SpriteEffects.None, 0f);

                // Draw him and his glowmask.
                spriteBatch.Draw(draedon, draedonPosition, Frame, Color.Lerp(Color.White, Color.Black, 0.5f), 0f, Frame.Size() * 0.5f, 5.5f, SpriteEffects.None, 0f);
                spriteBatch.Draw(draedonGlowmask, draedonPosition, Frame, Color.White, 0f, Frame.Size() * 0.5f, 5.5f, SpriteEffects.None, 0f);
            }
            spriteBatch.End();
        }

        public override void PostDraw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // Draw a black flash if the time is correct over everything.
            float flashOpacity = Utils.GetLerpValue(CutsceneLength - ScreenFlashLength, CutsceneLength - ScreenFlashLength + ScreenFlashHoldLength, Timer, true) *
                     Utils.GetLerpValue(CutsceneLength, CutsceneLength - ScreenFlashLength + ScreenFlashHoldLength * 2, Timer, true);

            if (flashOpacity > 0f)
                spriteBatch.Draw(pixel, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * flashOpacity);

            spriteBatch.End();
        }
        #endregion
    }
}
