using BepInEx;
using UnityEngine;
using HarmonyLib;
using System;
using System.Globalization;

// Developed with GPT-5.6 Luna

namespace BallastCrateMod
{
    [BepInPlugin("com.Exocet.ballastcrate", "Ballast Crate Mod", "1.0.0")]
    public class BallastCratePlugin : BaseUnityPlugin
    {
        private const string ModDataPrefix = "BallastCrateMod.item.";
        private const float BallastMass = 10000f;
        private const int BallastValue = 1;
        private const string BallastName = "10 tonne Ballast Crate";

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
            // Copying a lot of code from NANDCommand's item spawner script;

            // gets a directory list of all prefabs in the
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
            GameState.modData[GetModDataKey(saveable.instanceId)] = BallastMass.ToString(CultureInfo.InvariantCulture);
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
                if (GameState.modData.ContainsKey(GetModDataKey(saveable.instanceId)))
                {
                    ApplyBallastProperties(saveable.GetComponent<ShipItem>());
                }
            }
        }

        private static string GetModDataKey(int instanceId)
        {
            return ModDataPrefix + instanceId;
        }

        private static void ApplyBallastProperties(ShipItem item)
        {
            item.mass = BallastMass;
            item.name = BallastName;
            item.value = BallastValue;
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
}
