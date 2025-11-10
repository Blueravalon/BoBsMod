using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Chat;
using Terraria.Localization;
using Luminance.Core.Cutscenes;
using broilinghell.Content.Cutscenes;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.IO;

namespace broilinghell
{
    public class WallOfFleshTrigger : ModSystem
    {
        // This tracks whether we've already triggered our custom event
        private bool hasTriggered = false;

        public static readonly Color TextColor = new(0, 255, 0);


        public override void PostUpdateWorld()
        {
            // Check if hardmode is active and we haven't triggered yet
            if (Main.hardMode && !hasTriggered)
            {
                OnWallOfFleshDefeated();
                hasTriggered = true;
            }
        }

        private void OnWallOfFleshDefeated()
        {
                var cutscene = ModContent.GetInstance<DraedonPostMechsCutscene>();
                CutsceneManager.QueueCutscene(cutscene);
                Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("The Bitch Detector has detected you..."), TextColor);
        }

        public override void SaveWorldData(TagCompound tag)
        {
            // Save our trigger state so it persists between sessions
            tag["WoFTriggered"] = hasTriggered;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            // Load our trigger state
            hasTriggered = tag.GetBool("WoFTriggered");
        }

        public override void OnWorldUnload()
        {
            // Reset for new worlds
            hasTriggered = false;
        }
    }
}
