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
                
                //if(settings.UnlockHair) ChooseToggle(ref settings.UnlockAllHair, "Unlock All Hair Options (Includes incompatible options)");
                //if (BlueprintRoot.Instance.DlcSettings.Tieflings.Enabled)
                //{
                //    ChooseToggle(ref settings.UnlockHorns, "Unlock Horns");
                //    ChooseToggle(ref settings.UnlockTail, "Unlock Tails");
                //}
                //ChooseToggle(ref settings.UnlockFemaleDwarfBeards, "Unlock Female Dwarf Beards (Includes incompatible options)");
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
            CopyOptions();
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

        static void UnlockSkin()
        {
            if (!settings.UnlockSkin) return;

            var bodyEntities = new List<EquipmentEntity>();
            foreach (var race in ResourcesLibrary.GetBlueprints<BlueprintRaceVisualPreset>())
            {
                foreach (var gender in new Gender[] { Gender.Male, Gender.Female })
                {
                    var entities = race.Skin.GetLinks(gender, ((race != null) ? new Race?(race.RaceId) : null).Value);
                    bodyEntities.AddRange(entities.Select(e => e.Load()));
                }
            } 

            DebugLog("unlocking skins");
            List<BlueprintRace> Races = new List<BlueprintRace>(GetAllRaces().Where(
                bp => (bp.AssetGuid != "5c4e42124dc2b4647af6e36cf2590500" || Game.Instance.BlueprintRoot.DlcSettings.Tieflings.Enabled)));

            //DebugLog($"Races:\n {String.Join("\n",Races)}");

            var Heads = new List<EquipmentEntity>(Races.SelectMany(r => r.MaleOptions.Heads.Concat(r.FemaleOptions.Heads)).Select(h => h.Load(false) as EquipmentEntity));

            //DebugLog($"Heads:\n {String.Join("\n", Heads)}");

            Comparison<Texture2D> comparator = (a, b) => a.name.Split('_')[2].CompareTo(b.name.Split('_')[2]);
            List<Texture2D> dupeRamps = Heads.SelectMany(h => h.ColorsProfile.PrimaryRamps).ToList();
            List<Texture2D> Ramps = new List<Texture2D>();

            foreach(var eval_ramp in dupeRamps)
            {
                if (Ramps.Exists(ramp => comparator(ramp, eval_ramp) == 0) == false)
                {
                    Ramps.Add(eval_ramp);
                }
            }
            Ramps.Sort(comparator);
            
            //DebugLog($"ramps:\n {String.Join("\n", Ramps)}");

            foreach (var head in Heads)
            {
                head.ColorsProfile.PrimaryRamps = Ramps;
            }

            foreach(var body in bodyEntities)
            {
                if(body.ColorsProfile == null)
                {
                    body.ColorsProfile = new CharacterColorsProfile();
                }
                body.ColorsProfile.PrimaryRamps = Ramps;
            }
        }


        // PrimaryRamps does not have a setter so we have to make our own
         
        public delegate void SetPrimaryRampsDelegate(object obj, object value);

        public static SetPrimaryRampsDelegate GetPrimaryRampsSetter(object type_reference)
        {
            DebugLog("get type");
            Type EquipmentEntityType = type_reference.GetType();
            DebugLog($"get info: {EquipmentEntityType.FullName}");
            PropertyInfo rampProp = EquipmentEntityType.GetProperties().Where(prop => prop.Name == "PrimaryRamps").First();
            DebugLog("get field");
            var backingField = rampProp.DeclaringType.GetField($"<{rampProp.Name}>k__BackingField", BindingFlags.Public);
            return (obj, value) => backingField.SetValue(obj, value);
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
