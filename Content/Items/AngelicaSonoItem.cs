using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace broilinghell.Content.Items
{
    public class AngelicaSonoItem : ModItem
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
            Item.UseSound = SoundID.Roar;
        }

        public override bool? UseItem(Player player)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 spawnPosition = player.Center + new Vector2(-400f, 0f);
                int type = ModContent.NPCType<NPCs.AngelicaSono>();
                NPC.NewNPC(player.GetSource_ItemUse(Item), (int)spawnPosition.X, (int)spawnPosition.Y, type);
            }

            if (Main.netMode != NetmodeID.Server)
            {
                Main.NewText("The Angel has descended...", 200, 50, 255);
            }

            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SoulofLight, 10)
                .AddIngredient(ItemID.PearlstoneBlock, 20)
                .AddIngredient(ItemID.PearlsandBlock, 10)
                .AddTile(TileID.DemonAltar)
                .Register();
        }
    }
}