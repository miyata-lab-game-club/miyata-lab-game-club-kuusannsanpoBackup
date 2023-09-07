using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //[SerializeField] private EnvironmentController environmentController;
    public static GameManager instance;

    [SerializeField] private WindManager windManager;
    private float gameTimer;
    public int gameEndTime = 300;

    // ゲームがプレイ中かどうか
    public bool isPlaying = false;

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
        isPlaying = true;
        gameTimer = 0;
    }

    // Update is called once per frame
    private void Update()
    {
        gameTimer += Time.deltaTime;
        if (gameTimer > gameEndTime)
        {
            windManager.SetActiveGameEndPanel();
            isPlaying = false;
        }
    }
}