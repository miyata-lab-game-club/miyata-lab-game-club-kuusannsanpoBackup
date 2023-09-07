using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

class Define{
    public static Vector3[] windDirection = new Vector3[]
    {new Vector3(0,1,0), new Vector3(0, 1, 1),new Vector3(1, 1, 1),new Vector3(1, 1, 0),
     new Vector3(1,1,-1), new Vector3(0, 1, -1),new Vector3(-1, 1, -1),new Vector3(-1, 1, 0),
     new Vector3(-1, 1, 1)};
}
// 風の移動アニメーションの関数
public class WindMovement : MonoBehaviour
{
    // 範囲
    [SerializeField] float offset = 2;
    // 時間
    [SerializeField] float duration = 5;
    Tween tween;
    public void WindMove(int direction){
        // アニメーションを再生していたら停止
        if(tween != null){
            tween.Kill();
        }
        // 風が吹き始める位置
        Vector3 startPos = new Vector3(0,0,0) - Define.windDirection[direction] * offset;
        // Debug.Log("direction"+direction+"startPos:"+startPos);
        // 風が吹き終わる位置
        Vector3 endPos = new Vector3(0,0,0)  + Define.windDirection[direction] * offset;
        // Debug.Log("direction"+direction+"endPos:"+endPos);
        transform.localPosition = startPos;
        // 風の方向にアニメーション
        tween = this.transform.DOLocalMove(endPos,duration).SetLoops(-1,LoopType.Restart);
    }
}
