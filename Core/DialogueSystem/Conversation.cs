using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace broilinghell.Core.DialogueSystem;

/// <summary>
/// Represents a complete conversation with appearance conditions and configuration.
/// </summary>
public class Conversation
{
    /// <summary>
    /// The NPC type this conversation belongs to.
    /// </summary>
    public int NPCType { get; set; }

    /// <summary>
    /// Determines if this conversation can be selected right now.
    /// </summary>
    public Func<bool> AppearanceCondition { get; set; } = () => true;

    /// <summary>
    /// Determines if this conversation should be rerolled/changed.
    /// </summary>
    public Func<bool> RerollCondition { get; set; } = () => false;

    /// <summary>
    /// Function that selects which dialogue node to start from.
    /// By default, returns the root of the tree.
    /// </summary>
    public Func<Dialogue> RootSelector { get; set; }

    /// <summary>
    /// The dialogue tree for this conversation.
    /// </summary>
    public DialogueTree Tree { get; private set; }

    public Conversation(int npcType, string localizationPrefix, string rootNodeKey)
    {
        NPCType = npcType;
        Tree = new DialogueTree(localizationPrefix, rootNodeKey);
        RootSelector = () => Tree.Root;
    }

    /// <summary>
    /// Checks if a specific dialogue has been seen before.
    /// </summary>
    public bool SeenBefore(string relativeKey) => GetByRelativeKey(relativeKey).SeenBefore;

    /// <summary>
    /// Gets a dialogue node by its relative key.
    /// </summary>
    public Dialogue GetByRelativeKey(string key) => Tree.GetByRelativeKey(key);

    /// <summary>
    /// Links dialogue nodes in a chain.
    /// </summary>
    public void LinkChain(params string[] identifiers) => Tree.LinkChain(identifiers);

    /// <summary>
    /// Links all dialogue in order they appear in the localization file.
    /// </summary>
    public Conversation LinkFromStartToFinish()
    {
        // Get all keys in order, sorted to ensure consistent ordering
        var allKeys = Tree.PossibleDialogue.Keys.OrderBy(k => k).ToArray();

        if (allKeys.Length > 0)
        {
            // Only link if there's more than one dialogue node
            if (allKeys.Length > 1)
            {
                Tree.LinkChain(allKeys);
            }

            var mod = ModContent.GetInstance<broilinghell>();
            mod.Logger.Info($"LinkFromStartToFinish: Linked {allKeys.Length} nodes in order:");
            mod.Logger.Info($"  Chain: {string.Join(" -> ", allKeys)}");

            // Log the children of each node to verify
            foreach (var key in allKeys)
            {
                var dialogue = Tree.PossibleDialogue[key];
                mod.Logger.Info($"  '{key}' has {dialogue.Children.Count} children: {string.Join(", ", dialogue.Children.Select(c => Tree.PossibleDialogue.FirstOrDefault(kvp => kvp.Value == c).Key))}");
            }
        }

        return this;
    }

    /// <summary>
    /// Links all dialogue except specified keys.
    /// </summary>
    public Conversation LinkFromStartToFinishExcluding(params string[] keysToExclude)
    {
        // FIXED: Changed AllDialogue to PossibleDialogue
        string[] orderedChain = Tree.PossibleDialogue
            .Select(d => d.Key)
            .Where(k => !keysToExclude.Contains(k))
            .ToArray();
        Tree.LinkChain(orderedChain);
        return this;
    }

    /// <summary>
    /// Sets a custom root selection function. Returns this for method chaining.
    /// </summary>
    public Conversation WithRootSelector(Func<Conversation, Dialogue> function)
    {
        RootSelector = () => function(this);
        return this;
    }

    /// <summary>
    /// Marks specific dialogue as being spoken by the player. Returns this for method chaining.
    /// </summary>
    public Conversation MakeSpokenByPlayer(params string[] relativeKeys)
    {
        foreach (string key in relativeKeys)
            GetByRelativeKey(key).SpokenByPlayer = true;

        return this;
    }

    /// <summary>
    /// Sets when this conversation can appear. Returns this for method chaining.
    /// </summary>
    public Conversation WithAppearanceCondition(Func<Conversation, bool> condition)
    {
        AppearanceCondition = () => condition(this);
        return this;
    }

    /// <summary>
    /// Sets when this conversation should be rerolled. Returns this for method chaining.
    /// </summary>
    public Conversation WithRerollCondition(Func<Conversation, bool> condition)
    {
        RerollCondition = () => condition(this);
        return this;
    }
}