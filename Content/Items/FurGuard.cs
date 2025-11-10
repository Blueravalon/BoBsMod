using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent.Creative;
using Terraria.ID;

namespace broilinghell.Content.Items
{
    [AutoloadEquip(EquipType.Waist)]
    internal class FurGuard : ModItem
    {
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = Item.buyPrice(1000);
            Item.rare = ItemRarityID.Green;

            Item.defense = 2;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.moveSpeed *= 1.2f;
            player.noKnockback = true;
            player.statLifeMax2 -= 20;
            player.GetCritChance(DamageClass.Generic) += 20f;
            player.thorns = 1f;
        }
    }
}