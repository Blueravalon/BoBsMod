using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace broilinghell.Core.DialogueSystem;

/// <summary>
/// Saves and loads dialogue progress per-world.
/// </summary>
public class DialogueSaveSystem : ModSystem
{
    private static HashSet<string> seenDialogue = new();
    private static HashSet<string> clickedDialogue = new();

    /// <summary>
    /// Checks if a dialogue has been seen before.
    /// </summary>
    public static bool HasSeenDialogue(string textKey) => seenDialogue.Contains(textKey);

    /// <summary>
    /// Checks if a dialogue has been clicked before.
    /// </summary>
    public static bool HasClickedDialogue(string textKey) => clickedDialogue.Contains(textKey);

    /// <summary>
    /// Marks a dialogue as seen.
    /// </summary>
    public static void MarkDialogueSeen(string textKey)
    {
        if (!seenDialogue.Contains(textKey))
            seenDialogue.Add(textKey);
    }

    /// <summary>
    /// Marks a dialogue as clicked.
    /// </summary>
    public static void MarkDialogueClicked(string textKey)
    {
        if (!clickedDialogue.Contains(textKey))
            clickedDialogue.Add(textKey);
    }

    /// <summary>
    /// Clears all dialogue progress (new world).
    /// </summary>
    public override void ClearWorld()
    {
        seenDialogue.Clear();
        clickedDialogue.Clear();
    }

    /// <summary>
    /// Saves dialogue progress to the world file.
    /// </summary>
    public override void SaveWorldData(TagCompound tag)
    {
        tag["seenDialogue"] = seenDialogue.ToList();
        tag["clickedDialogue"] = clickedDialogue.ToList();
    }

    /// <summary>
    /// Loads dialogue progress from the world file.
    /// </summary>
    public override void LoadWorldData(TagCompound tag)
    {
        seenDialogue = tag.GetList<string>("seenDialogue").ToHashSet();
        clickedDialogue = tag.GetList<string>("clickedDialogue").ToHashSet();
    }

    /// <summary>
    /// Sends dialogue data to clients in multiplayer.
    /// </summary>
    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(seenDialogue.Count);
        foreach (string key in seenDialogue)
            writer.Write(key);

        writer.Write(clickedDialogue.Count);
        foreach (string key in clickedDialogue)
            writer.Write(key);
    }

    /// <summary>
    /// Receives dialogue data from server in multiplayer.
    /// </summary>
    public override void NetReceive(BinaryReader reader)
    {
        seenDialogue.Clear();
        clickedDialogue.Clear();

        int seenCount = reader.ReadInt32();
        for (int i = 0; i < seenCount; i++)
            seenDialogue.Add(reader.ReadString());

        int clickedCount = reader.ReadInt32();
        for (int i = 0; i < clickedCount; i++)
            clickedDialogue.Add(reader.ReadString());
    }
}