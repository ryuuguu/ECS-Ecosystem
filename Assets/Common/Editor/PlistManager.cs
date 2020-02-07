using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using System.Collections.Generic;

public class PlistManager : MonoBehaviour {

#if UNITY_IOS

    [PostProcessBuild]
    static void OnPostprocessBuild(BuildTarget buildTarget, string path) {
        // Read plist
        var plistPath = Path.Combine(path, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        // Update value
        PlistElementDict rootDict = plist.root;
        rootDict.SetString("NSCameraUsageDescription", "Used for adding picture while recording game play");

        // Write plist
        File.WriteAllText(plistPath, plist.WriteToString());
    }
#endif
}

