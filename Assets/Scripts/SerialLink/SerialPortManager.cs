using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System;

public class SerialPortManager : MonoBehaviour
{
    public static SerialPortManager Instance { get; private set; }

    public delegate void SerialDataReceivedEventHandler(string message);

    public event SerialDataReceivedEventHandler OnDataReceived;

    [Header("Port Names")]
    [SerializeField]
    private string LF_port = "COM1";

    [SerializeField]
    private string RF_Port = "COM2";

    [SerializeField]
    private string RB_Port = "COM11";

    [SerializeField]
    private string LB_Port = "COM4";

    [SerializeField]
    private string NF_Port = "COM5"; // s5をnfに変更

    [SerializeField]
    private string kasa_Port = "COM6"; // s6をkasaに変更

    public SerialPort[] serialPorts = new SerialPort[6];
    public int baudRate = 115200;

    private Thread[] threads = new Thread[6];
    private bool isRunning_ = false;

    private string[] messages = new string[6];
    private bool[] isNewMessageReceived_ = new bool[6];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        OpenAllPorts();
    }

    private void Update()
    {
        for (int i = 0; i < 6; i++)
        {
            if (isNewMessageReceived_[i] && OnDataReceived != null)
            {
                OnDataReceived(messages[i]);
                isNewMessageReceived_[i] = false;
            }
        }
    }

    private void OnApplicationQuit()
    {
        CloseAllPorts();
    }

    // 通信ポートをすべて開く
    private void OpenAllPorts()
    {
        string[] portNames = { LF_port, RF_Port, RB_Port, LB_Port, NF_Port, kasa_Port }; // s5とs6をnfとkasaに変更
        for (int i = 0; i < 6; i++)
        {
            try
            {
                serialPorts[i] = new SerialPort(portNames[i], baudRate, Parity.None, 8, StopBits.One);
                serialPorts[i].Open();

                int threadIndex = i; // Avoid lambda capture problem
                threads[i] = new Thread(() => Read(threadIndex));
                threads[i].Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error opening port or starting thread for index {i}: {e.Message}");
            }
        }
        isRunning_ = true;
    }

    // 通信ポートをすべてとじる
    private void CloseAllPorts()
    {
        isRunning_ = false;
        for (int i = 0; i < 6; i++)
        {
            if (threads[i] != null && threads[i].IsAlive)
            {
                threads[i].Join();
            }
            if (serialPorts[i] != null && serialPorts[i].IsOpen)
            {
                serialPorts[i].Close();
                serialPorts[i].Dispose();
            }
        }
    }

    // 引数のポートに対応するスレッドをよみこむ
    public void Read(int index)
    {
        while (isRunning_ && serialPorts[index] != null && serialPorts[index].IsOpen)
        {
            try
            {
                messages[index] = serialPorts[index].ReadLine();
                isNewMessageReceived_[index] = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }
        }
    }

    // 引数indexのポートにmessageをおくる
    public void WriteToPort(int index, string message)
    {
        try
        {
            serialPorts[index].Write(message);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
    }
}