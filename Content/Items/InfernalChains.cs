using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using broilinghell.Content.Projectiles;
using broilinghell.Content;

namespace broilinghell.Content.Items
{
    public class InfernalChains : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<BoBChain>(), 69, 2, 10, 35);
            Item.rare = ItemRarityID.Pink;
            Item.value = 100000;
            Item.damage = 5200;
            Item.DamageType = DamageClass.Summon;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var heatTooltip = new TooltipLine(Mod, "shart",
                "i sharted meself")
            {
                OverrideColor = Color.Lerp(Color.Lime, Color.Cyan,
                    0.5f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5f)
            };
            tooltips.Add(heatTooltip);
        }
    }
}