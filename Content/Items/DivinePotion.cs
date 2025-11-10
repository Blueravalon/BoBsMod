using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace broilinghell.Content.Items
{
    public class DivinePotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 5;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useTime = 17;
            Item.useAnimation = 17;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.UseSound = SoundID.Item3; // Potion drinking sound
            Item.useTurn = true;
            Item.maxStack = 30;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.buyPrice(silver: 50);

            Item.consumable = true;
            Item.buffType = ModContent.BuffType<Buffs.HealthBuff>(); // Your custom buff
            Item.buffTime = 60 * 60 * 5; // 5 minutes (60 ticks × 60 sec × 5 min)
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.BottledWater)
                .AddIngredient(ItemID.Daybloom, 500)
                .AddIngredient(ItemID.LunarBar, 50)
                .AddTile(TileID.Bottles)
                .Register();
        }
    }
}
