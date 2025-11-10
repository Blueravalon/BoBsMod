using Terraria;
using Terraria.ModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace broilinghell.Core.DialogueSystem;

/// <summary>
/// A component you can add to any ModNPC to give it dialogue capabilities.
/// </summary>
public class DialogueNPCComponent
{
    /// <summary>
    /// The NPC this component belongs to.
    /// </summary>
    public ModNPC Owner { get; private set; }

    /// <summary>
    /// The current conversation this NPC is having.
    /// </summary>
    public Conversation? CurrentConversation { get; set; }

    /// <summary>
    /// The current dialogue node being displayed.
    /// </summary>
    public Dialogue? CurrentDialogue { get; set; }

    /// <summary>
    /// All fallback conversations this NPC can choose from.
    /// </summary>
    public List<FallbackConversation> FallbackConversations { get; private set; } = new();

    /// <summary>
    /// Priority conversation selector event. Return a conversation to force it, or null to use fallbacks.
    /// </summary>
    public event Func<Conversation?> OnSelectPriorityConversation;

    public DialogueNPCComponent(ModNPC owner)
    {
        Owner = owner;
    }

    /// <summary>
    /// Updates the current conversation based on conditions.
    /// Call this in your NPC's AI.
    /// </summary>
    public void Update()
    {
        CheckForPriorityConversations();
        CheckForRerolls();
    }

    /// <summary>
    /// Adds a conversation as a fallback option.
    /// </summary>
    public void AddFallback(Conversation conversation, int priority = 0)
    {
        FallbackConversations.Add(new FallbackConversation(conversation, priority));
    }

    /// <summary>
    /// Chooses a random conversation from available fallbacks.
    /// </summary>
    public Conversation? ChooseRandomFallback()
    {
        var available = FallbackConversations
            .Where(fc => fc.Conversation.AppearanceCondition() && !fc.Conversation.RerollCondition())
            .ToList();

        if (available.Count == 0)
            return null;

        // Get highest priority conversations
        int maxPriority = available.Max(fc => fc.Priority);
        var highestPriority = available.Where(fc => fc.Priority == maxPriority).ToList();

        // Pick one randomly
        return Main.rand.Next(highestPriority).Conversation;
    }

    private void CheckForPriorityConversations()
    {
        // Check if any subscribers want to force a conversation
        if (OnSelectPriorityConversation != null)
        {
            foreach (Delegate del in OnSelectPriorityConversation.GetInvocationList())
            {
                Func<Conversation?> selector = (Func<Conversation?>)del;
                Conversation? priority = selector();

                if (priority != null && CurrentConversation != priority)
                {
                    CurrentConversation = priority;
                    CurrentDialogue = priority.RootSelector();
                    return;
                }
            }
        }

        // Check if a higher priority fallback is available
        CheckForHigherPriorityFallback();
    }

    private void CheckForHigherPriorityFallback()
    {
        // Only switch if current conversation is a fallback
        var currentFallback = FallbackConversations.FirstOrDefault(fc => fc.Conversation == CurrentConversation);
        if (currentFallback == null)
            return;

        // Find available conversations with higher priority
        var available = FallbackConversations
            .Where(fc => fc.Conversation.AppearanceCondition() && !fc.Conversation.RerollCondition())
            .Where(fc => fc.Priority > currentFallback.Priority)
            .ToList();

        if (available.Count > 0)
        {
            CurrentConversation = available.First().Conversation;
            CurrentDialogue = CurrentConversation.RootSelector();
        }
    }

    private void CheckForRerolls()
    {
        if (CurrentConversation == null)
            return;

        // Don't reroll if being talked to
        bool beingTalkedTo = Main.LocalPlayer.talkNPC == Owner.NPC.whoAmI;
        if (beingTalkedTo)
            return;

        // Check if current conversation wants to reroll
        if (CurrentConversation.RerollCondition())
        {
            Conversation? newConversation = ChooseRandomFallback();
            if (newConversation != null)
            {
                CurrentConversation = newConversation;
                CurrentDialogue = newConversation.RootSelector();
            }
        }
    }

    /// <summary>
    /// Moves to the next dialogue in the conversation.
    /// </summary>
    public void AdvanceDialogue()
    {
        if (CurrentDialogue == null)
            return;

        // Invoke click action
        CurrentDialogue.InvokeClick();

        // Find next valid child
        Dialogue? next = CurrentDialogue.Children
            .FirstOrDefault(child => child.SelectionCondition());

        if (next != null)
        {
            // Invoke end action on current
            CurrentDialogue.InvokeEnd();
            CurrentDialogue = next;
        }
        else
        {
            // End of conversation
            CurrentDialogue.InvokeEnd();
            CurrentDialogue = null;
        }
    }
}

/// <summary>
/// Represents a fallback conversation with priority.
/// </summary>
public record FallbackConversation(Conversation Conversation, int Priority);