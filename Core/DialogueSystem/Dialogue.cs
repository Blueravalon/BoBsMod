using Microsoft.Xna.Framework;
using Terraria.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace broilinghell.Core.DialogueSystem;

/// <summary>
/// Represents a single piece of dialogue in a conversation tree.
/// </summary>
public class Dialogue
{
    /// <summary>
    /// Whether this dialogue is spoken by the player, rather than the NPC.
    /// </summary>
    public bool SpokenByPlayer { get; set; }

    /// <summary>
    /// The localization key that points to this dialogue.
    /// </summary>
    public string TextKey { get; set; }

    /// <summary>
    /// Whether this dialogue has been seen before by the player.
    /// </summary>
    public bool SeenBefore => DialogueSaveSystem.HasSeenDialogue(TextKey);

    /// <summary>
    /// The condition which dictates whether this dialogue can be chosen from a parent node.
    /// </summary>
    public Func<bool> SelectionCondition { get; set; } = () => true;

    /// <summary>
    /// Action performed when moving away from this dialogue (via any method).
    /// </summary>
    public Action<bool> OnEnd { get; set; } = _ => { };

    /// <summary>
    /// Action performed when player clicks through this dialogue.
    /// </summary>
    public Action<bool> OnClick { get; set; } = _ => { };

    /// <summary>
    /// The child nodes for this dialogue (what comes next).
    /// </summary>
    public List<Dialogue> Children { get; set; } = new();

    /// <summary>
    /// Optional function to override the text color for this dialogue.
    /// </summary>
    public Func<Color>? ColorOverride { get; set; }

    /// <summary>
    /// The actual text content from localization.
    /// </summary>
    public string Text => Language.GetTextValue(TextKey);

    public Dialogue(string textKey)
    {
        TextKey = textKey;
    }

    /// <summary>
    /// Marks this dialogue as seen and invokes the end action.
    /// </summary>
    public void InvokeEnd()
    {
        bool wasSeenBefore = SeenBefore;

        if (!wasSeenBefore)
            DialogueSaveSystem.MarkDialogueSeen(TextKey);

        OnEnd?.Invoke(wasSeenBefore);
    }

    /// <summary>
    /// Marks this dialogue as clicked and invokes the click action.
    /// </summary>
    public void InvokeClick()
    {
        bool wasClickedBefore = DialogueSaveSystem.HasClickedDialogue(TextKey);

        if (!wasClickedBefore)
            DialogueSaveSystem.MarkDialogueClicked(TextKey);

        if (!SeenBefore)
            DialogueSaveSystem.MarkDialogueSeen(TextKey);

        OnClick?.Invoke(wasClickedBefore);
    }
}