using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class MoveModeState : MonoBehaviour
{
   
    public static MoveModeState Instance;
    public bool isActive = false;
    private EachTile hoveredTile = null;
    private EachTile selectedTile = null;
    private Coroutine _selectDestCoroutine;
    public GameObject alim;
    private int actorNum;
    private static readonly Vector3Int[] directions = new Vector3Int[]
    {
        new Vector3Int(-1, 0, 1),  // 좌상
        new Vector3Int(0, -1, 1),  // 우상
        new Vector3Int(-1, 1, 0),  // 좌
        new Vector3Int(1, -1, 0),  // 우
        new Vector3Int(0, 1, -1),  // 좌하
        new Vector3Int(1, 0, -1)   // 우하
    };
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
      
    }


    public void SetActive(bool active)
    {
        if (active == isActive) return; //이미중복 코루틴 시작 방지
        isActive = active;
        if (isActive) StartSelectDestLoop();
        else StopSelectDestLoop();
    }
    private void StartSelectDestLoop()
    {
        TilePreprocessing();
        if (_selectDestCoroutine == null)
            _selectDestCoroutine = StartCoroutine(SelectDestLoop());
    }
    private void TilePreprocessing()
    {
        actorNum = Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;
        int startIndex = LocalState.Instance.localPlayers[actorNum].curpos;
        int energy = LocalState.Instance.localPlayers[actorNum].energy;

        GridManagement.Instance.HighlightReachableTilesFrom(
            startIndex,
            LocalState.Instance.localPlayers[actorNum].defaultMove,
            Color.cyan,
            "Move"// 이동 가능 타일
        );
        // 모든 타일 초기화

        /*  foreach (var tileObj in GridManagement.Instance.tileObjects.Values)
          {
              EachTile tile = tileObj.GetComponent<EachTile>();
              tile.defaultColor = Color.white;
              tile.cost = 0;
              tile.canMove = false;
              tileObj.GetComponent<SpriteRenderer>().color = tile.defaultColor;
          }

          Queue<(int index, int dist)> queue = new Queue<(int, int)>();
          HashSet<int> visited = new HashSet<int>();

          queue.Enqueue((startIndex, 0));
          visited.Add(startIndex);

          while (queue.Count > 0)
          {
              var (currentIndex, dist) = queue.Dequeue();
              if (dist > energy)
                  continue;

              if (currentIndex == startIndex)
              {
                  // 시작 타일은 색을 다르게 표시
                  var startTileObj = GridManagement.Instance.tileObjects[currentIndex];
                  EachTile startTile = startTileObj.GetComponent<EachTile>();
                  startTile.defaultColor = Color.green;
                  startTile.canMove = false;
                  startTileObj.GetComponent<SpriteRenderer>().color = startTile.defaultColor;
              }
              else
              {
                  var tileObj = GridManagement.Instance.tileObjects[currentIndex];
                  EachTile tile = tileObj.GetComponent<EachTile>();
                  tile.defaultColor = Color.cyan;
                  tile.cost = dist;
                  tile.canMove = true;
                  tileObj.GetComponent<SpriteRenderer>().color = tile.defaultColor;
              }


              Vector3Int currentCoord = GridManagement.Instance.GetCoordFromIndex(currentIndex);

              foreach (var dir in directions)
              {
                  Vector3Int nextCoord = currentCoord + dir;
                  if (GridManagement.Instance.coordToIndex.TryGetValue(nextCoord, out int nextIndex) && !visited.Contains(nextIndex))
                  {
                      visited.Add(nextIndex);
                      queue.Enqueue((nextIndex, dist + 1));
                  }
              }
          }*/

    }
    private IEnumerator SelectDestLoop()
    {
        while (isActive)
        {
            SelectDest();     // 매 프레임 목적지 선택 로직
            yield return null;
        }
    }
    private void StopSelectDestLoop()
    {

        if (_selectDestCoroutine != null)
        {
            GridManagement.Instance.ResetAllTiles();
            StopCoroutine(_selectDestCoroutine);
            _selectDestCoroutine = null;
        }
    }
    void SelectDest()
    {
        int destinationIndex;
        int distance;

        GameObject hoveredObj = GridManagement.Instance.GetTileUnderMouse();

        if (hoveredObj != null)
        {
            EachTile currentTile = hoveredObj.GetComponent<EachTile>();

            if (currentTile != null && currentTile.canMove)
            {
                alim.SetActive(true);

                // 이전 hover 색 원복 (선택된 타일은 유지)
                if (hoveredTile != null && hoveredTile != selectedTile)
                    hoveredTile.GetComponent<SpriteRenderer>().color = hoveredTile.defaultColor;

                // 현재 hover 타일 색 노란색으로 변경
                if (currentTile != selectedTile)
                    hoveredObj.GetComponent<SpriteRenderer>().color = Color.yellow;

                distance = currentTile.cost;
                alim.GetComponent<TextMeshProUGUI>().text = $"이동까지 {distance} 행동 소모";
                hoveredTile = currentTile;

                // 클릭 처리
                if (Input.GetMouseButtonDown(0))
                {
                    if (currentTile.canMove == false)
                    {
                        alim.GetComponent<TextMeshProUGUI>().text = $"해당 위치로는 이동할 수 없다";

                    }
                    destinationIndex = currentTile.tileIndex;


                  

                    StopSelectDestLoop();

                    ActionData action = new ActionData
                    {
                        actionId = 0,
                        destindex = destinationIndex
                    };
                    CardEffect moveEffect = new CardEffect(HookType.Activate, EffectType.Move, destinationIndex, 0);
                    action.effects.Add(moveEffect);
                    int actualCost = Mathf.Min(distance, LocalState.Instance.localPlayers[actorNum].defaultMoveCast);

                    
                    Overmind.Instance?.SubmitSelection(action, actualCost);

                    isActive = false;
                    hoveredTile = null;
                    selectedTile = null;
                    alim.SetActive(false);
                }
            }
        }
        else
        {
            // 아무 것도 hover 안 했을 때 이전 hover 색 원복
            if (hoveredTile != null && hoveredTile != selectedTile)
            {
                hoveredTile.GetComponent<SpriteRenderer>().color = hoveredTile.defaultColor;
                hoveredTile = null;
            }
        }
    }

    /* IEnumerator ExitMoveModeAfterFrame()
     {
         yield return new WaitForSecondsRealtime(0.05f); ; // 1 프레임 기다린 후 종료 (클릭과 색 갱신 충돌 방지)

         foreach (var obj in grid.tileObjects.Values)
         {
             var sr = obj.GetComponent<SpriteRenderer>();
             sr.color = Color.white;
         }


         //캐릭터 위치 이동
         if (selectedTile != null)
         {
             Transform charPoint = selectedTile.transform.Find("charpoint");
             if (charPoint != null)
             {
                 chara.transform.position = charPoint.position;
             }
             else
             {
                 Debug.LogWarning("선택된 타일에 'charpoint' 오브젝트가 없습니다.");
             }
         }

         isActive = false;
         hoveredTile = null;
         selectedTile = null;
     }
 */
}