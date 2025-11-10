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

namespace broilinghell.Content.Items
{ 
	// This is a basic item template.
	// Please see tModLoader's ExampleMod for every other example:
	// https://github.com/tModLoader/tModLoader/tree/stable/ExampleMod
	public class bobsword : ModItem
	{
		// The Display Name and Tooltip of this item can be edited in the 'Localization/en-US_Mods.broilinghell.hjson' file.
		public override void SetDefaults()
		{
			Item.damage = 5200;
			Item.DamageType = DamageClass.Melee;
			Item.width = 80;
			Item.height = 80;
			Item.useTime = 10;
			Item.useAnimation = 10;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.knockBack = 0;
			Item.value = Item.buyPrice(silver: 1);
			Item.rare = ItemRarityID.Cyan;
			Item.UseSound = SoundID.Item1;
			Item.autoReuse = true;
		}
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var heatTooltip = new TooltipLine(Mod, "memes",
                "sendin memes in general be like:")
            {
                OverrideColor = Color.Lerp(Color.Lime, Color.Cyan,
                    0.5f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 0.5f)
            };
            tooltips.Add(heatTooltip);
        }
    }
}
