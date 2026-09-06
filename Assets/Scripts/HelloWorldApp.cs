using UnityEngine;
using System.Collections.Generic;

namespace OmniUnityApp
{
    public class HelloWorldApp : MonoBehaviour
    {
        // Campfire elements
        private Vector3 campfireCenter = new Vector3(0, 0.1f, 0);
        private Light campfireLight;
        private float baseLightIntensity = 2.5f;
        private List<GameObject> flameParticles = new List<GameObject>();
        private List<GameObject> smokeParticles = new List<GameObject>();
        private int maxParticles = 30;
        private bool isBurning = false;
        private float fireIntensity = 0f; // 0 to 1

        // Tree elements
        private GameObject proceduralTree;
        private List<Renderer> treeRenderers = new List<Renderer>();
        private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
        private float burnProgress = 0f; // 0 to 1
        private float treeHealth = 100f;

        // Camera controls
        private float cameraDistance = 8.0f;
        private float cameraHeight = 3.0f;
        private float cameraRotationSpeed = 100.0f;
        private float currentCameraAngle = 0f;

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
            if (dirLight == null || dirLight.type != LightType.Directional)
            {
                GameObject lightObj = new GameObject("Directional Light");
                Light l = lightObj.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.3f;
                l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
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

        void SetupScene()
        {
            // Create a ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = Vector3.one * 5f;
            ground.GetComponent<Renderer>().material.color = new Color(0.3f, 0.4f, 0.2f);

            BuildCampfire(campfireCenter);
            proceduralTree = BuildProceduralTree(new Vector3(0, 0, 2.5f), 4.0f);

            // Store tree renderers and original colors
            foreach (Renderer r in proceduralTree.GetComponentsInChildren<Renderer>())
            {
                treeRenderers.Add(r);
                originalColors[r] = r.material.color;
            }

            // Initialize particles
            for (int i = 0; i < maxParticles; i++)
            {
                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "FlameParticle_" + i;
                flame.transform.localScale = Vector3.zero; // Start invisible
                flame.transform.position = campfireCenter + Vector3.up * 0.1f;
                flame.GetComponent<Collider>().enabled = false;
                flameParticles.Add(flame);

                GameObject smoke = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                smoke.name = "SmokeParticle_" + i;
                smoke.transform.localScale = Vector3.zero; // Start invisible
                smoke.transform.position = campfireCenter + Vector3.up * 0.5f;
                smoke.GetComponent<Collider>().enabled = false;
                var smokeRend = smoke.GetComponent<Renderer>();
                if (smokeRend != null)
                {
                    smokeRend.material.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                }
                smokeParticles.Add(smoke);
            }
        }

        void Update()
        {
            UpdateCamera();

            if (campfireLight != null)
            {
                campfireLight.intensity = baseLightIntensity * fireIntensity + Mathf.PerlinNoise(Time.time * 8f, 0f) * 1.5f * fireIntensity;
            }

            if (isBurning)
            {
                // Update flame particles
                foreach (GameObject p in flameParticles)
                {
                    if (p.transform.localScale.x > 0.01f) // Only update visible particles
                    {
                        UpdateFlameParticle(p, 2.5f * fireIntensity, campfireCenter);
                    }
                }

                // Update smoke particles
                foreach (GameObject p in smokeParticles)
                {
                    if (p.transform.localScale.x > 0.01f)
                    {
                        UpdateSmokeParticle(p, 1.5f * fireIntensity, campfireCenter + Vector3.up * 0.5f);
                    }
                }

                // Burn simulation
                if (treeHealth > 0)
                {
                    UpdateBurnSimulation(fireIntensity, Time.deltaTime);
                    treeHealth -= fireIntensity * Time.deltaTime * 5f;
                    treeHealth = Mathf.Max(0, treeHealth);
                }
            }
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
            campfireLight = fireLightObj.AddComponent<Light>();
            campfireLight.type = LightType.Point;
            campfireLight.color = new Color(1f, 0.55f, 0.1f);
            campfireLight.range = 10f;
            campfireLight.intensity = 0f; // Start off
        }

        void UpdateFlameParticle(GameObject p, float speed, Vector3 origin)
        {
            p.transform.position += Vector3.up * speed * Time.deltaTime + new Vector3(Mathf.Sin(Time.time * 5f) * 0.05f, 0f, Mathf.Cos(Time.time * 5f) * 0.05f);
            float progress = (p.transform.position.y - origin.y) / 2.5f;
            float scale = Mathf.Lerp(0.35f, 0.05f, progress);
            p.transform.localScale = Vector3.one * scale * fireIntensity;
            var r = p.GetComponent<Renderer>();
            if (r != null) r.material.color = Color.Lerp(Color.yellow, Color.red, progress);
            if (progress >= 1f)
            {
                p.transform.position = origin + new Vector3(Random.Range(-0.2f, 0.2f), 0.1f, Random.Range(-0.2f, 0.2f));
            }
        }

        void UpdateSmokeParticle(GameObject p, float speed, Vector3 origin)
        {
            p.transform.position += Vector3.up * speed * Time.deltaTime + new Vector3(Mathf.Sin(Time.time * 3f + p.GetInstanceID()) * 0.08f, 0f, Mathf.Cos(Time.time * 3f + p.GetInstanceID()) * 0.08f);
            float progress = (p.transform.position.y - origin.y) / 4.0f;
            float scale = Mathf.Lerp(0.2f, 1.5f, progress);
            p.transform.localScale = Vector3.one * scale * fireIntensity;
            var r = p.GetComponent<Renderer>();
            if (r != null)
            {
                Color smokeColor = new Color(0.3f, 0.3f, 0.3f, Mathf.Lerp(0.5f, 0f, progress));
                r.material.color = smokeColor;
            }
            if (progress >= 1f)
            {
                p.transform.position = origin + new Vector3(Random.Range(-0.3f, 0.3f), 0.1f, Random.Range(-0.3f, 0.3f));
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
                if (rend != null)
                {
                    rend.material.color = Color.Lerp(originalColors[rend], charredColor, burnProgress);
                    // Shrink leaves slightly
                    if (rend.name.Contains("Foliage"))
                    {
                        rend.transform.localScale = Vector3.one * Mathf.Lerp(originalColors.Keys.ToList().Find(r => r == rend).transform.localScale.x, 0.8f, burnProgress);
                    }
                }
            }
        }

        void UpdateCamera()
        {
            currentCameraAngle += Input.GetAxis("Horizontal") * cameraRotationSpeed * Time.deltaTime;
            Vector3 cameraPosition = new Vector3(
                Mathf.Sin(currentCameraAngle * Mathf.Deg2Rad) * cameraDistance,
                cameraHeight,
                Mathf.Cos(currentCameraAngle * Mathf.Deg2Rad) * cameraDistance
            );
            Camera.main.transform.position = cameraPosition;
            Camera.main.transform.LookAt(Vector3.up * 1.5f); // Look at the center of the scene
        }

        void InitializeUIStyles()
        {
            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = 18;
            labelStyle.fontStyle = FontStyle.Bold;

            buttonStyle = new GUIStyle("button");
            buttonStyle.fontSize = 16;
            buttonStyle.fixedHeight = 30;
            buttonStyle.fixedWidth = 150;
        }

        void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 300, 30), "Burning Tree Campfire 3D", labelStyle);
            GUI.Label(new Rect(10, 40, 300, 30), $"Tree Health: {treeHealth:F1}%", labelStyle);
            GUI.Label(new Rect(10, 70, 300, 30), $"Burn Progress: {burnProgress * 100:F1}%", labelStyle);
            GUI.Label(new Rect(10, 100, 300, 30), $"Fire Intensity: {fireIntensity * 100:F0}%", labelStyle);

            if (GUI.Button(new Rect(10, Screen.height - 150, 150, 30), "Ignite", buttonStyle))
            {
                isBurning = true;
                fireIntensity = 0.5f;
                foreach (GameObject p in flameParticles) p.transform.localScale = Vector3.one * 0.1f;
                foreach (GameObject p in smokeParticles) p.transform.localScale = Vector3.one * 0.05f;
            }
            if (GUI.Button(new Rect(170, Screen.height - 150, 150, 30), "Extinguish", buttonStyle))
            {
                isBurning = false;
                fireIntensity = 0f;
                if (campfireLight != null) campfireLight.intensity = 0f;
                foreach (GameObject p in flameParticles) p.transform.localScale = Vector3.zero;
                foreach (GameObject p in smokeParticles) p.transform.localScale = Vector3.zero;
            }
            if (GUI.Button(new Rect(10, Screen.height - 110, 150, 30), "Increase Intensity", buttonStyle))
            {
                fireIntensity = Mathf.Min(1.0f, fireIntensity + 0.1f);
                isBurning = true;
            }
            if (GUI.Button(new Rect(170, Screen.height - 110, 150, 30), "Reset", buttonStyle))
            {
                ResetSimulation();
            }

            GUI.Label(new Rect(10, Screen.height - 70, 300, 20), "Controls: A/D or Left/Right Arrows to rotate camera", labelStyle);
            GUI.Label(new Rect(Screen.width - 200, 10, 190, 20), $"FPS: {1.0f / Time.deltaTime:F1}", labelStyle);
            GUI.Label(new Rect(Screen.width - 200, 30, 190, 20), $"Unity: {Application.unityVersion}", labelStyle);
            GUI.Label(new Rect(Screen.width - 200, 50, 190, 20), $"Platform: {Application.platform}", labelStyle);
        }

        void ResetSimulation()
        {
            isBurning = false;
            fireIntensity = 0f;
            burnProgress = 0f;
            treeHealth = 100f;

            if (campfireLight != null) campfireLight.intensity = 0f;
            foreach (GameObject p in flameParticles) p.transform.localScale = Vector3.zero;
            foreach (GameObject p in smokeParticles) p.transform.localScale = Vector3.zero;

            // Reset tree colors and scales
            foreach (var rend in treeRenderers)
            {
                if (rend != null)
                {
                    rend.material.color = originalColors[rend];
                    if (rend.name.Contains("Foliage"))
                    {
                        // Re-apply original scale (assuming originalColors.Keys maintains order or can be mapped)
                        // For simplicity, we'll just set a default scale if original scale isn't easily retrieved
                        rend.transform.localScale = Vector3.one * (rend.name.Contains("Foliage_0") ? 1.6f : (rend.name.Contains("Foliage_1") ? 1.3f : (rend.name.Contains("Foliage_2") ? 1.25f : (rend.name.Contains("Foliage_3") ? 1.2f : 1.0f))));
                    }
                }
            }
        }
    }
}

namespace OmniUnity
{
    public class MultipurposeRuntime : OmniUnityApp.HelloWorldApp { }
}