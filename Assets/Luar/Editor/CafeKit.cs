using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Luar.EditorTools
{
    /// <summary>
    /// Kit visual "Luar Café": paleta pastel única, materiales mate (sin brillos que cansen
    /// la vista) y props construidos con primitivas. Todas las escenas usan este kit para
    /// mantener exactamente la misma identidad visual.
    /// </summary>
    public static class CafeKit
    {
        public const string MatDir = "Assets/Luar/Materials";
        public const string TexDir = "Assets/Luar/Textures";
        public const string MeshDir = "Assets/Luar/Meshes";

        // ---------- Paleta (pasteles desaturados, cálidos) ----------
        public static readonly Color WallPink = Hex("F4D3D6");
        public static readonly Color Cream = Hex("FFF4E8");
        public static readonly Color Ivory = Hex("FFFBF6");
        public static readonly Color Blush = Hex("F2C1CB");
        public static readonly Color Rose = Hex("E3A1B0");
        public static readonly Color WoodLight = Hex("E5CCAD");
        public static readonly Color Latte = Hex("C9A583");
        public static readonly Color Sage = Hex("B8CFA6");
        public static readonly Color Leaf = Hex("93B68C");
        public static readonly Color Mint = Hex("C3E4D8");
        public static readonly Color Lavender = Hex("D8CAEA");
        public static readonly Color Butter = Hex("FBE5A8");
        public static readonly Color Peach = Hex("F7CDB5");
        public static readonly Color Cocoa = Hex("6E4B3E");
        public static readonly Color Coffee = Hex("8A5E4A");
        public static readonly Color Brass = Hex("D6B98A");
        public static readonly Color Sky = Hex("D7ECF6");
        public static readonly Color Terracotta = Hex("E6B9A0");
        public static readonly Color Skin = Hex("FBE3D2");

        public static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }

        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        // ---------- Materiales ----------
        public static Material Lit(string name, Color c, float smooth = 0.15f)
        {
            string key = "L_" + name;
            if (cache.TryGetValue(key, out var m) && m) return m;
            string path = $"{MatDir}/{name}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_EnvironmentReflections", 0f);
            m.SetFloat("_SpecularHighlights", smooth > 0.3f ? 1f : 0f);
            BaseShaderGUI.SetMaterialKeywords(m);
            EditorUtility.SetDirty(m);
            cache[key] = m;
            return m;
        }

        public static Material Unlit(string name, Color c, bool cullOff = false, bool transparent = false)
        {
            string key = "U_" + name;
            if (cache.TryGetValue(key, out var m) && m) return m;
            string path = $"{MatDir}/{name}.mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Cull", cullOff ? 0f : 2f);
            if (transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_ZWrite", 0f);
            }
            BaseShaderGUI.SetMaterialKeywords(m);
            if (transparent) m.renderQueue = 4000;
            EditorUtility.SetDirty(m);
            cache[key] = m;
            return m;
        }

        public static Material Textured(string name, Texture2D tex, Vector2 tiling, float smooth = 0.2f)
        {
            var m = Lit(name, Color.white, smooth);
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", tiling);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material FloorMat(float w, float d)
        {
            var m = Textured("Floor_Checker", CheckerTexture(), new Vector2(w / 1.2f, d / 1.2f), 0.1f);
            m.SetColor("_BaseColor", Hex("E9E1DC"));
            return m;
        }

        // Atajos de materiales de la identidad
        public static Material MWall => Lit("Wall_Pink", WallPink);
        public static Material MCream => Lit("Cream", Cream);
        public static Material MIvory => Lit("Ivory", Ivory, 0.25f);
        public static Material MMarble => Lit("Marble_White", Ivory, 0.45f);
        public static Material MBlush => Lit("Blush", Blush);
        public static Material MRose => Lit("Rose", Rose);
        public static Material MWood => Lit("Wood_Light", WoodLight);
        public static Material MLatte => Lit("Latte", Latte);
        public static Material MSage => Lit("Sage", Sage);
        public static Material MLeaf => Lit("Leaf", Leaf);
        public static Material MMint => Lit("Mint", Mint);
        public static Material MLavender => Lit("Lavender", Lavender);
        public static Material MButter => Lit("Butter", Butter);
        public static Material MPeach => Lit("Peach", Peach);
        public static Material MCocoa => Lit("Cocoa", Cocoa);
        public static Material MCoffee => Lit("Coffee", Coffee, 0.5f);
        public static Material MBrass => Lit("Brass", Brass, 0.4f);
        public static Material MTerracotta => Lit("Terracotta", Terracotta);
        public static Material MSkin => Lit("Skin", Skin);
        public static Material MDark => Lit("Eyes", Hex("4A3530"), 0.6f);
        public static Material MWindow => Unlit("Window_Sky", Hex("DCEFF8"));
        public static Material MBulb => Unlit("Bulb_Warm", Hex("FFF1D6"));
        public static Material MNeon => Unlit("Neon_Pink", Hex("FFC2D1"));

        // ---------- Texturas procedurales ----------
        public static Texture2D CheckerTexture()
        {
            string path = $"{TexDir}/Floor_Checker.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing) return existing;
            int n = 256;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            Color a = Hex("FFF3EA"), b = Hex("F5D9DC"), grout = Hex("EFE2D8");
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool g = x % 128 < 2 || y % 128 < 2;
                    bool check = (x / 128 + y / 128) % 2 == 0;
                    tex.SetPixel(x, y, g ? grout : (check ? a : b));
                }
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.filterMode = FilterMode.Trilinear;
            imp.anisoLevel = 4;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Texture2D StripeTexture()
        {
            string path = $"{TexDir}/Awning_Stripes.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing) return existing;
            int n = 128;
            var tex = new Texture2D(n, 8, TextureFormat.RGBA32, false);
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < n; x++)
                    tex.SetPixel(x, y, (x / 32) % 2 == 0 ? Blush : Ivory);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static Mesh TriangleMesh()
        {
            string path = $"{MeshDir}/Pennant.asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (m) return m;
            m = new Mesh { name = "Pennant" };
            // Banderín triangular con grosor (doble cara)
            var v = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0, -0.8f, 0) };
            m.vertices = new[] { v[0], v[1], v[2], v[0], v[1], v[2] };
            m.triangles = new[] { 0, 1, 2, 3, 5, 4 };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.forward, Vector3.forward, Vector3.forward };
            m.RecalculateBounds();
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        public static Mesh StarMesh()
        {
            string path = $"{MeshDir}/Star.asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (m) return m;
            m = new Mesh { name = "Star" };
            var verts = new List<Vector3>();
            var tris = new List<int>();
            float d = 0.12f;
            // Estrella extruida de 5 puntas
            var outline = new List<Vector3>();
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5;
                float r = i % 2 == 0 ? 0.5f : 0.22f;
                outline.Add(new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0));
            }
            // cara frontal y trasera
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? -d : d;
                int c = verts.Count;
                verts.Add(new Vector3(0, 0, z));
                foreach (var p in outline) verts.Add(p + new Vector3(0, 0, z));
                for (int i = 0; i < 10; i++)
                {
                    int a = c + 1 + i, b = c + 1 + (i + 1) % 10;
                    if (side == 0) { tris.Add(c); tris.Add(b); tris.Add(a); }
                    else { tris.Add(c); tris.Add(a); tris.Add(b); }
                }
            }
            // lados
            for (int i = 0; i < 10; i++)
            {
                var p0 = outline[i]; var p1 = outline[(i + 1) % 10];
                int c = verts.Count;
                verts.Add(p0 + Vector3.back * d); verts.Add(p1 + Vector3.back * d);
                verts.Add(p1 + Vector3.forward * d); verts.Add(p0 + Vector3.forward * d);
                tris.AddRange(new[] { c, c + 2, c + 1, c, c + 3, c + 2 });
            }
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        // ---------- Primitivas ----------
        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat,
            Vector3 euler = default, bool collider = false, bool shadows = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = euler;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            else if (type == PrimitiveType.Cylinder)
            {
                // El CapsuleCollider de un cilindro plano se vuelve una esfera enorme: usar la malla real.
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.AddComponent<MeshCollider>();
            }
            return go;
        }

        public static GameObject Box(string n, Transform p, Vector3 pos, Vector3 size, Material m, Vector3 euler = default, bool col = false, bool shadows = true)
            => Prim(PrimitiveType.Cube, n, p, pos, size, m, euler, col, shadows);

        /// <summary>Cilindro vertical con diámetro y altura reales (pivote en el centro).</summary>
        public static GameObject Cyl(string n, Transform p, Vector3 pos, float diameter, float height, Material m, Vector3 euler = default, bool col = false)
            => Prim(PrimitiveType.Cylinder, n, p, pos, new Vector3(diameter, height / 2f, diameter), m, euler, col);

        public static GameObject Ball(string n, Transform p, Vector3 pos, Vector3 size, Material m, bool col = false)
            => Prim(PrimitiveType.Sphere, n, p, pos, size, m, default, col);

        public static Transform Group(string name, Transform parent, Vector3 pos, float yaw = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(0, yaw, 0);
            return go.transform;
        }

        public static BoxCollider FitBox(GameObject go, bool trigger = false, float pad = 0f)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return go.AddComponent<BoxCollider>();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var col = go.AddComponent<BoxCollider>();
            var t = go.transform;
            // Solo válido si el objeto no tiene rotación extraña respecto a sus hijos
            Vector3 c = t.InverseTransformPoint(b.center);
            Vector3 s = t.InverseTransformVector(b.size);
            col.center = c;
            col.size = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)) + Vector3.one * pad;
            col.isTrigger = trigger;
            return col;
        }

        // ---------- Texto ----------
        public static TextMeshPro Text(string name, Transform parent, Vector3 pos, Vector2 size, string text, float maxSize, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.rectTransform.sizeDelta = size;
            tmp.text = text;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.3f;
            tmp.fontSizeMax = maxSize;
            tmp.fontSize = maxSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.lineSpacing = 4f;
            var mr = go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return tmp;
        }

        // ---------- Props de cafetería ----------
        /// <summary>Habitación: piso ajedrezado pastel, zócalo crema, paredes rosa, cornisa y techo.</summary>
        public static Transform Room(Transform parent, float w, float d, float h = 3.2f)
        {
            var room = Group("Habitacion", parent, Vector3.zero);
            var floor = Box("Piso", room, new Vector3(0, -0.05f, 0), new Vector3(w, 0.1f, d),
                FloorMat(w, d), col: true, shadows: false);
            floor.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box("Techo", room, new Vector3(0, h + 0.05f, 0), new Vector3(w, 0.1f, d), MCream, col: false, shadows: false);

            float wain = 1.05f;
            void Wall(string n, Vector3 center, float length, float yaw)
            {
                var g = Group(n, room, center, yaw);
                Box("Zocalo", g, new Vector3(0, wain / 2f, 0), new Vector3(length, wain, 0.12f), MIvory, col: true, shadows: false);
                Box("Moldura", g, new Vector3(0, wain + 0.02f, -0.04f), new Vector3(length, 0.05f, 0.08f), MBlush, shadows: false);
                Box("Pared", g, new Vector3(0, wain + (h - wain) / 2f, 0.02f), new Vector3(length, h - wain, 0.1f), MWall, col: true, shadows: false);
                Box("Cornisa", g, new Vector3(0, h - 0.06f, -0.05f), new Vector3(length, 0.12f, 0.1f), MIvory, shadows: false);
                // paneles del zócalo
                int panels = Mathf.Max(1, Mathf.RoundToInt(length / 1.2f));
                for (int i = 0; i < panels; i++)
                {
                    float x = -length / 2f + (i + 0.5f) * length / panels;
                    Box("Panel", g, new Vector3(x, wain / 2f, -0.065f), new Vector3(length / panels - 0.25f, wain - 0.3f, 0.02f), MCream, shadows: false);
                }
            }
            Wall("Pared_N", new Vector3(0, 0, d / 2f), w, 0);
            Wall("Pared_S", new Vector3(0, 0, -d / 2f), w, 180);
            Wall("Pared_E", new Vector3(w / 2f, 0, 0), d, 90);
            Wall("Pared_O", new Vector3(-w / 2f, 0, 0), d, -90);
            return room;
        }

        /// <summary>Ventana en arco (muy Pinterest). Se coloca sobre una pared; forward apunta hacia la pared.</summary>
        public static Transform ArchWindow(Transform parent, Vector3 pos, float yaw, float width = 1.1f, float height = 1.4f)
        {
            var g = Group("VentanaArco", parent, pos, yaw);
            float bottom = 1.05f;
            Box("Marco", g, new Vector3(0, bottom + height / 2f, 0), new Vector3(width + 0.14f, height, 0.04f), MIvory, shadows: false);
            Cyl("MarcoArco", g, new Vector3(0, bottom + height, 0), width + 0.14f, 0.04f, MIvory, new Vector3(90, 0, 0));
            Box("Vidrio", g, new Vector3(0, bottom + height / 2f, -0.025f), new Vector3(width, height, 0.02f), MWindow, shadows: false);
            Cyl("VidrioArco", g, new Vector3(0, bottom + height, -0.025f), width, 0.02f, MWindow, new Vector3(90, 0, 0));
            Box("Parteluz", g, new Vector3(0, bottom + height * 0.6f, -0.04f), new Vector3(0.04f, height * 1.2f + width * 0.5f, 0.02f), MIvory, shadows: false);
            Box("Travesano", g, new Vector3(0, bottom + height * 0.55f, -0.04f), new Vector3(width, 0.04f, 0.02f), MIvory, shadows: false);
            Box("Alfeizar", g, new Vector3(0, bottom - 0.03f, -0.08f), new Vector3(width + 0.3f, 0.05f, 0.18f), MIvory);
            // macetita en el alféizar
            Plant(g, new Vector3(width * 0.3f, bottom, -0.1f), 0.35f);
            return g;
        }

        public static Transform RoundTable(Transform parent, Vector3 pos, float diameter = 0.8f, bool surface = true)
        {
            var g = Group("MesaRedonda", parent, pos);
            var top = Cyl("Cubierta", g, new Vector3(0, 0.75f, 0), diameter, 0.04f, MMarble, col: surface);
            Cyl("Borde", g, new Vector3(0, 0.725f, 0), diameter * 0.96f, 0.03f, MBrass);
            Cyl("Pata", g, new Vector3(0, 0.37f, 0), 0.07f, 0.72f, MBrass);
            Cyl("Base", g, new Vector3(0, 0.015f, 0), diameter * 0.55f, 0.03f, MBrass);
            if (surface) top.AddComponent<PlaceableSurface>();
            return g;
        }

        /// <summary>Silla estilo bistró con respaldo redondeado y cojín.</summary>
        public static Transform Chair(Transform parent, Vector3 pos, float yaw, Material cushion = null)
        {
            cushion = cushion ?? MBlush;
            var g = Group("Silla", parent, pos, yaw);
            Cyl("Asiento", g, new Vector3(0, 0.46f, 0), 0.44f, 0.05f, MWood);
            Cyl("Cojin", g, new Vector3(0, 0.5f, 0), 0.4f, 0.04f, cushion);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2 + Mathf.PI / 4;
                Cyl("Pata", g, new Vector3(Mathf.Cos(a) * 0.15f, 0.22f, Mathf.Sin(a) * 0.15f), 0.035f, 0.45f, MWood);
            }
            Cyl("Respaldo", g, new Vector3(0, 0.82f, 0.2f), 0.4f, 0.03f, MWood, new Vector3(80, 0, 0));
            Cyl("RespaldoCojin", g, new Vector3(0, 0.82f, 0.18f), 0.32f, 0.03f, cushion, new Vector3(80, 0, 0));
            Cyl("Poste_L", g, new Vector3(-0.15f, 0.64f, 0.2f), 0.03f, 0.34f, MWood);
            Cyl("Poste_R", g, new Vector3(0.15f, 0.64f, 0.2f), 0.03f, 0.34f, MWood);
            return g;
        }

        public static Transform Plant(Transform parent, Vector3 pos, float size = 1f, Material pot = null)
        {
            pot = pot ?? MTerracotta;
            var g = Group("Planta", parent, pos);
            Cyl("Maceta", g, new Vector3(0, 0.15f * size, 0), 0.28f * size, 0.3f * size, pot);
            Cyl("Borde", g, new Vector3(0, 0.3f * size, 0), 0.31f * size, 0.04f * size, pot);
            var leaves = Group("Hojas", g, new Vector3(0, 0.3f * size, 0));
            var rnd = new System.Random(Mathf.RoundToInt(pos.x * 100 + pos.z * 37));
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2 / 6f;
                float r = 0.1f * size;
                float s = (0.22f + (float)rnd.NextDouble() * 0.08f) * size;
                Ball("Hoja", leaves, new Vector3(Mathf.Cos(a) * r, (0.12f + i % 3 * 0.08f) * size, Mathf.Sin(a) * r), new Vector3(s, s * 1.2f, s), i % 2 == 0 ? MSage : MLeaf);
            }
            Ball("HojaTop", leaves, new Vector3(0, 0.34f * size, 0), Vector3.one * 0.24f * size, MSage);
            var sway = leaves.gameObject.AddComponent<Sway>();
            sway.angle = 1.5f; sway.speed = 0.6f;
            return g;
        }

        public static Transform TallPlant(Transform parent, Vector3 pos)
        {
            var g = Plant(parent, pos, 1.9f, MIvory);
            return g;
        }

        public static Transform HangingPlant(Transform parent, Vector3 pos)
        {
            var g = Group("PlantaColgante", parent, pos);
            Cyl("Cuerda", g, new Vector3(0, -0.25f, 0), 0.01f, 0.5f, MLatte);
            Cyl("Maceta", g, new Vector3(0, -0.55f, 0), 0.25f, 0.18f, MIvory);
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2 / 7f;
                for (int k = 0; k < 3; k++)
                    Ball("Hoja", g, new Vector3(Mathf.Cos(a) * 0.13f, -0.6f - k * 0.1f - (i % 2) * 0.05f, Mathf.Sin(a) * 0.13f), Vector3.one * (0.09f - k * 0.015f), k % 2 == 0 ? MLeaf : MSage);
            }
            g.gameObject.AddComponent<Sway>().angle = 2.5f;
            return g;
        }

        public static Transform PendantLamp(Transform parent, Vector3 pos, float ceiling, Material shade = null, bool light = true, float intensity = 0.9f)
        {
            shade = shade ?? MButter;
            var g = Group("Lampara", parent, new Vector3(pos.x, ceiling, pos.z));
            float drop = ceiling - pos.y;
            Cyl("Cable", g, new Vector3(0, -drop / 2f, 0), 0.012f, drop, MCocoa);
            Cyl("Florón", g, new Vector3(0, -0.02f, 0), 0.12f, 0.04f, MBrass);
            Ball("Pantalla", g, new Vector3(0, -drop - 0.08f, 0), new Vector3(0.42f, 0.3f, 0.42f), shade);
            Cyl("Aro", g, new Vector3(0, -drop - 0.17f, 0), 0.3f, 0.02f, MBrass);
            Ball("Foco", g, new Vector3(0, -drop - 0.2f, 0), Vector3.one * 0.12f, MBulb);
            if (light)
            {
                var lgo = new GameObject("Luz");
                lgo.transform.SetParent(g, false);
                lgo.transform.localPosition = new Vector3(0, -drop - 0.3f, 0);
                var l = lgo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = Hex("FFE2C4");
                l.range = 4.5f;
                l.intensity = intensity;
                l.shadows = LightShadows.None;
            }
            var sway = g.gameObject.AddComponent<Sway>();
            sway.angle = 1.2f; sway.speed = 0.7f;
            return g;
        }

        /// <summary>Estante de pared con frascos, tazas y una planta. forward hacia la pared.</summary>
        public static Transform WallShelf(Transform parent, Vector3 pos, float yaw, float width = 1.4f)
        {
            var g = Group("Repisa", parent, pos, yaw);
            Box("Tabla", g, new Vector3(0, 0, -0.14f), new Vector3(width, 0.04f, 0.26f), MWood);
            Box("Soporte_L", g, new Vector3(-width / 2f + 0.1f, -0.08f, -0.08f), new Vector3(0.03f, 0.14f, 0.14f), MBrass);
            Box("Soporte_R", g, new Vector3(width / 2f - 0.1f, -0.08f, -0.08f), new Vector3(0.03f, 0.14f, 0.14f), MBrass);
            Material[] jars = { MMint, MLavender, MButter, MBlush, MPeach };
            int n = Mathf.FloorToInt(width / 0.22f) - 1;
            for (int i = 0; i < n; i++)
            {
                float x = -width / 2f + 0.2f + i * (width - 0.4f) / Mathf.Max(1, n - 1);
                if (i == n / 2) { Plant(g, new Vector3(x, 0.02f, -0.14f), 0.45f, MIvory); continue; }
                float h = 0.14f + (i % 3) * 0.04f;
                Cyl("Frasco", g, new Vector3(x, 0.02f + h / 2f, -0.14f), 0.11f, h, jars[i % jars.Length]);
                Cyl("Tapa", g, new Vector3(x, 0.03f + h, -0.14f), 0.12f, 0.025f, MBrass);
            }
            return g;
        }

        public static Transform Frame(Transform parent, Vector3 pos, float yaw, Vector2 size, Material art, Material art2 = null)
        {
            var g = Group("Cuadro", parent, pos, yaw);
            Box("Marco", g, Vector3.zero, new Vector3(size.x + 0.08f, size.y + 0.08f, 0.03f), MBrass, shadows: false);
            Box("Lienzo", g, new Vector3(0, 0, -0.017f), new Vector3(size.x, size.y, 0.01f), MIvory, shadows: false);
            Ball("Arte", g, new Vector3(-size.x * 0.12f, -size.y * 0.05f, -0.025f), new Vector3(size.x * 0.5f, size.x * 0.5f, 0.01f), art);
            Ball("Arte2", g, new Vector3(size.x * 0.18f, size.y * 0.15f, -0.03f), new Vector3(size.x * 0.3f, size.x * 0.3f, 0.01f), art2 ?? MButter);
            return g;
        }

        public static Transform Rug(Transform parent, Vector3 pos, float w, float d, Material m = null)
        {
            var g = Group("Alfombra", parent, pos);
            Prim(PrimitiveType.Cylinder, "Borde", g, new Vector3(0, 0.004f, 0), new Vector3(w + 0.16f, 0.004f, d + 0.16f), MIvory, shadows: false);
            Prim(PrimitiveType.Cylinder, "Centro", g, new Vector3(0, 0.008f, 0), new Vector3(w, 0.004f, d), m ?? MLavender, shadows: false);
            return g;
        }

        /// <summary>Nubecita decorativa de pared.</summary>
        public static Transform Cloud(Transform parent, Vector3 pos, float yaw, float s = 1f)
        {
            var g = Group("Nube", parent, pos, yaw);
            Ball("N1", g, new Vector3(0, 0, 0), new Vector3(0.5f, 0.36f, 0.1f) * s, MIvory);
            Ball("N2", g, new Vector3(0.24f, -0.04f, 0), new Vector3(0.36f, 0.28f, 0.1f) * s, MIvory);
            Ball("N3", g, new Vector3(-0.24f, -0.05f, 0), new Vector3(0.34f, 0.26f, 0.1f) * s, MIvory);
            return g;
        }

        /// <summary>Corazón 3D con primitivas.</summary>
        public static Transform Heart(Transform parent, Vector3 pos, float s, Material m, float yaw = 0)
        {
            var g = Group("Corazon", parent, pos, yaw);
            Ball("L", g, new Vector3(-0.25f, 0.1f, 0) * s, new Vector3(0.58f, 0.58f, 0.3f) * s, m);
            Ball("R", g, new Vector3(0.25f, 0.1f, 0) * s, new Vector3(0.58f, 0.58f, 0.3f) * s, m);
            Box("V", g, new Vector3(0, -0.14f, 0) * s, new Vector3(0.58f, 0.58f, 0.29f) * s, m, new Vector3(0, 0, 45));
            return g;
        }

        public static Transform Balloon(Transform parent, Vector3 pos, Material m)
        {
            var g = Group("Globo", parent, pos);
            Ball("Globo", g, Vector3.zero, new Vector3(0.32f, 0.38f, 0.32f), m);
            Cyl("Nudo", g, new Vector3(0, -0.2f, 0), 0.04f, 0.03f, m);
            Cyl("Hilo", g, new Vector3(0, -0.7f, 0), 0.006f, 1.0f, MIvory);
            var b = g.gameObject.AddComponent<Bob>();
            b.amplitude = 0.06f; b.speed = 1.1f;
            return g;
        }

        /// <summary>Guirnalda de banderines entre dos puntos.</summary>
        public static Transform Bunting(Transform parent, Vector3 a, Vector3 b, int count)
        {
            var g = Group("Banderines", parent, Vector3.zero);
            var mesh = TriangleMesh();
            Material[] mats = { MBlush, MMint, MButter, MLavender, MPeach };
            var dir = (b - a);
            var cord = Cyl("Cordel", g, (a + b) / 2f - Vector3.up * 0.05f, 0.01f, dir.magnitude, MIvory);
            cord.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                var p = Vector3.Lerp(a, b, t) - Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.25f + 0.05f);
                var go = new GameObject("Banderin");
                go.transform.SetParent(g, false);
                go.transform.localPosition = p;
                go.transform.localRotation = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up).normalized, Vector3.up);
                go.transform.localScale = Vector3.one * 0.28f;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mats[i % mats.Length];
                var sw = go.AddComponent<Sway>(); sw.angle = 4f; sw.axis = Vector3.right; sw.speed = 1.3f;
            }
            return g;
        }

        // ---------- Comida y bebida (pivote en la base) ----------
        public static GameObject Cupcake(Transform parent, Vector3 pos, Material frosting, string name = "Cupcake")
        {
            var g = Group(name, parent, pos).gameObject;
            Cyl("Capacillo", g.transform, new Vector3(0, 0.035f, 0), 0.075f, 0.07f, MIvory);
            Cyl("Bizcocho", g.transform, new Vector3(0, 0.075f, 0), 0.085f, 0.02f, MLatte);
            Ball("Crema", g.transform, new Vector3(0, 0.1f, 0), new Vector3(0.095f, 0.07f, 0.095f), frosting);
            Ball("Crema2", g.transform, new Vector3(0, 0.13f, 0), new Vector3(0.065f, 0.05f, 0.065f), frosting);
            Ball("Cereza", g.transform, new Vector3(0, 0.162f, 0), Vector3.one * 0.028f, MRose);
            return g;
        }

        public static GameObject Macaron(Transform parent, Vector3 pos, Material shell, string name = "Macaron")
        {
            var g = Group(name, parent, pos).gameObject;
            Ball("Tapa_Inf", g.transform, new Vector3(0, 0.012f, 0), new Vector3(0.07f, 0.028f, 0.07f), shell);
            Cyl("Relleno", g.transform, new Vector3(0, 0.026f, 0), 0.062f, 0.012f, MIvory);
            Ball("Tapa_Sup", g.transform, new Vector3(0, 0.04f, 0), new Vector3(0.07f, 0.028f, 0.07f), shell);
            return g;
        }

        /// <summary>Plato con 3 macarons (se agarra como una unidad).</summary>
        public static GameObject MacaronPlate(Transform parent, Vector3 pos, string name = "Macarons")
        {
            var g = Group(name, parent, pos).gameObject;
            Cyl("Platito", g.transform, new Vector3(0, 0.008f, 0), 0.18f, 0.016f, MIvory);
            Macaron(g.transform, new Vector3(-0.035f, 0.016f, 0.02f), MMint);
            Macaron(g.transform, new Vector3(0.035f, 0.016f, 0.02f), MLavender);
            Macaron(g.transform, new Vector3(0, 0.016f, -0.035f), MBlush);
            return g;
        }

        public static GameObject CoffeeCup(Transform parent, Vector3 pos, Material cup = null, string name = "Cafe")
        {
            cup = cup ?? MBlush;
            var g = Group(name, parent, pos).gameObject;
            Cyl("Platillo", g.transform, new Vector3(0, 0.006f, 0), 0.15f, 0.012f, MIvory);
            Cyl("Taza", g.transform, new Vector3(0, 0.05f, 0), 0.085f, 0.08f, cup);
            Cyl("Cafe", g.transform, new Vector3(0, 0.089f, 0), 0.072f, 0.004f, MCoffee);
            Ball("Arte", g.transform, new Vector3(0, 0.092f, 0), new Vector3(0.03f, 0.003f, 0.025f), MCream);
            Box("Asa", g.transform, new Vector3(0.052f, 0.055f, 0), new Vector3(0.025f, 0.04f, 0.012f), cup);
            return g;
        }

        public static GameObject Donut(Transform parent, Vector3 pos, Material glaze)
        {
            var g = Group("Dona", parent, pos).gameObject;
            Ball("Masa", g.transform, new Vector3(0, 0.02f, 0), new Vector3(0.09f, 0.04f, 0.09f), MLatte);
            Ball("Glaseado", g.transform, new Vector3(0, 0.03f, 0), new Vector3(0.085f, 0.03f, 0.085f), glaze);
            Cyl("Hoyo", g.transform, new Vector3(0, 0.03f, 0), 0.025f, 0.045f, MIvory);
            return g;
        }

        public static Transform CakeStand(Transform parent, Vector3 pos, bool withCake = true)
        {
            var g = Group("Pastelero", parent, pos);
            Cyl("Base", g, new Vector3(0, 0.01f, 0), 0.16f, 0.02f, MIvory);
            Cyl("Tallo", g, new Vector3(0, 0.07f, 0), 0.03f, 0.12f, MIvory);
            Cyl("Plato", g, new Vector3(0, 0.135f, 0), 0.32f, 0.015f, MIvory);
            if (withCake)
            {
                var cake = Group("Torta", g, new Vector3(0, 0.14f, 0));
                Cyl("Piso1", cake, new Vector3(0, 0.06f, 0), 0.24f, 0.12f, MBlush);
                Cyl("Crema1", cake, new Vector3(0, 0.125f, 0), 0.25f, 0.02f, MIvory);
                Cyl("Piso2", cake, new Vector3(0, 0.18f, 0), 0.16f, 0.1f, MCream);
                Cyl("Crema2", cake, new Vector3(0, 0.235f, 0), 0.17f, 0.02f, MBlush);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI / 4;
                    Ball("Fresa", cake, new Vector3(Mathf.Cos(a) * 0.1f, 0.13f, Mathf.Sin(a) * 0.1f), Vector3.one * 0.03f, MRose);
                }
                Ball("Cereza", cake, new Vector3(0, 0.26f, 0), Vector3.one * 0.04f, MRose);
                cake.gameObject.AddComponent<Spin>().degreesPerSecond = new Vector3(0, 15, 0);
            }
            return g;
        }

        /// <summary>Florero con flores pastel.</summary>
        public static Transform Vase(Transform parent, Vector3 pos)
        {
            var g = Group("Florero", parent, pos);
            Cyl("Jarron", g, new Vector3(0, 0.07f, 0), 0.08f, 0.14f, MMint);
            Material[] fl = { MBlush, MIvory, MButter };
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2 / 5f;
                var p = new Vector3(Mathf.Cos(a) * 0.04f, 0.2f + (i % 2) * 0.04f, Mathf.Sin(a) * 0.04f);
                Cyl("Tallo", g, p - Vector3.up * 0.05f, 0.006f, 0.1f, MLeaf);
                Ball("Flor", g, p, Vector3.one * 0.05f, fl[i % fl.Length]);
            }
            return g;
        }

        /// <summary>Mostrador con frente de paneles, cubierta de mármol y toldo a rayas. forward = hacia el cliente.</summary>
        public static Transform Counter(Transform parent, Vector3 pos, float yaw, float length, out GameObject top)
        {
            var g = Group("Mostrador", parent, pos, yaw);
            Box("Cuerpo", g, new Vector3(0, 0.5f, 0), new Vector3(length, 1f, 0.65f), MBlush, col: true);
            Box("Zocalo", g, new Vector3(0, 0.05f, 0.33f), new Vector3(length, 0.1f, 0.03f), MBrass);
            int n = Mathf.Max(2, Mathf.RoundToInt(length / 0.25f));
            for (int i = 0; i < n; i++)
            {
                float x = -length / 2f + (i + 0.5f) * length / n;
                Cyl("Acanalado", g, new Vector3(x, 0.52f, 0.33f), 0.12f, 0.8f, MBlush);
            }
            top = Box("Cubierta", g, new Vector3(0, 1.02f, 0.02f), new Vector3(length + 0.1f, 0.05f, 0.75f), MMarble, col: true);
            top.AddComponent<PlaceableSurface>();
            return g;
        }

        public static Transform Awning(Transform parent, Vector3 pos, float yaw, float width)
        {
            var g = Group("Toldo", parent, pos, yaw);
            var stripes = Textured("Awning", StripeTexture(), new Vector2(width / 0.5f, 1));
            Box("Lona", g, new Vector3(0, 0, 0.25f), new Vector3(width, 0.02f, 0.55f), stripes, new Vector3(-22, 0, 0));
            int n = Mathf.RoundToInt(width / 0.25f);
            for (int i = 0; i < n; i++)
            {
                float x = -width / 2f + (i + 0.5f) * width / n;
                Cyl("Festón", g, new Vector3(x, -0.22f, 0.5f), 0.25f, 0.02f, i % 2 == 0 ? MBlush : MIvory, new Vector3(90, 0, 0));
            }
            return g;
        }

        /// <summary>Cafetera espresso menta. forward = hacia el cliente. Devuelve la bandeja donde aparece el café.</summary>
        public static Transform EspressoMachine(Transform parent, Vector3 pos, float yaw, out Transform spawn, out Light glow, out ParticleSystem steam)
        {
            var g = Group("Cafetera", parent, pos, yaw);
            Box("Cuerpo", g, new Vector3(0, 0.22f, -0.05f), new Vector3(0.56f, 0.44f, 0.4f), MMint, col: true);
            Box("Tapa", g, new Vector3(0, 0.46f, -0.05f), new Vector3(0.6f, 0.04f, 0.44f), MIvory);
            Box("Panel", g, new Vector3(0, 0.3f, 0.155f), new Vector3(0.44f, 0.12f, 0.01f), MIvory);
            Cyl("Manometro", g, new Vector3(0, 0.3f, 0.165f), 0.07f, 0.01f, MBrass, new Vector3(90, 0, 0));
            Cyl("Grupo", g, new Vector3(0, 0.17f, 0.2f), 0.08f, 0.06f, MBrass);
            Box("Portafiltro", g, new Vector3(0, 0.13f, 0.26f), new Vector3(0.03f, 0.02f, 0.14f), MCocoa);
            Box("Bandeja", g, new Vector3(0, 0.015f, 0.2f), new Vector3(0.3f, 0.03f, 0.18f), MBrass);
            Cyl("TazaDeco1", g, new Vector3(-0.15f, 0.51f, -0.05f), 0.07f, 0.06f, MBlush);
            Cyl("TazaDeco2", g, new Vector3(0.05f, 0.51f, -0.05f), 0.07f, 0.06f, MIvory);
            Cyl("TazaDeco3", g, new Vector3(0.18f, 0.51f, -0.05f), 0.07f, 0.06f, MLavender);
            var sp = new GameObject("SpawnCafe").transform;
            sp.SetParent(g, false);
            sp.localPosition = new Vector3(0, 0.03f, 0.2f);
            spawn = sp;

            var lgo = new GameObject("Brillo");
            lgo.transform.SetParent(g, false);
            lgo.transform.localPosition = new Vector3(0, 0.25f, 0.4f);
            glow = lgo.AddComponent<Light>();
            glow.type = LightType.Point; glow.range = 1.2f; glow.intensity = 0f; glow.color = Hex("FFD9A8");

            steam = Steam(g, new Vector3(0, 0.12f, 0.24f), false);
            return g;
        }

        public static ParticleSystem Steam(Transform parent, Vector3 pos, bool loop)
        {
            var go = new GameObject("Vapor");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(-90, 0, 0);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = loop;
            main.duration = loop ? 3f : 1.5f;
            main.startLifetime = 1.6f;
            main.startSpeed = 0.18f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new Color(1f, 1f, 1f, 0.45f);
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = loop;
            var em = ps.emission; em.rateOverTime = loop ? 5f : 18f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8f; sh.radius = 0.02f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.5f, 0.2f), new GradientAlphaKey(0, 1) });
            col.color = grad;
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.6f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = ParticleMaterial();
            if (!loop) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        public static ParticleSystem Confetti(Transform parent, Vector3 pos)
        {
            var go = new GameObject("Confeti");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localEulerAngles = new Vector3(-90, 0, 0);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = 4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.gravityModifier = 0.35f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Blush, 0), new GradientColorKey(Mint, 0.33f), new GradientColorKey(Butter, 0.66f), new GradientColorKey(Lavender, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
            main.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
            main.maxParticles = 150;
            main.playOnAwake = false;
            var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 120) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 35f; sh.radius = 0.1f;
            var vel = ps.limitVelocityOverLifetime; vel.enabled = true; vel.limit = 1.2f; vel.dampen = 0.15f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = ParticleMaterial();
            return ps;
        }

        static Material ParticleMaterial()
        {
            string path = $"{MatDir}/Particle_Soft.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m) return m;
            var urp = GraphicsSettings.defaultRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            m = new Material(urp.defaultParticleMaterial);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Cliente "frijolito" tierno sentado (cápsula + cabeza con cachetes).</summary>
        public static Transform Customer(Transform parent, Vector3 pos, float yaw, Material shirt, Material hair)
        {
            var g = Group("Cliente", parent, pos, yaw);
            Prim(PrimitiveType.Capsule, "Cuerpo", g, new Vector3(0, 0.72f, 0), new Vector3(0.36f, 0.28f, 0.32f), shirt);
            Ball("Cabeza", g, new Vector3(0, 1.12f, 0), Vector3.one * 0.3f, MSkin);
            Ball("Pelo", g, new Vector3(0, 1.18f, 0.03f), new Vector3(0.32f, 0.24f, 0.32f), hair);
            Ball("Ojo_L", g, new Vector3(-0.055f, 1.12f, -0.14f), new Vector3(0.035f, 0.045f, 0.02f), MDark);
            Ball("Ojo_R", g, new Vector3(0.055f, 1.12f, -0.14f), new Vector3(0.035f, 0.045f, 0.02f), MDark);
            Ball("Cachete_L", g, new Vector3(-0.09f, 1.08f, -0.125f), new Vector3(0.05f, 0.03f, 0.02f), MRose);
            Ball("Cachete_R", g, new Vector3(0.09f, 1.08f, -0.125f), new Vector3(0.05f, 0.03f, 0.02f), MRose);
            Box("Sonrisa", g, new Vector3(0, 1.055f, -0.145f), new Vector3(0.04f, 0.008f, 0.01f), MDark);
            return g;
        }
    }
}
