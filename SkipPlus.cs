using System;
using ThunderRoad;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;

namespace SkipPlus
{
    public class SkipPlus : LevelModule
    {
        private string[] bannedIDs = { "Master", "World", ""};
        private string[] bannedWaveIDs = { "Master", "World", "CharacterSelection", "Home", "Test", "", "Dungeon"};
        private string modTag = "(SkipPlus)";
        private string startWave = "";
        private bool isFirstLoad = true;

        public override IEnumerator OnLoadCoroutine()
        {
            Debug.Log(modTag + " Loaded successfully!");
            CreateIDList();
            CreateWaveIDList();
            SetLevels();
            EventManager.onPossess += OnPossessionEvent;
            return base.OnLoadCoroutine();
        }

        private void CreateIDList()
        {
            // Creates AvailableLevelIDs.txt && filters out the useless / broken levels
            try
            {
                string text = "";
                foreach (string id in Regex.Split(ConsoleCommands.GetAvailableLevels(), Environment.NewLine))
                {
                    if (!bannedIDs.Contains(id))
                    {
                        text += id + "\n";
                        foreach (LevelData.Mode mode in Catalog.GetData<LevelData>(id).modes)
                            text += "\t" + mode.name + "\n";
                    }
                }
                File.WriteAllText(Application.streamingAssetsPath + "\\Mods\\SkipPlus_U10\\AvailableLevelIDs.txt", text);
            }
            catch
            {
                Debug.LogError(modTag + " Unable to write all level IDs to file!");
            }
        }

        private void CreateWaveIDList()
        {
            // Creates AvailableWaveIDs folder w/ files of each map && their respective waves
            try
            {
                string path = Application.streamingAssetsPath + "\\Mods\\SkipPlus_U10\\AvailableWaveIDs";
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
                Directory.CreateDirectory(path);
                
                foreach (string levelId in Regex.Split(ConsoleCommands.GetAvailableLevels(), Environment.NewLine))
                {
                    if (!bannedWaveIDs.Contains(levelId))
                        File.WriteAllText(path + "\\" + levelId + ".txt", "");
                }

                foreach (string waveId in Catalog.GetAllID<WaveData>())
                {
                    WaveData wave = Catalog.GetData<WaveData>(waveId);
                    if (wave.alwaysAvailable)
                    {
                        foreach (string levelId in Regex.Split(ConsoleCommands.GetAvailableLevels(), Environment.NewLine))
                        {
                            if (!bannedWaveIDs.Contains(levelId))
                                File.AppendAllText(path + "\\" + levelId + ".txt", waveId + "\n");
                        }
                    } 
                    else
                    {
                        foreach (string levelId in wave.waveSelectors)
                            File.AppendAllText(path + "\\" + levelId + ".txt", waveId + "\n");
                    }
                }
            } 
            catch
            {
                Debug.LogError(modTag + " Unable to write all wave IDs to their respective files!");
            }
        }

        private void SetLevels()
        {
            /*
            * Scans AutoLevelLoader's Game.json and checks if the homeID is valid && not banned, then
            * sets new home level; reverts to default home if bannedIDs contains homeID
            */
            try
            {
                JsonTextReader reader = new JsonTextReader(new StringReader(File.ReadAllText(Application.streamingAssetsPath + "\\Mods\\SkipPlus_U10\\Game.json")));
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if (reader.Value.ToString().Equals("levelStart"))
                        {
                            reader.Read();
                            string id = reader.Value.ToString();
                            if (ConsoleCommands.GetAvailableLevels().Contains(id) && !bannedIDs.Contains(id))
                                Debug.Log(modTag + " Boot level set to " + id + ".");
                            else
                            {
                                Debug.LogError(modTag + " \"" + id + "\" is not a valid booting level ID. Booting normally! (Was it capitalized and spelled properly?)");
                                Catalog.gameData.levelStart = "CharacterSelection";
                            }
                        }
                        else if (reader.Value.ToString().Equals("levelHome"))
                        {
                            reader.Read();
                            string id = reader.Value.ToString();
                            if (ConsoleCommands.GetAvailableLevels().Contains(id) && !bannedIDs.Contains(id))
                            {
                                Debug.Log(modTag + " Home level set to " + id + ".");
                            }
                            else
                            {
                                Debug.LogError(modTag + " \"" + id + "\" is not a valid home ID. Reverting to default! (Was it capitalized and spelled properly?)");
                                Catalog.gameData.GetGameMode("Sandbox").levelHome = "Home";
                            }
                        }
                        else if (reader.Value.ToString().Equals("levelStartWaveName"))
                        {
                            reader.Read();
                            startWave = reader.Value.ToString();
                        }
                    }
                }
            }
            catch
            {
                Debug.LogError(modTag + " There was an issue reading from SkipPlus's Game.json. Any custom settings will not be set!");
            }
        }

        private void OnPossessionEvent(Creature creature, EventTime eventTime)
        {
            if (eventTime == EventTime.OnEnd)
            {
                Catalog.gameData.levelStart = "CharacterSelection";
                try
                {
                    if (isFirstLoad && !bannedWaveIDs.Contains(Level.current.name) && startWave != "")
                    {
                        isFirstLoad = false;
                        WaveSpawner.instances[0].StartWave(Catalog.GetData<WaveData>(startWave));
                    }
                }
                catch
                {
                    Debug.LogError(modTag + " \"" + startWave + "\" is not a valid wave ID. No wave will be started! (Was it capitalized and spelled properly?)");
                }
            }
        }
    }
}