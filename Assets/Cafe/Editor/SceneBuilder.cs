using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static Luar.EditorTools.CafeKit;

namespace Luar.EditorTools
{
    /// <summary>
    /// Genera las 4 escenas (Inicio, Tutorial, Recorrido, Felicitaciones) con la misma
    /// identidad visual de cafetería pastel cozy. Menú: Luar Café VR > 2. Construir escenas.
    /// </summary>
    public static class SceneBuilder
    {
        const string SceneDir = "Assets/Scenes";
        const float EyeHeight = 1.6f;

        [MenuItem("Luar Café VR/2. Construir las 4 escenas")]
        public static void BuildAll()
        {
            BuildInicio();
            BuildTutorial();
            BuildRecorrido();
            BuildFelicitaciones();
            EditorBuildSettings.scenes = new[] { SceneFlow.Inicio, SceneFlow.Tutorial, SceneFlow.Recorrido, SceneFlow.Felicitaciones }
                .Select(n => new EditorBuildSettingsScene($"{SceneDir}/{n}.unity", true)).ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene($"{SceneDir}/{SceneFlow.Inicio}.unity");
            Debug.Log("[Luar] Escenas construidas");
        }

        // =====================================================================
        // Comunes
        // =====================================================================
        static Scene NewScene()
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return s;
        }

        static readonly System.Type[] DynamicTypes =
        {
            typeof(Sway), typeof(Bob), typeof(Spin), typeof(Pulse), typeof(Billboard), typeof(GazeInteractable),
            typeof(LazyFollowPanel), typeof(ParticleSystem), typeof(Light), typeof(PlayerRig)
        };

        /// <summary>Marca como estático (static batching) todo lo que nunca se mueve: mejora el rendimiento en móvil.</summary>
        static void MarkStatic(Scene s)
        {
            foreach (var root in s.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    bool dynamic = false;
                    for (var a = t; a != null && !dynamic; a = a.parent)
                        foreach (var type in DynamicTypes)
                            if (a.GetComponent(type) != null) { dynamic = true; break; }
                    if (!dynamic && t.GetComponent<Renderer>() != null)
                        GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic);
                }
        }

        static void Save(Scene s, string name)
        {
            MarkStatic(s);
            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s, $"{SceneDir}/{name}.unity");
        }

        static void Lighting(float warmth = 1f)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("F3E6E2") * 0.95f * warmth;
            RenderSettings.ambientEquatorColor = Hex("E8D3CF") * 0.92f * warmth;
            RenderSettings.ambientGroundColor = Hex("EEDDD6") * 0.92f * warmth;
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var go = new GameObject("Luz Sol (calida)");
            go.transform.eulerAngles = new Vector3(58, -30, 0);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = Hex("FFEBDB");
            l.intensity = 0.55f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.35f;
        }

        static Transform Env() => new GameObject("Entorno").transform;

        static float YawTo(Vector3 from, Vector3 to)
        {
            var d = to - from;
            return Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }

        /// <summary>Jugador: Rig > CameraOffset (1.6 m) > Main Camera (Gaze, fader, mano).</summary>
        static PlayerRig Rig(Vector3 pos, float yaw)
        {
            var rigGo = new GameObject("PlayerRig");
            rigGo.transform.position = pos;
            rigGo.transform.eulerAngles = new Vector3(0, yaw, 0);
            var rig = rigGo.AddComponent<PlayerRig>();

            var offset = new GameObject("CameraOffset").transform;
            offset.SetParent(rigGo.transform, false);
            offset.localPosition = new Vector3(0, EyeHeight, 0);
            offset.gameObject.AddComponent<EditorMouseLook>();

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(offset, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 40f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Cream;
            camGo.AddComponent<AudioListener>();

            var tpd = camGo.AddComponent<TrackedPoseDriver>();
            tpd.positionInput = new InputActionProperty(new InputAction("Posicion", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
            tpd.rotationInput = new InputActionProperty(new InputAction("Rotacion", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));

            // Mano virtual: donde flotan los objetos agarrados
            var hold = new GameObject("HoldAnchor").transform;
            hold.SetParent(camGo.transform, false);
            hold.localPosition = new Vector3(0.1f, -0.25f, 0.6f);

            // Fundido
            var fader = Prim(PrimitiveType.Quad, "Fader", camGo.transform, new Vector3(0, 0, 0.1f), new Vector3(2f, 2f, 1f),
                Unlit("Fader", Cream, cullOff: true, transparent: true), shadows: false);
            fader.GetComponent<MeshRenderer>().receiveShadows = false;
            fader.AddComponent<ScreenFader>().color = Cream;

            // Puntero Gaze
            var gp = camGo.AddComponent<GazePointer>();
            gp.lightMat = Unlit("Reticle_White", Color.white, cullOff: true);
            gp.darkMat = Unlit("Reticle_Cocoa", Cocoa, cullOff: true);
            gp.accentMat = Unlit("Reticle_Rose", Hex("E58FA6"), cullOff: true);
            var label = Text("HintLabel", camGo.transform, Vector3.zero, new Vector2(11.5f, 1.9f), "", 14f, Cocoa);
            label.fontStyle = FontStyles.Bold;
            gp.hintLabel = label;
            var back = Prim(PrimitiveType.Quad, "HintBackdrop", camGo.transform, Vector3.zero, Vector3.one, Unlit("Hint_Backdrop", Ivory, cullOff: true), shadows: false);
            gp.hintBackdrop = back;

            rig.head = camGo.transform;
            rig.holdAnchor = hold;
            return rig;
        }

        /// <summary>
        /// Pizarra/cartel con marco rosa y encabezado festoneado. yaw = dirección en la que mira quien la lee.
        /// </summary>
        static Transform Board(Transform parent, Vector3 pos, float yaw, float w, float h, string title, string body,
            out TextMeshPro titleT, out TextMeshPro bodyT, bool easel = false, float titleSize = 2.2f, float bodySize = 1.2f)
        {
            var g = Group("Cartel_" + title.Replace(" ", ""), parent, pos, yaw);
            Box("Marco", g, new Vector3(0, 0, 0.012f), new Vector3(w + 0.1f, h + 0.1f, 0.035f), MRose, col: true);
            Box("Fondo", g, new Vector3(0, 0, -0.01f), new Vector3(w, h, 0.02f), MIvory);
            // festón superior
            int n = Mathf.RoundToInt(w / 0.16f);
            for (int i = 0; i < n; i++)
            {
                float x = -w / 2f + (i + 0.5f) * w / n;
                Cyl("Feston", g, new Vector3(x, h / 2f + 0.05f, 0.005f), w / n, 0.03f, i % 2 == 0 ? MBlush : MIvory, new Vector3(90, 0, 0));
            }
            Heart(g, new Vector3(0, h / 2f + 0.12f, -0.01f), 0.14f, MRose).gameObject.AddComponent<Pulse>();
            float titleH = Mathf.Min(0.26f, h * 0.22f);
            titleT = Text("Titulo", g, new Vector3(0, h / 2f - titleH / 2f - 0.06f, -0.025f), new Vector2(w - 0.16f, titleH), title, titleSize, Hex("C9738A"), style: FontStyles.Bold);
            Box("Separador", g, new Vector3(0, h / 2f - titleH - 0.09f, -0.022f), new Vector3(w * 0.35f, 0.008f, 0.004f), MBlush, shadows: false);
            float bodyTop = h / 2f - titleH - 0.12f;
            float bodyH = h - titleH - 0.2f;
            bodyT = Text("Texto", g, new Vector3(0, bodyTop - bodyH / 2f, -0.025f), new Vector2(w - 0.2f, bodyH), body, bodySize, Cocoa, TextAlignmentOptions.Top);
            if (easel)
            {
                float legH = pos.y + h / 2f - 0.2f;
                Cyl("Pata_L", g, new Vector3(-w * 0.35f, -pos.y + legH / 2f, 0.07f), 0.04f, legH, MWood, new Vector3(3, 0, 3));
                Cyl("Pata_R", g, new Vector3(w * 0.35f, -pos.y + legH / 2f, 0.07f), 0.04f, legH, MWood, new Vector3(3, 0, -3));
                Cyl("Pata_T", g, new Vector3(0, -pos.y + legH / 2f, 0.3f), 0.04f, legH, MWood, new Vector3(-12, 0, 0));
            }
            return g;
        }

        /// <summary>Botón píldora 3D (cuerpo + extremos redondos + texto).</summary>
        static GazeButton Button(Transform parent, Vector3 localPos, string label, Material color, float w = 0.62f, float h = 0.2f)
        {
            var g = Group("Boton_" + label.Replace(" ", ""), parent, localPos);
            var visual = Group("Visual", g, Vector3.zero);
            Box("Sombra", g, new Vector3(0, -0.012f, 0.02f), new Vector3(w + h, h, 0.02f), MLatte, shadows: false);
            Box("Cuerpo", visual, Vector3.zero, new Vector3(w, h, 0.05f), color);
            Cyl("Extremo_L", visual, new Vector3(-w / 2f, 0, 0), h, 0.05f, color, new Vector3(90, 0, 0));
            Cyl("Extremo_R", visual, new Vector3(w / 2f, 0, 0), h, 0.05f, color, new Vector3(90, 0, 0));
            var t = Text("Etiqueta", visual, new Vector3(0, 0, -0.03f), new Vector2(w + h * 0.5f, h * 0.8f), label, 1.1f, Cocoa, style: FontStyles.Bold);
            var col = g.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(w + h, h + 0.02f, 0.08f);
            var b = g.gameObject.AddComponent<GazeButton>();
            b.pressVisual = visual;
            b.hoverTarget = visual;
            b.hoverScale = 1.08f;
            b.hint = label;
            return b;
        }

        static GazeButton SceneButton(Transform parent, Vector3 pos, string label, Material color, string scene, float w = 0.62f)
        {
            var b = Button(parent, pos, label, color, w);
            b.action = GazeButton.Action.LoadScene;
            b.sceneName = scene;
            return b;
        }

        static GazeButton QuitButton(Transform parent, Vector3 pos, float w = 0.62f)
        {
            var b = Button(parent, pos, "Salir", MLavender, w);
            b.action = GazeButton.Action.Quit;
            b.hint = "Salir de la app";
            return b;
        }

        /// <summary>Flecha (chevron) que rebota; se usa para marcar puntos de salto y objetivos del tutorial.</summary>
        static Transform Chevron(Transform parent, Vector3 pos, float s, Material m)
        {
            var g = Group("Flecha", parent, pos);
            var inner = Group("Chevron", g, Vector3.zero);
            Box("A", inner, new Vector3(-0.09f, 0.09f, 0) * s, new Vector3(0.07f, 0.26f, 0.07f) * s, m, new Vector3(0, 0, 45));
            Box("B", inner, new Vector3(0.09f, 0.09f, 0) * s, new Vector3(0.07f, 0.26f, 0.07f) * s, m, new Vector3(0, 0, -45));
            inner.gameObject.AddComponent<Billboard>();
            var bob = g.gameObject.AddComponent<Bob>(); bob.amplitude = 0.07f; bob.speed = 2.5f;
            return g;
        }

        static TeleportPoint Teleport(Transform parent, Vector3 pos, string label, bool start = false)
        {
            var g = Group("Salto_" + label.Replace(" ", ""), parent, pos);
            var col = g.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.05f, 0);
            col.size = new Vector3(0.9f, 0.1f, 0.9f);
            var visual = Group("Visual", g, Vector3.zero);
            Cyl("Aro", visual, new Vector3(0, 0.012f, 0), 0.8f, 0.012f, MIvory);
            Cyl("Disco", visual, new Vector3(0, 0.016f, 0), 0.68f, 0.012f, Lit("Teleport_Mint", Hex("A9D9C8")));
            Cyl("Centro", visual, new Vector3(0, 0.02f, 0), 0.34f, 0.012f, MIvory);
            var marker = Chevron(visual, new Vector3(0, 1.05f, 0), 1f, Lit("Teleport_Mint", Hex("A9D9C8")));
            var mcol = marker.gameObject.AddComponent<SphereCollider>();
            mcol.radius = 0.14f;
            var t = Text("Etiqueta", marker, new Vector3(0, 0.3f, 0), new Vector2(0.9f, 0.16f), label, 1.1f, Cocoa, style: FontStyles.Bold);
            t.gameObject.AddComponent<Billboard>();
            var tp = g.gameObject.AddComponent<TeleportPoint>();
            tp.label = label;
            tp.visual = visual;
            tp.dwellTime = 1.2f;
            tp.startHere = start;
            return tp;
        }

        static Grabbable MakeGrabbable(GameObject go, string id, string display)
        {
            FitBox(go, false, 0.03f);
            var gr = go.AddComponent<Grabbable>();
            gr.itemId = id;
            gr.displayName = display;
            gr.hoverScale = 1.12f;
            return gr;
        }

        /// <summary>Plato sobre la mesa donde se coloca un pedido, con aro indicador pulsante.</summary>
        static PlacementZone Zone(Transform parent, Vector3 pos, string acceptedId, string hint)
        {
            var g = Group("Zona_" + (string.IsNullOrEmpty(acceptedId) ? "Libre" : acceptedId), parent, pos);
            Cyl("Plato", g, new Vector3(0, 0.008f, 0), 0.24f, 0.016f, MIvory);
            Cyl("PlatoBorde", g, new Vector3(0, 0.012f, 0), 0.2f, 0.016f, MBlush);
            var ind = Group("Indicador", g, Vector3.zero);
            Cyl("Aro", ind.transform, new Vector3(0, 0.02f, 0), 0.34f, 0.006f, Unlit("Zone_Glow", Hex("F6B7C6")));
            Cyl("Hueco", ind.transform, new Vector3(0, 0.021f, 0), 0.28f, 0.006f, MIvory);
            var col = g.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.08f, 0);
            col.size = new Vector3(0.36f, 0.16f, 0.36f);
            var snap = Group("Snap", g, new Vector3(0, 0.017f, 0));
            var z = g.gameObject.AddComponent<PlacementZone>();
            z.acceptedId = acceptedId;
            z.snapPoint = snap;
            z.indicator = ind.gameObject;
            z.hint = hint;
            z.dwellTime = 1.2f;
            return z;
        }

        static TextMeshPro Neon(Transform parent, Vector3 pos, float yaw, string text, float w, float h)
        {
            var g = Group("Neon", parent, pos, yaw);
            var t = Text("Letrero", g, new Vector3(0, 0, -0.02f), new Vector2(w, h), text, 6f, Hex("E994AE"), style: FontStyles.Bold | FontStyles.Italic);
            var lgo = new GameObject("Resplandor");
            lgo.transform.SetParent(g, false);
            lgo.transform.localPosition = new Vector3(0, 0, -0.4f);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point; l.color = Hex("FFB8CB"); l.range = 2.2f; l.intensity = 0.8f; l.shadows = LightShadows.None;
            lgo.AddComponent<LightFlicker>().baseIntensity = 0.8f;
            Heart(g, new Vector3(w / 2f + 0.05f, 0.05f, -0.02f), 0.22f, MNeon).gameObject.AddComponent<Pulse>();
            Heart(g, new Vector3(-w / 2f - 0.05f, 0.05f, -0.02f), 0.22f, MNeon).gameObject.AddComponent<Pulse>();
            return t;
        }

        /// <summary>Decoración común: ventanas en arco, lámparas, plantas, cuadros, nubes. Mantiene la identidad en todas las escenas.</summary>
        static void CommonDecor(Transform env, float w, float d, float h, IEnumerable<Vector3> lampSpots, bool eastWindows = true, bool westWindows = true, bool southArt = true)
        {
            float hx = w / 2f - 0.07f, hz = d / 2f - 0.07f;
            if (eastWindows)
                for (float z = -d / 2f + 1.8f; z < d / 2f - 1.2f; z += 2.6f)
                    ArchWindow(env, new Vector3(hx, 0, z), 90);
            if (westWindows)
                for (float z = -d / 2f + 1.8f; z < d / 2f - 1.2f; z += 2.6f)
                    ArchWindow(env, new Vector3(-hx, 0, z), -90);

            Plant(env, new Vector3(w / 2f - 0.45f, 0, d / 2f - 0.45f), 1.8f, MIvory);
            Plant(env, new Vector3(-w / 2f + 0.45f, 0, d / 2f - 0.45f), 1.8f, MIvory);
            Plant(env, new Vector3(w / 2f - 0.45f, 0, -d / 2f + 0.45f), 1.6f);
            Plant(env, new Vector3(-w / 2f + 0.45f, 0, -d / 2f + 0.45f), 1.6f);

            Material[] shades = { MButter, MBlush, MMint, MLavender };
            int i = 0;
            foreach (var p in lampSpots)
                PendantLamp(env, p, h, shades[i++ % shades.Length], light: i <= 3);

            // Pared sur: cuadros y nubes
            if (southArt)
            {
            Frame(env, new Vector3(-1.2f, 1.9f, -hz), 180, new Vector2(0.6f, 0.8f), MBlush, MMint);
            Frame(env, new Vector3(0f, 2.0f, -hz), 180, new Vector2(0.5f, 0.5f), MLavender, MButter);
            Frame(env, new Vector3(1.2f, 1.9f, -hz), 180, new Vector2(0.6f, 0.8f), MMint, MPeach);
            }
            Cloud(env, new Vector3(-2.6f, 2.6f, -hz + 0.02f), 180, 1.1f);
            Cloud(env, new Vector3(2.4f, 2.7f, -hz + 0.02f), 180, 0.8f);

            HangingPlant(env, new Vector3(-w / 2f + 1.0f, h, -d / 2f + 1.0f));
            HangingPlant(env, new Vector3(w / 2f - 1.0f, h, -d / 2f + 1.0f));
        }

        static void TableSet(Transform env, Vector3 pos, float chairYaw, int chairs = 2, bool surface = true, bool vase = true)
        {
            RoundTable(env, pos, 0.8f, surface);
            Material[] cush = { MBlush, MMint, MLavender, MButter };
            for (int i = 0; i < chairs; i++)
            {
                float a = (chairYaw + i * 360f / chairs) * Mathf.Deg2Rad;
                var p = pos + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 0.62f;
                // la silla mira hacia -z local: orientarla hacia la mesa
                Chair(env, p, YawTo(pos, p), cush[i % cush.Length]);
            }
            if (vase) Vase(env, pos + new Vector3(0.12f, 0.77f, 0.1f));
        }

        // =====================================================================
        // 1. INICIO
        // =====================================================================
        static void BuildInicio()
        {
            var s = NewScene();
            Lighting();
            var env = Env();
            float W = 9, D = 9, H = 3.2f;
            Room(env, W, D, H);
            var start = new Vector3(0, 0, -2.4f);
            Rig(start, 0);

            // Fondo: mostrador con toldo y letrero neón
            Counter(env, new Vector3(0, 0, 3.75f), 180, 4f, out _);
            EspressoMachine(env, new Vector3(-1.3f, 1.045f, 3.8f), 180, out _, out _, out var steam);
            Steam(env, new Vector3(-1.3f, 1.2f, 3.55f), true);
            CakeStand(env, new Vector3(1.1f, 1.045f, 3.7f));
            Vase(env, new Vector3(0.2f, 1.045f, 3.6f));
            Cupcake(env, new Vector3(1.6f, 1.045f, 3.55f), MBlush);
            Cupcake(env, new Vector3(1.75f, 1.045f, 3.7f), MMint);
            Awning(env, new Vector3(0, 2.25f, 4.4f), 180, 4.4f);
            Neon(env, new Vector3(0, 2.72f, 4.36f), 0, "Luar Café", 2.6f, 0.55f);
            WallShelf(env, new Vector3(-3.1f, 1.85f, 4.43f), 0, 1.6f);
            WallShelf(env, new Vector3(3.1f, 1.85f, 4.43f), 0, 1.6f);

            // Cartel principal + botones
            var mainPos = new Vector3(0, 1.75f, 0.45f);
            Board(env, mainPos, 0, 2.3f, 1.2f, "¡Bienvenidos a Luar Café!",
                "Una experiencia en realidad virtual para aprender a atender una cafetería cozy.\n\n" +
                "Elige una opción <b>mirando un botón</b> hasta llenar el anillo, o toca el botón de tu visor Cardboard.",
                out _, out _, easel: true);
            var menu = Group("Menu", env, new Vector3(0, 0.95f, 0.3f));
            SceneButton(menu, new Vector3(-0.8f, 0, 0), "Tutorial", MMint, SceneFlow.Tutorial);
            SceneButton(menu, new Vector3(0, 0, 0), "Recorrido", MBlush, SceneFlow.Recorrido);
            QuitButton(menu, new Vector3(0.8f, 0, 0));
            Box("Atril", menu, new Vector3(0, -0.22f, 0.08f), new Vector3(2.5f, 0.06f, 0.2f), MWood);

            // Paneles laterales de instrucciones
            var head = start + Vector3.up * EyeHeight;
            var lp = new Vector3(-2.15f, 1.7f, -0.3f);
            Board(env, lp, YawTo(head, lp), 1.5f, 1.35f, "¿Cómo se juega?",
                "• El <b>punto blanco</b> es tu puntero: mueve la cabeza para apuntar.\n" +
                "• Mira un botón <b>1,5 s</b> o toca el botón del visor.\n" +
                "• Los <b>círculos menta</b> del piso son saltos: míralos para moverte.\n" +
                "• Mira un objeto para <b>recogerlo</b> y luego un plato para <b>colocarlo</b>.",
                out _, out _, easel: true, bodySize: 1f);
            var rp = new Vector3(2.15f, 1.7f, -0.3f);
            Board(env, rp, YawTo(head, rp), 1.5f, 1.35f, "Escenas",
                "<b>1. Tutorial</b>\nAprende a mirar, saltar, agarrar y colocar.\n\n" +
                "<b>2. Recorrido</b>\nSirve los pedidos de 3 clientes.\n\n" +
                "<b>3. Felicitaciones</b>\n¡Celebra tu primer turno!\n\n" +
                "Para salir: botón <b>Salir</b> o la <b>X</b> del visor.",
                out _, out _, easel: true, bodySize: 1f);

            // Mesas y ambientación
            TableSet(env, new Vector3(-3.0f, 0, 1.9f), 0, 2);
            TableSet(env, new Vector3(3.0f, 0, 1.9f), 0, 2);
            TableSet(env, new Vector3(-3.2f, 0, -2.6f), 90, 2);
            TableSet(env, new Vector3(3.2f, 0, -2.6f), 90, 2);
            Rug(env, new Vector3(0, 0, -1.2f), 2.6f, 2.2f);
            CommonDecor(env, W, D, H, new[] { new Vector3(-3.0f, 2.2f, 1.9f), new Vector3(3.0f, 2.2f, 1.9f), new Vector3(0, 2.4f, 3.2f), new Vector3(0, 2.5f, -3.4f) });
            for (int i = 0; i < 5; i++)
            {
                var hp = Heart(env, new Vector3(-1.8f + i * 0.9f, 2.95f, 2.5f), 0.12f, i % 2 == 0 ? MBlush : MLavender);
                var b = hp.gameObject.AddComponent<Bob>(); b.amplitude = 0.05f;
                var c = Cyl("Hilo", hp, new Vector3(0, 0.15f, 0), 0.005f, 0.3f, MIvory);
            }
            Save(s, SceneFlow.Inicio);
        }

        // =====================================================================
        // 2. TUTORIAL
        // =====================================================================
        static void BuildTutorial()
        {
            var s = NewScene();
            Lighting();
            var env = Env();
            float W = 9, D = 10, H = 3.2f;
            Room(env, W, D, H);
            var start = new Vector3(0, 0, -3.2f);
            var rig = Rig(start, 0);
            Teleport(env, start, "Inicio", start: true);

            // Barra de práctica
            Counter(env, new Vector3(1.9f, 0, 1.35f), 180, 1.8f, out _);
            EspressoMachine(env, new Vector3(2.45f, 1.045f, 1.45f), 180, out _, out _, out _);
            Steam(env, new Vector3(2.45f, 1.2f, 1.2f), true);
            Awning(env, new Vector3(1.9f, 2.25f, 4.9f), 180, 3.2f);
            Neon(env, new Vector3(1.9f, 2.72f, 4.86f), 0, "Escuela de baristas", 2.8f, 0.45f);
            WallShelf(env, new Vector3(-2.0f, 1.85f, 4.93f), 0, 1.8f);
            Counter(env, new Vector3(1.9f, 0, 4.3f), 180, 3.0f, out _);
            CakeStand(env, new Vector3(1.2f, 1.045f, 4.25f));
            Vase(env, new Vector3(2.6f, 1.045f, 4.2f));

            var cupGo = CoffeeCup(env, new Vector3(1.45f, 1.045f, 1.15f), MBlush, "Taza de práctica");
            var cup = MakeGrabbable(cupGo, "cafe", "Taza");

            // Mesa de la clienta con plato
            TableSet(env, new Vector3(-0.1f, 0, 1.0f), 0, 1, surface: true, vase: false);
            Customer(env, new Vector3(-0.1f, 0, 1.62f), 0, MLavender, MLatte);
            var zone = Zone(env, new Vector3(-0.1f, 0.77f, 0.82f), "", "Colocar aquí");

            // Punto de salto del tutorial
            var tpPos = new Vector3(1.0f, 0, -0.2f);
            var tp = Teleport(env, tpPos, "Barra de práctica");

            // Panel de instrucciones que acompaña al jugador
            var head = start + Vector3.up * EyeHeight;
            var panelPos = head + new Vector3(-0.75f, 0.15f, 2.1f);
            var panel = Board(env, panelPos, YawTo(head, panelPos), 1.6f, 1.05f, "Tutorial", "", out var titleT, out var bodyT, bodySize: 1.1f);
            panel.name = "PanelTutorial";
            Object.DestroyImmediate(panel.Find("Marco").GetComponent<Collider>()); // no bloquear la mirada
            panel.gameObject.AddComponent<LazyFollowPanel>().offset = new Vector3(-0.75f, 0.15f, 2.1f);
            var counter = Text("Paso", panel, new Vector3(0.62f, -0.44f, -0.025f), new Vector2(0.35f, 0.08f), "Paso 1 de 4", 0.8f, Rose, TextAlignmentOptions.Right);

            var startBtn = Button(panel, new Vector3(0, -0.72f, 0), "Comenzar", MMint, 0.6f);
            var finish = Group("BotonesFinal", panel, new Vector3(0, -0.72f, 0));
            SceneButton(finish, new Vector3(-0.45f, 0, 0), "Ir al Recorrido", MBlush, SceneFlow.Recorrido, 0.62f);
            SceneButton(finish, new Vector3(0.45f, 0, 0), "Menú", MLavender, SceneFlow.Inicio, 0.4f);

            // Flechas guía por paso
            var pulse = startBtn.gameObject.AddComponent<Pulse>(); pulse.amount = 0.05f; pulse.speed = 4f;
            finish.gameObject.SetActive(false);
            var hl2 = Chevron(env, tpPos + new Vector3(0, 1.65f, 0), 1.0f, MRose);
            var hl3 = Chevron(env, cupGo.transform.position + new Vector3(0, 0.3f, 0), 0.5f, MRose);
            var hl4 = Chevron(env, zone.transform.position + new Vector3(0, 0.32f, 0), 0.5f, MRose);

            var ctrl = new GameObject("TutorialController").AddComponent<TutorialController>();
            ctrl.titleText = titleT;
            ctrl.bodyText = bodyT;
            ctrl.stepCounter = counter;
            ctrl.startButton = startBtn.gameObject;
            ctrl.finishButtons = finish.gameObject;
            ctrl.steps = new[]
            {
                new TutorialController.Step { title = "1. Tu puntero (Gaze)", highlight = null,
                    body = "El <b>punto blanco</b> del centro es tu puntero. Mueve la cabeza para apuntar.\n\nMira el botón <b>Comenzar</b> sin mover la cabeza hasta llenar el anillo (o toca el botón del visor)." },
                new TutorialController.Step { title = "2. Teletranspórtate", highlight = hl2.gameObject,
                    body = "Los <b>círculos menta</b> del piso son puntos de salto.\n\nMira el círculo de la <b>Barra de práctica</b> (junto a la flecha rosa) para moverte hasta allí." },
                new TutorialController.Step { title = "3. Agarra un objeto", highlight = hl3.gameObject,
                    body = "Mira la <b>taza rosada</b> sobre la barra hasta llenar el anillo.\n\nLa taza flotará frente a ti: la llevas contigo aunque te muevas." },
                new TutorialController.Step { title = "4. Colócalo", highlight = hl4.gameObject,
                    body = "Ahora mira el <b>plato con aro rosado</b> de la mesa de la clienta.\n\nTambién puedes dejar objetos mirando cualquier mesa o barra." },
                new TutorialController.Step { title = "¡Lo lograste!", highlight = null,
                    body = "Ya sabes mirar, saltar, agarrar y colocar.\n\nAhora ve al <b>Recorrido</b> y atiende a los clientes de Luar Café." },
            };
            UnityEventTools.AddPersistentListener(startBtn.onClick, ctrl.CompleteLookStep);
            startBtn.hint = "Comenzar";

            // Ambientación
            TableSet(env, new Vector3(-2.8f, 0, -1.2f), 45, 2);
            TableSet(env, new Vector3(-2.8f, 0, 3.0f), 0, 2);
            Rug(env, new Vector3(0.4f, 0, -1.8f), 2.2f, 2.0f, MMint);
            CommonDecor(env, W, D, H, new[] { new Vector3(-0.1f, 2.2f, 1.0f), new Vector3(1.9f, 2.3f, 1.35f), new Vector3(-2.8f, 2.2f, -1.2f), new Vector3(-2.8f, 2.2f, 3.0f) }, westWindows: true);
            Save(s, SceneFlow.Tutorial);
        }

        // =====================================================================
        // 3. RECORRIDO
        // =====================================================================
        static void BuildRecorrido()
        {
            var s = NewScene();
            Lighting();
            var env = Env();
            float W = 12, D = 12, H = 3.3f;
            Room(env, W, D, H);
            var start = new Vector3(0, 0, -4.6f);
            Rig(start, 0);
            Teleport(env, start, "Entrada", start: true);

            // Puerta de entrada (detrás del jugador)
            var door = Group("Puerta", env, new Vector3(0, 0, -5.93f), 180);
            Box("Marco", door, new Vector3(0, 1.15f, 0), new Vector3(1.3f, 2.3f, 0.06f), MIvory);
            Box("Hoja", door, new Vector3(0, 1.1f, -0.02f), new Vector3(1.1f, 2.15f, 0.04f), MMint);
            Cyl("Arco", door, new Vector3(0, 2.3f, 0), 1.3f, 0.06f, MIvory, new Vector3(90, 0, 0));
            Ball("Pomo", door, new Vector3(0.42f, 1.05f, -0.05f), Vector3.one * 0.06f, MBrass);
            Text("Bienvenidos", door, new Vector3(0, 2.55f, -0.05f), new Vector2(1.4f, 0.25f), "Bienvenidos", 2f, Rose, style: FontStyles.Bold | FontStyles.Italic);

            // ----- Barra (pared oeste) -----
            Counter(env, new Vector3(-4.9f, 0, 0.5f), 90, 4f, out _);
            Awning(env, new Vector3(-5.85f, 2.25f, 0.5f), 90, 4.2f);
            Neon(env, new Vector3(-5.87f, 2.78f, 0.5f), -90, "Luar Café", 2.6f, 0.55f);
            WallShelf(env, new Vector3(-5.93f, 1.7f, -2.6f), -90, 1.6f);
            WallShelf(env, new Vector3(-5.93f, 1.7f, 3.6f), -90, 1.6f);

            var machine = EspressoMachine(env, new Vector3(-4.95f, 1.045f, 1.8f), 90, out var spawn, out var glow, out var steamBurst);
            Steam(env, new Vector3(-4.7f, 1.2f, 1.8f), true);
            var templates = new GameObject("Plantillas").transform;
            templates.SetParent(env, false);
            var coffeeTemplate = MakeGrabbable(CoffeeCup(templates, new Vector3(0, -10, 0), MBlush, "Café"), "cafe", "Café");
            coffeeTemplate.gameObject.SetActive(false);
            var disp = machine.gameObject.AddComponent<ItemDispenser>();
            disp.template = coffeeTemplate;
            disp.spawnPoint = spawn;
            disp.glow = glow;
            disp.steam = steamBurst;
            disp.hint = "Preparar café";
            disp.hoverScale = 1.03f;
            Tag(env, new Vector3(-4.46f, 0.88f, 1.8f), -90, "Café: mira la cafetera");

            // Vitrina abierta: cupcakes y macarons
            CakeStand(env, new Vector3(-5.05f, 1.045f, 0.9f), false);
            Donut(env, new Vector3(-5.1f, 1.195f, 0.85f), MBlush);
            Donut(env, new Vector3(-5.0f, 1.195f, 0.97f), MLavender);
            MakeGrabbable(Cupcake(env, new Vector3(-4.7f, 1.045f, 0.35f), MBlush), "cupcake", "Cupcake");
            MakeGrabbable(Cupcake(env, new Vector3(-4.7f, 1.045f, 0.0f), MMint), "cupcake", "Cupcake");
            MakeGrabbable(Cupcake(env, new Vector3(-5.0f, 1.045f, 0.18f), MLavender), "cupcake", "Cupcake");
            Tag(env, new Vector3(-4.46f, 0.88f, 0.18f), -90, "Cupcakes");
            MakeGrabbable(MacaronPlate(env, new Vector3(-4.7f, 1.045f, -0.75f)), "macaron", "Macarons");
            MakeGrabbable(MacaronPlate(env, new Vector3(-4.7f, 1.045f, -1.1f)), "macaron", "Macarons");
            Tag(env, new Vector3(-4.46f, 0.88f, -0.92f), -90, "Macarons");
            Vase(env, new Vector3(-5.1f, 1.045f, -1.4f));
            Vase(env, new Vector3(-5.1f, 1.045f, 2.35f));

            Teleport(env, new Vector3(-3.4f, 0, 0.5f), "Barra");

            // ----- Mesas de clientes -----
            var orders = new List<OrderManager.Order>();
            orders.Add(CustomerTable(env, new Vector3(2.8f, 0, 3.4f), 0f, "cafe", "Un café, por favor", MMint, MCocoa, MBlush, o => CoffeeCup(o, Vector3.zero, MBlush, "Icono")));
            orders.Add(CustomerTable(env, new Vector3(3.6f, 0, -1.6f), 90f, "cupcake", "¡Un cupcake!", MButter, MLatte, MLavender, o => Cupcake(o, Vector3.zero, MBlush, "Icono")));
            orders.Add(CustomerTable(env, new Vector3(-1.2f, 0, 4.1f), 0f, "macaron", "Macarons, porfa", MPeach, Lit("Hair_Dark", Hex("7B5A4C")), MMint, o => MacaronPlate(o, Vector3.zero, "Icono")));

            Teleport(env, new Vector3(2.0f, 0, 2.1f), "Mesa 1");
            Teleport(env, new Vector3(2.2f, 0, -1.6f), "Mesa 2");
            Teleport(env, new Vector3(-1.2f, 0, 2.8f), "Mesa 3");
            Teleport(env, new Vector3(0.2f, 0, -1.0f), "Centro");

            // Panel HUD que acompaña (instrucciones + progreso + menú)
            var head = start + Vector3.up * EyeHeight;
            var off = new Vector3(-1.2f, 0.35f, 2.5f);
            var panelPos = head + off;
            var panel = Board(env, panelPos, YawTo(head, panelPos), 1.55f, 0.85f, "¡A servir!",
                "1. Mira la <b>cafetera</b> para preparar un café.\n2. <b>Recoge</b> el pedido de la barra.\n3. <b>Salta</b> hasta la mesa y mira el <b>plato</b>.",
                out _, out _, bodySize: 0.9f);
            panel.name = "PanelRecorrido";
            Object.DestroyImmediate(panel.Find("Marco").GetComponent<Collider>()); // no bloquear la mirada
            panel.gameObject.AddComponent<LazyFollowPanel>().offset = off;
            Box("Tira", panel, new Vector3(0, -0.56f, 0.0f), new Vector3(1.65f, 0.17f, 0.03f), MButter);
            var progress = Text("Progreso", panel, new Vector3(-0.25f, -0.56f, -0.02f), new Vector2(1.05f, 0.12f), "Pedidos servidos 0 / 3", 0.9f, Cocoa, TextAlignmentOptions.Left, FontStyles.Bold);
            SceneButton(panel, new Vector3(0.58f, -0.56f, -0.01f), "Menú", MLavender, SceneFlow.Inicio, 0.22f).transform.localScale = Vector3.one * 0.62f;
            var msgBox = Group("CajaMensaje", panel, new Vector3(0, -0.76f, 0));
            Box("Fondo", msgBox, Vector3.zero, new Vector3(1.65f, 0.17f, 0.03f), MIvory);
            Box("Borde", msgBox, new Vector3(0, 0, 0.01f), new Vector3(1.69f, 0.21f, 0.02f), MRose);
            var message = Text("Mensaje", msgBox, new Vector3(0, 0, -0.02f), new Vector2(1.55f, 0.14f), "", 0.8f, Cocoa);

            var mgr = new GameObject("OrderManager").AddComponent<OrderManager>();
            mgr.orders = orders.ToArray();
            mgr.progressText = progress;
            mgr.messageText = message;
            mgr.messageBox = msgBox.gameObject;

            // Ambientación extra
            TableSet(env, new Vector3(-2.6f, 0, -2.6f), 45, 2);
            TableSet(env, new Vector3(3.8f, 0, -4.4f), 0, 2);
            Rug(env, new Vector3(0.6f, 0, 0.8f), 3.2f, 2.8f);
            Rug(env, new Vector3(0, 0, -4.4f), 1.8f, 1.2f, MMint);
            CommonDecor(env, W, D, H, new[] { new Vector3(2.8f, 2.2f, 3.4f), new Vector3(3.6f, 2.2f, -1.6f), new Vector3(-1.2f, 2.2f, 4.1f), new Vector3(-3.9f, 2.4f, 0.5f), new Vector3(0.6f, 2.5f, 0.8f) }, westWindows: false, southArt: false);
            Bunting(env, new Vector3(-5.8f, 3.1f, -5.8f), new Vector3(5.8f, 3.1f, -5.8f) + new Vector3(0, 0, 0.1f), 16);
            Save(s, SceneFlow.Recorrido);
        }

        static void Tag(Transform env, Vector3 pos, float yaw, string text)
        {
            var g = Group("Etiqueta", env, pos, yaw);
            Box("Tarjeta", g, Vector3.zero, new Vector3(0.5f, 0.13f, 0.01f), MIvory);
            Box("Borde", g, new Vector3(0, 0, 0.004f), new Vector3(0.54f, 0.17f, 0.01f), MRose);
            Text("Texto", g, new Vector3(0, 0, -0.008f), new Vector2(0.46f, 0.11f), text, 0.7f, Cocoa, style: FontStyles.Bold);
        }

        /// <summary>Mesa con cliente, globo de pedido flotante, plato de colocación y corazón de "servido".</summary>
        static OrderManager.Order CustomerTable(Transform env, Vector3 pos, float customerYaw, string id, string request,
            Material shirt, Material hair, Material cushion, System.Func<Transform, GameObject> icon)
        {
            var g = Group("Mesa_" + id, env, pos);
            RoundTable(g, Vector3.zero, 0.8f, true);
            // cliente al lado "lejano" (según customerYaw): mira hacia -z local
            var rot = Quaternion.Euler(0, customerYaw, 0);
            var seat = rot * new Vector3(0, 0, 0.62f);
            Chair(g, seat, customerYaw, cushion);
            Customer(g, seat, customerYaw, shirt, hair);
            var opposite = rot * new Vector3(0, 0, -0.62f);
            Chair(g, opposite, customerYaw + 180, MBlush);
            var zone = Zone(g, rot * new Vector3(0, 0.77f, -0.18f), id, "Servir aquí");

            // Globo de pedido
            var bubble = Group("GloboPedido", g, seat + new Vector3(0, 1.75f, 0));
            var face = Group("Cara", bubble, Vector3.zero);
            face.gameObject.AddComponent<Billboard>();
            Box("Fondo", face, Vector3.zero, new Vector3(0.62f, 0.36f, 0.04f), MIvory);
            Cyl("Borde_L", face, new Vector3(-0.31f, 0, 0), 0.36f, 0.04f, MIvory, new Vector3(90, 0, 0));
            Cyl("Borde_R", face, new Vector3(0.31f, 0, 0), 0.36f, 0.04f, MIvory, new Vector3(90, 0, 0));
            Ball("Colita", face, new Vector3(-0.15f, -0.2f, 0), new Vector3(0.1f, 0.1f, 0.04f), MIvory);
            Ball("Colita2", face, new Vector3(-0.2f, -0.28f, 0), new Vector3(0.06f, 0.06f, 0.03f), MIvory);
            Text("Pedido", face, new Vector3(0.12f, 0, -0.03f), new Vector2(0.52f, 0.28f), request, 1f, Cocoa, style: FontStyles.Bold);
            var iconRoot = Group("Icono", face, new Vector3(-0.33f, -0.07f, -0.06f));
            var ic = icon(iconRoot);
            ic.transform.localScale = Vector3.one * 1.3f;
            iconRoot.gameObject.AddComponent<Spin>().degreesPerSecond = new Vector3(0, 40, 0);
            var bob = bubble.gameObject.AddComponent<Bob>(); bob.amplitude = 0.04f; bob.speed = 1.6f;

            // Corazón de "servido"
            var done = Heart(g, seat + new Vector3(0, 1.7f, 0), 0.3f, MRose);
            done.gameObject.AddComponent<Pulse>().amount = 0.12f;
            done.gameObject.AddComponent<Billboard>();

            return new OrderManager.Order { zone = zone, bubble = bubble.gameObject, doneMark = done.gameObject };
        }

        // =====================================================================
        // 4. FELICITACIONES
        // =====================================================================
        static void BuildFelicitaciones()
        {
            var s = NewScene();
            Lighting(1.02f);
            var env = Env();
            float W = 9, D = 9, H = 3.2f;
            Room(env, W, D, H);
            var start = new Vector3(0, 0, -2.2f);
            Rig(start, 0);

            Board(env, new Vector3(0, 1.8f, 0.6f), 0, 2.3f, 1.2f, "¡Felicitaciones!", "", out _, out var bodyT, easel: true, titleSize: 3f);
            bodyT.text = "";
            var stats = bodyT;
            stats.fontSizeMax = 1.3f;
            stats.alignment = TextAlignmentOptions.Center;

            var menu = Group("Menu", env, new Vector3(0, 0.95f, 0.45f));
            SceneButton(menu, new Vector3(-0.85f, 0, 0), "Jugar otra vez", MBlush, SceneFlow.Recorrido, 0.62f);
            SceneButton(menu, new Vector3(0.05f, 0, 0), "Menú principal", MMint, SceneFlow.Inicio, 0.62f);
            QuitButton(menu, new Vector3(0.9f, 0, 0), 0.4f);
            Box("Atril", menu, new Vector3(0, -0.22f, 0.08f), new Vector3(2.6f, 0.06f, 0.2f), MWood);

            // Estrella trofeo
            var star = new GameObject("Estrella");
            star.transform.SetParent(env, false);
            star.transform.localPosition = new Vector3(0, 2.8f, 0.7f);
            star.transform.localScale = Vector3.one * 0.45f;
            star.AddComponent<MeshFilter>().sharedMesh = StarMesh();
            star.AddComponent<MeshRenderer>().sharedMaterial = Lit("Star_Butter", Hex("F9DE95"), 0.4f);
            star.AddComponent<Spin>().degreesPerSecond = new Vector3(0, 45, 0);
            star.AddComponent<Bob>().amplitude = 0.05f;

            // Mesa de celebración con torta y velas
            RoundTable(env, new Vector3(2.3f, 0, -0.3f), 0.9f, false);
            CakeStand(env, new Vector3(2.3f, 0.77f, -0.3f));
            for (int i = 0; i < 3; i++)
            {
                var c = new Vector3(2.3f + (i - 1) * 0.06f, 0.77f + 0.14f + 0.245f, -0.36f);
                Cyl("Vela", env, c + Vector3.up * 0.04f, 0.015f, 0.08f, i % 2 == 0 ? MMint : MLavender);
                Ball("Llama", env, c + Vector3.up * 0.1f, new Vector3(0.02f, 0.035f, 0.02f), MBulb);
            }
            var candle = new GameObject("LuzVelas");
            candle.transform.SetParent(env, false);
            candle.transform.localPosition = new Vector3(2.3f, 1.35f, -0.3f);
            var cl = candle.AddComponent<Light>(); cl.type = LightType.Point; cl.range = 2f; cl.intensity = 0.8f; cl.color = Hex("FFD2A1");
            candle.AddComponent<LightFlicker>().baseIntensity = 0.8f;
            Chair(env, new Vector3(2.9f, 0, 0.1f), YawTo(new Vector3(2.3f, 0, -0.3f), new Vector3(2.9f, 0, 0.1f)), MMint);

            RoundTable(env, new Vector3(-2.3f, 0, -0.3f), 0.8f, false);
            CoffeeCup(env, new Vector3(-2.4f, 0.77f, -0.2f), MMint);
            CoffeeCup(env, new Vector3(-2.15f, 0.77f, -0.4f), MBlush);
            MacaronPlate(env, new Vector3(-2.3f, 0.77f, -0.05f));
            Cupcake(env, new Vector3(-2.5f, 0.77f, -0.45f), MLavender);

            // Globos
            Material[] bm = { MBlush, MMint, MButter, MLavender, MPeach };
            Vector3[] clusters = { new Vector3(-1.6f, 0, 0.9f), new Vector3(1.6f, 0, 0.9f), new Vector3(-3.6f, 0, 3.4f), new Vector3(3.6f, 0, 3.4f) };
            int k = 0;
            foreach (var c in clusters)
            {
                var w = Group("Pesa", env, c);
                Cyl("Pesa", w, new Vector3(0, 0.05f, 0), 0.12f, 0.1f, MRose);
                for (int i = 0; i < 3; i++)
                    Balloon(env, c + new Vector3((i - 1) * 0.22f, 1.1f + (i % 2) * 0.2f, (i % 2) * 0.1f), bm[k++ % bm.Length]);
            }

            Bunting(env, new Vector3(-4.4f, 3.05f, 2.5f), new Vector3(4.4f, 3.05f, 2.5f), 14);
            Bunting(env, new Vector3(-4.4f, 3.05f, -1.0f), new Vector3(4.4f, 3.05f, -1.0f), 14);
            Neon(env, new Vector3(0, 2.75f, 4.36f), 0, "Luar Café", 2.6f, 0.55f);
            Counter(env, new Vector3(0, 0, 3.75f), 180, 4f, out _);
            CakeStand(env, new Vector3(-1.0f, 1.045f, 3.7f));
            Vase(env, new Vector3(1.0f, 1.045f, 3.6f));
            Awning(env, new Vector3(0, 2.25f, 4.4f), 180, 4.4f);
            Rug(env, new Vector3(0, 0, -1.4f), 2.4f, 2.0f, MBlush);
            CommonDecor(env, W, D, H, new[] { new Vector3(0, 2.5f, -3.3f), new Vector3(2.3f, 2.3f, -0.3f), new Vector3(-2.3f, 2.3f, -0.3f) });

            var celebr = new GameObject("Celebracion").AddComponent<CelebrationController>();
            celebr.statsText = stats;
            celebr.confetti = new[]
            {
                Confetti(env, new Vector3(-1.6f, 0.3f, 0.9f)),
                Confetti(env, new Vector3(1.6f, 0.3f, 0.9f)),
                Confetti(env, new Vector3(0f, 0.2f, -0.6f)),
            };
            Save(s, SceneFlow.Felicitaciones);
        }
    }
}
