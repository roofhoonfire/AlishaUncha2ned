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

    //추가됨 낄렵
    //
    public void SetActive(bool active, ActionPacketData apData)
    {
        if (active == isActive) return;
        isActive = active;
        if (isActive) StartSelectDestLoop(apData);
        else StopSelectDestLoop();
    }
    /*
    public void SetActive(bool active, ActionPacketData apData)
    {
        if (active == isActive) return; //이미중복 코루틴 시작 방지
        isActive = active;
        if (isActive) StartSelectDestLoop(apData);
        else StopSelectDestLoop();
    }*/

    private void StartSelectDestLoop(ActionPacketData apData)
    {

        LocalState.Instance.PlayerObDic[PhotonNetwork.LocalPlayer.ActorNumber].GetComponentInChildren<Animator>().SetTrigger("Trig_Think");

        TilePreprocessing(apData);
        GridManagement.Instance.ClearAllRageTriggers();

        GridManagement.Instance.RageOnWhereCanMove();

        if (_selectDestCoroutine == null)
            _selectDestCoroutine = StartCoroutine(SelectDestLoop(apData));
    }
    private void TilePreprocessing(ActionPacketData apData)
    {


        actorNum = Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;

        var data = LocalRenderingStatic.localRenderingDatas[actorNum];
        int startIndex = data.curpos;

        GridManagement.Instance.HighlightReachableTilesFrom(
            startIndex,
            apData.defaultMove,
            Color.cyan,
            "Move"// 이동 가능 타일
        );


    }

    //추가됨 낄렵
    private IEnumerator SelectDestLoop(ActionPacketData apData)
    {
        while (isActive)
        {
            // ★ ESC 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelAndReturn();
                yield break;
            }

            SelectDest(apData);
            yield return null;
        }
    }

    private void CancelAndReturn()
    {
        StopSelectDestLoop();                // 하이라이트/코루틴 정리
        isActive = false;
        hoveredTile = null; selectedTile = null;
        if (alim) alim.SetActive(false);
        GridManagement.Instance.RageDone();

        LocalState.Instance.PlayerObDic[PhotonNetwork.LocalPlayer.ActorNumber].GetComponentInChildren<Animator>().SetTrigger("Trig_Think_Done");

        LocalState.Instance.ReturnToChooseLoop();   // ★ 중앙 게이트 호출
    }
    /*
    private IEnumerator SelectDestLoop(ActionPacketData apData)
    {
        while (isActive)
        {
            SelectDest(apData);     // 매 프레임 목적지 선택 로직
            yield return null;
        }
    }*/


    private void StopSelectDestLoop()
    {

        if (_selectDestCoroutine != null)
        {
            GridManagement.Instance.RageDone();

            LocalState.Instance.PlayerObDic[PhotonNetwork.LocalPlayer.ActorNumber].GetComponentInChildren<Animator>().SetTrigger("Trig_Think_Done");
            GridManagement.Instance.ResetAllTiles();
            StopCoroutine(_selectDestCoroutine);
            _selectDestCoroutine = null;
        }
    }
    void SelectDest(ActionPacketData apData)
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
                    int actualCost = Mathf.Min(distance, apData.defaultMoveCast);


                    Overmind.Instance?.SubmitSelection(action, actualCost, actorNum, LocalState.Instance.btmPacket);

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