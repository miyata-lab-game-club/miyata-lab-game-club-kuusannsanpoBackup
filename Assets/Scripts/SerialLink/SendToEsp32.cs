using System;
using System.IO.Ports;
using UnityEngine;
using System.Collections;

public class SendToEsp32 : MonoBehaviour
{
    //private SerialPortManager spManager; // SerialPortManager の参照を持つ変数

    // WindManagerの参照
    //public WindManager windManager;

    // Port４とネックファンは後で追加
    private int LF_UnderCap;

    private int RF_UnderCap;
    private int RB_UnderCap;
    private int LB_UnderCap;

    private int LF_AboveCap = 4;
    private int RF_AboveCap = 4;
    private int RB_AboveCap = 4;
    private int LB_AboveCap = 4;

    // ABOVE:全開
    private const int ABOVE_FULL＿OPEN = 4;

    // ABOVE:全閉じ
    private const int ABOVE_HALF＿OPEN = 5;

    // ABOVE:全閉じ
    private const int ABOVE_CLOSE = 6;

    // UNDER:全開
    private const int UNDER_FULL＿OPEN = 3;

    // UNDER:中くらい
    private const int UNDER_HALF＿OPEN = 2;

    // UNDER:閉じる
    private const int UNDER_CLOSE = 1;

    // WindManagerのup(bool)をa or b　のstringにして格納するようの変数
    private string windBoostedRise;

    //文字送る用の型
    private string LF_Data;

    private string RF_Data;
    private string RB_Data;
    private string LB_Data;
    private string NF_Data;

    private void Start()
    {
        // spManager = SerialPortManager.Instance;

        // if (spManager == null)
        // {
        //     Debug.LogError("SerialPortManager instance not found!");
        //     return;
        // }

        // if (windManager == null)
        // {
        //     Debug.LogError("WindManager reference is not set on SendToEsp32.");
        //     return;
        // }

        // {
        //     StartCoroutine(SendDataCoroutine());
        // }
    }

    public void StartSendData(SerialPortManager spManager, WindManager windManager)
    {
        StartCoroutine(SendDataCoroutine(spManager, windManager));
    }

    // 0.5秒ずつ情報を送信　
    private IEnumerator SendDataCoroutine(SerialPortManager spManager, WindManager windManager)
    {
        while (true)  // 無限ループで送信処理を繰り返す
        {
            try
            {
                // windManager.upが真なら1、偽なら0を格納
                windBoostedRise = windManager.up ? "a" : "b";
                SetPortIndices(windManager);
                SetUnderPortIndices(windManager);

                // 力覚装置1~4s用に文字型に変換(引っ張る力と急上昇を送信)
                //1つめにAbove, 2つめにUnder
                LF_Data = LF_AboveCap.ToString() + LF_UnderCap.ToString() + windBoostedRise;
                RF_Data = RF_AboveCap.ToString() + RF_UnderCap.ToString() + windBoostedRise;
                //string tmpString = RB_AboveCap.ToString();
                //Debug.Log(RB_AboveCap.ToString() + "型" + RB_AboveCap.ToString().GetType());
                //RB_Data = RB_AboveCap.ToString() + RB_UnderCap.ToString();
                RB_Data = RB_AboveCap.ToString() + RB_UnderCap.ToString() + windBoostedRise;
                Debug.Log("RB_Data: " + RB_Data);
                LB_Data = LB_AboveCap.ToString() + LB_UnderCap.ToString() + windBoostedRise;

                NF_Data = windManager.currentWindIndex.ToString() + windBoostedRise;
                SetPortIndices(windManager);
                SetUnderPortIndices(windManager);
                // それぞれ送信
                spManager.WriteToPort(0, LF_Data);//LFに送信
                spManager.WriteToPort(1, RF_Data);//RFに送信
                spManager.WriteToPort(2, RB_Data);//RBに送信
                spManager.WriteToPort(3, LB_Data);//LBに送信
                spManager.WriteToPort(4, NF_Data);//Neckfanに送信

                // spManager.Read(5)の結果をデバッグログで表示
            }
            catch (Exception ex)
            {
                Debug.LogError("Could not send to ESP32: " + ex.Message);
            }

            yield return new WaitForSeconds(0.1f);  // 0.5秒待機
            //spManager.WriteToPort(2, RB_AboveCap.ToString());//RBに送信
            //yield return new WaitForSeconds(0.1f);  // 0.1秒待機
            //spManager.WriteToPort(2, LF_AboveCap.ToString());//RBに送信
        }
    }

    private void FixedUpdate()
    {
        //Debug.Log("データ" + LF_Data);
    }

    //caseの中でpullpowerを考慮してそれぞれ(LF_port~4)の力を決める
    private void SetPortIndices(WindManager windManager)
    {
        /*
        if(windManager.isMatching == false){
            LF_power = CLOSE; RF_power = CLOSE; RB_power = CLOSE; LB_power = CLOSE;
            return;
        }*/
        switch (windManager.currentWindIndex)
        {
            case 0:
                LF_UnderCap = UNDER_FULL＿OPEN; RF_UnderCap = UNDER_FULL＿OPEN; RB_UnderCap = UNDER_FULL＿OPEN; LB_UnderCap = UNDER_FULL＿OPEN;
                break;
            /*
            // 1が下のモータ閉める、2が半分、3が下のモータ開いて上がく。
            */
            case 1://北(前)
                LF_UnderCap = UNDER_FULL＿OPEN; RF_UnderCap = UNDER_FULL＿OPEN; RB_UnderCap = UNDER_CLOSE; LB_UnderCap = UNDER_CLOSE;
                break;

            case 2://北東(右前)
                LF_UnderCap = UNDER_CLOSE; RF_UnderCap = UNDER_FULL＿OPEN; RB_UnderCap = UNDER_CLOSE; LB_UnderCap = UNDER_CLOSE;
                break;

            case 3://東(右)
                LF_UnderCap = UNDER_CLOSE; RF_UnderCap = UNDER_FULL＿OPEN; RB_UnderCap = UNDER_FULL＿OPEN; LB_UnderCap = UNDER_CLOSE;
                break;

            case 4://南東(右後)
                LF_UnderCap = UNDER_CLOSE; RF_UnderCap = UNDER_CLOSE; RB_UnderCap = UNDER_FULL＿OPEN; LB_UnderCap = UNDER_CLOSE;
                break;

            case 5://南(後)
                LF_UnderCap = UNDER_CLOSE; RF_UnderCap = UNDER_CLOSE; RB_UnderCap = UNDER_FULL＿OPEN; LB_UnderCap = UNDER_FULL＿OPEN;
                break;

            case 6://南西(左後)
                LF_UnderCap = UNDER_CLOSE; RF_UnderCap = UNDER_CLOSE; RB_UnderCap = UNDER_CLOSE; LB_UnderCap = UNDER_FULL＿OPEN;
                break;

            case 7://西(左)
                LF_UnderCap = UNDER_FULL＿OPEN; RF_UnderCap = UNDER_CLOSE; RB_UnderCap = UNDER_CLOSE; LB_UnderCap = UNDER_FULL＿OPEN;
                break;

            case 8://北西(左前)
                LF_UnderCap = UNDER_FULL＿OPEN; RF_UnderCap = UNDER_CLOSE; RB_UnderCap = UNDER_CLOSE; LB_UnderCap = UNDER_CLOSE;
                break;
        }
    }

    // 上のふたの制御をする　上のふたに命令を送る
    private void SetUnderPortIndices(WindManager windManager)
    {/*
            private const int ABOVE_FULL＿OPEN = 6;

    // ABOVE:全閉じ
    private const int ABOVE_HALF＿OPEN = 5;

    // ABOVE:全閉じ
    private const int ABOVE_CLOSE = 4;
        */
        // 上：全部あいてる
        // 上昇準備＆上昇中じゃなかったら
        if (windManager.sendToHardUpSignal == false)
        {
            //Debug.Log(windManager.isMatchingFinal);
            if (windManager.isMatchingFinal == true)
            {
                if (LF_UnderCap == UNDER_FULL＿OPEN)
                {
                    LF_AboveCap = ABOVE_CLOSE;
                }
                if (RF_UnderCap == UNDER_FULL＿OPEN)
                {
                    RF_AboveCap = ABOVE_CLOSE;
                }
                if (RB_UnderCap == UNDER_FULL＿OPEN)
                {
                    Debug.Log("aaa");
                    RB_AboveCap = ABOVE_CLOSE;
                }
                if (LB_UnderCap == UNDER_FULL＿OPEN)
                {
                    LB_AboveCap = ABOVE_CLOSE;
                }
            }
            else
            {
                if (LF_UnderCap == UNDER_FULL＿OPEN)
                {
                    LF_AboveCap = ABOVE_FULL＿OPEN;
                }
                if (RF_UnderCap == UNDER_FULL＿OPEN)
                {
                    RF_AboveCap = ABOVE_FULL＿OPEN;
                }
                if (RB_UnderCap == UNDER_FULL＿OPEN)
                {
                    // Debug.Log("bbb");
                    RB_AboveCap = ABOVE_FULL＿OPEN;
                }
                if (LB_UnderCap == UNDER_FULL＿OPEN)
                {
                    LB_AboveCap = ABOVE_FULL＿OPEN;
                }
            }
        }
        // 上昇準備 & 上昇中だったら
        else
        {
            // 上昇が終わっていなかったら
            if (windManager.upFinish == false)
            {
                // 蓋が閉じる
                LF_AboveCap = ABOVE_CLOSE; RF_AboveCap = ABOVE_CLOSE; RB_AboveCap = ABOVE_CLOSE; LB_AboveCap = ABOVE_CLOSE;
            }
            else
            {
                // 上昇が終わったらふたがあく
                LF_AboveCap = ABOVE_FULL＿OPEN; RF_AboveCap = ABOVE_FULL＿OPEN; RB_AboveCap = ABOVE_FULL＿OPEN; LB_AboveCap = ABOVE_FULL＿OPEN;
            }
        }
    }

    /*
    if(windManager.isMatching == false){
        LF_power = CLOSE; RF_power = CLOSE; RB_power = CLOSE; LB_power = CLOSE;
        return;
    }*/

    /*
    if (windManager.isMatchingFinal == true)
    {
        switch (windManager.currentWindIndex)
        {
            // 1が下のモータ閉める、2が半分、3が下のモータ開いて上がく。
            case 1://北(前)
                LF_AboveCap = FULL＿OPEN; RF_AboveCap = FULL＿OPEN; RB_AboveCap = CLOSE; LB_AboveCap = CLOSE;
                break;

            case 2://北東(右前)
                LF_AboveCap = CLOSE; RF_AboveCap = FULL＿OPEN; RB_AboveCap = CLOSE; LB_AboveCap = CLOSE;
                break;

            case 3://東(右)
                LF_AboveCap = CLOSE; RF_AboveCap = FULL＿OPEN; RB_AboveCap = FULL＿OPEN; LB_AboveCap = CLOSE;
                break;

            case 4://南東(右後)
                LF_AboveCap = CLOSE; RF_AboveCap = CLOSE; RB_AboveCap = FULL＿OPEN; LB_AboveCap = CLOSE;
                break;

            case 5://南(後)
                LF_AboveCap = CLOSE; RF_AboveCap = CLOSE; RB_AboveCap = FULL＿OPEN; LB_AboveCap = FULL＿OPEN;
                break;

            case 6://南西(左後)
                LF_AboveCap = CLOSE; RF_AboveCap = CLOSE; RB_AboveCap = CLOSE; LB_AboveCap = FULL＿OPEN;
                break;

            case 7://西(左)
                LF_AboveCap = FULL＿OPEN; RF_AboveCap = CLOSE; RB_AboveCap = CLOSE; LB_AboveCap = FULL＿OPEN;
                break;

            case 8://北西(左前)
                LF_AboveCap = FULL＿OPEN; RF_AboveCap = CLOSE; RB_AboveCap = CLOSE; LB_AboveCap = CLOSE;
                break;
        }
    }
    // 該当の箇所のふたを閉める
    else
    {
        switch (windManager.currentWindIndex)
        {
            // 1が下のモータ閉める、2が半分、3が下のモータ開いて上がく。
            case 1://北(前)
                LF_AboveCap = CLOSE; RF_AboveCap = CLOSE; RB_AboveCap = FULL＿OPEN; LB_AboveCap = FULL＿OPEN;
                break;

            case 2://北東(右前)
                LF_AboveCap = FULL＿OPEN; RF_AboveCap = CLOSE; RB_AboveCap = FULL＿OPEN; LB_AboveCap = FULL＿OPEN;
                break;

            case 3://東(右)
                LF_AboveCap = FULL＿OPEN; RF_AboveCap = CLOSE; RB_AboveCap = FULL＿OPEN; LB_AboveCap = CLOSE;
                break;

            case 4://南東(右後)
                LF_AboveCap = CLOSE; RF_AboveCap = CLOSE; RB_AboveCap = FULL＿OPEN; LB_AboveCap = CLOSE;
                break;

            case 5://南(後)
                LF_AboveCap = CLOSE; RF_AboveCap = CLOSE; RB_AboveCap = FULL＿OPEN; LB_AboveCap = FULL＿OPEN;
                break;

            case 6://南西(左後)
                LF_AboveCap = CLOSE; RF_AboveCap = CLOSE; RB_AboveCap = CLOSE; LB_AboveCap = FULL＿OPEN;
                break;

            case 7://西(左)
                LF_AboveCap = FULL＿OPEN; RF_AboveCap = CLOSE; RB_AboveCap = CLOSE; LB_AboveCap = FULL＿OPEN;
                break;

            case 8://北西(左前)
                LF_AboveCap = FULL＿OPEN; RF_AboveCap = CLOSE; RB_AboveCap = CLOSE; LB_AboveCap = CLOSE;
                break;
        }
    }
}*/
}