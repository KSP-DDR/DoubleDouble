using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = System.Object;

/*-----------------------------------------*\
|   SUBSTITUTE YOUR MOD'S NAMESPACE HERE.   |
\*-----------------------------------------*/
namespace TerrasIgnotas
{

    /**
     * This utility displays a warning with a list of mods that determine themselves
     * to be incompatible with the current running version of Kerbal Space Program.
     *
     * See this forum thread for details:
     * http://forum.kerbalspaceprogram.com/threads/65395-Voluntarily-Locking-Plugins-to-a-Particular-KSP-Version
     */

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class CompatibilityChecker : MonoBehaviour
    {
        // Compatible version
        internal const Int32 version_major = 1;
        internal const Int32 version_minor1 = 3;
        internal const Int32 version_minor2 = 5;
        internal const Int32 Revision = 1;
        internal const Int32 TI = 1;

        public static Boolean IsCompatible()
        {
            /*-----------------------------------------------*\
            |    BEGIN IMPLEMENTATION-SPECIFIC EDITS HERE.    |
            \*-----------------------------------------------*/

            // TODO: Implement your own compatibility check.
            //
            // If you want to disable some behavior when incompatible, other parts of the plugin
            // should query this method:
            //
            //    if (!CompatibilityChecker.IsCompatible()) {
            //        ...disable some features...
            //    }
            //
            // Even if you don't lock down functionality, you should return true if your users
            // can expect a future update to be available.
            //
            public static int KerbalVersion;

            if (Versioning.version_minor == 3)
            {
                KerbalVersion == 131;
            }


            return true;

            /*-----------------------------------------------*\
            | IMPLEMENTERS SHOULD NOT EDIT BEYOND THIS POINT! |
            \*-----------------------------------------------*/
        }

        public static Boolean IsUnityCompatible()
        {
            /*-----------------------------------------------*\
            |    BEGIN IMPLEMENTATION-SPECIFIC EDITS HERE.    |
            \*-----------------------------------------------*/

            // Not going to care about Unity-Version
            return true;

            /*-----------------------------------------------*\
            | IMPLEMENTERS SHOULD NOT EDIT BEYOND THIS POINT! |
            \*-----------------------------------------------*/
        }

        // Version of the compatibility checker itself.
        private static Int32 _version = 4;

        void Awake()
        {
            // If Kopernicus isn't compatible, activate cat-ception
            if (IsCompatible())
                return;
            Type moduleManager = Parser.ModTypes.FirstOrDefault(t => t.Name == "ModuleManager" && t.Namespace == "ModuleManager");
            if (moduleManager == null)
                return; // no cat :(
            FieldInfo nyan = moduleManager.GetField("nyan", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo ncats = moduleManager.GetField("nCats", BindingFlags.Instance | BindingFlags.NonPublic);
            UnityEngine.Object mm = FindObjectOfType(moduleManager);
            nyan.SetValue(mm, true);
            ncats.SetValue(mm, true);

            // Nobody can read that popup
#if !KSP131
            ScreenMessages.PostScreenMessage(
                "Kopernicus will not work on this version of KSP!\nPlease don't try to open your savegames!",
                Single.MaxValue,
                ScreenMessageStyle.UPPER_LEFT, true);
#else
            DontDestroyOnLoad(ScreenMessages.PostScreenMessage(
                "Kopernicus will not work on this version of KSP!\nPlease don't try to open your savegames!",
                Single.MaxValue,
                ScreenMessageStyle.UPPER_LEFT).textInstance);
#endif
        }

        public void Start()
        {
            // Checkers are identified by the type name and version field name.
            FieldInfo[] fields =
                Parser.ModTypes
                .Where(t => t.Name == "CompatibilityChecker")
                .Select(t => t.GetField("_version", BindingFlags.Static | BindingFlags.NonPublic))
                .Where(f => f != null)
                .Where(f => f.FieldType == typeof(Int32))
                .ToArray();

            // Let the latest version of the checker execute.
            if (_version != fields.Max(f => (Int32)f.GetValue(null))) { return; }

            Debug.Log(String.Format("[CompatibilityChecker] Running checker version {0} from '{1}'", _version, Assembly.GetExecutingAssembly().GetName().Name));

            // Other checkers will see this version and not run.
            // This accomplishes the same as an explicit "ran" flag with fewer moving parts.
            _version = Int32.MaxValue;

            // A mod is incompatible if its compatibility checker has an IsCompatible method which returns false.
            String[] incompatible =
                fields
                .Select(f => f.DeclaringType.GetMethod("IsCompatible", Type.EmptyTypes))
                .Where(m => m.IsStatic)
                .Where(m => m.ReturnType == typeof(Boolean))
                .Where(m =>
                {
                    try
                    {
                        return !(Boolean)m.Invoke(null, new Object[0]);
                    }
                    catch (Exception e)
                    {
                        // If a mod throws an exception from IsCompatible, it's not compatible.
                        Debug.LogWarning(String.Format("[CompatibilityChecker] Exception while invoking IsCompatible() from '{0}':\n\n{1}", m.DeclaringType.Assembly.GetName().Name, e));
                        return true;
                    }
                })
                .Select(m => m.DeclaringType.Assembly.GetName().Name)
                .ToArray();

            // A mod is incompatible with Unity if its compatibility checker has an IsUnityCompatible method which returns false.
            String[] incompatibleUnity =
                fields
                .Select(f => f.DeclaringType.GetMethod("IsUnityCompatible", Type.EmptyTypes))
                .Where(m => m != null)  // Mods without IsUnityCompatible() are assumed to be compatible.
                .Where(m => m.IsStatic)
                .Where(m => m.ReturnType == typeof(Boolean))
                .Where(m =>
                {
                    try
                    {
                        return !(Boolean)m.Invoke(null, new Object[0]);
                    }
                    catch (Exception e)
                    {
                        // If a mod throws an exception from IsUnityCompatible, it's not compatible.
                        Debug.LogWarning(String.Format("[CompatibilityChecker] Exception while invoking IsUnityCompatible() from '{0}':\n\n{1}", m.DeclaringType.Assembly.GetName().Name, e));
                        return true;
                    }
                })
                .Select(m => m.DeclaringType.Assembly.GetName().Name)
                .ToArray();

            Array.Sort(incompatible);
            Array.Sort(incompatibleUnity);

            String message = String.Empty;

            // if (IsWin64())
            // {
            //    message += "WARNING: You are using 64-bit KSP on Windows. This version of KSP is known to cause crashes. It's highly recommended that you use either 32-bit KSP on Windows or switch to Linux.";
            // }

            if ((incompatible.Length > 0) || (incompatibleUnity.Length > 0))
            {
                message += ((message == String.Empty) ? "Some" : "\n\nAdditionally, some") + " installed mods may be incompatible with this version of Kerbal Space Program. Features may be broken or disabled. Please check for updates to the listed mods.";

                if (incompatible.Length > 0)
                {
                    Debug.LogWarning("[CompatibilityChecker] Incompatible mods detected: " + String.Join(", ", incompatible));
                    message += String.Format("\n\nThese mods are incompatible with KSP {0}.{1}.{2}:\n\n", Versioning.version_major, Versioning.version_minor, Versioning.Revision);
                    message += String.Join("\n", incompatible);
                }

                if (incompatibleUnity.Length > 0)
                {
                    Debug.LogWarning("[CompatibilityChecker] Incompatible mods (Unity) detected: " + String.Join(", ", incompatibleUnity));
                    message += String.Format("\n\nThese mods are incompatible with Unity {0}:\n\n", Application.unityVersion);
                    message += String.Join("\n", incompatibleUnity);
                }
            }

            if ((incompatible.Length > 0) || (incompatibleUnity.Length > 0) /*|| IsWin64()*/)
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "CompatibilityChecker", "Incompatible Mods Detected", message, "OK", true, UISkinManager.defaultSkin);
            }
        }

        public static Boolean IsWin64()
        {
            return (IntPtr.Size == 8) && (Environment.OSVersion.Platform == PlatformID.Win32NT);
        }
    }
}