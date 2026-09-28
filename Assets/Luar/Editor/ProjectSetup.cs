using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;

namespace Luar.EditorTools
{
    /// <summary>
    /// Configura el proyecto para Google Cardboard en Android (URP, XR, Player Settings)
    /// y compila el APK. Menú: Luar Café VR.
    /// </summary>
    public static class ProjectSetup
    {
        const string CardboardLoader = "Google.XR.Cardboard.XRLoader";
        public const string ApkPath = "Builds/LuarCafeVR.apk";

        [MenuItem("Luar Café VR/0. Importar TMP Essentials")]
        public static void ImportTmp()
        {
            if (File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset")) { Debug.Log("[Luar] TMP ya importado"); return; }
            string pkg = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            AssetDatabase.ImportPackage(pkg, false);
            AssetDatabase.Refresh();
            Debug.Log("[Luar] TMP Essentials importado");
        }

        [MenuItem("Luar Café VR/1. Configurar proyecto (Android + Cardboard)")]
        public static void Configure()
        {
            // Render pipeline URP (perfil móvil ajustado: sin HDR, MSAA 2x).
            var rp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            GraphicsSettings.defaultRenderPipeline = rp;
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = rp;
            }
            QualitySettings.SetQualityLevel(names.Length - 1, false);

            // Plataforma
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            // Player Settings requeridos por Cardboard
            PlayerSettings.companyName = "Luar";
            PlayerSettings.productName = "Luar Café VR";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.luar.cafevr");
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.optimizedFramePacing = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.forceInternetPermission = true;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            PlayerSettings.Android.useCustomKeystore = false;

            ConfigureXR();
            AssetDatabase.SaveAssets();
            Debug.Log("[Luar] Proyecto configurado para Android + Cardboard");
        }

        static void ConfigureXR()
        {
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
            {
                Directory.CreateDirectory("Assets/XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perTarget, true);
            }
            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android))
                perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            var general = perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
            general.InitManagerOnStart = true;
            var manager = perTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
            bool ok = XRPackageMetadataStore.AssignLoader(manager, CardboardLoader, BuildTargetGroup.Android);
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(perTarget);
            Debug.Log("[Luar] Cardboard loader asignado: " + ok + " / loaders: " +
                      string.Join(",", manager.activeLoaders.Select(l => l.GetType().FullName)));
        }

        [MenuItem("Luar Café VR/3. Compilar APK")]
        public static void BuildApk()
        {
            Directory.CreateDirectory("Builds");
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            });
            Debug.Log("[Luar] Resultado build: " + report.summary.result + " — " + report.summary.totalSize / (1024 * 1024) + " MB — errores: " + report.summary.totalErrors);
            if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
