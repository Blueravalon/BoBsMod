using Terraria;
using Terraria.ModLoader;
using broilinghell.Content.Events;

namespace broilinghell.Content.Events
{
    public class HallowEventGlobalNPC : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            HallowEvent.OnEnemyKill(npc);
        }
    }
}