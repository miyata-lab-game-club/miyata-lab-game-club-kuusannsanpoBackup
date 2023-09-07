using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

/* ああ
 * 風の中を散歩するプレイヤーのコントローラー
* 風のベクトルに傘を傾けると風の方向にプレイヤーが進む
* 傾けないと静かに少しずつ落ちていく
* 現在の高度を表示する
*/

public class PlayerController : MonoBehaviour
{
    // 右のコントローラーのTransform
    [SerializeField] private Transform rightControllerTransform;

    // 落ちていく速度
    [SerializeField] private Vector3 gravityDirection;

    // 今いるチェックポイント
    private int currentCheckPointIndex = 0;

    [SerializeField] private WindManager windController;

    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        /*
        // コントローラーの角度を取得
        Quaternion rightControllerRotation = rightControllerTransform.rotation;

        Vector3 rightControllerTilt = (rightControllerRotation * Vector3.forward).normalized;
        //Debug.Log("回転" + rightControllerRotation.eulerAngles);
        Debug.Log("傾き" + rightControllerTilt);
        Debug.DrawLine(new Vector3(0, 15, 2), new Vector3(0, 15, 2) + rightControllerTilt * 3, Color.red);
        bool existNextCheckPoint = windController.currentWindDirection(currentCheckPointIndex, this.transform);
        float similarity;
        if (existNextCheckPoint)
        {
            similarity = Vector3.Dot(rightControllerTilt, windController.windDirection);
        }
        else
        {
            similarity = -1;
        }
        // 類似度が0.7よりおおきいとき
        Debug.Log(similarity);
        if (similarity >= 0.7)
        {
            //チェックポイントに向かって風が吹く
            Vector3 currentWind = windController.windDirection;
            playerRigidbody.velocity = currentWind;
        }
        // 類似していなければ
        else
        {
            // おちていく
            playerRigidbody.velocity = gravityDirection;
        }
        //Debug.Log(rightControllerTransform.position);
        */
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "CheckPoint")
        {
            currentCheckPointIndex = int.Parse(other.gameObject.name.Substring(10));
            Debug.Log(currentCheckPointIndex);
        }
    }
}