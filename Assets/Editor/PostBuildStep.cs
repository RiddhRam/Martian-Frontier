#if UNITY_IPHONE
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
 
public class PostBuildStep {
 
    [PostProcessBuild(0)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToXcode) {
        if (buildTarget == BuildTarget.iOS) {
            AddPListValues(pathToXcode);
        }
    }
 
    // Implement a function to read and write values to the plist file:
    static void AddPListValues(string pathToXcode) {
        // Retrieve the plist file from the Xcode project directory:
        string plistPath = pathToXcode + "/Info.plist";
        PlistDocument plistObj = new PlistDocument();
 
        // Read the values from the plist file:
        plistObj.ReadFromString(File.ReadAllText(plistPath));
 
        // Set values from the root object:
        PlistElementDict plistRoot = plistObj.root;
 
        // Set the description key-value in the plist:
        plistRoot.SetString("NSUserTrackingUsageDescription", "Your data will be used to provide you a better and personalized ad experience.");
        plistRoot.SetString("NSUserNotificationUsageDescription", "We use notifications to remind you of tournament rewards!");
 
        // Save changes to the plist:
        File.WriteAllText(plistPath, plistObj.WriteToString());
    }
}
#endif