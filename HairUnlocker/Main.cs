using System.Linq;
using Harmony12;
using UnityModManagerNet;
using System.Reflection;
using System;
using Kingmaker;
using UnityEngine.SceneManagement;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
namespace HairUnlocker
{

    public class Main
    {
        public static UnityModManager.ModEntry.ModLogger logger;
        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugLog(string msg)
        {
            if (logger != null) logger.Log(msg);
        }
        public static bool loaded = false;
        public static bool enabled;
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                modEntry.OnToggle = OnToggle;
                logger = modEntry.Logger;
                SceneManager.sceneLoaded += OnSceneManagerOnSceneLoaded;
            }
            catch (Exception e)
            {
                modEntry.Logger.Log(e.ToString() + "\n" + e.StackTrace);
            }
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool enabled)
        {
            //Cannot be disabled without restart
            return enabled;
        }

        static void OnSceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneName.MainMenu)
            {
                if (loaded) return;
                UnlockHair();
                
            }
        }
        static void AddHair(BlueprintRace from, BlueprintRace to)
        {
            foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
            {
                var fromOptions = gender == Gender.Male ? from.MaleOptions : from.FemaleOptions;
                var toOptions = gender == Gender.Male ? to.MaleOptions : to.FemaleOptions;
                foreach (var ee in fromOptions.Hair) if (!toOptions.Hair.Contains(ee)) toOptions.Hair = toOptions.Hair.Add(ee).ToArray();
                foreach (var ee in fromOptions.Eyebrows) if (!toOptions.Eyebrows.Contains(ee)) toOptions.Eyebrows = toOptions.Eyebrows.Add(ee).ToArray();
                foreach (var ee in fromOptions.Beards) if (!toOptions.Beards.Contains(ee)) toOptions.Beards = toOptions.Beards.Add(ee).ToArray();
            }
        }
        static void UnlockHair()
        {
            var groups = new BlueprintRace[][]
            {
                new BlueprintRace[]{
                    ResourcesLibrary.TryGetBlueprint<BlueprintRace>("0a5d473ead98b0646b94495af250fdc4"), //Human
                    ResourcesLibrary.TryGetBlueprint<BlueprintRace>("b7f02ba92b363064fb873963bec275ee"), //Aasimar
                    ResourcesLibrary.TryGetBlueprint<BlueprintRace>("c4faf439f0e70bd40b5e36ee80d06be7"), //Dwarf
                },
                new BlueprintRace[]{
                    ResourcesLibrary.TryGetBlueprint<BlueprintRace>("ef35a22c9a27da345a4528f0d5889157"), //Gnome
                    ResourcesLibrary.TryGetBlueprint<BlueprintRace>("b0c3ef2729c498f47970bb50fa1acd30"), //Halfling
                }
            };
            foreach (var group in groups)
            {
                foreach (var from in group)
                {
                    foreach (var to in group)
                    {
                        if (from == to) continue;
                        AddHair(from, to);
                    }
                }
            }
        }
    }
}
