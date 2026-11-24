using System;
using System.Collections.Generic;
using System.IO;
using CozyIsland.Modding;
using ES3Internal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CozyMods.Examples
{
    public sealed class WitchRobe : ICozyMod
    {
        private const long MagicRobeProp = 1853934543L;
        private const long MagicHatProp = 815921587L;
        private static readonly Color ClothColor = new Color32(38, 123, 115, 255);
        private readonly Dictionary<Material, Material> tintedMaterials = new Dictionary<Material, Material>();
        private readonly List<CozyRecipe> recipes = new List<CozyRecipe>();
        private CozyWorkshopRecipeMgr recipeManager;
        private Transform equipmentTemplates;
        private GameObject robe, hat;
        private bool quitting;

        public void OnLoad(CozyModContext context)
        {
            var bundlePath = context.ReadPath("Assets/witch-robe-win64.bundle");
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null) throw new InvalidDataException("Could not open " + bundlePath);
            Application.quitting += OnQuitting;
            context.AddCleanup(() =>
            {
                Application.quitting -= OnQuitting;
                // Unity releases loaded bundles during player shutdown.
                if (!quitting) bundle.Unload(true);
            });

            var model = bundle.LoadAsset<GameObject>("Assets/WitchRobe/WitchRobe.prefab");
            Mesh Part(string name) => model.transform.Find(name).GetComponent<MeshFilter>().sharedMesh;

            var templates = new GameObject("Witch outfit equipment templates");
            templates.SetActive(false);
            templates.transform.SetParent(context.Root.transform, false);
            equipmentTemplates = templates.transform;
            context.AddCleanup(() =>
            {
                RemoveRecipes();
                foreach (var material in tintedMaterials.Values) Object.Destroy(material);
            });

            context.OnContentReady(() =>
            {
                if (robe != null) return;
                robe = Register(context, "witch-robe", "青苔魔法袍", MagicRobeProp, Part("Robe"), Part("Sleeve"));
                hat = Register(context, "witch-hat", "青苔魔法帽", MagicHatProp, Part("Hat"), null);
                context.Log($"Registered witch outfit: robe={robe.GetComponent<ES3Prefab>().prefabId}, hat={hat.GetComponent<ES3Prefab>().prefabId}");
            });
            context.OnUpdate(_ => UpdateRecipes());
        }

        public void OnUnload() { }

        private void OnQuitting() => quitting = true;

        private GameObject Register(CozyModContext context, string key, string itemName, long sourceId, Mesh mainMesh, Mesh sleeveMesh)
        {
            var source = ES3ReferenceMgrBase.Current.GetPrefab(sourceId, false).gameObject;
            var original = source.GetComponent<PCDEquipmentProp>().equipmentPrefab.GetComponent<PCDEquipment>();
            var wearable = CloneWearable(original, itemName, mainMesh, out var sourceMainMesh);
            Mesh sourceSleeveMesh = null;
            if (sleeveMesh != null)
            {
                var sleeve = CloneWearable(original.sleevePrefab.GetComponent<PCDEquipment>(), itemName + " Sleeve",
                    sleeveMesh, out sourceSleeveMesh);
                wearable.sleevePrefab = sleeve.gameObject;
            }

            var prop = CozyModRegistry.RegisterPrefab(context, key, source, clone =>
            {
                clone.GetComponent<CozyObjectIdentifier>().objectName = itemName;
                clone.GetComponent<PCDEquipmentProp>().equipmentPrefab = wearable.gameObject;
                foreach (var filter in clone.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == sourceMainMesh) ApplyMesh(filter, mainMesh);
                    else if (sourceSleeveMesh != null && filter.sharedMesh == sourceSleeveMesh) ApplyMesh(filter, sleeveMesh);
                }
            });
            wearable.equipmentPropPrefab = prop;
            return prop;
        }

        private PCDEquipment CloneWearable(PCDEquipment original, string name, Mesh mesh, out Mesh sourceMesh)
        {
            var copy = Object.Instantiate(original, equipmentTemplates, false);
            copy.name = name;
            var filter = copy.GetComponentInChildren<MeshFilter>(true);
            sourceMesh = filter.sharedMesh;
            ApplyMesh(filter, mesh);
            return copy;
        }

        private void ApplyMesh(MeshFilter filter, Mesh mesh)
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            var materials = renderer.sharedMaterials;
            var original = materials[0];
            if (!tintedMaterials.TryGetValue(original, out var material))
            {
                material = new Material(original) { name = original.name + " (Moss Witch)" };
                material.SetColor("_AlbedoColor", ClothColor);
                tintedMaterials.Add(original, material);
            }
            filter.sharedMesh = mesh;
            materials[0] = material;
            renderer.sharedMaterials = materials;
        }

        private void UpdateRecipes()
        {
            if (robe == null || hat == null) return;
            var current = CozyWorkshopRecipeMgr.TryGetInstance();
            if (current == null || current == recipeManager) return;
            RemoveRecipes();
            recipeManager = current;
            AddRecipe(robe);
            AddRecipe(hat);
        }

        private void AddRecipe(GameObject product)
        {
            var recipe = ScriptableObject.CreateInstance<CozyRecipe>();
            recipe.name = recipe.productName = product.GetComponent<CozyObjectIdentifier>().objectName;
            recipe.productPrefab = product;
            recipe.productDesc = "青苔配色的魔法服装 / Moss Witch outfit";
            recipe.workshopName = "基础工作台";
            recipe.category = "物体";
            recipe.needUnlock = false;
            recipe.demands = new List<ResourceDemand> { new ResourceDemand { resourceName = "原木", amount = 1 } };
            recipes.Add(recipe);
            if (!recipeManager.AddRecipe(recipe)) throw new InvalidOperationException("Recipe already exists: " + recipe.productName);
        }

        private void RemoveRecipes()
        {
            foreach (var recipe in recipes)
            {
                if (recipeManager != null && recipeManager.GetRecipeByName(recipe.productName) == recipe)
                    recipeManager.RemoveRecipe(recipe);
                Object.Destroy(recipe);
            }
            recipes.Clear();
        }
    }
}
