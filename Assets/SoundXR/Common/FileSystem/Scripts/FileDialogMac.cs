﻿/*!
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
/// @file FileDialogMac.cs
/// @brief FileDialog class for macOS
///

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX

using System;
using System.Runtime.InteropServices;

namespace Soundxr.Common.FileSystem {

    internal static class FileDialogMac
    {
        private delegate void FileDialogCallback(string path);

        [AOT.MonoPInvokeCallback(typeof(FileDialogCallback))]
        private static void openFileCallback(string result)
        {
            _openFileCallback.Invoke(result);
        }

        private static Action<string> _openFileCallback;

        public static void OpenFileDialog(string title, string directory, string extension, Action<string> cb)
        {
            _openFileCallback = cb;
            Binding.OpenFileDialog(title ?? "", directory ?? "", extension ?? "", openFileCallback);
        }

        private static class Binding
        {
            const string LIBNAME = "SoundxrFileSystem";
            [DllImport(LIBNAME)] internal static extern void OpenFileDialog(string title, string directory, string extension, FileDialogCallback callback);
        }
    }

} // namespace Soundxr.Common.FileSystem

#endif