using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class BoardManager : MonoBehaviour
{
    [Header("블록 프리팹들")]
    public GameObject[] blockPrefabs; // 블록 색상별 프리팹들

    [Header("보드 설정")]
    public int width = 8; // 보드 가로 크기
    public int height = 8; // 보드 세로 크기
    public float spacing = 1.0f; // 블록 간격

    [Header("UI 및 타이머 설정")]
    public float gameTime = 60f; // 제한 시간 (초)
    public TextMeshProUGUI timerText; // UI 텍스트 (남은 시간 표시)

    private float currentTime;
    private bool gameOver = false;

    private GameObject[,] board; // 보드 블록 배열
    private Block selectedBlock = null; // 선택된 블록 저장
    private Vector2 offset; // 보드 중앙 정렬을 위한 위치 오프셋

    private bool isResolving = false;

    [SerializeField] AudioClip swapSound; // 블록 스왑 사운드
    [SerializeField] AudioClip selectSound; // 블록 선택 사운드
    [SerializeField] AudioClip notMatchSound; // 블록 매칭 실패 사운드 
    [SerializeField] AudioClip matchSound; // 블록 매칭 사운드
    [SerializeField] AudioClip fillSound; // 블록 채우기 사운드
    [SerializeField] AudioClip dropSound; // 블록 드롭 사운드
    [SerializeField] AudioClip gameOverSound; // 게임 오버 사운드
    [SerializeField] AudioSource audioSource; // 오디오 소스
    void Start()
    {
        audioSource = GetComponent<AudioSource>(); // 오디오 소스 초기화
        offset = new Vector2(-width / 2f + 0.5f, -height / 2f + 0.5f); // 중심 정렬용 오프셋 계산
        board = new GameObject[width, height]; // 보드 배열 초기화
        GenerateBoard(); // 보드 생성
        StartCoroutine(InitialCheckMatches()); // 초기 매칭 처리 시작
        currentTime = gameTime; // 제한 시간 초기화
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
                Debug.Log("게임 종료: 시간 초과");
                timerText.text = "Time:0";
                // 게임 오버 처리 추가 가능
                return;
            }

            if (!isResolving && !HasPossibleMatches())
            {
                Debug.Log("자동 리셋: 이동 가능한 조합 없음");
                RegenerateBoard();
            }
        }
    }

    void GenerateBoard()
    {
        Debug.Log("보드 생성 시작");
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 spawnPos = new Vector2(x * spacing, y * spacing) + offset; // 블록 위치 계산
                int randomIndex = Random.Range(0, blockPrefabs.Length); // 랜덤 블록 선택
                GameObject newBlock = Instantiate(blockPrefabs[randomIndex], spawnPos, Quaternion.identity);
                newBlock.transform.parent = transform;
                board[x, y] = newBlock;
            }
        }
        Debug.Log("보드 생성 완료");
    }

    // 게임 시작 시 초기 매칭 제거 및 불가능한 조합 처리
    IEnumerator InitialCheckMatches()
    {
        isResolving = true;
        yield return new WaitForSeconds(0.1f);

        List<Vector2Int> matched = GetMatchedPositions();
        while (matched.Count > 0 || !HasPossibleMatches())
        {
            if (matched.Count > 0)
            {
                Debug.Log("초기 매칭 제거");
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
                Debug.Log("초기 조합 없음, 보드 재생성");
                RegenerateBoard();
                yield break;
            }
        }
        isResolving = false;
    }
    // 이동 가능한 조합이 없을 경우 보드를 재생성
    void RegenerateBoard()
    {
        Debug.Log("재배치 시작: 이동 가능한 조합 없음");
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
    // 블록이 인접했는지 확인
    bool AreAdjacent(Block a, Block b)
    {
        Vector2Int posA = GetBlockPosition(a);
        Vector2Int posB = GetBlockPosition(b);

        int dx = Mathf.Abs(posA.x - posB.x);
        int dy = Mathf.Abs(posA.y - posB.y);
        return (dx + dy == 1); // 상하좌우 1칸 차이면 인접
    }
    // 블록의 현재 위치를 반환
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
    // 위치가 보드 내에 있는지 확인
    bool IsValidPosition(Vector2Int pos)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.x < width && pos.y < height;
    }
    // 블록 스왑 처리 (배열과 실제 위치 모두 교환)
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
    // 가능한 스왑이 있는지 확인
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
    // 두 블록을 가상으로 스왑하고 매칭이 발생하는지 확인
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
            Debug.Log($"{matchedPositions.Count}개 매칭됨");
            GameManager.Instance.AddScore(matchedPositions.Count);
            foreach (Vector2Int pos in matchedPositions)
            {
                Destroy(board[pos.x, pos.y]);
                board[pos.x, pos.y] = null;
            }

            yield return new WaitForSeconds(0.2f);
            audioSource.PlayOneShot(matchSound); // 매칭 사운드 재생
            yield return StartCoroutine(DropBlocks()); // 낙하 처리
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(FillEmptySpaces()); // 새 블록 채움

            yield return new WaitForSeconds(0.2f);

            matchedPositions = GetMatchedPositions(); // 새롭게 매칭된 블록 재검사
        }
    }

    List<Vector2Int> GetMatchedPositions()
    {
        List<Vector2Int> matched = new List<Vector2Int>();

        // 가로 방향 매칭 확인
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

        // 세로 방향 매칭 확인
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
        Debug.Log("블록 드롭 시작");
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
                    StartCoroutine(SmoothMove(movingBlock, targetPos)); // 부드럽게 이동
                }
            }
        }
        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator SmoothMove(GameObject block, Vector2 targetPos)
    {
        Vector2 startPos = block.transform.position;
        float elapsed = 0f;
        float duration = 0.25f; // 낙하 시간

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
        Debug.Log("빈 칸 채우기 시작");
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (board[x, y] == null)
                {
                    Vector2 spawnPos = new Vector2(x * spacing, height * spacing + 1f) + offset; // 보드 위쪽에서 생성
                    Vector2 targetPos = new Vector2(x * spacing, y * spacing) + offset;
                    int randomIndex = Random.Range(0, blockPrefabs.Length);
                    GameObject newBlock = Instantiate(blockPrefabs[randomIndex], spawnPos, Quaternion.identity);
                    newBlock.transform.parent = transform;
                    board[x, y] = newBlock;
                    StartCoroutine(SmoothMove(newBlock, targetPos)); // 부드럽게 이동
                }
            }
        }
        yield return new WaitForSeconds(0.2f);
    }
}
