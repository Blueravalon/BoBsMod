using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using System.IO;
using broilinghell.Content.Events;

namespace broilinghell
{
    public class broilinghell : Mod
    {
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            // Delegate handling to the HallowEvent
            HallowEvent.ReceivePacket(reader, whoAmI);
        }
    }
}