using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace broilinghell.Content.Items
{
    public class VehementBlade : ModItem
    {
        public override void SetDefaults()
        {
            Item.damage = 140;
            Item.DamageType = DamageClass.Melee;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 10;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 5f;
            Item.value = Item.buyPrice(silver: 50);
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.AngelicSlash>();
            Item.shootSpeed = 10f;
        }
    }
}