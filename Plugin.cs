using BepInEx;
using UnityEngine;
using HarmonyLib;
using System;
using System.Globalization;

// Developed with GPT-5.6 Luna

namespace BallastCrate
{
    [BepInPlugin("com.Exocet.ballastcrate", "Ballast Crate Mod", "1.0.0")]
    public class BallastCratePlugin : BaseUnityPlugin
    {
        private const string ModDataPrefix = "BallastCrateMod.item.";
        private const string MassSuffix = ".mass";
        private const string NameSuffix = ".name";
        private const string ValueSuffix = ".value";
        private const float BallastMass = 10000f;
        private const int BallastValue = 1;
        private const string BallastName = "10,000 Weight Ballast Crate";

        private void Awake()
        {
            new Harmony("com.Exocet.ballastcrate").PatchAll();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                SpawnBallastCrate();
            }

        }

        private void SpawnBallastCrate()
        {

            var directory = SaveLoadManager.instance.GetComponent<PrefabsDirectory>();  
            
            GameObject ballast = directory.directory[23]; //copper ore crate

            Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 3f;
            Quaternion spawnRot = Quaternion.identity;

            GameObject obj = UnityEngine.Object.Instantiate(ballast, spawnPos, spawnRot);
            var item = obj.GetComponent<ShipItem>();
            var saveable = obj.GetComponent<SaveablePrefab>();

            item.sold = true;
            saveable.RegisterToSave();
            ApplyBallastProperties(item);
            SaveBallastProperties(saveable.instanceId, item);
            obj.GetComponent<Good>().RegisterAsMissionless();



            Logger.LogInfo("Spawned Ballast Crate (pickupable, mass=10000, affects ship stability)");
        }

        internal static void RestoreLoadedBallastCrates()
        {
            if (GameState.modData == null)
            {
                return;
            }

            foreach (var saveable in UnityEngine.Object.FindObjectsOfType<SaveablePrefab>())
            {
                if (IsBallastCrate(saveable.instanceId))
                {
                    ApplyBallastProperties(saveable.GetComponent<ShipItem>());
                }
            }
        }

        internal static void RestoreLoadedBallastCrate(ShipItem item)
        {
            if (item == null || item.GetComponent<SaveablePrefab>() == null)
            {
                return;
            }

            if (IsBallastCrate(item.GetComponent<SaveablePrefab>().instanceId))
            {
                ApplyBallastProperties(item);
            }
        }

        private static bool IsBallastCrate(int instanceId)
        {
            return GameState.modData != null &&
                (GameState.modData.ContainsKey(GetModDataKey(instanceId)) ||
                 GameState.modData.ContainsKey(GetModDataKey(instanceId) + MassSuffix));
        }

        private static string GetModDataKey(int instanceId)
        {
            return ModDataPrefix + instanceId;
        }

        private static void SaveBallastProperties(int instanceId, ShipItem item)
        {
            string key = GetModDataKey(instanceId);
            GameState.modData[key] = BallastMass.ToString(CultureInfo.InvariantCulture);
            GameState.modData[key + MassSuffix] = item.mass.ToString(CultureInfo.InvariantCulture);
            GameState.modData[key + NameSuffix] = item.name;
            GameState.modData[key + ValueSuffix] = item.value.ToString(CultureInfo.InvariantCulture);
        }

        private static void ApplyBallastProperties(ShipItem item)
        {
            if (item == null)
            {
                return;
            }

            SaveablePrefab saveable = item.GetComponent<SaveablePrefab>();
            string key = GetModDataKey(saveable.instanceId);
            float mass;
            int value;

            item.mass = GameState.modData != null &&
                float.TryParse(GetModDataValue(key + MassSuffix, BallastMass.ToString(CultureInfo.InvariantCulture)),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out mass)
                ? mass
                : BallastMass;
            item.name = GetModDataValue(key + NameSuffix, BallastName);
            item.value = GameState.modData != null &&
                int.TryParse(GetModDataValue(key + ValueSuffix, BallastValue.ToString(CultureInfo.InvariantCulture)),
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                ? value
                : BallastValue;
        }

        private static string GetModDataValue(string key, string fallback)
        {
            string value;
            return GameState.modData != null && GameState.modData.TryGetValue(key, out value)
                ? value
                : fallback;
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), nameof(SaveLoadManager.LoadModData))]
    internal static class BallastCrateLoadPatch
    {
        private static void Postfix()
        {
            BallastCratePlugin.RestoreLoadedBallastCrates();
        }
    }

    [HarmonyPatch(typeof(ShipItem), nameof(ShipItem.OnLoad))]
    internal static class BallastCrateShipItemLoadPatch
    {
        private static void Postfix(ShipItem __instance)
        {
            BallastCratePlugin.RestoreLoadedBallastCrate(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemRigidbody), nameof(ItemRigidbody.UpdateMass))]
    internal static class BallastCrateMassPatch
    {
        private static void Prefix(ItemRigidbody __instance)
        {
            BallastCratePlugin.RestoreLoadedBallastCrate(__instance.GetShipItem());
        }
    }
}
