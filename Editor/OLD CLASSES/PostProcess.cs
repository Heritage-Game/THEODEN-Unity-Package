#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
 
public class PostProcess {
 
    [PostProcessBuild]
    public static void ChangeBuildManifest ( BuildTarget buildTarget, string pathToBuiltProject )
    {
        if (buildTarget != BuildTarget.iOS) return;
        // paths
        var xCodeProjFolderPath = $"{pathToBuiltProject}/Unity-iPhone.xcodeproj";
        var xcSettingsPath = $"{xCodeProjFolderPath}/project.xcworkspace/xcshareddata/WorkspaceSettings.xcsettings";
        Debug.Log($"xCodeProjFolderPath: {xCodeProjFolderPath}");
        Debug.Log($"xcSettingsPath: {xcSettingsPath}");
        // change the xcode project to use the new build system, without doing this can not compile and get an error in xcode, plus the legacy build system is now deprecated
        var xcSettingsDoc = new PlistDocument();
        xcSettingsDoc.ReadFromString( File.ReadAllText( xcSettingsPath ) );
        var xcSettingsDict = xcSettingsDoc.root;
        var xcSettingsValues = xcSettingsDict.values;
        const string buildSystemTypeKey = "BuildSystemType";
        if ( xcSettingsValues.ContainsKey( buildSystemTypeKey ) ) {
            xcSettingsValues.Remove( buildSystemTypeKey ); // the removal of this key/value pair <key>BuildSystemType</key><string>Original</string> allows xcode to use the default new build system setting
        }
        File.WriteAllText( xcSettingsPath, xcSettingsDoc.WriteToString() );
    }
}
#endif