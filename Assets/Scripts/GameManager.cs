using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // �̱��� �ν��Ͻ�
    int score = 0; // ���� ����
    public TextMeshProUGUI scoreText; // ���� UI �ؽ�Ʈ

    public BoardManager boardManager; // ���� �Ŵ��� ����
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
        score += amount; // ���� �߰�
        scoreText.text = $"Score:\n{score:N0}"; // UI ������Ʈ
    }
}
