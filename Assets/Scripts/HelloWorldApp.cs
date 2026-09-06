using UnityEngine;
using System.Collections.Generic;

namespace OmniUnityApp
{
    public class HelloWorldApp : MonoBehaviour
    {
        // Campfire elements
        private Vector3 campfireCenter = new Vector3(0, 0, 0);
        private Light fireLight;
        private float baseLightIntensity = 2.5f;
        private List<GameObject> flameParticles = new List<GameObject>();
        private List<GameObject> smokeParticles = new List<GameObject>();
        private int maxFlameParticles = 30;
        private int maxSmokeParticles = 15;
        private bool isFireActive = false;
        private float fireIntensity = 1.0f;

        // Tree elements
        private GameObject proceduralTree;
        private List<Renderer> treeRenderers = new List<Renderer>();
        private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
        private float burnProgress = 0f;
        private float treeHealth = 100f;
        private bool isTreeBurning = false;

        // Camera controls
        private float cameraDistance = 10.0f;
        private float cameraYaw = 0.0f;
        private float cameraPitch = 20.0f;
        private float cameraSensitivity = 3.0f;

        // UI
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootstrap()
        {
            // Ensure Camera
            if (Camera.main == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                Camera cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.transform.position = new Vector3(0, 5, -10);
                cam.transform.LookAt(Vector3.zero);
                camObj.tag = "MainCamera";
            }

            // Ensure Directional Light
            Light dirLight = FindObjectOfType<Light>();
            if (dirLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                dirLight = lightObj.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                dirLight.intensity = 1.3f;
                dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // Ensure HelloWorldApp exists
            if (FindObjectOfType<HelloWorldApp>() == null)
            {
                GameObject runner = new GameObject("OmniUnity_Runner");
                runner.AddComponent<HelloWorldApp>();
                DontDestroyOnLoad(runner);
            }
        }

        void Start()
        {
            InitializeUIStyles();
            SetupScene();
        }

        void InitializeUIStyles()
        {
            labelStyle = new GUIStyle
            {
                fontSize = 18,
                normal = { textColor = Color.white }
            };
            buttonStyle = new GUIStyle("Button")
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
        }

        void SetupScene()
        {
            // Create ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = Vector3.one * 5f;
            ground.GetComponent<Renderer>().material.color = new Color(0.3f, 0.2f, 0.15f); // Dirt color

            BuildCampfire(campfireCenter);
            proceduralTree = BuildProceduralTree(new Vector3(0, 0, 3f), 4.5f);

            // Store tree renderers and original colors
            foreach (Renderer r in proceduralTree.GetComponentsInChildren<Renderer>())
            {
                treeRenderers.Add(r);
                originalColors[r] = r.material.color;
            }

            // Get the light component
            fireLight = GameObject.Find("CampfireLight")?.GetComponent<Light>();
        }

        void Update()
        {
            UpdateCamera();

            if (isFireActive)
            {
                UpdateFireLight();
                UpdateFlameParticles();
                UpdateSmokeParticles();

                if (isTreeBurning)
                {
                    UpdateBurnSimulation(fireIntensity, Time.deltaTime);
                    treeHealth -= fireIntensity * Time.deltaTime * 2f;
                    treeHealth = Mathf.Max(0, treeHealth);
                }
            }
        }

        void UpdateCamera()
        {
            if (Input.GetMouseButton(0))
            {
                cameraYaw += Input.GetAxis("Mouse X") * cameraSensitivity;
                cameraPitch -= Input.GetAxis("Mouse Y") * cameraSensitivity;
                cameraPitch = Mathf.Clamp(cameraPitch, 5f, 80f);
            }

            Quaternion rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0);
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -cameraDistance);
            Vector3 position = rotation * negDistance + campfireCenter + Vector3.up * 2f;

            Camera.main.transform.rotation = rotation;
            Camera.main.transform.position = position;
        }

        void BuildCampfire(Vector3 center)
        {
            // 1. Logs in crossed circle
            int logCount = 5;
            for (int i = 0; i < logCount; i++)
            {
                float angle = i * (360f / logCount);
                GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                log.name = "CampfireLog_" + i;
                log.transform.localScale = new Vector3(0.18f, 0.7f, 0.18f);
                log.transform.position = center + new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 0.3f, 0.15f, Mathf.Cos(angle * Mathf.Deg2Rad) * 0.3f);
                log.transform.rotation = Quaternion.Euler(60f, angle + 90f, 0f);
                var rend = log.GetComponent<Renderer>();
                if (rend != null) rend.material.color = new Color(0.25f, 0.14f, 0.08f); // Charred bark
            }

            // 2. Flickering Fire Light
            GameObject fireLightObj = new GameObject("CampfireLight");
            fireLightObj.transform.position = center + Vector3.up * 0.5f;
            Light fireLight = fireLightObj.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.color = new Color(1f, 0.55f, 0.1f);
            fireLight.range = 10f;
            fireLight.intensity = 0f; // Start off
        }

        void UpdateFireLight()
        {
            if (fireLight != null)
            {
                fireLight.intensity = baseLightIntensity * fireIntensity + Mathf.PerlinNoise(Time.time * 8f, 0f) * 1.5f * fireIntensity;
            }
        }

        void UpdateFlameParticles()
        {
            while (flameParticles.Count < maxFlameParticles)
            {
                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "FlameParticle";
                flame.transform.position = campfireCenter + new Vector3(Random.Range(-0.2f, 0.2f), 0.1f, Random.Range(-0.2f, 0.2f));
                flame.transform.localScale = Vector3.one * 0.1f;
                flame.GetComponent<Collider>().enabled = false;
                flameParticles.Add(flame);
            }

            foreach (GameObject p in flameParticles)
            {
                if (p != null)
                {
                    UpdateFlameParticle(p, 1.5f * fireIntensity, campfireCenter);
                }
            }
        }

        void UpdateFlameParticle(GameObject p, float speed, Vector3 origin)
        {
            p.transform.position += Vector3.up * speed * Time.deltaTime + new Vector3(Mathf.Sin(Time.time * 5f) * 0.05f, 0f, Mathf.Cos(Time.time * 5f) * 0.05f);
            float progress = (p.transform.position.y - origin.y) / (2.5f * fireIntensity);
            float scale = Mathf.Lerp(0.35f, 0.05f, progress);
            p.transform.localScale = Vector3.one * scale;
            var r = p.GetComponent<Renderer>();
            if (r != null) r.material.color = Color.Lerp(Color.yellow, Color.red, progress);
            if (progress >= 1f)
            {
                p.transform.position = origin + new Vector3(Random.Range(-0.2f, 0.2f), 0.1f, Random.Range(-0.2f, 0.2f));
            }
        }

        void UpdateSmokeParticles()
        {
            while (smokeParticles.Count < maxSmokeParticles)
            {
                GameObject smoke = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                smoke.name = "SmokeParticle";
                smoke.transform.position = campfireCenter + Vector3.up * 1.0f + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
                smoke.transform.localScale = Vector3.one * 0.2f;
                smoke.GetComponent<Collider>().enabled = false;
                var rend = smoke.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Dark gray translucent
                }
                smokeParticles.Add(smoke);
            }

            foreach (GameObject p in smokeParticles)
            {
                if (p != null)
                {
                    p.transform.position += Vector3.up * 0.5f * Time.deltaTime * fireIntensity + new Vector3(Mathf.Sin(Time.time * 2f) * 0.02f, 0f, Mathf.Cos(Time.time * 2f) * 0.02f);
                    float progress = (p.transform.position.y - (campfireCenter.y + 1.0f)) / (5f * fireIntensity);
                    float scale = Mathf.Lerp(0.2f, 1.5f, progress);
                    p.transform.localScale = Vector3.one * scale;
                    var rend = p.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        Color c = rend.material.color;
                        rend.material.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.5f, 0f, progress));
                    }
                    if (progress >= 1f)
                    {
                        p.transform.position = campfireCenter + Vector3.up * 1.0f + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
                        var rendReset = p.GetComponent<Renderer>();
                        if (rendReset != null) rendReset.material.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                    }
                }
            }
        }

        GameObject BuildProceduralTree(Vector3 basePosition, float height)
        {
            GameObject tree = new GameObject("ProceduralTree");
            tree.transform.position = basePosition;

            // 1. Trunk
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.parent = tree.transform;
            trunk.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            trunk.transform.localScale = new Vector3(0.45f, height * 0.5f, 0.45f);
            var trunkRend = trunk.GetComponent<Renderer>();
            if (trunkRend != null) trunkRend.material.color = new Color(0.38f, 0.22f, 0.12f);

            // 2. Layered Foliage Clusters
            Vector3[] clusterOffsets = new Vector3[] {
                new Vector3(0f, height * 0.85f, 0f),
                new Vector3(0.35f, height * 0.75f, 0.2f),
                new Vector3(-0.3f, height * 0.8f, -0.25f),
                new Vector3(0.2f, height * 0.95f, -0.2f),
                new Vector3(-0.2f, height * 1.05f, 0.15f)
            };
            float[] clusterScales = new float[] { 1.6f, 1.3f, 1.25f, 1.2f, 1.0f };

            for (int i = 0; i < clusterOffsets.Length; i++)
            {
                GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                foliage.name = "Foliage_" + i;
                foliage.transform.parent = tree.transform;
                foliage.transform.localPosition = clusterOffsets[i];
                foliage.transform.localScale = Vector3.one * clusterScales[i];
                var fRend = foliage.GetComponent<Renderer>();
                if (fRend != null)
                {
                    float greenTint = 0.6f + (i * 0.08f);
                    fRend.material.color = new Color(0.18f, greenTint, 0.22f);
                }
            }
            return tree;
        }

        void UpdateBurnSimulation(float burnIntensity, float dt)
        {
            burnProgress = Mathf.Clamp01(burnProgress + (burnIntensity * dt * 0.08f));
            Color charredColor = new Color(0.1f, 0.08f, 0.08f);

            foreach (var rend in treeRenderers)
            {
                if (rend != null && originalColors.ContainsKey(rend))
                {
                    rend.material.color = Color.Lerp(originalColors[rend], charredColor, burnProgress);
                    // Shrink leaves slightly
                    if (rend.name.Contains("Foliage"))
                    {
                        rend.transform.localScale = Vector3.Lerp(Vector3.one * originalColors.Keys.IndexOf(rend), Vector3.one * 0.8f, burnProgress);
                    }
                }
            }
        }

        void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 300, 30), "Burning Tree Campfire 3D", labelStyle);
            GUI.Label(new Rect(10, 40, 300, 30), $"Tree Health: {treeHealth:0.0}%", labelStyle);
            GUI.Label(new Rect(10, 70, 300, 30), $"Fire Intensity: {fireIntensity:0.0}", labelStyle);

            if (GUI.Button(new Rect(10, 110, 150, 40), "Ignite Campfire", buttonStyle))
            {
                isFireActive = true;
                fireIntensity = 1.0f;
                if (fireLight != null) fireLight.intensity = baseLightIntensity;
            }
            if (GUI.Button(new Rect(10, 160, 150, 40), "Extinguish Campfire", buttonStyle))
            {
                isFireActive = false;
                fireIntensity = 0f;
                if (fireLight != null) fireLight.intensity = 0f;
                foreach (var p in flameParticles) Destroy(p);
                flameParticles.Clear();
                foreach (var p in smokeParticles) Destroy(p);
                smokeParticles.Clear();
            }
            if (GUI.Button(new Rect(10, 210, 150, 40), "Increase Fire", buttonStyle))
            {
                fireIntensity = Mathf.Min(fireIntensity + 0.2f, 2.0f);
            }
            if (GUI.Button(new Rect(10, 260, 150, 40), "Decrease Fire", buttonStyle))
            {
                fireIntensity = Mathf.Max(fireIntensity - 0.2f, 0.1f);
            }
            if (GUI.Button(new Rect(10, 310, 150, 40), "Start Tree Burning", buttonStyle))
            {
                isTreeBurning = true;
                isFireActive = true; // Ensure fire is active to burn tree
                fireIntensity = Mathf.Max(fireIntensity, 1.0f);
            }
            if (GUI.Button(new Rect(10, 360, 150, 40), "Reset Scene", buttonStyle))
            {
                // Destroy existing objects
                Destroy(GameObject.Find("CampfireLight"));
                foreach (GameObject log in GameObject.FindGameObjectsWithTag("CampfireLog")) Destroy(log);
                foreach (GameObject p in flameParticles) Destroy(p);
                foreach (GameObject p in smokeParticles) Destroy(p);
                Destroy(proceduralTree);
                Destroy(GameObject.Find("Plane"));

                flameParticles.Clear();
                smokeParticles.Clear();
                treeRenderers.Clear();
                originalColors.Clear();

                // Re-setup
                burnProgress = 0f;
                treeHealth = 100f;
                isFireActive = false;
                isTreeBurning = false;
                fireIntensity = 1.0f;
                SetupScene();
            }

            GUI.Label(new Rect(Screen.width - 200, 10, 190, 30), $"FPS: {1.0f / Time.deltaTime:00}", labelStyle);
            GUI.Label(new Rect(Screen.width - 200, 40, 190, 30), $"Unity: {Application.unityVersion}", labelStyle);
            GUI.Label(new Rect(Screen.width - 200, 70, 190, 30), $"Platform: {Application.platform}", labelStyle);
            GUI.Label(new Rect(Screen.width - 200, 100, 190, 30), "Mouse Drag: Orbit Camera", labelStyle);
        }
    }
}

namespace OmniUnity
{
    public class MultipurposeRuntime : OmniUnityApp.HelloWorldApp { }
}