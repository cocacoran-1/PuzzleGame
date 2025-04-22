using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class BoardManager : MonoBehaviour
{
    [Header("��� �����յ�")]
    public GameObject[] blockPrefabs; // ��� ���� �����յ�

    [Header("���� ����")]
    public int width = 8; // ���� ���� ũ��
    public int height = 8; // ���� ���� ũ��
    public float spacing = 1.0f; // ��� ����

    [Header("UI �� Ÿ�̸� ����")]
    public float gameTime = 60f; // ���� �ð� (��)
    public TextMeshProUGUI timerText; // UI �ؽ�Ʈ (���� �ð� ǥ��)

    private float currentTime;
    private bool gameOver = false;

    private GameObject[,] board; // ���� ��� �迭
    private Block selectedBlock = null; // ���õ� ��� ����
    private Vector2 offset; // ���� �߾� ������ ���� ��ġ ������

    private bool isResolving = false;

    [SerializeField] AudioClip swapSound; // ��� ���� ����
    [SerializeField] AudioClip selectSound; // ��� ���� ����
    [SerializeField] AudioClip notMatchSound; // ��� ��Ī ���� ���� 
    [SerializeField] AudioClip matchSound; // ��� ��Ī ����
    [SerializeField] AudioClip fillSound; // ��� ä��� ����
    [SerializeField] AudioClip dropSound; // ��� ��� ����
    [SerializeField] AudioClip gameOverSound; // ���� ���� ����
    [SerializeField] AudioSource audioSource; // ����� �ҽ�
    void Start()
    {
        audioSource = GetComponent<AudioSource>(); // ����� �ҽ� �ʱ�ȭ
        offset = new Vector2(-width / 2f + 0.5f, -height / 2f + 0.5f); // �߽� ���Ŀ� ������ ���
        board = new GameObject[width, height]; // ���� �迭 �ʱ�ȭ
        GenerateBoard(); // ���� ����
        StartCoroutine(InitialCheckMatches()); // �ʱ� ��Ī ó�� ����
        currentTime = gameTime; // ���� �ð� �ʱ�ȭ
    }
    void Update()
    {
        if (!gameOver)
        {
            currentTime -= Time.deltaTime;
            timerText.text = "Time:" + Mathf.CeilToInt(currentTime).ToString();

            if (currentTime <= 0f)
            {
                gameOver = true;
                Debug.Log("���� ����: �ð� �ʰ�");
                timerText.text = "Time:0";
                // ���� ���� ó�� �߰� ����
                return;
            }

            if (!isResolving && !HasPossibleMatches())
            {
                Debug.Log("�ڵ� ����: �̵� ������ ���� ����");
                RegenerateBoard();
            }
        }
    }

    void GenerateBoard()
    {
        Debug.Log("���� ���� ����");
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 spawnPos = new Vector2(x * spacing, y * spacing) + offset; // ��� ��ġ ���
                int randomIndex = Random.Range(0, blockPrefabs.Length); // ���� ��� ����
                GameObject newBlock = Instantiate(blockPrefabs[randomIndex], spawnPos, Quaternion.identity);
                newBlock.transform.parent = transform;
                board[x, y] = newBlock;
            }
        }
        Debug.Log("���� ���� �Ϸ�");
    }

    // ���� ���� �� �ʱ� ��Ī ���� �� �Ұ����� ���� ó��
    IEnumerator InitialCheckMatches()
    {
        isResolving = true;
        yield return new WaitForSeconds(0.1f);

        List<Vector2Int> matched = GetMatchedPositions();
        while (matched.Count > 0 || !HasPossibleMatches())
        {
            if (matched.Count > 0)
            {
                Debug.Log("�ʱ� ��Ī ����");
                foreach (var pos in matched)
                {
                    Destroy(board[pos.x, pos.y]);
                    board[pos.x, pos.y] = null;
                }
                yield return new WaitForSeconds(0.1f);
                yield return StartCoroutine(DropBlocks());
                yield return StartCoroutine(FillEmptySpaces());
                yield return new WaitForSeconds(0.1f);
                matched = GetMatchedPositions();
            }
            else if (!HasPossibleMatches())
            {
                Debug.Log("�ʱ� ���� ����, ���� �����");
                RegenerateBoard();
                yield break;
            }
        }
        isResolving = false;
    }
    // �̵� ������ ������ ���� ��� ���带 �����
    void RegenerateBoard()
    {
        Debug.Log("���ġ ����: �̵� ������ ���� ����");
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] != null)
                {
                    Destroy(board[x, y]);
                    board[x, y] = null;
                }
            }
        }
        GenerateBoard();
        StartCoroutine(InitialCheckMatches());
    }
    public void SelectBlock(Block block)
    {
        if (block == null || block.gameObject == null) return;

        if (selectedBlock == block)
        {
            selectedBlock.SetSelected(false);
            selectedBlock = null;
            return;
        }

        if (selectedBlock == null)
        {
            selectedBlock = block;
            selectedBlock.SetSelected(true);
        }
        else
        {
            if (!AreAdjacent(selectedBlock, block))
            {
                selectedBlock.SetSelected(false);
                selectedBlock = block;
                selectedBlock.SetSelected(true);
                return;
            }

            selectedBlock.SetSelected(false);
            block.SetSelected(false);

            StartCoroutine(TrySwapAndMatch(selectedBlock, block));
            selectedBlock = null;
        }
    }

    IEnumerator TrySwapAndMatch(Block a, Block b)
    {
        if (a == null || b == null) yield break;

        Vector2Int posA = GetBlockPosition(a);
        Vector2Int posB = GetBlockPosition(b);

        if (!IsValidPosition(posA) || !IsValidPosition(posB)) yield break;

        SwapBlocks(a, b);
        yield return new WaitForSeconds(0.25f);

        List<Vector2Int> matched = GetMatchedPositions();
        if (matched.Count > 0)
        {
            yield return StartCoroutine(ResolveMatches());
        }
        else
        {
            SwapBlocks(a, b);
        }
    }
    // ����� �����ߴ��� Ȯ��
    bool AreAdjacent(Block a, Block b)
    {
        Vector2Int posA = GetBlockPosition(a);
        Vector2Int posB = GetBlockPosition(b);

        int dx = Mathf.Abs(posA.x - posB.x);
        int dy = Mathf.Abs(posA.y - posB.y);
        return (dx + dy == 1); // �����¿� 1ĭ ���̸� ����
    }
    // ����� ���� ��ġ�� ��ȯ
    Vector2Int GetBlockPosition(Block block)
    {
        if (block == null)
        {
            return new Vector2Int(-1, -1);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] == block.gameObject)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }
    // ��ġ�� ���� ���� �ִ��� Ȯ��
    bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.x < width && pos.y < height;
    }
    // ��� ���� ó�� (�迭�� ���� ��ġ ��� ��ȯ)
    void SwapBlocks(Block a, Block b)
    {
        Vector2Int posA = GetBlockPosition(a);
        Vector2Int posB = GetBlockPosition(b);

        if (!IsValidPosition(posA) || !IsValidPosition(posB))
        {
            return;
        }

        GameObject temp = board[posA.x, posA.y];
        board[posA.x, posA.y] = board[posB.x, posB.y];
        board[posB.x, posB.y] = temp;

        Vector3 tempPos = a.transform.position;
        a.transform.position = b.transform.position;
        b.transform.position = tempPos;

    }
    // ������ ������ �ִ��� Ȯ��
    bool HasPossibleMatches()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x < width - 1 && TrySwapAndCheck(x, y, x + 1, y)) return true;
                if (y < height - 1 && TrySwapAndCheck(x, y, x, y + 1)) return true;
            }
        }
        return false;
    }
    // �� ����� �������� �����ϰ� ��Ī�� �߻��ϴ��� Ȯ��
    bool TrySwapAndCheck(int x1, int y1, int x2, int y2)
    {
        GameObject temp = board[x1, y1];
        board[x1, y1] = board[x2, y2];
        board[x2, y2] = temp;

        List<Vector2Int> matched = GetMatchedPositions();

        board[x2, y2] = board[x1, y1];
        board[x1, y1] = temp;

        return matched.Count > 0;
    }
    IEnumerator ResolveMatches()
    {

        yield return new WaitForSeconds(0.1f);

        List<Vector2Int> matchedPositions = GetMatchedPositions();
        while (matchedPositions.Count > 0)
        {
            Debug.Log($"{matchedPositions.Count}�� ��Ī��");
            GameManager.Instance.AddScore(matchedPositions.Count);
            foreach (Vector2Int pos in matchedPositions)
            {
                Destroy(board[pos.x, pos.y]);
                board[pos.x, pos.y] = null;
            }

            yield return new WaitForSeconds(0.2f);
            audioSource.PlayOneShot(matchSound); // ��Ī ���� ���
            yield return StartCoroutine(DropBlocks()); // ���� ó��
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(FillEmptySpaces()); // �� ��� ä��

            yield return new WaitForSeconds(0.2f);

            matchedPositions = GetMatchedPositions(); // ���Ӱ� ��Ī�� ��� ��˻�
        }
    }

    List<Vector2Int> GetMatchedPositions()
    {
        List<Vector2Int> matched = new List<Vector2Int>();

        // ���� ���� ��Ī Ȯ��
        for (int y = 0; y < height; y++)
        {
            int matchCount = 1;
            for (int x = 1; x < width; x++)
            {
                Block current = board[x, y]?.GetComponent<Block>();
                Block previous = board[x - 1, y]?.GetComponent<Block>();

                if (current != null && previous != null && current.type == previous.type)
                {
                    matchCount++;
                }
                else
                {
                    if (matchCount >= 3)
                        for (int i = 0; i < matchCount; i++)
                            matched.Add(new Vector2Int(x - 1 - i, y));

                    matchCount = 1;
                }
            }

            if (matchCount >= 3)
                for (int i = 0; i < matchCount; i++)
                    matched.Add(new Vector2Int(width - 1 - i, y));
        }

        // ���� ���� ��Ī Ȯ��
        for (int x = 0; x < width; x++)
        {
            int matchCount = 1;
            for (int y = 1; y < height; y++)
            {
                Block current = board[x, y]?.GetComponent<Block>();
                Block previous = board[x, y - 1]?.GetComponent<Block>();

                if (current != null && previous != null && current.type == previous.type)
                {
                    matchCount++;
                }
                else
                {
                    if (matchCount >= 3)
                        for (int i = 0; i < matchCount; i++)
                            matched.Add(new Vector2Int(x, y - 1 - i));

                    matchCount = 1;
                }
            }

            if (matchCount >= 3)
                for (int i = 0; i < matchCount; i++)
                    matched.Add(new Vector2Int(x, height - 1 - i));
        }

        return matched;
    }

    IEnumerator DropBlocks()
    {
        Debug.Log("��� ��� ����");
        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height; y++)
            {
                if (board[x, y] != null && board[x, y - 1] == null)
                {
                    int dropY = y - 1;
                    while (dropY > 0 && board[x, dropY - 1] == null)
                    {
                        dropY--;
                    }

                    GameObject movingBlock = board[x, y];
                    board[x, dropY] = movingBlock;
                    board[x, y] = null;

                    Vector2 targetPos = new Vector2(x * spacing, dropY * spacing) + offset;
                    StartCoroutine(SmoothMove(movingBlock, targetPos)); // �ε巴�� �̵�
                }
            }
        }
        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator SmoothMove(GameObject block, Vector2 targetPos)
    {
        Vector2 startPos = block.transform.position;
        float elapsed = 0f;
        float duration = 0.25f; // ���� �ð�

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            block.transform.position = Vector2.Lerp(startPos, targetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        block.transform.position = targetPos;
    }

    IEnumerator FillEmptySpaces()
    {
        Debug.Log("�� ĭ ä��� ����");
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] == null)
                {
                    Vector2 spawnPos = new Vector2(x * spacing, height * spacing + 1f) + offset; // ���� ���ʿ��� ����
                    Vector2 targetPos = new Vector2(x * spacing, y * spacing) + offset;
                    int randomIndex = Random.Range(0, blockPrefabs.Length);
                    GameObject newBlock = Instantiate(blockPrefabs[randomIndex], spawnPos, Quaternion.identity);
                    newBlock.transform.parent = transform;
                    board[x, y] = newBlock;
                    StartCoroutine(SmoothMove(newBlock, targetPos)); // �ε巴�� �̵�
                }
            }
        }
        yield return new WaitForSeconds(0.2f);
    }
}
