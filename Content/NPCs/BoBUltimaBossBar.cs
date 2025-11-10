using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent.UI.BigProgressBar;

namespace broilinghell.Content.NPCs
{
    public class BobUltimaBossBar : ModBossBar
    {
        public override bool? ModifyInfo(
            ref BigProgressBarInfo info,
            ref float life,
            ref float lifeMax,
            ref float shield,
            ref float shieldMax
        )
        {
            int leftType = ModContent.NPCType<handleft>();
            int rightType = ModContent.NPCType<handright>();

            int cur = 0;
            int max = 0;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active)
                    continue;

                if (npc.type == leftType || npc.type == rightType)
                {
                    cur += Utils.Clamp(npc.life, 0, npc.lifeMax);
                    max += npc.lifeMax;
                }
            }

            if (max > 0)
            {
                life = cur;
                lifeMax = max;
                shield = 0f;
                shieldMax = 0f;
                return true; // Show this custom boss bar
            }

            return false; // Hide if both hands are gone
        }
    }
}