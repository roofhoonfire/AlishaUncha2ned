using System.Collections.Generic;
using UnityEngine;

public class GridManagement : MonoBehaviour
{
    public static GridManagement Instance;

    public List<Vector3Int> hexCoordinates;
    public Dictionary<Vector3Int, int> coordToIndex;
    public Dictionary<int, GameObject> tileObjects;//시발 태그로 찾을 필요가 전혀 없다
    public int playerCur;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        InitGrid();
    }
    public void InitGrid()
    {
        //타일 정보
        tileObjects = new Dictionary<int, GameObject>();
        GameObject[] tiles = GameObject.FindGameObjectsWithTag("Tile");
        foreach (var tile in tiles)
        {
            EachTile tileComp = tile.GetComponent<EachTile>();
            if (tileComp != null)
            {
                tileObjects[tileComp.tileIndex] = tile;
            }
        }


        hexCoordinates = new List<Vector3Int>
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, -1, 0),
            new Vector3Int(2, -2, 0),
            new Vector3Int(3, -3, 0),

            new Vector3Int(0, 1, -1),
            new Vector3Int(1, 0, -1),
            new Vector3Int(2, -1, -1),
            new Vector3Int(3, -2, -1),
            new Vector3Int(4, -3, -1),

            new Vector3Int(0, 2, -2),
            new Vector3Int(1, 1, -2),
            new Vector3Int(2, 0, -2),
            new Vector3Int(3, -1, -2),
            new Vector3Int(4, -2, -2),
            new Vector3Int(5, -3, -2),

            new Vector3Int(0, 3, -3),
            new Vector3Int(1, 2, -3),
            new Vector3Int(2, 1, -3),
            new Vector3Int(3, 0, -3),
            new Vector3Int(4, -1, -3),
            new Vector3Int(5, -2, -3),
            new Vector3Int(6, -3, -3),

            new Vector3Int(1, 3, -4),
            new Vector3Int(2, 2, -4),
            new Vector3Int(3, 1, -4),
            new Vector3Int(4, 0, -4),
            new Vector3Int(5, -1, -4),
            new Vector3Int(6, -2, -4),

            new Vector3Int(2, 3, -5),
            new Vector3Int(3, 2, -5),
            new Vector3Int(4, 1, -5),
            new Vector3Int(5, 0, -5),
            new Vector3Int(6, -1, -5),

            new Vector3Int(3, 3, -6),
            new Vector3Int(4, 2, -6),
            new Vector3Int(5, 1, -6),
            new Vector3Int(6, 0, -6),
        };

        coordToIndex = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < hexCoordinates.Count; i++)
        {
            coordToIndex[hexCoordinates[i]] = i;
        }
    }

    public int GetIndexFromCoord(Vector3Int coord)
    {
        if (coordToIndex.TryGetValue(coord, out int index))
        {
            return index;
        }
        else
        {
            //Debug.LogWarning($"Coord {coord} is not in the grid.");
            return -1;
        }
    }

    public Vector3Int GetCoordFromIndex(int index)
    {
        if (index >= 0 && index < hexCoordinates.Count)
        {
            return hexCoordinates[index];
        }
        else
        {
            Debug.LogWarning($"Index {index} is out of range.");
            return default;
        }
    }
    public void RageOn()
    {
        if (tileObjects == null || tileObjects.Count == 0) return;

        foreach (var go in tileObjects.Values)
        {
            if (go == null) continue;
            var anim = go.GetComponentInChildren<Animator>(true); // 자식에 붙어있어도 대응
            if (anim != null) anim.SetTrigger("Trig_Raging");
        }
    }

    public void RageDone()
    {
        if (tileObjects == null || tileObjects.Count == 0) return;

        foreach (var go in tileObjects.Values)
        {
            if (go == null) continue;
            var anim = go.GetComponentInChildren<Animator>(true);
            if (anim != null) anim.SetTrigger("Trig_Raging_Done");
        }
    }

    public List<int> ReturnEveryTile()
    {
        List<int> everytile = new List<int>();

        for (int i = 0; i <= 36; i++)
        {
            everytile.Add(i);
        }
        return everytile;
    }
    public GameObject GetTileUnderMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Tile"))
            {
                return hit.collider.gameObject;
            }
        }
        return null;
    }

    public void HighlightReachableTilesFrom(int startIndex, int maxCost, Color tileColor, string MoveOrSkill)
    {
        // 모든 타일 초기화
        foreach (var tileObj in tileObjects.Values)
        {
            EachTile tile = tileObj.GetComponent<EachTile>();
            tile.defaultColor = Color.white;
            tile.cost = 0;
            tile.canMove = false;
            tileObj.GetComponent<SpriteRenderer>().color = tile.defaultColor;
        }

        Queue<(int index, int dist)> queue = new();
        HashSet<int> visited = new();

        queue.Enqueue((startIndex, 0));
        visited.Add(startIndex);

        if (MoveOrSkill == "LinearSkill")
        {
            int linearLength = maxCost - 10;
            Vector3Int playerCoord = GetCoordFromIndex(startIndex);

            foreach (var dir in HexSkill.Directions)
            {
                Vector3Int curCoord = playerCoord;

                for (int i = 1; i <= linearLength; i++)
                {
                    curCoord += dir;

                    if (coordToIndex.TryGetValue(curCoord, out int nextIndex))
                    {
                        GameObject tileObj = tileObjects[nextIndex];
                        EachTile tile = tileObj.GetComponent<EachTile>();

                        tile.cost = i;
                        tile.canMove = true;
                        tile.defaultColor = tileColor;
                        tileObj.GetComponent<SpriteRenderer>().color = tileColor;
                    }
                    else
                    {
                        break; // 맵 바깥
                    }
                }
            }
        }
        else if (MoveOrSkill == "Kawari")
        {
            // "좌상(4)", "좌하(2)", "우상(5)", "우하(1)" 방향만 허용
            int[] allowedDirs = { 1, 2, 4, 5 };

            while (queue.Count > 0)
            {
                var (currentIndex, dist) = queue.Dequeue();
                if (dist > maxCost) continue;

                GameObject tileObj = tileObjects[currentIndex];
                EachTile tile = tileObj.GetComponent<EachTile>();

                bool isStartTile = currentIndex == startIndex;
                bool isPlayerTile = false;

                foreach (var pl in LocalRenderingStatic.localRenderingDatas.Values)
                {
                    if (currentIndex == pl.curpos)
                    {
                        isPlayerTile = true;
                        break;
                    }
                }

                bool shouldHighlight = !isStartTile && !isPlayerTile;

                if (shouldHighlight)
                {
                    tile.cost = dist;
                    tile.canMove = true;
                    tile.defaultColor = tileColor;
                    tileObj.GetComponent<SpriteRenderer>().color = tileColor;
                }

                Vector3Int currentCoord = GetCoordFromIndex(currentIndex);
                foreach (int dirIdx in allowedDirs)
                {
                    Vector3Int nextCoord = currentCoord + HexSkill.Directions[dirIdx];
                    if (coordToIndex.TryGetValue(nextCoord, out int nextIndex) && !visited.Contains(nextIndex))
                    {
                        visited.Add(nextIndex);
                        queue.Enqueue((nextIndex, dist + 1));
                    }
                }
            }
        }
        else
        {
            while (queue.Count > 0)
            {
                var (currentIndex, dist) = queue.Dequeue();
                if (dist > maxCost) continue;

                GameObject tileObj = tileObjects[currentIndex];
                EachTile tile = tileObj.GetComponent<EachTile>();

                bool isStartTile = currentIndex == startIndex;
                bool isPlayerTile = false;

                if (MoveOrSkill == "Move")
                {
                    foreach (var pl in LocalRenderingStatic.localRenderingDatas.Values)
                    {
                        if (currentIndex == pl.curpos)
                        {
                            isPlayerTile = true;
                            break;
                        }
                    }
                }

                bool shouldHighlight = false;

                if (MoveOrSkill == "Skill")
                {
                    shouldHighlight = !isStartTile;
                }
                else if (MoveOrSkill == "Move")
                {
                    shouldHighlight = !isStartTile && !isPlayerTile;
                }

                if (shouldHighlight)
                {
                    tile.cost = dist;
                    tile.canMove = true;
                    tile.defaultColor = tileColor;
                    tileObj.GetComponent<SpriteRenderer>().color = tileColor;
                }

                Vector3Int currentCoord = GetCoordFromIndex(currentIndex);
                foreach (var dir in HexSkill.Directions)
                {
                    Vector3Int nextCoord = currentCoord + dir;
                    if (coordToIndex.TryGetValue(nextCoord, out int nextIndex) && !visited.Contains(nextIndex))
                    {
                        visited.Add(nextIndex);
                        queue.Enqueue((nextIndex, dist + 1));
                    }
                }
            }
        }
    }


    //위에꺼 안되면 아래의 봉인을 푼다 
    /*public void HighlightReachableTilesFrom(int startIndex, int maxCost, Color tileColor, string MoveOrSkill)
    {
        // 모든 타일 초기화
        foreach (var tileObj in tileObjects.Values)
        {
            EachTile tile = tileObj.GetComponent<EachTile>();
            tile.defaultColor = Color.white;
            tile.cost = 0;
            tile.canMove = false;
            tileObj.GetComponent<SpriteRenderer>().color = tile.defaultColor;
        }

        Queue<(int index, int dist)> queue = new();
        HashSet<int> visited = new();

        queue.Enqueue((startIndex, 0));
        visited.Add(startIndex);
        if (MoveOrSkill == "LinearSkill") // 새로운 case
        {
            // 타일타입에서 길이 계산
            int linearLength = maxCost - 10; // tileType - 10

            Vector3Int playerCoord = GetCoordFromIndex(startIndex);

            foreach (var dir in HexSkill.Directions)
            {
                Vector3Int curCoord = playerCoord;

                for (int i = 1; i <= linearLength; i++)
                {
                    curCoord += dir;

                    if (coordToIndex.TryGetValue(curCoord, out int nextIndex))
                    {
                        GameObject tileObj = tileObjects[nextIndex];
                        EachTile tile = tileObj.GetComponent<EachTile>();

                        tile.cost = i;
                        tile.canMove = true;
                        tile.defaultColor = tileColor;
                        tileObj.GetComponent<SpriteRenderer>().color = tileColor;
                    }
                    else
                    {
                        // 맵 바깥 나감 → 이 방향 중단
                        break;
                    }
                }
            }
        }
        else
        {
            while (queue.Count > 0)
            {
                var (currentIndex, dist) = queue.Dequeue();
                if (dist > maxCost) continue;

                GameObject tileObj = tileObjects[currentIndex];
                EachTile tile = tileObj.GetComponent<EachTile>();

                bool isStartTile = currentIndex == startIndex;
                bool isPlayerTile = false;

                // Move일 경우 현재 플레이어들이 위치한 타일인지 체크
                if (MoveOrSkill == "Move")
                {
                    foreach (var pl in LocalRenderingStatic.localRenderingDatas.Values)
                    {
                        if (currentIndex == pl.curpos)
                        {
                            isPlayerTile = true;
                            break;
                        }
                    }
                }

                // 타일 초기화 조건:
                // - Skill일 때 → currentIndex != startIndex면 칠함
                // - Move일 때 → currentIndex != startIndex && 해당 타일이 플레이어 위치가 아니면 칠함
                bool shouldHighlight = false;

                if (MoveOrSkill == "Skill")
                {
                    shouldHighlight = !isStartTile;
                }
                else if (MoveOrSkill == "Move")
                {
                    shouldHighlight = !isStartTile && !isPlayerTile;
                }

                if (shouldHighlight)
                {
                    tile.cost = dist;
                    tile.canMove = true;
                    tile.defaultColor = tileColor;
                    tileObj.GetComponent<SpriteRenderer>().color = tileColor;
                }

                // BFS 탐색 계속 진행
                Vector3Int currentCoord = GetCoordFromIndex(currentIndex);
                foreach (var dir in HexSkill.Directions)
                {
                    Vector3Int nextCoord = currentCoord + dir;
                    if (coordToIndex.TryGetValue(nextCoord, out int nextIndex) && !visited.Contains(nextIndex))
                    {
                        visited.Add(nextIndex);
                        queue.Enqueue((nextIndex, dist + 1));
                    }
                }
            }
        }
    }*/



    public void ResetAllTiles()
    {
        foreach (var tileObj in tileObjects.Values)
        {
            EachTile tile = tileObj.GetComponent<EachTile>();
            tile.defaultColor = Color.white;
            tile.cost = 0;
            tile.canMove = false;
            tileObj.GetComponent<SpriteRenderer>().color = tile.defaultColor;
        }
    }
}
