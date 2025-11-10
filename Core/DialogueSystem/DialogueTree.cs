using System.Text;
using Hjson;
using Newtonsoft.Json.Linq;
using Terraria.ModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace broilinghell.Core.DialogueSystem;

/// <summary>
/// A tree structure that holds all dialogue nodes for a conversation.
/// </summary>
public class DialogueTree
{
    /// <summary>
    /// The root dialogue node for this tree (where conversation starts).
    /// </summary>
    public Dialogue Root { get; private set; }

    /// <summary>
    /// All dialogue nodes in this tree, indexed by their relative key.
    /// </summary>
    public Dictionary<string, Dialogue> PossibleDialogue { get; private set; } = new();

    /// <summary>
    /// Whether any dialogue in this tree has been seen before.
    /// </summary>
    public bool HasBeenSeenBefore => PossibleDialogue.Values.Any(d => d.SeenBefore);

    /// <summary>
    /// The full localization prefix for this tree (e.g., "Mods.broilinghell.NPCs.MyNPC.Greeting.")
    /// </summary>
    public string LocalizationPrefix { get; private set; }

    public DialogueTree(string localizationPrefix, string rootNodeKey)
    {
        LocalizationPrefix = localizationPrefix;
        LoadDialogueFromLocalization(localizationPrefix);

        // Check if root node exists before trying to access it
        if (!PossibleDialogue.ContainsKey(rootNodeKey))
        {
            var mod = ModContent.GetInstance<broilinghell>();
            mod.Logger.Error($"Root node '{rootNodeKey}' not found in dialogue tree for prefix: {localizationPrefix}");
            mod.Logger.Error($"Available keys: {string.Join(", ", PossibleDialogue.Keys)}");

            // Create a dummy root to prevent crashes
            Root = new Dialogue($"{localizationPrefix}Error");
        }
        else
        {
            Root = PossibleDialogue[rootNodeKey];
        }
    }

    /// <summary>
    /// Loads all dialogue entries from the localization file.
    /// </summary>
    private void LoadDialogueFromLocalization(string localizationPrefix)
    {
        var mod = ModContent.GetInstance<broilinghell>();

        // Try multiple possible localization file paths
        string[] possiblePaths = new[]
        {
            "Localization/en-US_Mods.broilinghell.hjson",  // Underscore format (most common)
            "Localization/en-US/Mods.broilinghell.hjson",  // Directory format
            "Localization/en-US.hjson"                      // Single file format
        };

        byte[]? fileBytes = null;
        string? usedPath = null;

        foreach (string path in possiblePaths)
        {
            if (mod.FileExists(path))
            {
                fileBytes = mod.GetFileBytes(path);
                usedPath = path;
                break;
            }
        }

        if (fileBytes == null)
        {
            mod.Logger.Error($"Failed to load localization file. Tried paths:");
            foreach (string path in possiblePaths)
                mod.Logger.Error($"  - {path}");
            mod.Logger.Error($"Looking for prefix: {localizationPrefix}");
            return;
        }

        mod.Logger.Info($"Successfully loaded localization from: {usedPath}");

        string fileContents;
        try
        {
            fileContents = Encoding.UTF8.GetString(fileBytes);
        }
        catch (Exception ex)
        {
            mod.Logger.Error($"Failed to decode localization file: {ex.Message}");
            return;
        }

        JObject? jsonObject;
        try
        {
            string jsonString = HjsonValue.Parse(fileContents).ToString();
            jsonObject = JObject.Parse(jsonString);
        }
        catch (Exception ex)
        {
            mod.Logger.Error($"Failed to parse localization file: {ex.Message}");
            return;
        }

        // Find all keys that match our prefix
        int foundCount = 0;
        foreach (JToken token in jsonObject.SelectTokens("$..*"))
        {
            if (token.HasValues)
                continue;

            if (token is JObject obj && obj.Count == 0)
                continue;

            // Build the full path
            string path = BuildPath(token);
            path = $"Mods.broilinghell.{path}.";

            // Only include dialogue that matches our prefix
            if (path.Contains(localizationPrefix))
            {
                string relativeKey = path.Replace(localizationPrefix, string.Empty).TrimEnd('.');

                // CRITICAL FIX: Skip DisplayName and other non-dialogue keys
                if (relativeKey.Contains("DisplayName") ||
                    relativeKey.Contains("Tooltip") ||
                    relativeKey.Contains("TownNPCMood"))
                    continue;

                Dialogue dialogue = new(path.TrimEnd('.'));
                PossibleDialogue[relativeKey] = dialogue;
                foundCount++;
            }
        }

        if (foundCount == 0)
        {
            mod.Logger.Warn($"No dialogue entries found for prefix: {localizationPrefix}");
        }
        else
        {
            mod.Logger.Info($"Loaded {foundCount} dialogue entries for prefix: {localizationPrefix}");
        }
    }

    /// <summary>
    /// Builds a path string from a JSON token.
    /// </summary>
    private string BuildPath(JToken token)
    {
        string path = "";
        JToken current = token;

        for (JToken parent = token.Parent!; parent != null; parent = parent.Parent!)
        {
            path = parent switch
            {
                JProperty property => property.Name + (path == string.Empty ? string.Empty : "." + path),
                JArray array => array.IndexOf(current) + (path == string.Empty ? string.Empty : "." + path),
                _ => path
            };
            current = parent;
        }

        return path.Replace(".$parentVal", string.Empty);
    }

    /// <summary>
    /// Gets a dialogue node by its relative key.
    /// </summary>
    public Dialogue GetByRelativeKey(string key) => PossibleDialogue[key];

    /// <summary>
    /// Links multiple dialogue nodes in a chain (A -> B -> C -> ...).
    /// </summary>
    public void LinkChain(params string[] identifiers)
    {
        for (int i = 0; i < identifiers.Length - 1; i++)
        {
            Dialogue current = PossibleDialogue[identifiers[i]];
            Dialogue next = PossibleDialogue[identifiers[i + 1]];

            if (!current.Children.Contains(next))
                current.Children.Add(next);
        }
    }
}