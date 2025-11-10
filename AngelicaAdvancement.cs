using broilinghell.Content.NPCs;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.ModLoader.Utilities;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;

namespace broilinghell;

public class AngelicaAdvancement : ModAchievement
{
    public override string TextureName => "broilinghell/AngelicaAdvancement";
    public override int Index => 0;
    public override void SetStaticDefaults()
    {
        AddNPCKilledCondition(ModContent.NPCType<AngelicaSono>());
    }
}