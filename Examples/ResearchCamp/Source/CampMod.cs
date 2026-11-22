using System;
using System.IO;
using System.Collections.Generic;
using CozyIsland.Modding;
using ES3Internal;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

namespace FreshEye.ResearchCamp
{
    [Serializable]
    public sealed class Progress
    {
        public int crafted, eaten, visited;
        public bool rewarded;
    }

    [Serializable]
    public sealed class Legacy
    {
        public int made, ate, visits;
        public bool claimed;
    }

    [Serializable]
    public sealed class Preference
    {
        public bool showHud = true;
    }

    public struct CampHello : NetworkMessage
    {
        public int protocol;
    }

    public struct CampSnapshot : NetworkMessage
    {
        public string json;
    }

    public sealed class CampMod : ICozyMod
    {
        const string RationName = "研究口粮";
        const string BlueprintName = "研究营地蓝图";

        // These IDs preserve the GameObject/component assignments in the original release.
        // Resolve by prefab-relative path, independent of sibling and component order.
        static readonly (string Path, long ObjectId, bool HasMesh)[] CampVisualNodes = {
            ("", 8010001, false),
            ("SurveyDesk", 8010003, true),
            ("Leg0", 8010007, true),
            ("Leg1", 8010011, true),
            ("Leg2", 8010015, true),
            ("Leg3", 8010019, true),
            ("MapBoard", 8010023, true),
            ("SampleJar", 8010027, true),
            ("Mast", 8010031, true),
            ("Flag", 8010035, true),
            ("Compass", 8010039, true)
        };
        static readonly (string Path, long ObjectId, bool HasMesh)[] RationVisualNodes = {
            ("", 8010001, false),
            ("Tin", 8010003, true),
            ("Lid", 8010007, true)
        };

        public static CampMod Active;
        public CozyModContext Context;
        public Progress State = new Progress();
        public Preference Pref = new Preference();
        public GameObject Ration, Blueprint, Camp;
        public string Notice = "Craft supplies at the base workbench.";
        public bool Ready;
        AssetBundle bundle;
        GameObject campVisual, rationVisual;
        CozyWorkshopRecipeMgr recipes;
        readonly List<CozyRecipe> ownRecipes = new List<CozyRecipe>();
        EasyCallbackItemWithArgument eat;
        bool serverRegistered, clientRegistered;
        float syncTime;
        string PreferencePath => Path.Combine(Application.persistentDataPath, Context.Id, "preferences.json");

        bool Running => Context != null && !Context.IsDisposed && Context.Root != null && Context.Root.activeSelf;

        public void OnLoad(CozyModContext ctx)
        {
            Active = this;
            Context = ctx;
            // Register each acquired resource immediately so a later OnLoad failure is safe.
            ctx.AddCleanup(() => { if (Active == this) Active = null; });
            if (File.Exists(PreferencePath))
                Pref = JsonUtility.FromJson<Preference>(File.ReadAllText(PreferencePath)) ?? new Preference();
            string bundleFile = Application.platform switch
            {
                RuntimePlatform.WindowsPlayer => "Assets/researchcamp-win64.bundle",
                RuntimePlatform.OSXPlayer => "Assets/researchcamp-macos.bundle",
                _ => throw new PlatformNotSupportedException("ResearchCamp supports the Windows and macOS Players")
            };
            bundle = AssetBundle.LoadFromFile(ctx.ReadPath(bundleFile));
            if (bundle == null)
                throw new InvalidDataException("Bundle failed to load: " + bundleFile);
            ctx.AddCleanup(() => { if (bundle != null) bundle.Unload(true); });
            campVisual = bundle.LoadAsset<GameObject>("assets/camp.prefab");
            rationVisual = bundle.LoadAsset<GameObject>("assets/ration.prefab");
            if (campVisual == null || rationVisual == null)
                throw new InvalidDataException("Missing bundle prefab");
            ctx.OnContentReady(Prepare);
            ctx.RegisterSaveData(2, Capture, Restore);
            ctx.Harmony.Patch(AccessTools.Method(typeof(CozyWorkshopMgr), "CreateProductWithRecipe"), postfix: new HarmonyMethod(typeof(CampMod), nameof(Crafted)));
            ctx.Harmony.Patch(AccessTools.Method(typeof(CozyWorkshop), "OnInteract"), postfix: new HarmonyMethod(typeof(CampMod), nameof(Visited)));
            ctx.Root.AddComponent<CampHud>().Mod = this;
            Writer<CampHello>.write = (w, m) => w.WriteInt(m.protocol);
            Reader<CampHello>.read = r => new CampHello
            {
                protocol = r.ReadInt()
            };
            Writer<CampSnapshot>.write = (w, m) => w.WriteString(m.json);
            Reader<CampSnapshot>.read = r => new CampSnapshot
            {
                json = r.ReadString()
            };
            ctx.OnUpdate(Update);
            ctx.OnSceneLoaded(s =>
            {
                if (s.name != "CozyIsland 2.0")
                {
                    Ready = false;
                    if (eat != null)
                        eat.RemoveSelf();
                    eat = null;
                }
            });
            ctx.AddCleanup(() =>
            {
                if (eat != null)
                    eat.RemoveSelf();
                RemoveRecipes();
                NetworkServer.UnregisterHandler<CampHello>();
                NetworkClient.UnregisterHandler<CampSnapshot>();
            });
            ctx.Log("AUTHOR_LOAD path=" + ctx.Directory + " bundle=" + bundleFile);
        }

        public void OnUnload()
        {
        }

        void Prepare()
        {
            var rm = ES3ReferenceMgrBase.Current;
            if (rm == null)
                throw new InvalidOperationException("No ES3 manager");
            Ration = CozyModRegistry.FindPrefab(Context.Id, "field-ration");
            if (Ration == null)
                Ration = CozyModRegistry.RegisterPrefab(Context, "field-ration", rm.GetPrefab(2773636279L, false).gameObject, g =>
                {
                    g.GetComponent<CozyObjectIdentifier>().objectName = RationName;
                    g.GetComponent<CozyFood>().powerContain = 4;
                    AttachVisual(g, rationVisual, RationVisualNodes);
                });
            Camp = CozyModRegistry.FindPrefab(Context.Id, "survey-camp");
            if (Camp == null)
                Camp = CozyModRegistry.RegisterPrefab(Context, "survey-camp", rm.GetPrefab(2174121949L, false).gameObject, g =>
                {
                    g.GetComponent<CozyObjectIdentifier>().objectName = "研究营地";
                    g.GetComponent<CozyWorkshop>().workshopName = "基础工作台";
                    var b = g.GetComponent<PCDBuilding>();
                    if (b != null)
                        b.buildingName = "研究营地";
                    AttachVisual(g, campVisual, CampVisualNodes);
                });
            Blueprint = CozyModRegistry.FindPrefab(Context.Id, "camp-blueprint");
            if (Blueprint == null)
                Blueprint = CozyModRegistry.RegisterPrefab(Context, "camp-blueprint", rm.GetPrefab(3112758868L, false).gameObject, g =>
                {
                    g.GetComponent<CozyObjectIdentifier>().objectName = BlueprintName;
                    g.GetComponent<PCDBuildingPart>().buildingPrefab = Camp;
                });
            var part = Blueprint.GetComponent<PCDBuildingPart>();
            part.buildingPrefab = Camp;
            var building = Camp.GetComponent<PCDBuilding>();
            if (building != null)
                building.buildingPartPrefab = Blueprint;
            Context.Log("CONTENT_READY bundleNodes=" + campVisual.GetComponentsInChildren<Transform>(true).Length + " campRefs=" + Camp.GetComponent<ES3Prefab>().localRefs.Count);
        }

        static void AttachVisual(GameObject target, GameObject asset, (string Path, long ObjectId, bool HasMesh)[] nodes)
        {
            foreach (var r in target.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
            var v = UnityEngine.Object.Instantiate(asset, target.transform, false);
            v.name = "ResearchCampVisual";
            var refs = target.GetComponent<ES3Prefab>().localRefs;
            foreach (var (path, id, hasMesh) in nodes)
            {
                var node = path.Length == 0 ? v.transform : v.transform.Find(path);
                if (node == null)
                    throw new InvalidDataException($"Missing saved visual node '{path}' in {asset.name}");
                refs.Add(node.gameObject, id);
                refs.Add(node, id + 1);
                if (hasMesh)
                {
                    var filter = node.GetComponent<MeshFilter>();
                    var renderer = node.GetComponent<MeshRenderer>();
                    if (filter == null || renderer == null)
                        throw new InvalidDataException($"Missing saved mesh components at '{path}' in {asset.name}");
                    refs.Add(filter, id + 2);
                    refs.Add(renderer, id + 3);
                }
            }

            // Every visual object/component needs an intentional, unique ID. This also
            // rejects duplicate paths or multiple components competing for a typed entry.
            foreach (var component in v.GetComponentsInChildren<Component>(true))
                if (component == null || !refs.ContainsKey(component) || !refs.ContainsKey(component.gameObject))
                    throw new InvalidDataException($"Unmapped saved visual object/component '{component}' in {asset.name}");
        }

        void Update(float dt)
        {
            if (!Running)
                return;
            if (!NetworkServer.active)
                serverRegistered = false;
            if (!NetworkClient.active)
                clientRegistered = false;
            if (NetworkServer.active && !serverRegistered)
            {
                NetworkServer.RegisterHandler<CampHello>((conn, msg) =>
                {
                    if (Running && msg.protocol == 1 && Ready)
                        conn.Send(new CampSnapshot { json = Capture() });
                }, true);
                serverRegistered = true;
                Context.Log("NETWORK_SERVER_READY");
            }

            if (NetworkClient.active && !clientRegistered)
            {
                NetworkClient.RegisterHandler<CampSnapshot>(msg =>
                {
                    if (Running && !Context.IsHost)
                    {
                        State = Validate(JsonUtility.FromJson<Progress>(msg.json));
                        Ready = true;
                        Notice = "Host progress received.";
                    }
                }, true);
                clientRegistered = true;
            }

            if (SceneManager.GetActiveScene().name != "CozyIsland 2.0")
                return;
            EnsureRecipes();
            if (Context.IsHost && Ready && eat == null && UnityEngine.Object.FindObjectOfType<EasyEvent>() != null)
                eat = EasyEvent.RegisterCallback("EatFood", OnEat);
            syncTime += dt;
            if (syncTime > 2)
            {
                syncTime = 0;
                if (NetworkClient.isConnected && !Context.IsHost)
                    NetworkClient.Send(new CampHello { protocol = 1 });
            }
        }

        void EnsureRecipes()
        {
            var m = UnityEngine.Object.FindObjectOfType<CozyWorkshopRecipeMgr>();
            if (m == null || Ration == null || Blueprint == null || m == recipes)
                return;
            RemoveRecipes();
            if (m.GetRecipeByName(RationName) != null || m.GetRecipeByName(BlueprintName) != null)
                throw new InvalidOperationException("A research camp recipe name is already in use");
            recipes = m;
            try
            {
                AddRecipe(RationName, Ration, 1);
                AddRecipe(BlueprintName, Blueprint, 2);
            }
            catch
            {
                RemoveRecipes();
                throw;
            }
            foreach (var recipe in ownRecipes)
                Context.Log("RECIPE_READY " + recipe.productName);
        }

        void AddRecipe(string name, GameObject prefab, int cost)
        {
            var r = ScriptableObject.CreateInstance<CozyRecipe>();
            ownRecipes.Add(r);
            r.name = r.productName = name;
            r.productDesc = "岛屿研究营地 / Island Research Camp";
            r.productPrefab = prefab;
            r.workshopName = "基础工作台";
            r.category = "物体";
            r.needUnlock = false;
            r.demands = new List<ResourceDemand>
            {
                new ResourceDemand
                {
                    resourceName = "原木",
                    amount = cost
                }
            };
            if (!recipes.AddRecipe(r))
                throw new InvalidOperationException("Recipe rejected: " + name);
        }

        void RemoveRecipes()
        {
            foreach (var r in ownRecipes)
            {
                if (recipes != null && recipes.GetRecipeByName(r.productName) == r)
                    recipes.RemoveRecipe(r);
                UnityEngine.Object.Destroy(r);
            }

            ownRecipes.Clear();
            recipes = null;
        }

        bool Is(GameObject g, string id) => g != null && g.GetComponent<CozyObjectIdentifier>()?.objectId == Context.Id + ":" + id;
        static void Crafted(CozyWorkshop __0, CozyRecipe __1, bool __result)
        {
            var m = Active;
            if (m == null || !m.Running || !m.Context.IsHost || !m.Ready || !__result || !m.Is(__1.productPrefab, "field-ration"))
                return;
            m.State.crafted = Math.Min(1, m.State.crafted + 1);
            m.Context.Log("QUEST_CRAFT " + m.Capture());
            m.TryReward(__0.transform.position);
        }

        static void Visited(CozyWorkshop __instance, bool __result)
        {
            var m = Active;
            if (m == null || !m.Running || !m.Context.IsHost || !m.Ready || !__result || !m.Is(__instance.gameObject, "survey-camp"))
                return;
            m.State.visited = 1;
            m.Context.Log("QUEST_VISIT " + m.Capture());
            m.TryReward(__instance.transform.position);
        }

        void OnEat(object arg)
        {
            if (!Running || !Context.IsHost || !Ready)
                return;
            var e = arg as EventFinishInfo;
            var g = e?.infoData as GameObject;
            if (!Is(g, "field-ration"))
                return;
            State.eaten = 1;
            Context.Log("QUEST_EAT " + Capture());
            TryReward(e.finisher.transform.position);
        }

        void TryReward(Vector3 pos)
        {
            if (State.rewarded || State.crafted < 1 || State.eaten < 1 || State.visited < 1)
                return;
            var toast = ES3ReferenceMgrBase.Current.GetPrefab(2773636279L, false);
            var obj = SpawnMgr.SpawnGameObject(toast.gameObject, pos + Vector3.up * 1.5f, Quaternion.identity, null, false);
            if (obj == null)
                throw new InvalidOperationException("Reward spawn failed");
            State.rewarded = true;
            Notice = "Survey complete! Toast reward delivered once.";
            Context.Log("QUEST_REWARD " + Capture());
        }

        string Capture()
        {
            if (!Ready)
                throw new InvalidOperationException("State not restored");
            return JsonUtility.ToJson(State);
        }

        void Restore(string json, int version)
        {
            Progress next;
            if (version == 0 && json == null)
                next = new Progress();
            else if (version == 1)
            {
                var x = JsonUtility.FromJson<Legacy>(json);
                if (x == null)
                    throw new InvalidDataException("Missing v1 state");
                next = new Progress
                {
                    crafted = x.made,
                    eaten = x.ate,
                    visited = x.visits,
                    rewarded = x.claimed
                };
            }
            else if (version == 2)
                next = JsonUtility.FromJson<Progress>(json);
            else
                throw new InvalidDataException("Unknown state version " + version);
            State = Validate(next);
            Ready = true;
            Context.Log("RESTORE version=" + version + " " + Capture());
        }

        static Progress Validate(Progress p)
        {
            if (p == null || p.crafted < 0 || p.eaten < 0 || p.visited < 0 || (p.rewarded && (p.crafted < 1 || p.eaten < 1 || p.visited < 1)))
                throw new InvalidDataException("Invalid research progress");
            return p;
        }

        public void ToggleHud()
        {
            Pref.showHud = !Pref.showHud;
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencePath));
            File.WriteAllText(PreferencePath, JsonUtility.ToJson(Pref));
        }
    }

    public sealed class CampHud : MonoBehaviour
    {
        public CampMod Mod;
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
                Mod.ToggleHud();
        }

        void OnGUI()
        {
            if (Mod == null || !Mod.Pref.showHud || SceneManager.GetActiveScene().name != "CozyIsland 2.0")
                return;
            GUI.Box(new Rect(20, 20, 330, 170), "ISLAND RESEARCH CAMP · F8");
            GUI.Label(new Rect(35, 50, 300, 110), "Craft a field ration: " + Mod.State.crafted + " / 1\nEat a field ration: " + Mod.State.eaten + " / 1\nVisit your survey camp: " + Mod.State.visited + " / 1\nReward: " + (Mod.State.rewarded ? "claimed" : "pending") + "\n" + (Mod.Ready ? Mod.Notice : "Waiting for world state..."));
        }
    }
}
