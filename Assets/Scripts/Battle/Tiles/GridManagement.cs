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
}
