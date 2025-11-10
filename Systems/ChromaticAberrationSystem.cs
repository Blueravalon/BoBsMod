using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace broilinghell
{
    public class ChromaticAberrationSystem : ModSystem
    {
        public static bool AberrationActive { get; private set; }
        public static float AberrationStrength { get; set; } = 1.0f;
        public static Vector2 AberrationDirection { get; set; } = new Vector2(1, 0);

        public override void Load()
        {
            if (Main.netMode != Terraria.ID.NetmodeID.Server)
            {
                Ref<Effect> aberrationShader = new Ref<Effect>(Mod.Assets.Request<Effect>("Assets/Effects/ChromaticAberration", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value);

                // Register all three passes
                Filters.Scene["broilinghell:ChromaticAberration"] = new Filter(new ScreenShaderData(aberrationShader, "ChromaticAberrationPass"), EffectPriority.VeryHigh);
                Filters.Scene["broilinghell:ChromaticAberrationRadial"] = new Filter(new ScreenShaderData(aberrationShader, "ChromaticAberrationRadialPass"), EffectPriority.VeryHigh);
                Filters.Scene["broilinghell:ChromaticAberrationPulse"] = new Filter(new ScreenShaderData(aberrationShader, "ChromaticAberrationPulsePass"), EffectPriority.VeryHigh);
            }
        }

        public override void Unload()
        {
            AberrationActive = false;
        }

        public override void PostUpdateEverything()
        {
            if (AberrationActive && Main.netMode != Terraria.ID.NetmodeID.Server)
            {
                UpdateShaderParameters("broilinghell:ChromaticAberration");
                UpdateShaderParameters("broilinghell:ChromaticAberrationRadial");
                UpdateShaderParameters("broilinghell:ChromaticAberrationPulse");
            }
        }

        private void UpdateShaderParameters(string filterName)
        {
            if (Filters.Scene[filterName] != null && Filters.Scene[filterName].IsActive())
            {
                var shader = Filters.Scene[filterName].GetShader();
                shader.Shader.Parameters["uAberrationStrength"]?.SetValue(AberrationStrength);
                shader.Shader.Parameters["uAberrationDirection"]?.SetValue(AberrationDirection);
            }
        }

        /// <summary>
        /// Activates directional chromatic aberration
        /// </summary>
        public static void ActivateAberration(Vector2 position, float strength = 1.0f, Vector2? direction = null)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            AberrationStrength = strength;
            AberrationDirection = direction ?? new Vector2(1, 0);

            if (!Filters.Scene["broilinghell:ChromaticAberration"].IsActive())
            {
                Filters.Scene.Activate("broilinghell:ChromaticAberration", position);
            }

            AberrationActive = true;
        }

        /// <summary>
        /// Activates radial chromatic aberration (from center outward)
        /// </summary>
        public static void ActivateAberrationRadial(Vector2 position, float strength = 1.5f)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            AberrationStrength = strength;

            if (!Filters.Scene["broilinghell:ChromaticAberrationRadial"].IsActive())
            {
                Filters.Scene.Activate("broilinghell:ChromaticAberrationRadial", position);
            }

            AberrationActive = true;
        }

        /// <summary>
        /// Activates pulsing radial chromatic aberration
        /// </summary>
        public static void ActivateAberrationPulse(Vector2 position, float strength = 2.0f)
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            AberrationStrength = strength;

            if (!Filters.Scene["broilinghell:ChromaticAberrationPulse"].IsActive())
            {
                Filters.Scene.Activate("broilinghell:ChromaticAberrationPulse", position);
            }

            AberrationActive = true;
        }

        /// <summary>
        /// Deactivates all chromatic aberration effects
        /// </summary>
        public static void DeactivateAberration()
        {
            if (Main.netMode == Terraria.ID.NetmodeID.Server) return;

            if (Filters.Scene["broilinghell:ChromaticAberration"].IsActive())
            {
                Filters.Scene.Deactivate("broilinghell:ChromaticAberration");
            }

            if (Filters.Scene["broilinghell:ChromaticAberrationRadial"].IsActive())
            {
                Filters.Scene.Deactivate("broilinghell:ChromaticAberrationRadial");
            }

            if (Filters.Scene["broilinghell:ChromaticAberrationPulse"].IsActive())
            {
                Filters.Scene.Deactivate("broilinghell:ChromaticAberrationPulse");
            }

            AberrationActive = false;
        }
    }
}