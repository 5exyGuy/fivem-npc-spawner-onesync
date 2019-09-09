using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using NpcSpawnerServer.Utility;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NpcSpawnerServer
{
    public class MainServer : BaseScript
    {
        private static List<NPC> NPCs;
        private static Config Config;

        public MainServer()
        {
            EventHandlers.Add("npcspawner:onPlayerLoaded", new Action<Player>(OnPlayerLoaded));

            Initialize();
        }

        private void OnPlayerLoaded([FromSource] Player player)
        {
            player.TriggerEvent("npcspawner:onConfigLoaded", Config.Client);
        }

        private void Initialize()
        {
            string npcsJson = API.LoadResourceFile(API.GetCurrentResourceName(), "npcs.json");
            string configJson = API.LoadResourceFile(API.GetCurrentResourceName(), "config.json");

            NPCs = new List<NPC>();
            NPCs = JsonConvert.DeserializeObject<List<NPC>>(npcsJson);

            Config = new Config();
            Config = JsonConvert.DeserializeObject<Config>(configJson);

            for (int i = 0; i < NPCs.Count; i++)
            {
                NPCs[i].HashKey = (uint) API.GetHashKey(NPCs[i].Model);
                NPCs[i].Code = int.Parse(new Random().Next(111111, 999999).ToString() + i);
            }

            Tick += OnTick;

            Debug.WriteLine($"{Config.Server.DebugPrefix} Resource has been successfuly initialized");
        }

        private async Task OnTick()
        {
            for (int i = 0; i < NPCs.Count; i++)
            {
                Vector3 npcPos = NPCs[i].Position; // NPC's position

                foreach (Player player in Players)
                {
                    if (object.ReferenceEquals(player.Character, null)) continue;
                    if (object.ReferenceEquals(player.Character.Position, null)) continue;

                    Vector3 playerPos = player.Character.Position; // Player's position

                    float distance = Vector3.DistanceSquared(playerPos, npcPos); // Distance between player and npc

                    // Checking if the player is within a certain distance between NPC
                    if (distance <= Config.Server.VisibilityDistance)
                    {
                        if (NPCs[i].isSpawned) continue;

                        player.TriggerEvent("npcspawner:spawnNPC", NPCs[i]);

                        NPCs[i].isSpawned = true;

                        if (Config.Server.EnableDebugMode)
                            Debug.WriteLine($"{Config.Server.DebugPrefix} {NPCs[i].Name} has been created for player {player.Name}");
                    }
                    else
                    {
                        if (NPCs[i].isSpawned) NPCs[i].isSpawned = false;
                        else continue;

                        player.TriggerEvent("npcspawner:deleteNPC", NPCs[i].Code);

                        if (Config.Server.EnableDebugMode)
                            Debug.WriteLine($"{Config.Server.DebugPrefix} {NPCs[i].Name} has been removed for player {player.Name}");
                    }
                }
            }

            await Delay(100);
        }
    }
}
