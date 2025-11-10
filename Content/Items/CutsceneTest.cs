using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Luminance.Core.Cutscenes;
using broilinghell.Content.Cutscenes;

namespace broilinghell.Content.Items
{
    public class CutsceneTest : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = 20;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.buyPrice(gold: 10);
            Item.consumable = true;

            Item.UseSound = SoundID.DD2_DefeatScene;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer == player.whoAmI)
            {
                var cutscene = ModContent.GetInstance<DraedonPostMechsCutscene>();
                CutsceneManager.QueueCutscene(cutscene);
            }
            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SoulofNight, 3)
                .AddIngredient(ItemID.LunarBar, 1)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }
}
