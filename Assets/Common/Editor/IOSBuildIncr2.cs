using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor;

#if UNITY_IOS

public class IncrementBuildNumber : IPreprocessBuildWithReport {
    public int callbackOrder { get { return 0; } } // Part of the IPreprocessBuildWithReport interface

    public void OnPreprocessBuild(BuildReport report) {
        if (report.summary.platform == BuildTarget.iOS) // Check if the build is for iOS
        {
            // Increment build number if proper int, ignore otherwise
            int currentBuildNumber;
            if (int.TryParse(PlayerSettings.iOS.buildNumber, out currentBuildNumber)) {
                string newBuildNumber = (currentBuildNumber + 1).ToString();
                Debug.Log("Setting new iOS build number to " + newBuildNumber);
                PlayerSettings.iOS.buildNumber = newBuildNumber;
            } else {
                Debug.LogError("Failed to parse build number " + PlayerSettings.iOS.buildNumber + " as int.");
            }
        }
    }
}
#endif