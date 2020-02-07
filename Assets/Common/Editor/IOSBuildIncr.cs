using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;
/*
public static class IOSBuilIncr {
    [PostProcessBuild]
    public static void OnBuildComplete(BuildTarget buildTarget, string pathToBuiltProject) {
        if (buildTarget != BuildTarget.iOS) {
            return;
        }

        IncrementBuildNumber();
    }

    private static void IncrementBuildNumber() {
        // Load the PlayerSettings asset.
        var playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();

        if (playerSettings != null) {
            SerializedObject so = new SerializedObject(playerSettings);

            // Find the build number property.
            var sp = so.FindProperty("iPhoneBuildNumber");

            var currentValue = sp.stringValue;
            int ver = 0;

            if (int.TryParse(currentValue, out ver)) {
                // Increment version.
                sp.stringValue = (ver + 1).ToString();

                // Save player settings.
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }
    }
}
*/
