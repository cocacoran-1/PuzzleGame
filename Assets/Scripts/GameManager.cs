using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 싱글톤 인스턴스
    int score = 0; // 게임 점수
    public TextMeshProUGUI scoreText; // 점수 UI 텍스트

    public BoardManager boardManager; // 보드 매니저 참조
    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
    public void AddScore(int amount)
    {
        score += amount; // 점수 추가
        scoreText.text = $"Score:\n{score:N0}"; // UI 업데이트
    }
}
