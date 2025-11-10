using broilinghell.Core.DialogueSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace broilinghell.Content.NPCs;

public class ExampleDialogueNPC : ModNPC
{
    private DialogueNPCComponent dialogueComponent;
    private bool dialogueSetup = false;

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 1;
        NPCID.Sets.ActsLikeTownNPC[Type] = true;
        NPCID.Sets.SpawnsWithCustomName[Type] = true;
    }

    public override void SetDefaults()
    {
        NPC.friendly = true;
        NPC.width = 18;
        NPC.height = 40;
        NPC.aiStyle = 7; // Town NPC AI
        NPC.damage = 10;
        NPC.defense = 15;
        NPC.lifeMax = 250;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.knockBackResist = 0.5f;

        // CRITICAL: This makes the NPC talkable!
        NPC.townNPC = true;

        // Initialize dialogue component
        dialogueComponent = new DialogueNPCComponent(this);
    }

    public override void AI()
    {
        // Setup dialogue on first frame
        if (!dialogueSetup)
        {
            SetupDialogue();
        }

        // Update dialogue component
        dialogueComponent?.Update();
    }

    private void SetupDialogue()
    {
        var mod = ModContent.GetInstance<broilinghell>();
        mod.Logger.Info("=== Setting up dialogue ===");

        // Register greeting conversation with MANUAL chain order
        Conversation greeting = DialogueManager.RegisterConversation(
            Type,
            "Greeting",
            "NPCs.ExampleNPC.Greeting",
            "Start"
        );
        // Manually specify the correct order instead of using LinkFromStartToFinish
        greeting.LinkChain("Start", "Response1", "Response2", "Response3");

        // Register morning conversation
        Conversation morning = DialogueManager.RegisterConversation(
            Type,
            "Morning",
            "NPCs.ExampleNPC.Morning",
            "Start"
        );
        morning.LinkChain("Start", "Response1", "Response2");
        morning.WithAppearanceCondition(c => Main.dayTime);

        // Register danger conversation
        Conversation danger = DialogueManager.RegisterConversation(
            Type,
            "Danger",
            "NPCs.ExampleNPC.Danger",
            "Start"
        );
        danger.LinkChain("Start", "Response1", "Response2");
        danger.WithAppearanceCondition(c => NPC.AnyNPCs(NPCID.EyeofCthulhu));

        // Register quest conversation with branching dialogue
        Conversation quest = DialogueManager.RegisterConversation(
            Type,
            "Quest",
            "NPCs.ExampleNPC.Quest",
            "Start"
        );

        // Linear start
        quest.LinkChain("Start", "Question");

        // Branch from Question based on whether player has diamond
        var questionNode = quest.GetByRelativeKey("Question");
        var yesResponse = quest.GetByRelativeKey("Yes_Response");
        var noResponse = quest.GetByRelativeKey("No_Response");

        // Add both responses as children of Question
        questionNode.Children.Add(yesResponse);
        questionNode.Children.Add(noResponse);

        // Set conditions for each branch
        yesResponse.SelectionCondition = () => Main.LocalPlayer.HasItem(ItemID.Diamond);
        noResponse.SelectionCondition = () => !Main.LocalPlayer.HasItem(ItemID.Diamond);

        // Optional: Make the question spoken by the NPC asking, then show player responses
        quest.MakeSpokenByPlayer("Yes_Response", "No_Response");

        // Add quest as lower priority so it doesn't spam
        dialogueComponent.AddFallback(quest, priority: 2);

        mod.Logger.Info($"Greeting: {greeting.Tree.PossibleDialogue.Count} entries");
        mod.Logger.Info($"Morning: {morning.Tree.PossibleDialogue.Count} entries");
        mod.Logger.Info($"Danger: {danger.Tree.PossibleDialogue.Count} entries");
        mod.Logger.Info($"Quest: {quest.Tree.PossibleDialogue.Count} entries");

        // Add all as fallbacks with different priorities
        dialogueComponent.AddFallback(danger, priority: 10);   // Highest priority
        dialogueComponent.AddFallback(morning, priority: 5);   // Medium priority
        dialogueComponent.AddFallback(greeting, priority: 0);  // Default/lowest priority

        // Set initial conversation
        var initial = dialogueComponent.ChooseRandomFallback();
        dialogueComponent.CurrentConversation = initial;
        dialogueComponent.CurrentDialogue = initial?.RootSelector();

        if (dialogueComponent.CurrentDialogue != null)
        {
            mod.Logger.Info($"Initial dialogue: {dialogueComponent.CurrentDialogue.TextKey}");
        }

        dialogueSetup = true;
    }

    // CRITICAL: This is what makes the dialogue actually appear!
    public override string GetChat()
    {
        if (dialogueComponent?.CurrentDialogue != null)
        {
            return dialogueComponent.CurrentDialogue.Text;
        }

        return "..."; // Fallback text
    }

    // Handle clicking through dialogue
    public override void OnChatButtonClicked(bool firstButton, ref string shop)
    {
        if (firstButton)
        {
            // Check if we're at the end of the dialogue chain (no children)
            bool isLastDialogue = dialogueComponent?.CurrentDialogue?.Children.Count == 0;

            dialogueComponent?.AdvanceDialogue();

            // If we've run out of dialogue or reached the end, pick a new conversation
            if (dialogueComponent?.CurrentDialogue == null || isLastDialogue)
            {
                var newConversation = dialogueComponent.ChooseRandomFallback();
                if (newConversation != null)
                {
                    dialogueComponent.CurrentConversation = newConversation;
                    dialogueComponent.CurrentDialogue = newConversation.RootSelector();
                }
            }

            // CRITICAL: Force the chat to refresh with new dialogue
            Main.npcChatText = GetChat();
        }
    }

    // Optional: Add chat buttons if needed
    public override void SetChatButtons(ref string button, ref string button2)
    {
        button = "Next"; // This is the default chat button
    }

    // Make the NPC not despawn
    public override bool CheckActive()
    {
        return false;
    }

    // Optional: Custom name
    public override void FindFrame(int frameHeight)
    {
        // Animation code if needed
    }
}