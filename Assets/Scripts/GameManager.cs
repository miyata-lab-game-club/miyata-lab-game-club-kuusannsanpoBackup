using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //[SerializeField] private EnvironmentController environmentController;
    public static GameManager instance;

    [SerializeField] private WindManager windManager;
    public float gameTimer;
    public int gameEndTime = 300;

    // ゲームがプレイ中かどうか
    public bool isPlaying = false;

    // ハードの準備ができたか
    public bool isAblePlayingHard = false;

    // 夜に行くまでの1フレームごとに進む時間
    private float DaydeltatTime = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        gameTimer = 0;
        isPlaying = true;
    }

    // Update is called once per frame
    private void Update()
    {
        gameTimer += Time.deltaTime;
        // 3秒たったら
        if (gameTimer > 3)
        {
            isAblePlayingHard = true;
        }
        if (gameTimer > gameEndTime)
        {
            windManager.SetActiveGameEndPanel();
            isPlaying = false;
        }
        Debug.Log(gameTimer);
    }
}