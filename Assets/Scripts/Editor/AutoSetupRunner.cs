#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace PopPocket.EditorTools
{
    /// <summary>
    /// Lanceur automatique pour la ligne de commande :
    /// Unity.exe -projectPath ... -executeMethod PopPocket.EditorTools.AutoSetupRunner.RunAll -quit
    /// Importe TMP, reconstruit la scène ET les préfabs sans intervention manuelle.
    /// </summary>
    public static class AutoSetupRunner
    {
        public static void RunAll()
        {
            EnsureTmpEssentials();

            Debug.Log("[AUTOSETUP] Étape 1 : création de la scène complète...");
            PopPocketSceneSetup.SetupFullScene();
            Debug.Log("[AUTOSETUP] Étape 1 terminée ✔");

            Debug.Log("[AUTOSETUP] Étape 2 : création des préfabs slimes + bulles...");
            PopPocketSceneSetup.SetupPrefabsPlaceholders();
            Debug.Log("[AUTOSETUP] Étape 2 terminée ✔");
        }

        /// <summary>Contrôle TMP en tolérant les plantages quand les settings n'existent pas encore.</summary>
        static bool TmpReady()
        {
            try { return TMPro.TMP_Settings.defaultFontAsset != null; }
            catch { return false; }
        }

        /// <summary>
        /// Importe "TMP Essential Resources" si TMP n'a pas encore de police.
        /// Essaie d'abord l'importeur officiel par réflexion, puis les
        /// unitypackage embarqués dans le package cache.
        /// </summary>
        static void EnsureTmpEssentials()
        {
            if (TmpReady()) return;
            Debug.Log("[AUTOSETUP] TMP Essentials manquants → import...");

            // 1) Importeur officiel (TMP_PackageResourceImporter), par réflexion
            try
            {
                var type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                    .FirstOrDefault(t => t.FullName == "TMPro.EditorUtilities.TMP_PackageResourceImporter");
                if (type != null)
                {
                    var instance = Activator.CreateInstance(type);
                    var m = type.GetMethod("LoadTMPResources", Type.EmptyTypes)
                         ?? type.GetMethod("LoadEssentialResources", Type.EmptyTypes);
                    if (m != null)
                    {
                        m.Invoke(instance, null);
                        AssetDatabase.Refresh();
                        if (TmpReady())
                        {
                            Debug.Log("[AUTOSETUP] TMP Essentials importés ✔");
                            return;
                        }
                    }
                }
            }
            catch (Exception) { /* on passe au plan B */ }

            // 2) Plan B : importer le .unitypackage présent dans le Package Cache
            try
            {
                var lib = Directory.GetDirectories("Library/PackageCache", "*ugui*")
                    .SelectMany(d => Directory.GetFiles(d, "TMP Essential Resources.unitypackage", SearchOption.AllDirectories))
                    .FirstOrDefault();
                if (lib == null)
                    lib = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories)
                        .FirstOrDefault();
                if (lib != null)
                {
                    AssetDatabase.ImportPackage(lib, false);
                    AssetDatabase.Refresh();
                    Debug.Log("[AUTOSETUP] TMP Essentials importés via unitypackage : " + lib);
                }
                else
                {
                    Debug.LogWarning("[AUTOSETUP] Aucun unitypackage TMP trouvé (plan B échoué)");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AUTOSETUP] Plan B TMP : " + e.Message);
            }
        }
    }
}
#endif