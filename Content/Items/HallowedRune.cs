using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using broilinghell.Content.Events;

namespace broilinghell.Content.Items
{
    public class HallowedRune: ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Hallow Sigil");
            // Tooltip.SetDefault("Summons a radiant invasion in the Hallow.");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.maxStack = 20;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.consumable = true;
            Item.rare = ItemRarityID.Pink;
        }

        public override bool CanUseItem(Player player)
        {
            // Only usable if player is in Hallow and event is not already active
            return player.ZoneHallow && !HallowEvent.HallowEventIsOngoing;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.myPlayer == player.whoAmI) // Only run for the player using it
            {
                HallowEvent.TryStartEvent(player);
            }
            return true;
        }
    }
}
