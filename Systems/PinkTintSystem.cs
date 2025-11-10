using Terraria;
using System;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace broilinghell.Systems
{
    public class PinkTintSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            if (!Main.dedServ)
            {
                try
                {
                    Mod.Logger.Info("Attempting to load pinkShader...");
                    var asset = Mod.Assets.Request<Effect>("Assets/Effects/pinkShader", ReLogic.Content.AssetRequestMode.ImmediateLoad);

                    if (asset?.Value == null)
                    {
                        Mod.Logger.Error("Shader asset loaded but value was null.");
                        return;
                    }

                    Mod.Logger.Info("Shader asset loaded successfully, creating ScreenShaderData...");

                    // Use the correct pass name from the shader
                    var shaderData = new ScreenShaderData(new Ref<Effect>(asset.Value), "FilterPinkShader");
                    Filters.Scene["PinkTintSystem"] = new Filter(shaderData, EffectPriority.High);

                    Mod.Logger.Info("pinkShader shader registered successfully as scene filter.");
                }
                catch (Exception ex)
                {
                    Mod.Logger.Error("Failed to load pinkShader shader: " + ex.Message);
                    Mod.Logger.Error("Stack trace: " + ex.StackTrace);
                }
            }
        }

        // Method to activate the shader
        public static void ActivatePinkTint(float intensity = 1f)
        {
            if (!Main.dedServ)
            {
                try
                {
                    if (Filters.Scene["PinkTintSystem"] != null)
                    {
                        Main.NewText("Activating pink tint with intensity: " + intensity, Color.Pink);
                        var filter = Filters.Scene.Activate("PinkTintSystem");
                        if (filter != null)
                        {
                            filter.GetShader().UseIntensity(intensity);
                        }
                    }
                    else
                    {
                        Main.NewText("PinkTintSystem filter is null!", Color.Red);
                    }
                }
                catch (Exception ex)
                {
                    Main.NewText("Error activating pink tint: " + ex.Message, Color.Red);
                }
            }
        }

        // Method to deactivate the shader
        public static void DeactivatePinkTint()
        {
            if (!Main.dedServ)
            {
                try
                {
                    if (Filters.Scene["PinkTintSystem"] != null)
                    {
                        Main.NewText("Deactivating pink tint", Color.White);
                        Filters.Scene.Deactivate("PinkTintSystem");
                    }
                    else
                    {
                        Main.NewText("PinkTintSystem filter is null during deactivation!", Color.Red);
                    }
                }
                catch (Exception ex)
                {
                    Main.NewText("Error deactivating pink tint: " + ex.Message, Color.Red);
                }
            }
        }
    }
}