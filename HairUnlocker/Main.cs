using System.Linq;
using Harmony12;
using UnityModManagerNet;
using System.Reflection;
using System;
using Kingmaker;
using UnityEngine.SceneManagement;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.ResourceLinks;
using System.Collections.Generic;
using Kingmaker.Blueprints.CharGen;
using UnityEngine;

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
        static bool loaded = false;
        static bool enabled;
        static Settings settings;
        static Dictionary<string, CustomizationOptions> originalOptions = new Dictionary<string, CustomizationOptions>();
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            try
            {
                logger = modEntry.Logger;
                settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
                modEntry.OnToggle = OnToggle;
                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;
                SceneManager.sceneLoaded += OnSceneManagerOnSceneLoaded;
            }
            catch (Exception e)
            {
                modEntry.Logger.Log(e.ToString() + "\n" + e.StackTrace);
            }
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            if (loaded)
            {
                if (!enabled) RestoreOptions();
                else UnlockHair();
            }
            return true; // Permit or not.
        }
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            if (!loaded) return;
            var result = GUILayout.Toggle(settings.UnlockAllHair, "Unlock All Options (Including incompatible options)");
            if (result != settings.UnlockAllHair)
            {
                settings.UnlockAllHair = result;
                RestoreOptions();
                UnlockHair();
            }
#if (DEBUG)
            DisplayInfo.ShowDoll();
            DisplayInfo.ShowHair();
#endif
        }
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
        static void OnSceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == SceneName.MainMenu)
            {
                if (loaded) return;
                foreach(var race in Game.Instance.BlueprintRoot.Progression.CharacterRaces)
                {
                    if (race.AssetGuid.Length != 32) continue;
                    originalOptions[race.name + Gender.Male] = race.MaleOptions;
                    originalOptions[race.name + Gender.Female] = race.FemaleOptions;
                    race.MaleOptions = new CustomizationOptions()
                    {
                        Heads = (EquipmentEntityLink[])race.MaleOptions.Heads.Clone(),
                        Hair = (EquipmentEntityLink[])race.MaleOptions.Hair.Clone(),
                        Eyebrows = (EquipmentEntityLink[])race.MaleOptions.Eyebrows.Clone(),
                        Beards = (EquipmentEntityLink[])race.MaleOptions.Beards.Clone(),
                    };
                    race.FemaleOptions = new CustomizationOptions()
                    {
                        Heads = (EquipmentEntityLink[])race.FemaleOptions.Heads.Clone(),
                        Hair = (EquipmentEntityLink[])race.FemaleOptions.Hair.Clone(),
                        Eyebrows = (EquipmentEntityLink[])race.FemaleOptions.Eyebrows.Clone(),
                        Beards = (EquipmentEntityLink[])race.FemaleOptions.Beards.Clone(),
                    };
                }
                loaded = true;
                if (!enabled) return;
                UnlockHair();
                
            }
        }
        static void RestoreOptions()
        {
            foreach (var race in Game.Instance.BlueprintRoot.Progression.CharacterRaces)
            {
                if (race.AssetGuid.Length != 32) continue;
                if (originalOptions.ContainsKey(race.name + Gender.Male))
                {
                    race.MaleOptions = originalOptions[race.name + Gender.Male];
                }
                if (originalOptions.ContainsKey(race.name + Gender.Male))
                {
                    race.FemaleOptions = originalOptions[race.name + Gender.Female];
                }
            }
        }
        static EquipmentEntityLink[] Combine(EquipmentEntityLink[] from, EquipmentEntityLink[] to)
        {
            var result = new List<EquipmentEntityLink>(to);
            foreach(var eel in from)
            {
                if(result.Exists(toEEL => eel.AssetId == toEEL.AssetId))
                {
                    continue;
                }
                result.Add(eel);
            }
            return result.ToArray();
        }
        /*
         * DollState looks up the index of eyebrows by the index of heads,
         * so existing heads are duplicated and a default eyebrow from the
         * target class is added         * 
         */ 
        static void AddEyebrowsDefaultEyebrows(CustomizationOptions newSource, CustomizationOptions newTarget, CustomizationOptions originalTarget)
        {
            var newHeads = newTarget.Heads
                .Where(link => originalTarget.Heads.Contains(link))
                .Select(link => new EquipmentEntityLink() { AssetId = link.AssetId });
            newTarget.Heads = newTarget.Heads.AddRangeToArray(newHeads.ToArray());
            var newEyebrows = Enumerable.Repeat(newSource.Eyebrows[0], newTarget.Heads.Length - newTarget.Eyebrows.Length);
            newTarget.Eyebrows = newTarget.Eyebrows.AddRangeToArray(newEyebrows.ToArray());
        }
        static void AddHair(BlueprintRace sourceRace, BlueprintRace targetRace)
        {
            foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
            {
                var originalSource = originalOptions[sourceRace.name + gender];
                var originalTarget = originalOptions[targetRace.name + gender];
                var newSource = gender == Gender.Male ? sourceRace.MaleOptions : sourceRace.FemaleOptions;
                var newTarget = gender == Gender.Male ? targetRace.MaleOptions : targetRace.FemaleOptions;
                newTarget.Hair = Combine(newSource.Hair, newTarget.Hair);
                newTarget.Beards = Combine(newSource.Beards, newTarget.Beards);
                AddEyebrowsDefaultEyebrows(newSource, newTarget, originalTarget);
            }
        }
        static void UnlockHair()
        {

            BlueprintRace[][] groups;
            if (settings.UnlockAllHair)
            {
                groups = new BlueprintRace[][]
                {
                    ResourcesLibrary.GetBlueprints<BlueprintRace>().Where(
                        bp => bp.AssetGuid.Length == 32 && bp.AssetGuid != "f414c5b12f2296c41901e71b889ef436").ToArray()
                };

            } else {
               groups = new BlueprintRace[][]
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
            }
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
