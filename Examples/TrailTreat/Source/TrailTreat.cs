using System;
using System.IO;
using System.Collections.Generic;
using CozyIsland.Modding;
using ES3Internal;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Trial.NoBridge
{
    public sealed class TrailTreat : ICozyMod
    {
        public const string LocalId = "trail-biscuit", RecipeName = "Trial Trail Biscuit";
        public static TrailTreat Active;
        public CozyModContext Context;
        public GameObject Template;
        public Sprite Icon;
        public QuestData Progress = new QuestData();
        public string Notice = "Craft at 基础工作台: 1 原木 → Trail Biscuit";
        bool stateReady;
        EasyCallbackItemWithArgument eatSubscription;
        CozyWorkshopRecipeMgr manager;
        CozyRecipe recipe;
        readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        [Serializable]
        public sealed class QuestData
        {
            public int eaten;
            public bool rewarded;
        }

        public void OnLoad(CozyModContext ctx)
        {
            Active = this;
            Context = ctx;
            Icon = ctx.LoadSprite("Assets/trail.png");
            ctx.OnContentReady(Prepare);
            ctx.RegisterSaveData(2, CaptureProgress, RestoreProgress);
            ctx.Harmony.Patch(AccessTools.Method(typeof(CozyBackpackItemUI), "SetItemInfo"), postfix: new HarmonyMethod(typeof(TrailTreat), nameof(ReplaceIcon)));
            var hud = ctx.Root.AddComponent<TrailHud>();
            hud.Author = this;
            ctx.OnSceneLoaded(s =>
            {
                stateReady = false;
                if (eatSubscription != null)
                    eatSubscription.RemoveSelf();
                eatSubscription = null;
            });
            ctx.OnUpdate(dt =>
            {
                if (SceneManager.GetActiveScene().name != "CozyIsland 2.0")
                    return;
                EnsureRecipe();
                if (!ctx.IsHost || !stateReady)
                    return;
                if (eatSubscription == null && UnityEngine.Object.FindObjectOfType<EasyEvent>() != null)
                    eatSubscription = EasyEvent.RegisterCallback("EatFood", OnEat);
            });
            ctx.AddCleanup(() =>
            {
                if (eatSubscription != null)
                    eatSubscription.RemoveSelf();
                RemoveRecipe();
                foreach (var a in assets)
                    if (a != null)
                        UnityEngine.Object.Destroy(a);
                assets.Clear();
                if (Active == this)
                    Active = null;
            });
            ctx.Log("Author implementation loaded from " + ctx.Directory + "; native source Toast ID=2773636279; F8 journal");
        }

        public void OnUnload()
        {
        }

        void Prepare()
        {
            Context.Log("CONTENT_READY scene=" + SceneManager.GetActiveScene().name + " server=" + Mirror.NetworkServer.active + " client=" + Mirror.NetworkClient.active + " frame=" + Time.frameCount + " time=" + Time.realtimeSinceStartup + " recipeMgr=" + (UnityEngine.Object.FindObjectOfType<CozyWorkshopRecipeMgr>() != null));
            Template = CozyModRegistry.FindPrefab(Context.Id, LocalId);
            if (Template == null)
            {
                var rm = ES3ReferenceMgrBase.Current;
                if (rm == null)
                    throw new InvalidOperationException("No current native ES3 reference manager at content preparation");
                var source = rm.GetPrefab(2773636279L, false);
                if (source == null)
                    throw new InvalidOperationException("Native Toast template 2773636279 not available");
                Context.Log("Source " + source.name + " localRefs=" + source.localRefs.Count);
                Template = CozyModRegistry.RegisterPrefab(Context, LocalId, source.gameObject, Configure);
                Context.Log("Registered independent biscuit id=" + CozyModRegistry.GetPrefabId(Context.Id, LocalId) + " refs=" + Template.GetComponent<ES3Prefab>().localRefs.Count);
            }

            EnsureRecipe();
        }

        void EnsureRecipe()
        {
            if (Template == null)
                return;
            var next = UnityEngine.Object.FindObjectOfType<CozyWorkshopRecipeMgr>();
            if (next == null)
                return;
            if (manager == next && recipe != null)
                return;
            RemoveRecipe();
            var candidate = ScriptableObject.CreateInstance<CozyRecipe>();
            candidate.name = RecipeName;
            candidate.productName = RecipeName;
            candidate.productDesc = "Eat two trail biscuits to earn a Toast. F8 journal.";
            candidate.productPrefab = Template;
            candidate.workshopName = "基础工作台";
            candidate.category = "物体";
            candidate.needUnlock = false;
            candidate.demands = new List<ResourceDemand>
            {
                new ResourceDemand
                {
                    resourceName = "原木",
                    amount = 1
                }
            };
            try
            {
                if (!next.AddRecipe(candidate))
                    throw new InvalidOperationException("Native AddRecipe rejected " + RecipeName);
            }
            catch
            {
                UnityEngine.Object.Destroy(candidate);
                throw;
            }
            // Own only a successfully inserted recipe. Native removal is keyed by productName.
            manager = next;
            recipe = candidate;
            Context.Log("RECIPE_READY scene=" + SceneManager.GetActiveScene().name + " server=" + Mirror.NetworkServer.active + " client=" + Mirror.NetworkClient.active + " frame=" + Time.frameCount + " time=" + Time.realtimeSinceStartup + " Native recipe added manager=" + manager.GetInstanceID() + " workshop=" + recipe.workshopName + " demands=原木:1");
        }

        void RemoveRecipe()
        {
            if (manager != null && recipe != null && manager.GetRecipeByName(recipe.productName) == recipe)
                manager.RemoveRecipe(recipe);
            if (recipe != null)
                UnityEngine.Object.Destroy(recipe);
            manager = null;
            recipe = null;
        }

        void Configure(GameObject go)
        {
            go.GetComponent<CozyObjectIdentifier>().objectName = "Trail Biscuit";
            go.GetComponent<CozyFood>().powerContain = 3;
            // This new model is constructed in author code; retained Toast renderers are disabled.
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
            var body = new GameObject("Author_OctagonalSandwich");
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = Vector3.one * .15f;
            var refs = go.GetComponent<ES3Prefab>().localRefs;
            long id = 9100000;
            RegisterRef(refs, body, ref id);
            RegisterRef(refs, body.transform, ref id);
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            for (int i = 0; i < 3; i++)
            {
                var layer = new GameObject(i == 1 ? "MintFilling" : "BiscuitLayer" + i);
                layer.transform.SetParent(body.transform, false);
                layer.transform.localPosition = new Vector3(0, (i - 1) * .21f, 0);
                var filter = layer.AddComponent<MeshFilter>();
                var renderer = layer.AddComponent<MeshRenderer>();
                var mesh = Prism(8, 1f, i == 1 ? .14f : .24f);
                filter.sharedMesh = mesh;
                assets.Add(mesh);
                var mat = new Material(shader);
                mat.color = i == 1 ? new Color(.15f, .8f, .61f) : new Color(.94f, .69f, .3f);
                if (i == 2)
                {
                    mat.mainTexture = Icon.texture;
                }

                renderer.sharedMaterial = mat;
                assets.Add(mat);
                RegisterRef(refs, layer, ref id);
                RegisterRef(refs, layer.transform, ref id);
                RegisterRef(refs, filter, ref id);
                RegisterRef(refs, renderer, ref id);
            }

            // Additional independent hierarchy: four raised compass chips, each with its own mesh.
            var crown = new GameObject("CompassChips");
            crown.transform.SetParent(body.transform, false);
            RegisterRef(refs, crown, ref id);
            RegisterRef(refs, crown.transform, ref id);
            for (int i = 0; i < 4; i++)
            {
                var chip = new GameObject("Chip_" + i);
                chip.transform.SetParent(crown.transform, false);
                float a = i * Mathf.PI / 2;
                chip.transform.localPosition = new Vector3(Mathf.Cos(a) * .45f, .42f, Mathf.Sin(a) * .45f);
                chip.transform.localScale = new Vector3(.16f, .12f, .16f);
                var f = chip.AddComponent<MeshFilter>();
                var r = chip.AddComponent<MeshRenderer>();
                f.sharedMesh = Prism(4, 1, 1);
                assets.Add(f.sharedMesh);
                var m = new Material(shader);
                m.color = new Color(.12f, .25f, .2f);
                r.sharedMaterial = m;
                assets.Add(m);
                RegisterRef(refs, chip, ref id);
                RegisterRef(refs, chip.transform, ref id);
                RegisterRef(refs, f, ref id);
                RegisterRef(refs, r, ref id);
            }
        }

        static void RegisterRef(ES3RefIdDictionary refs, UnityEngine.Object obj, ref long id)
        {
            refs.Add(obj, ++id);
        }

        static Mesh Prism(int n, float rad, float h)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var t = new List<int>();
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < n; i++)
                {
                    float a = 2 * Mathf.PI * i / n;
                    v.Add(new Vector3(Mathf.Cos(a) * rad, (side - .5f) * h, Mathf.Sin(a) * rad));
                    uv.Add(new Vector2(.5f + Mathf.Cos(a) * .5f, .5f + Mathf.Sin(a) * .5f));
                }

            for (int i = 1; i < n - 1; i++)
            {
                t.Add(0);
                t.Add(i + 1);
                t.Add(i);
                t.Add(n);
                t.Add(n + i);
                t.Add(n + i + 1);
            }

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                t.Add(i);
                t.Add(j);
                t.Add(n + j);
                t.Add(i);
                t.Add(n + j);
                t.Add(n + i);
            }

            var m = new Mesh();
            m.name = "AuthorPrism" + n;
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        [Serializable]
        public sealed class SaveDataV2
        {
            public int biscuitsEaten;
            public bool rewardClaimed;
        }

        string CaptureProgress()
        {
            if (!stateReady) throw new InvalidOperationException("TrailTreat state was not restored");
            return JsonUtility.ToJson(new SaveDataV2 { biscuitsEaten = Progress.eaten, rewardClaimed = Progress.rewarded });
        }

        void RestoreProgress(string json, int dataVersion)
        {
            stateReady = false;
            QuestData next;
            if (dataVersion == 0 && json == null) next = new QuestData();
            else if (dataVersion == 1) next = JsonUtility.FromJson<QuestData>(json); // Earlier in-world payload: eaten/rewarded.
            else if (dataVersion == 2)
            {
                var saved = JsonUtility.FromJson<SaveDataV2>(json);
                if (saved == null) throw new InvalidDataException("Missing TrailTreat state");
                next = new QuestData { eaten = saved.biscuitsEaten, rewarded = saved.rewardClaimed };
            }
            else throw new InvalidDataException("Unsupported TrailTreat data version: " + dataVersion);
            if (next == null || next.eaten < 0 || (next.rewarded && next.eaten < 2))
                throw new InvalidDataException("Invalid TrailTreat progress");
            // Publish only after complete validation. No world mutation during restoration.
            Progress = next;
            stateReady = true;
            Notice = Progress.rewarded ? "Quest complete! Reward already claimed." : "Craft at 基础工作台: 1 原木 → Trail Biscuit";
            Context.Log("Quest restored dataVersion=" + dataVersion + " eaten=" + Progress.eaten + " rewarded=" + Progress.rewarded);
        }

        void OnEat(object arg)
        {
            // Native events are not managed callbacks: suspension retains their subscription
            // until world exit, so the author must stop them explicitly at the entry point.
            if (Context == null || Context.IsDisposed || Context.Root == null || !Context.Root.activeSelf ||
                !Context.IsHost || !stateReady)
                return;
            var e = arg as EventFinishInfo;
            var obj = e == null ? null : e.infoData as GameObject;
            if (obj == null || obj.GetComponent<CozyObjectIdentifier>()?.objectId != Context.Id + ":" + LocalId)
                return;
            Progress.eaten = checked(Progress.eaten + 1);
            if (Progress.eaten >= 2 && !Progress.rewarded)
            {
                var toast = ES3ReferenceMgrBase.Current.GetPrefab(2773636279L, false);
                var reward = SpawnMgr.SpawnGameObject(toast.gameObject, e.finisher.transform.position + e.finisher.transform.forward + Vector3.up, Quaternion.identity, null, false);
                Progress.rewarded = true;
                Notice = "Quest complete! Original Toast reward spawned.";
                Context.Log("QUEST_REWARD native Toast spawned=" + (reward != null));
            }
            else
                Notice = "Trail supply quest " + Math.Min(2, Progress.eaten) + "/2";
            Context.Log("NATIVE_EAT_EVENT eaten=" + Progress.eaten + " rewarded=" + Progress.rewarded);
        }

        static void ReplaceIcon(CozyBackpackItemUI __instance, CozyBackpackItemInfo __0)
        {
            var a = Active;
            if (a != null && __0 != null && __0.prefabId == CozyModRegistry.GetPrefabId(a.Context.Id, LocalId) && __instance.itemIconImage != null)
            {
                __instance.itemIconImage.sprite = a.Icon;
                __instance.itemIconImage.color = Color.white;
            }
        }
    }

    public sealed class TrailHud : MonoBehaviour
    {
        public TrailTreat Author;
        bool visible = true;
        Rect panel = new Rect(24, 24, 370, 155);
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
                visible = !visible;
        }

        void OnGUI()
        {
            if (!visible || Author == null || SceneManager.GetActiveScene().name != "CozyIsland 2.0")
                return;
            panel = GUI.Window(789312, panel, Draw, "TRAIL JOURNAL  ·  F8");
        }

        void Draw(int id)
        {
            GUI.DrawTexture(new Rect(14, 30, 60, 60), Author.Icon.texture);
            GUI.Label(new Rect(85, 30, 270, 48), "Trail supply quest\nEat two crafted biscuits: " + Math.Min(2, Author.Progress.eaten) + " / 2");
            GUI.Label(new Rect(14, 100, 340, 48), Author.Notice);
            GUI.DragWindow(new Rect(0, 0, 370, 24));
        }
    }
}
