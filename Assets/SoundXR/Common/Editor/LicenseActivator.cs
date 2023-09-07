/*!
 * Copyright 2022 Yamaha Corp. All Rights Reserved.
 *
 * The content of this file includes portions of the Yamaha Sound xR
 * released in source code form as part of the plugin package.
 *
 * Commercial License Usage
 *
 * Licensees holding valid commercial licenses to the Yamaha Sound xR
 * may use this file in accordance with the end user license agreement
 * provided with the software or, alternatively, in accordance with the
 * terms contained in a written agreement between you and Yamaha Corp.
 *
 * Apache License Usage
 *
 * Alternatively, this file may be used under the Apache License, Version 2.0 (the "Apache License");
 * you may not use this file except in compliance with the Apache License.
 * You may obtain a copy of the Apache License at
 * http://www.apache.org/licenses/LICENSE-2.0.
 *
 * Unless required by applicable law or agreed to in writing, software distributed
 * under the Apache License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES
 * OR CONDITIONS OF ANY KIND, either express or implied. See the Apache License for
 * the specific language governing permissions and limitations under the License.
 */

///
/// @file LicenseActivator.cs
/// @brief License Activator class
///

using System.Runtime.InteropServices; // DllImport
using System.Collections.Generic; // IEnumerable
using System.IO; // Directory, File
using System.Linq; // FirstOrDefault
using UnityEngine; // AudioSettings
using UnityEditor;

namespace Soundxr {

[InitializeOnLoad]
public class LicenseActivator {

    private static string SpatializerPluginName { get; } = "Sound xR Core";
    private static string LicenseKeySaveDir { get; } = "Assets/SoundXR/Common/.tmp/";

    /// @brief check the license key when Unity loads or this script is recompiled.
    static LicenseActivator() {
        Binding.ReturnLicense();
        if (!Directory.Exists(LicenseKeySaveDir))
            Directory.CreateDirectory(LicenseKeySaveDir);
        string[] files = Directory.GetFiles(LicenseKeySaveDir, "*.key");
        bool activated = false;
        foreach (string fpath in files) {
            var serial = LoadFileString(fpath);
            if (serial != null  && Binding.ActivateLicense(serial, serial.Length)) {
                activated = true;
                break;
            }
        }
        if (!activated)
            Debug.LogError("Sound xR: License key is not activated. Please activate it from UnityEditor's menu \"Sound xR > Activate License Key\".");
    }

    /// @brief activate license key from Unity Editor's Menu.
    [MenuItem("Sound xR/Activation/Activate License Key")]
    private static void ActivateLiceseKey() {
        //--- Step 1. select the license file.
        string fpath = EditorUtility.OpenFilePanel("Import license key", "", "key");
        if (string.IsNullOrEmpty(fpath))
            return;

        //--- Step 2. remove old saved license files.
        IEnumerable<string> files = Directory.EnumerateFiles(LicenseKeySaveDir, "*.key");
        foreach (var path in files)
            File.Delete(path);

        //--- Step 3. activate the license and show result.
        var serial = LoadFileString(fpath);
        bool res = serial != null && Binding.ActivateLicense(serial, serial.Length);
        EditorUtility.DisplayDialog("Activation Result", res ? "Success! " : "Failed. ", "Close");
        if (!res)
            return;

        //--- Step 4. save the license file.
        File.Copy(fpath, LicenseKeySaveDir + Path.GetFileName(fpath));

        //--- Step 5. change the current spatializer plugin to "Sound xR Core" if required.
        string currentPluginName = AudioSettings.GetSpatializerPluginName();
        if (currentPluginName.Equals(SpatializerPluginName))
            return;
        bool pluginChangeRequired = EditorUtility.DisplayDialog(
            "Warning",
            "Current spatializer plugin is not " + SpatializerPluginName + "." + System.Environment.NewLine + "Do you want to enable it automatically?",
            "Yes", "No");
        if (pluginChangeRequired)
            ChangeAudioSpatializer(SpatializerPluginName);
    }

    /// @brief activate license key from Unity Editor's Menu.
    [MenuItem("Sound xR/Activation/Return License Key")]
    private static void ReturnLiceseKey() {
        if (!Directory.Exists(LicenseKeySaveDir)) {
            EditorUtility.DisplayDialog("Return License Key", "No found License Key.", "Close");
            return;
        }
        IEnumerable<string> files = Directory.EnumerateFiles(LicenseKeySaveDir, "*.key");
        if (files.Count() == 0) {
            EditorUtility.DisplayDialog("Return License Key", "No found License Key.", "Close");
            return;
        }
        string deletedFiles = "";
        foreach (string fpath in files) {
            deletedFiles += fpath + System.Environment.NewLine;
            File.Delete(fpath);
        }

        Binding.ReturnLicense();
        EditorUtility.DisplayDialog("Return License Key",
            "Success. The bellow files are deleted."+System.Environment.NewLine + deletedFiles,
            "Close");
    }

    /// @brief load file data as UTF-8 string.
    /// @param[in] filePath path to file
    private static byte[] LoadFileString(string filePath) {
        return File.ReadAllBytes(filePath);
    }

    /// @brief Change the project's spatializer plugin.
    /// @param[in] spatializerPluginName new spatializer plugin
    /// @return true:success false:failed
    private static bool ChangeAudioSpatializer(string spatializerPluginName) {
        const string assetPath = "ProjectSettings/AudioManager.asset";
        UnityEngine.Object manager = AssetDatabase.LoadAllAssetsAtPath(assetPath).FirstOrDefault();
        SerializedObject obj = new SerializedObject(manager);
        // Project Settings > Audio > Spatializer
        obj.FindProperty("m_SpatializerPlugin").stringValue = spatializerPluginName;
        return obj.ApplyModifiedProperties();
    }

    /// @brief Binding of Native Audio Plugin's methods
    internal static class Binding {
#if UNITY_IOS && !UNITY_EDITOR
        private const string LIBNAME = "__Internal"; // iOS requires static link library
#else
        private const string LIBNAME = "AudioPluginSoundxR";
#endif
        [DllImport(LIBNAME)] internal static extern bool ActivateLicense(byte[] key, int size);
        [DllImport(LIBNAME)] internal static extern bool ReturnLicense();
        [DllImport(LIBNAME)] internal static extern bool IsActivated();
    }
}

} // namespace Soundxr