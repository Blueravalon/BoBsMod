using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace broilinghell.Content.Events
{
    public enum HallowSpawnRequirement
    {
        Land,
        Anywhere
    }

    public struct HallowSpawnData
    {
        public int InvasionContributionPoints { get; set; }
        public float SpawnRate { get; set; }
        public HallowSpawnRequirement SpawnRequirement { get; set; }

        public HallowSpawnData(int totalPoints, float spawnRate, HallowSpawnRequirement requirement)
        {
            InvasionContributionPoints = totalPoints;
            SpawnRate = spawnRate;
            SpawnRequirement = requirement;
        }
    }

    public class HallowEvent : ModSystem
    {
        public static readonly Color TextColor = new(200, 180, 255);

        public const int InvasionNoKillPersistTime = 9000;
        public static int MaxEnemyCount = 5;

        public static Dictionary<int, HallowSpawnData> PossibleEnemies = new();
        public static Dictionary<int, HallowSpawnData> PossibleMinibosses = new();

        public static bool HallowEventIsOngoing;
        public static int AccumulatedKillPoints;
        public static int TimeSinceLastHallowKill;
        public static int TimeSinceEventStarted;

        public static float HallowCompletionRatio =>
            MathHelper.Clamp(AccumulatedKillPoints / (float)NeededEnemyKills, 0f, 1f);

        public static int NeededEnemyKills
        {
            get
            {
                int playerCount = Math.Max(1, Main.CurrentFrameFlags.ActivePlayersCount);
                return (int)(Math.Log(playerCount + Math.E - 1) * 100f);
            }
        }

        private enum HallowPacketType : byte
        {
            SyncState,
            SyncKillPoints,
            StartEvent,
            StopEvent
        }

        public override void OnModLoad()
        {
            PossibleEnemies = new()
            {
                { NPCID.UndeadMiner, new HallowSpawnData(1, 1f, HallowSpawnRequirement.Land) },
                { NPCID.Unicorn, new HallowSpawnData(2, 1f, HallowSpawnRequirement.Land) },
                { NPCID.Gastropod, new HallowSpawnData(2, 0.8f, HallowSpawnRequirement.Land) }
            };

            PossibleMinibosses = new()
            {
            };
        }

        public override void Unload()
        {
            PossibleEnemies.Clear();
            PossibleMinibosses.Clear();
        }

        // Hook into the game loop
        public override void PostUpdateWorld()
        {
            Update();
        }

        // Hook into NPC deaths via GlobalNPC
        // Note: This needs to be in a separate GlobalNPC class
        // See below for implementation

        public static void ReceivePacket(BinaryReader reader, int whoAmI)
        {
            HallowPacketType type = (HallowPacketType)reader.ReadByte();

            switch (type)
            {
                case HallowPacketType.SyncState:
                    HallowEventIsOngoing = reader.ReadBoolean();
                    AccumulatedKillPoints = reader.ReadInt32();
                    TimeSinceLastHallowKill = reader.ReadInt32();
                    TimeSinceEventStarted = reader.ReadInt32();
                    break;

                case HallowPacketType.SyncKillPoints:
                    AccumulatedKillPoints = reader.ReadInt32();
                    break;

                case HallowPacketType.StartEvent:
                    HallowEventIsOngoing = true;
                    AccumulatedKillPoints = reader.ReadInt32();
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        BroadcastEventText("Angels are descending into the Hallow!");
                    break;

                case HallowPacketType.StopEvent:
                    HallowEventIsOngoing = false;
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                        BroadcastEventText("The Angels get bored and go back to Heaven...");
                    break;
            }
        }

        // --- Networking helpers ---
        private static void SendPacket(HallowPacketType type, params object[] data)
        {
            if (Main.netMode != NetmodeID.Server) return;

            ModPacket packet = ModContent.GetInstance<broilinghell>().GetPacket();
            packet.Write((byte)type);

            foreach (var obj in data)
            {
                switch (obj)
                {
                    case bool b: packet.Write(b); break;
                    case int i: packet.Write(i); break;
                }
            }

            packet.Send();
        }

        public static void BroadcastEventText(string text)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                Main.NewText(text, TextColor);
            else if (Main.netMode == NetmodeID.Server)
                Terraria.Chat.ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), TextColor);
        }

        // --- Event Logic ---
        public static void TryStartEvent(Player player)
        {
            if (HallowEventIsOngoing)
                return;

            if (!player.ZoneHallow)
                return;

            int playerCount = Main.CurrentFrameFlags.ActivePlayersCount;
            if (playerCount <= 0)
                return;

            HallowEventIsOngoing = true;
            AccumulatedKillPoints = 0; // Start at 0, count UP to goal
            TimeSinceLastHallowKill = 0;
            TimeSinceEventStarted = 0;

            BroadcastEventText("Angels are descending into the Hallow!");

            if (Main.netMode == NetmodeID.Server)
                SendPacket(HallowPacketType.StartEvent, AccumulatedKillPoints);
        }

        public static void Update()
        {
            if (!HallowEventIsOngoing)
                return;

            TimeSinceLastHallowKill++;
            TimeSinceEventStarted++;

            // Spawn logic: pick a random NPC from PossibleEnemies
            if (Main.rand.NextFloat() < 0.02f) // Adjust spawn chance per tick
            {
                var enemyKeys = PossibleEnemies.Keys.ToList();
                if (enemyKeys.Count == 0) return;

                int npcType = enemyKeys[Main.rand.Next(enemyKeys.Count)];

                // Find an active player
                var activePlayers = Main.player.Where(p => p != null && p.active).ToList();
                if (activePlayers.Count == 0) return;

                Vector2 spawnPosition = activePlayers[Main.rand.Next(activePlayers.Count)].Center;

                // Spawn above or around player
                spawnPosition += new Vector2(Main.rand.Next(-600, 600), -400);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    var source = new EntitySource_WorldEvent();
                    NPC.NewNPC(source, (int)spawnPosition.X, (int)spawnPosition.Y, npcType);
                }
            }

            // End event if players leave Hallow or timeout
            bool anyInHallow = Main.player.Any(p => p != null && p.active && p.ZoneHallow);
            if (!anyInHallow && TimeSinceEventStarted > 60)
            {
                HallowEventIsOngoing = false;
                BroadcastEventText("The Angels get bored and go back to Heaven...");

                if (Main.netMode == NetmodeID.Server)
                    SendPacket(HallowPacketType.StopEvent);

                return;
            }

            // Check for timeout (no kills for a long time)
            if (TimeSinceLastHallowKill > InvasionNoKillPersistTime)
            {
                HallowEventIsOngoing = false;
                BroadcastEventText("The Angels get bored and go back to Heaven...");

                if (Main.netMode == NetmodeID.Server)
                    SendPacket(HallowPacketType.StopEvent);

                return;
            }

            UpdateInvasion();
        }

        public static void UpdateInvasion()
        {
            if (!HallowEventIsOngoing)
                return;

            // Set invasion progress (current/max format)
            Main.invasionSize = NeededEnemyKills;
            Main.invasionSizeStart = NeededEnemyKills;
            Main.invasionProgress = AccumulatedKillPoints;
            Main.invasionProgressMax = NeededEnemyKills;
            Main.invasionType = 696969; // arbitrary unique ID
            Main.invasionProgressIcon = NPCID.Pixie; // Fixed: removed ModContent.NPCType

            // Check if event is complete
            if (AccumulatedKillPoints >= NeededEnemyKills)
            {
                HallowEventIsOngoing = false;
                BroadcastEventText("The Angels get bored of dying and go back to Heaven...");

                if (Main.netMode == NetmodeID.Server)
                    SendPacket(HallowPacketType.StopEvent);
            }

            if (Main.netMode == NetmodeID.Server)
                SendPacket(HallowPacketType.SyncState, HallowEventIsOngoing, AccumulatedKillPoints, TimeSinceLastHallowKill, TimeSinceEventStarted);
        }

        public static void OnEnemyKill(NPC npc)
        {
            if (!HallowEventIsOngoing)
                return;

            int pointsEarned = 0;

            if (PossibleEnemies.ContainsKey(npc.type))
                pointsEarned = PossibleEnemies[npc.type].InvasionContributionPoints;

            if (PossibleMinibosses.ContainsKey(npc.type))
                pointsEarned = PossibleMinibosses[npc.type].InvasionContributionPoints;

            if (pointsEarned > 0)
            {
                AccumulatedKillPoints += pointsEarned; // Add points, don't subtract
                TimeSinceLastHallowKill = 0;

                UpdateInvasion();

                if (Main.netMode == NetmodeID.Server)
                    SendPacket(HallowPacketType.SyncKillPoints, AccumulatedKillPoints);
            }
        }
    }
}