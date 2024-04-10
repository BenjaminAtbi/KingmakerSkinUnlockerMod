using System.Linq;
using HarmonyLib;
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
using Kingmaker.Blueprints.Root;
using System.Text.RegularExpressions;
using Kingmaker.Visual.CharacterSystem;
using Kingmaker.Blueprints.Classes.Prerequisites;
using Kingmaker.Utility;
using Kingmaker.UnitLogic.Class.LevelUp;
using System.Diagnostics;
using Kingmaker.UI.LevelUp;
using TMPro;
using Kingmaker.Localization;
using System.Reflection.Emit;
//using Harmony12;
using Kingmaker.Visual;
using System.Runtime.Remoting.Messaging;
using static HBAO_Core;

namespace SkinUnlocker
{

    public class Main
    {
        public static UnityModManager.ModEntry.ModLogger logger;
        [System.Diagnostics.Conditional("DEBUG")]
        public static void DebugLog(string msg)
        {
            if (logger != null) logger.Log(msg);
        }
        public static void DebugError(Exception ex)
        {
            if (logger != null) logger.Log(ex.ToString() + "\n" + ex.StackTrace);
        }
        static bool loaded = false;
        static bool enabled;
        static bool displayDebug = false;
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
                var harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                DebugError(ex);
            }
            return true;
        }

        static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            if (loaded)
            {
                if (!enabled) RestoreOptions();
                else Unlock();
            }
            return true; // Permit or not.
        }
        static void ChooseToggle(ref bool value, string text)
        {
            var result = GUILayout.Toggle(value, text);
            if (result != value)
            {
                value = result;
                RestoreOptions();
                Unlock();
            }
        }
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            try
            {
                if (!loaded) return;
                ChooseToggle(ref settings.UnlockSkin, "Unlock Skin Options");
                ChooseToggle(ref settings.UnlockHair, "Unlock Hair Options");
          
                /*#if (DEBUG)
                                displayDebug = GUILayout.Toggle(displayDebug, "Show DisplayOptions");
                                if (displayDebug)
                                {
                                    DisplayInfo.ShowDoll();
                                    DisplayInfo.ShowHair();
                                }
                #endif*/
            }
            catch (Exception ex)
            {
                DebugError(ex);
            }
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
                try
                {
                    foreach (var race in GetAllRaces())
                    {
                        originalOptions[race.name + Gender.Male] = race.MaleOptions;
                        originalOptions[race.name + Gender.Female] = race.FemaleOptions;
                    }
                    loaded = true;
                    if (!enabled) return;
                    Unlock();
                } catch (Exception ex)
                {
                    DebugError(ex);
                }
            }
        }

        static BlueprintRace[] GetAllRaces()
        {
            return Game.Instance.BlueprintRoot.Progression.CharacterRaces;
        }
        static void RestoreOptions()
        {
            foreach (var race in GetAllRaces())
            {
                race.MaleOptions = GetOriginalOptions(race, Gender.Male);
                race.FemaleOptions = GetOriginalOptions(race, Gender.Female);
            }
        }
        static CustomizationOptions GetOriginalOptions(BlueprintRace race, Gender gender)
        {
            if (originalOptions.ContainsKey(race.name + gender))
            {
                return originalOptions[race.name + gender];
            }
            throw new KeyNotFoundException($"Couldn't find key {race.name + gender}");
        }
        static EquipmentEntityLink[] Combine(EquipmentEntityLink[] from, EquipmentEntityLink[] to)
        {
            var result = new List<EquipmentEntityLink>(to);
            foreach (var eel in from)
            {
                if (result.Exists(toEEL => eel.AssetId == toEEL.AssetId))
                {
                    continue;
                }
                result.Add(eel);
            }
            return result.ToArray();
        }


        static void Unlock()
        {
            //CopyOptions();
            UnlockHair();
            UnlockSkin();
            //UnlockHornsAndTails();
            //UnlockFemaleDwarfBeards();
        }
        static void UnlockHair()
        {
            if (!settings.UnlockHair) return;
            BlueprintRace[][] groups;
            if (settings.UnlockAllHair)
            {
                groups = new BlueprintRace[][]
                {
                    GetAllRaces().Where(bp =>
                        (bp.AssetGuid != "5c4e42124dc2b4647af6e36cf2590500" || Game.Instance.BlueprintRoot.DlcSettings.Tieflings.Enabled)).ToArray()
                };

            }
            else
            {
                groups = new BlueprintRace[][]
                 {
                    new BlueprintRace[]{
                        ResourcesLibrary.TryGetBlueprint<BlueprintRace>("0a5d473ead98b0646b94495af250fdc4"), //Human
                        ResourcesLibrary.TryGetBlueprint<BlueprintRace>("b7f02ba92b363064fb873963bec275ee"), //Aasimar
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

        class RampNameCompare : IEqualityComparer<Texture2D>
        {
            public bool Equals(Texture2D a, Texture2D b)
            {
                return a.name.Split('_')[2].Equals(b.name.Split('_')[2]);
            }

            public int GetHashCode(Texture2D obj)
            {
                return obj.name.Split('_')[2].GetHashCode();
            }
        }

        static void UnlockSkin()
        {
            if (!settings.UnlockSkin) return;

            var raceBodies = new Dictionary<Race, List<EquipmentEntity>>();
            var raceHeads = new Dictionary<Race, List<EquipmentEntity>>();
            var raceRamps = new Dictionary<Race, List<Texture2D>>();

            foreach (var racePreset in ResourcesLibrary.GetBlueprints<BlueprintRaceVisualPreset>())
            {
                var entityLinks = new List<EquipmentEntityLink>();
                Race racePresetRace;

                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    entityLinks.AddRange(racePreset.Skin.GetLinks(gender, racePreset.RaceId));
                }
                
                if (Enum.TryParse(racePreset.name.Split('_')[0], out racePresetRace)){
                    if (raceBodies.ContainsKey(racePresetRace))
                    {
                        raceBodies.Get(racePresetRace).AddRange(entityLinks.Select(e => e.Load()));
                    }
                    else raceBodies.Add(racePresetRace, entityLinks.Select(e => e.Load()).ToList());
                }
            } 

            foreach(var race in GetAllRaces().Where(bp => (bp.AssetGuid != "5c4e42124dc2b4647af6e36cf2590500" || Game.Instance.BlueprintRoot.DlcSettings.Tieflings.Enabled)))
            {
                var heads = (race.MaleOptions.Heads.Concat(race.FemaleOptions.Heads)).Select(h => h.Load() as EquipmentEntity);

                raceHeads.Add(race.RaceId, heads.ToList());
                raceRamps.Add(race.RaceId, heads.First().ColorsProfile.PrimaryRamps.ToList());
            }

            //DebugLog("body part keys: " + raceBodies.Keys.Aggregate("", (str, k) => str+k.ToString()));
            //DebugLog("colours base: " + raceBodies.Aggregate("", (str, k) => str + $" {k.Key} [ " +
            //$"{(String.Join(",", k.Value.First().PrimaryRamps.Select(r => r.name)))} ] \n"));
            //DebugLog("colours profile: " + raceBodies.Aggregate("", (str, k) => str + $" {k.Key} [ " +
            //$"{ (k.Value.First().ColorsProfile == null ? "no colorsprofile" : String.Join(",",k.Value.First().ColorsProfile.PrimaryRamps.Select(r => r.name)))} ] \n"));
            ////Select(v => (v.ColorsProfile != null ? v.ColorsProfile.PrimaryRamps : "")))}]\n"));
            //DebugLog("head parts: " + raceHeads.Aggregate("", (str, k) => str + $" {k.Key} [ {String.Join(",", k.Value.Select(v => v.name))}]\n"));
            //DebugLog("ramp parts: " + raceRamps.Aggregate("", (str, k) => str + $" {k.Key} [ {String.Join(",", k.Value.Select(v => v.name))}]\n"));

            var races = (IEnumerable<Race>)Enum.GetValues(typeof(Race));
            List<Texture2D> allRamps = races.Where(r => raceRamps.ContainsKey(r)).SelectMany(r => raceRamps.Get(r)).Distinct(new RampNameCompare()).ToList();
            allRamps.Sort((a, b) => a.name.Split('_')[2].CompareTo(b.name.Split('_')[2]));

            foreach (var race in races)
            {
                if (raceHeads.ContainsKey(race) && raceBodies.ContainsKey(race))
                {
                    var currentRamps = raceHeads.Get(race).First().PrimaryRamps;
                    var unifiedRamps = currentRamps.Concat(allRamps.Except(currentRamps)).ToList();
                    var profile = new CharacterColorsProfile { PrimaryRamps = unifiedRamps, SecondaryRamps = raceHeads.Get(race).First().SecondaryRamps };

                    foreach (var head in raceHeads.Get(race))
                    {
                        head.ColorsProfile = profile;
                    }

                    foreach (var part in raceBodies.Get(race))
                    {                    
                        part.ColorsProfile = profile;
                    }
                }
            }
        }

        static void AddHair(BlueprintRace sourceRace, BlueprintRace targetRace)
        {
            foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
            {
                var originalSource = GetOriginalOptions(sourceRace, gender);
                var originalTarget = GetOriginalOptions(targetRace, gender);
                var newSource = gender == Gender.Male ? sourceRace.MaleOptions : sourceRace.FemaleOptions;
                var newTarget = gender == Gender.Male ? targetRace.MaleOptions : targetRace.FemaleOptions;
                newTarget.Hair = Combine(newSource.Hair, newTarget.Hair);
                newTarget.Beards = Combine(newSource.Beards, newTarget.Beards);
                //AddEyebrowsDefaultEyebrows(newSource, newTarget, originalTarget);
            }
        }
        static void CopyOptions()
        {
            foreach (var race in GetAllRaces())
            {
                race.MaleOptions = new CustomizationOptions()
                {
                    Heads = (EquipmentEntityLink[])race.MaleOptions.Heads.Clone(),
                    Hair = (EquipmentEntityLink[])race.MaleOptions.Hair.Clone(),
                    Eyebrows = (EquipmentEntityLink[])race.MaleOptions.Eyebrows.Clone(),
                    Beards = (EquipmentEntityLink[])race.MaleOptions.Beards.Clone(),
                    Horns = (EquipmentEntityLink[])race.MaleOptions.Horns.Clone(),
                    TailSkinColors = (EquipmentEntityLink[])race.MaleOptions.TailSkinColors.Clone(),
                };
                race.FemaleOptions = new CustomizationOptions()
                {
                    Heads = (EquipmentEntityLink[])race.FemaleOptions.Heads.Clone(),
                    Hair = (EquipmentEntityLink[])race.FemaleOptions.Hair.Clone(),
                    Eyebrows = (EquipmentEntityLink[])race.FemaleOptions.Eyebrows.Clone(),
                    Beards = (EquipmentEntityLink[])race.FemaleOptions.Beards.Clone(),
                    Horns = (EquipmentEntityLink[])race.FemaleOptions.Horns.Clone(),
                    TailSkinColors = (EquipmentEntityLink[])race.FemaleOptions.TailSkinColors.Clone(),
                };
            }
        }

        [HarmonyPatch(typeof(CharBColorSelector), nameof(CharBColorSelector.SetData))]
        static class CharBColorSelector_SetData_Patch
        {
            //Postfix must be spelt correctly to be applied

            static void Postfix(CharBColorSelector __instance, LocalizedString label)
            {
                __instance.m_Label.text = "dicks per second";
            }
        }
        /*
 * DollState looks up the index of eyebrows by the index of heads,
 * so existing heads are duplicated and a default eyebrow from the
 * target class is added         * 
 */
        //static void AddEyebrowsDefaultEyebrows(CustomizationOptions newSource, CustomizationOptions newTarget, CustomizationOptions originalTarget)
        //{
        //    var newHeads = newTarget.Heads
        //        .Where(link => originalTarget.Heads.Contains(link))
        //        .Select(link => new EquipmentEntityLink() { AssetId = link.AssetId });
        //    newTarget.Heads = newTarget.Heads.AddRangeToArray(newHeads.ToArray());
        //    var newEyebrows = Enumerable.Repeat(newSource.Eyebrows[0], newTarget.Heads.Length - newTarget.Eyebrows.Length);
        //    newTarget.Eyebrows = newTarget.Eyebrows.AddRangeToArray(newEyebrows.ToArray());
        //}

        //static void UnlockFemaleDwarfBeards()
        //{
        //    if (!settings.UnlockFemaleDwarfBeards) return;
        //    var dwarf = ResourcesLibrary.TryGetBlueprint<BlueprintRace>("c4faf439f0e70bd40b5e36ee80d06be7");
        //    foreach (var race in GetAllRaces())
        //    {
        //        var originalSource = GetOriginalOptions(race, Gender.Male);
        //        dwarf.FemaleOptions.Beards = Combine(originalSource.Beards, dwarf.FemaleOptions.Beards);
        //    }
        //}

        //static void UnlockHornsAndTails()
        //{
        //    if (!Game.Instance.BlueprintRoot.DlcSettings.Tieflings.Enabled) return;
        //    var races = GetAllRaces().Where(bp =>
        //                bp.AssetGuid != "5c4e42124dc2b4647af6e36cf2590500" ).ToArray();
        //    var sourceRace = ResourcesLibrary.TryGetBlueprint<BlueprintRace>("5c4e42124dc2b4647af6e36cf2590500");
        //    foreach (var targetRace in races)
        //    {
        //        foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
        //        {
        //            var newSource = gender == Gender.Male ? sourceRace.MaleOptions : sourceRace.FemaleOptions;
        //            var newTarget = gender == Gender.Male ? targetRace.MaleOptions : targetRace.FemaleOptions;
        //            if (settings.UnlockHorns)
        //            {
        //                newTarget.Horns = Combine(newSource.Horns, newTarget.Horns);
        //            }
        //            if (settings.UnlockTail)
        //            {
        //                newTarget.TailSkinColors = Combine(newSource.TailSkinColors, newTarget.TailSkinColors);
        //            }
        //        }
        //    }
        //}

    }
}
