using Terraria;
using Terraria.ModLoader;

namespace broilinghell.Content.Buffs
{
	public class HealthBuff : ModBuff
	{
		public override void SetStaticDefaults() {
		}

		public override void Update(Player player, ref int buffIndex) {
			player.statDefense += 400;
            player.statLifeMax2 += 10000;

        }
	}
}
