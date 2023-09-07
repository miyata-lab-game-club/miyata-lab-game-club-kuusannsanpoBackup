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
/// @file AmbisonicsAudioListener.cs
/// @brief AudioListener extension component required when using AmbisonicsAudioFilter
///

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Soundxr.Effect.Spatializer {

    /// AudioListener extension component required when using AmbisonicsAudioFilter
    [AddComponentMenu("Sound xR/Effect/Spatializer/AmbisonicsAudioListener")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioListener))]
    public class AmbisonicsAudioListener : MonoBehaviour
    {
        void Start()
        {
            SendAudioListenrMatrix();
        }

        void Update()
        {
            if (!transform.hasChanged)
                return;

            transform.hasChanged = false;
            SendAudioListenrMatrix();
        }

        /// list of notification destination AmbisonicsAudioFilter
        List<AmbisonicsAudioFilter> filters = new List<AmbisonicsAudioFilter>();

        /// register AmbisonicsAudioFilter
        public void AddAudioFilter(AmbisonicsAudioFilter filter)
        {
            filters.Add(filter);
        }

        /// unregister AmbisonicsAudioFilter
        public void RemoveAudioFilter(AmbisonicsAudioFilter filter)
        {
            filters.Remove(filter);
        }
        
        // notify registered AmbisonicsAudioFilters of transform changes
        void SendAudioListenrMatrix()
        {
            foreach (var filter in filters)
            {
                filter.OnChangedAudioListenerTransform();
            }
        }

    }

} // namespace Soundxr.Effect.Spatializer
