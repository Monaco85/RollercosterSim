using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Achterbahn.EditorTools
{
    /// <summary>„Achterbahn/Wikinger-Assets einrichten“: sammelt die Viking-Village-Prefabs in Assets/Viking Village/Resources/VikingSet.asset.</summary>
    public static class VikingSetup
    {
        const string F = "Assets/Viking Village";

        static GameObject P(string sub, string name)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{F}/{sub}/{name}");
            if (go == null) Debug.LogWarning("Viking Village: fehlt " + sub + "/" + name);
            return go;
        }
        static GameObject B(string n) => P("Prefabs/Buildings", "pf_build_" + n + ".prefab");
        static GameObject Pr(string n) => P("Prefabs/Props", "pf_" + n + ".prefab");
        static GameObject[] Arr(params GameObject[] a) => a.Where(x => x != null).ToArray();

        [MenuItem("Achterbahn/Wikinger-Assets einrichten")]
        public static void Run()
        {
            if (!AssetDatabase.IsValidFolder(F)) { Debug.LogWarning("Viking Village ist nicht importiert."); return; }
            if (!AssetDatabase.IsValidFolder(F + "/Resources")) AssetDatabase.CreateFolder(F, "Resources");
            string p = F + "/Resources/VikingSet.asset";
            var s = AssetDatabase.LoadAssetAtPath<VikingSet>(p);
            if (s == null) { s = ScriptableObject.CreateInstance<VikingSet>(); AssetDatabase.CreateAsset(s, p); }

            s.bigHouses = Arr(B("bighouse_01"), B("bighouse_02"), B("barracks_01"));
            s.houses = Arr(B("small_house_01"), B("small_house_straw_roof_01"), B("small_house_tall_roof_01"), B("barracks_single_01"));
            s.storages = Arr(B("storage_01"), B("big_storage_01"));
            s.smithies = Arr(B("blacksmith_01"));
            s.tower = B("tower_01"); s.gate = B("gate_01"); s.crane = B("crane_01");
            s.wallPanel = B("wall_panel_01"); s.wallCorner = B("wall_corner_01");
            s.boats = Arr(B("boat_01"), B("boat_02"));
            s.dragonhead = B("dragonhead_01");
            s.barrels = Arr(Pr("barrel_01"), Pr("barrels_01"), Pr("barrels_02"), Pr("barrel_fish_01"), Pr("bucket_01"), Pr("buckets_01"));
            s.logs = Arr(Pr("logpile_01"), Pr("wall_logs_04"));
            s.planks = Arr(Pr("plankpile_01"), Pr("plankpile_02"), Pr("plankpile_03"));
            s.fences = Arr(Pr("fence_01"), Pr("fence_01_double"), Pr("fence_01_tripple"), Pr("fence_02"), Pr("fence_02_double"), Pr("fence_02_quad"));
            s.sheds = Arr(Pr("shed_01"), Pr("shed_02"));
            s.shields = Arr(Pr("shield_01"), Pr("shield_02"), Pr("shield_03"));
            s.weapons = Arr(Pr("sword_01"), Pr("halberd_01"), Pr("shovel_01"));
            s.menhirs = Arr(Pr("menhir_01"), Pr("menhir_02"));
            s.boulders = Arr(Pr("boulder_01"), Pr("boulder_01_040"), Pr("boulder_01_150"), Pr("boulder_02_015"), Pr("boulder_02_025"));
            s.misc = Arr(Pr("pillar_03"), Pr("pillar_04"), Pr("pillar_05"), Pr("pillar_06"), Pr("plank_01"), Pr("plank_02"), Pr("plank_03"), Pr("fish_01"));
            s.plankPath = Pr("plankpath_01"); s.scaffold = Pr("scaffold_01"); s.skullStick = Pr("skull_stick_01");
            s.fishStick = Pr("fish_stick_01"); s.runeStone = Pr("rune_01");
            const string veg = "Book of the Dead/Vegetation";
            s.plants = Arr(P(veg + "/Ferns", "Fern_var01_Prefab.prefab"), P(veg + "/Ferns", "Fern_var02_Prefab.prefab"), P(veg + "/Ferns", "Fern_var03_Prefab.prefab"),
                P(veg + "/GreenBush", "GreenBush_Var01_Prefab.prefab"),
                P(veg + "/Plant_Perennials", "PH_Plant_Perennials_a2_1x1x2_A_Prefab.prefab"), P(veg + "/Plant_Perennials", "PH_Plant_Perennials_a2_1x1x2_B_Prefab.prefab"));
            s.grass = Arr(P(veg + "/MeadowGrass_01", "Meadow_Grass_01_Var1_Prefab.prefab"), P(veg + "/MeadowGrass_01", "Meadow_Grass_01_Var3_Prefab.prefab"),
                P(veg + "/MeadowGrass_01", "Meadow_Grass_01_Var5_Prefab.prefab"));
            s.mountains = AssetDatabase.LoadAssetAtPath<GameObject>(F + "/Models/Terrain/terrain_01.fbx");
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
            Debug.Log("Wikinger-Assets eingerichtet: " + p);
        }
    }
}
