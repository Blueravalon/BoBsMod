using Terraria.ModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace broilinghell.Core.DialogueSystem;

/// <summary>
/// Manages all registered conversations for all NPCs.
/// </summary>
public class DialogueManager : ModSystem
{
    /// <summary>
    /// All registered conversations, organized by NPC type, then by conversation ID.
    /// </summary>
    private static Dictionary<int, Dictionary<string, Conversation>> allConversations = new();

    public override void PostSetupContent()
    {
        allConversations.Clear();
    }

    /// <summary>
    /// Registers a new conversation for a specific NPC type.
    /// </summary>
    /// <param name="npcType">The NPC type this conversation belongs to.</param>
    /// <param name="conversationID">Unique identifier for this conversation.</param>
    /// <param name="localizationPath">Path in localization (e.g., "NPCs.MyNPC.Greeting")</param>
    /// <param name="rootNodeKey">The key of the starting dialogue node.</param>
    public static Conversation RegisterConversation(int npcType, string conversationID, string localizationPath, string rootNodeKey)
    {
        // Ensure the localization path has a trailing period
        if (!localizationPath.EndsWith("."))
            localizationPath += ".";

        // Build the full localization prefix
        string fullPrefix = $"Mods.broilinghell.{localizationPath}";

        // Create the conversation
        Conversation conversation = new(npcType, fullPrefix, rootNodeKey);

        // Register it
        if (!allConversations.ContainsKey(npcType))
            allConversations[npcType] = new();

        allConversations[npcType][conversationID] = conversation;

        return conversation;
    }

    /// <summary>
    /// Gets all conversations for a specific NPC type.
    /// </summary>
    public static List<Conversation> GetConversationsForNPC(int npcType)
    {
        if (allConversations.TryGetValue(npcType, out var conversations))
            return conversations.Values.ToList();

        return new List<Conversation>();
    }

    /// <summary>
    /// Gets a specific conversation by NPC type and conversation ID.
    /// </summary>
    public static Conversation? GetConversation(int npcType, string conversationID)
    {
        if (allConversations.TryGetValue(npcType, out var conversations))
        {
            if (conversations.TryGetValue(conversationID, out var conversation))
                return conversation;
        }

        return null;
    }

    /// <summary>
    /// Finds a dialogue node by its text key across all conversations.
    /// </summary>
    public static Dialogue? FindDialogue(string textKey)
    {
        foreach (var npcConversations in allConversations.Values)
        {
            foreach (var conversation in npcConversations.Values)
            {
                // FIXED: Changed AllDialogue to PossibleDialogue
                foreach (var dialogue in conversation.Tree.PossibleDialogue.Values)
                {
                    if (dialogue.TextKey == textKey)
                        return dialogue;
                }
            }
        }

        return null;
    }
}