using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoveModeState : MonoBehaviour
{
   
    public static MoveModeState Instance;
    public bool isActive = false;
    private EachTile hoveredTile = null;
    private EachTile selectedTile = null;
    private Coroutine _selectDestCoroutine;
    public GameObject alim;
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
        if (_selectDestCoroutine == null)
            _selectDestCoroutine = StartCoroutine(SelectDestLoop());
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
            
            StopCoroutine(_selectDestCoroutine);
            _selectDestCoroutine = null;
        }
    }
    void SelectDest()
    {
        //Debug.Log("무브모드돌입");
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hoveredObj = hit.collider.gameObject;
            EachTile currentTile = hoveredObj.GetComponent<EachTile>();

            if (currentTile != null)
            {
                alim.SetActive(true);
                // 이전 hover 색 원복 (선택된 타일은 유지)
                if (hoveredTile != null && hoveredTile != selectedTile)
                    hoveredTile.GetComponent<SpriteRenderer>().color = Color.white;

                // 현재 hover 타일 색 노란색으로 변경
                if (currentTile != selectedTile)
                    hoveredObj.GetComponent<SpriteRenderer>().color = Color.yellow;

                hoveredTile = currentTile;
                int destinationIndex = currentTile.tileIndex;
                int distance = BFS(LocalState.Instance.localPlayers[PhotonNetwork.LocalPlayer.ActorNumber].curpos, destinationIndex);

                alim.GetComponent<TextMeshProUGUI>().text = $"이동까지 {distance} 행동 소모";

                // 클릭 처리
                if (Input.GetMouseButtonDown(0))
                {
                    if (distance != -1)
                    {
                        //Debug.Log($"플레이어가 {distance}칸 이동을선택했다");
                        selectedTile = currentTile;
                        selectedTile.GetComponent<SpriteRenderer>().color = Color.red;
                        
                    }
                    else
                    {
                      //  Debug.Log("이동 불가한 타일입니다.");
                    }
                    StopSelectDestLoop();
              
                    ActionData action = new ActionData();
                    action.actionId = 0;
                    action.destindex = destinationIndex; //렌더링할 때는 이값
                    CardEffect moveEffect = new CardEffect(HookType.Activate, EffectType.Move, destinationIndex, 0); //이동 계산은 이펙트로
                    action.effects.Add(moveEffect);


                    Overmind.Instance?.SubmitSelection(action, distance);
                    selectedTile.GetComponent<SpriteRenderer>().color = Color.white;

                    isActive = false;
                    hoveredTile = null;
                    selectedTile = null;
                    alim.SetActive(false);
                    // 모드 종료 및 색 초기화
                    ////StartCoroutine(ExitMoveModeAfterFrame());
                }
            }
        }
        else
        {
            // 아무 것도 hover 안 했을 때 이전 hover 색 원복
            if (hoveredTile != null && hoveredTile != selectedTile)
            {
                hoveredTile.GetComponent<SpriteRenderer>().color = Color.white;
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
    int BFS(int startIndex, int goalIndex)
    {

        Vector3Int start = GridManagement.Instance.GetCoordFromIndex(startIndex);
        Vector3Int goal = GridManagement.Instance.GetCoordFromIndex(goalIndex);

        Queue<(Vector3Int coord, int dist)> queue = new Queue<(Vector3Int, int)>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, dist) = queue.Dequeue();
            if (current == goal)
                return dist;

            foreach (var dir in directions)
            {
                Vector3Int next = current + dir;
                if (GridManagement.Instance.coordToIndex.ContainsKey(next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue((next, dist + 1));
                }
            }
        }

        return -1;
    }
}