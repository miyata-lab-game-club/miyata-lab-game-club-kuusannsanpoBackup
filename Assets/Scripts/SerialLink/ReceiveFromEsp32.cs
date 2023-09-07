using System;
using System.Collections;
using UnityEngine;

public class ReceiveFromEsp32 : MonoBehaviour
{
    public GameObject rotateObject;
    private Queue outputQueue;

    public int buttonState = 9;  // buttonState変数

    private void Start()
    {
        outputQueue = Queue.Synchronized(new Queue());
        if (SerialPortManager.Instance != null)
        {
            SerialPortManager.Instance.OnDataReceived += ProcessReceivedData;
        }
        else
        {
            Debug.LogError("SerialPortManager instance not found!");
        }
    }

    private void ProcessReceivedData(string message)
    {
        try
        {
            string[] parts = message.Split(',');
            if (parts.Length == 4)  // 4つの要素を期待
            {
                int[] data = new int[3];
                for (int i = 0; i < 3; i++)
                {
                    float tmpData = float.Parse(parts[i]);
                    data[i] = (int)tmpData;
                }
                buttonState = char.Parse(parts[3].Trim());  // 追加: buttonStateにデータを格納
                outputQueue.Enqueue(data);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error while processing received data: {e.Message}");
        }
    }

    private struct Rotate
    {
        public int xRotate;
        public int zRotate;
    }

    // 平均をとるための一時的なrotate配列
    private Rotate[] tmpRotates = new Rotate[5];

    private int frame = 0;
    private Rotate averageRotate;
    private int targetFrame = 3;

    private void Update()
    {
        if (outputQueue.Count != 0)
        {
            int[] rotate = (int[])outputQueue.Dequeue();  // ここをfloatからintに変更
            Rotate tmpRotate;
            tmpRotate.xRotate = -rotate[1];
            tmpRotate.zRotate = -rotate[0];
            //rotateObject.transform.eulerAngles = new Vector3(-rotate[1], 0, -rotate[0]);  // intをfloatにキャスト

            //Debug.Log($"Button State: {buttonState}");  // 追加: buttonStateを表示

            if (frame < targetFrame)
            {
                tmpRotates[frame] = tmpRotate;
                frame++;
            }
            else if (frame == targetFrame)
            {
                // 5フレームの平均をとる
                for (int i = 0; i < targetFrame; i++)
                {
                    averageRotate.xRotate += tmpRotates[i].xRotate / targetFrame;
                    averageRotate.zRotate += tmpRotates[i].zRotate / targetFrame;
                }
                rotateObject.transform.eulerAngles = new Vector3(tmpRotate.xRotate, 0, tmpRotate.zRotate);
                //Debug.Log("角度 " + averageRotate.xRotate+"," +averageRotate.zRotate);
                // 初期化
                tmpRotates = new Rotate[targetFrame];
                frame = 0;
            }
        }
    }

    private void OnDestroy()
    {
        if (SerialPortManager.Instance != null)
        {
            SerialPortManager.Instance.OnDataReceived -= ProcessReceivedData;
        }
    }
}