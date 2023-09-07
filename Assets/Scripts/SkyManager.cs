using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;

using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.VFX;

public class SkyManager : MonoBehaviour
{
    [SerializeField] private Light _light;
    [SerializeField] private Rigidbody lightRigidbody;
    private float deltaRotateX = 0;
    private Vector3 deltaRotate;
    private float m_isNight;

    // Start is called before the first frame update
    private void Start()
    {
        // 1秒ごとに回転するX軸方向の回転量
        deltaRotateX = 360 / GameManager.instance.gameEndTime;
        Debug.Log(deltaRotateX);
        Debug.Log(Time.deltaTime);
        // 1秒ごとに回転する量
        deltaRotate = new Vector3(deltaRotateX, 0, 0) / 60;
        Debug.Log(deltaRotate);
    }

    // Update is called once per frame
    private void Update()
    {
        //
        if (GameManager.instance.isPlaying == true)
        {
            lightRigidbody.angularVelocity = deltaRotate;
        }
        else
        {
            lightRigidbody.angularVelocity = new Vector3(0, 0, 0);
        }
        UpdateDayNightShift();
    }

    private void UpdateDayNightShift()
    {
        float y = _light.transform.forward.y;
        y = Mathf.Clamp(y, -0.2f, 0.2f);
        m_isNight = Remap(y, -0.2f, 0.2f, 0f, 1f);
        Shader.SetGlobalFloat("_IsNight", m_isNight);
    }

    private float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}