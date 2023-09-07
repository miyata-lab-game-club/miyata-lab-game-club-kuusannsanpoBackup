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
/// @file AmbisonicsApi.cs
/// @brief Wrapper class for Ambisonics Native Plugin
///

using System.Runtime.InteropServices;
using UnityEngine;

namespace Soundxr.Effect.Spatializer {

    /// Wrapper class for Ambisonics Native Plugin
    public static class AmbisonicsApi {

        static public int Attach(int id)
        {
            return Binding.Ambisonics_Attach(id);
        }

        static public int Detach(int id)
        {
            return Binding.Ambisonics_Detach(id);
        }

        static public int SetWaveFile(int id, string path)
        {
            if (path == null) path =  "";
            return Binding.Ambisonics_SetWaveFile(id, path);
        }

        static public int GetOrder(int id, out int order)
        {
            return Binding.Ambisonics_GetOrder(id, out order);
        }

        static public int GetLength(int id, out uint length)
        {
            return Binding.Ambisonics_GetLength(id, out length);
        }

        static public int SetListenerMatrix(int id, Matrix4x4 matrix)
        {
            float[] position = new float[3];
            float[] rotation = new float[9];
            for (var i = 0; i < 3; i++) {
                position[i] = matrix[3 * 4 + i];
                for (var j = 0; j < 3; j++) {
                    rotation[i * 3 + j] = matrix[j * 4 + i];
                }
            }
            return Binding.Ambisonics_SetListenerInfo(id, position, rotation);
        }

        static public int SetSourceMatrix(int id, Matrix4x4 matrix)
        {
            float[] position = new float[3];
            float[] rotation = new float[9];
            for (var i = 0; i < 3; i++) {
                position[i] = matrix[3 * 4 + i];
                for (var j = 0; j < 3; j++) {
                    rotation[i * 3 + j] = matrix[j * 4 + i];
                }
            }
            return Binding.Ambisonics_SetSourceInfo(id, position, rotation);
        }

        static public int SetDecay(int id, bool on, int curve)
        {
            return Binding.Ambisonics_SetDecay(id, on, curve);
        }

        static public int SetHRTFType(int id, int type)
        {
            return Binding.Ambisonics_SetHRTFType(id, type);
        }

        static public int Process(int id, float[] buffer, ref int length, uint position, int order, int speakerSet)
        {
            return Binding.Ambisonics_Process(id, buffer, ref length, position, order, speakerSet);
        }
    }

    /// DllImport each method of Ambisonics Native Plugin (internal)
    internal static class Binding {
#if UNITY_IOS && !UNITY_EDITOR
        private const string LIBNAME = "__Internal"; // iOS requires static link library
#else
        private const string LIBNAME = "AudioPluginSoundxR";
#endif
        [DllImport(LIBNAME)] internal static extern int Ambisonics_Attach(int id);
        [DllImport(LIBNAME)] internal static extern int Ambisonics_Detach(int id);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport(LIBNAME, CharSet=CharSet.Unicode)] internal static extern int Ambisonics_SetWaveFile(int id, string waveFilePath);
#else
        [DllImport(LIBNAME, CharSet=CharSet.Ansi)] internal static extern int Ambisonics_SetWaveFile(int id, string waveFilePath);
#endif
        
        [DllImport(LIBNAME)] internal static extern int Ambisonics_GetOrder(int id, out int order);
        [DllImport(LIBNAME)] internal static extern int Ambisonics_GetLength(int id, out uint length);
        [DllImport(LIBNAME)] internal static extern int Ambisonics_SetListenerInfo(int id, float[] position, float[] rotation);
        [DllImport(LIBNAME)] internal static extern int Ambisonics_SetSourceInfo(int id, float[] position, float[] rotation);
        [DllImport(LIBNAME)] internal static extern int Ambisonics_SetDecay(int id, bool on, int curve);
        [DllImport(LIBNAME)] internal static extern int Ambisonics_SetHRTFType(int id, int type);
        [DllImport(LIBNAME)] internal static extern int Ambisonics_Process(int id, [In, Out] float[] buffer, ref int length, uint position, int order, int speakerSet);
    }

} // namespace Soundxr.Effect.Spatializer
