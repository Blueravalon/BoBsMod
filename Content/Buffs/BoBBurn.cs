using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace broilinghell.Content.Buffs
{
    public class BoBBurn : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Damage every 60 ticks (1 second)
            if (player.buffTime[buffIndex] % 15 == 0)
            {
                player.statLife -= 5; // Lose 5 HP

                // Optional: Add some fire dust
                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(player.position, player.width, player.height, DustID.Terra);
                }
            }
        }
    }
}