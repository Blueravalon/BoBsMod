using Terraria;
using System;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace broilinghell.Systems
{
    public class GreenTintSystem : ModSystem
    {
        public override void PostSetupContent()
        {
            if (!Main.dedServ)
            {
                try
                {
                    // Load the shader effect - keeping original filename
                    var asset = Mod.Assets.Request<Effect>("Assets/Effects/YourShader", ReLogic.Content.AssetRequestMode.ImmediateLoad);
                    if (asset?.Value == null)
                    {
                        Mod.Logger.Error("Shader asset loaded but value was null.");
                        return;
                    }

                    // Create a custom screen shader data - use the correct pass name from the shader
                    var shaderData = new ScreenShaderData(new Ref<Effect>(asset.Value), "FilterMyShader");

                    // Remove the UseImage1 line since your shader uses uImage0 for the main screen

                    // Register as a scene filter (for screen-wide effects)
                    Filters.Scene["GreenTintSystem"] = new Filter(shaderData, EffectPriority.High);
                    Mod.Logger.Info("GreenTint shader registered successfully as scene filter.");
                }
                catch (Exception ex)
                {
                    Mod.Logger.Error("Failed to load GreenTint shader: " + ex.Message);
                }
            }
        }

        // Method to activate the shader
        public static void ActivateGreenTint(float intensity = 1f)
        {
            if (!Main.dedServ && Filters.Scene["GreenTintSystem"] != null)
            {
                Filters.Scene.Activate("GreenTintSystem").GetShader().UseIntensity(intensity);
            }
        }

        // Method to deactivate the shader
        public static void DeactivateGreenTint()
        {
            if (!Main.dedServ && Filters.Scene["GreenTintSystem"] != null)
            {
                Filters.Scene.Deactivate("GreenTintSystem");
            }
        }
    }
}