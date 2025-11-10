using Terraria;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Microsoft.Xna.Framework;

namespace broilinghell.Systems // match your mod's namespace!
{
    public class broilinghellsystem : ModSystem
    {
        public override void Load()
        {
            if (!Main.dedServ) // Don't run this on servers
            {
                // Register the filter that mimics the Moon Lord effect
                Filters.Scene["BoblingMoonlordEffect"] = new Filter(
                    new ScreenShaderData("FilterMoonLord").UseColor(1f, 0f, 0f).UseOpacity(1f),
                    EffectPriority.High);

                Filters.Scene["BoblingMoonlordEffect"].Load();
            }
        }
    }
}