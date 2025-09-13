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

    // Raging(분노 애니) 관리용 집합
    private HashSet<int> _ragingNow = new();  // 현재 Raging 유지 중인 타일 인덱스
    private HashSet<int> _scratch = new();  // 매 프레임 임시 집합(할당 줄이기용)

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

    public void SetActive(bool active, ActionPacketData apData)
    {
        if (active == isActive) return;
        isActive = active;
        if (isActive) StartSelectDestLoop(apData);
        else StopSelectDestLoop();
    }

    private void StartSelectDestLoop(ActionPacketData apData)
    {
        // 연출: 생각 포즈
        LocalState.Instance.PlayerObDic[PhotonNetwork.LocalPlayer.ActorNumber]
            .GetComponentInChildren<Animator>().SetTrigger("Trig_Think");

        // 이동 가능 타일 하이라이트
        TilePreprocessing(apData);

        // Raging 트리거/상태 초기화 (이전 라운드 잔여 트리거로 튀는 것 방지)
        _ragingNow.Clear();
        GridManagement.Instance.ClearAllRageTriggers();

        // ✅ 기존처럼 "가능 타일 전체 Rage ON"은 하지 않는다 (hover만 켬)
        // GridManagement.Instance.RageOnWhereCanMove();

        if (_selectDestCoroutine == null)
            _selectDestCoroutine = StartCoroutine(SelectDestLoop(apData));
    }

    private void TilePreprocessing(ActionPacketData apData)
    {
        actorNum = Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;

        var data = LocalRenderingStatic.localRenderingDatas[actorNum];
        int startIndex = data.curpos;

        // 이동 가능 범위(Cyan) 표시만 하고, Raging은 hover에서만 담당
        GridManagement.Instance.HighlightReachableTilesFrom(
            startIndex,
            apData.defaultMove,
            Color.cyan,
            "Move"
        );
    }

    private IEnumerator SelectDestLoop(ActionPacketData apData)
    {
        while (isActive)
        {
            // ESC 취소
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
        // 하이라이트/코루틴/연출 정리
        StopSelectDestLoop();
        isActive = false;
        hoveredTile = null;
        selectedTile = null;
        if (alim) alim.SetActive(false);

        // 중앙 게이트 복귀
        LocalState.Instance.ReturnToChooseLoop();
    }

    private void StopSelectDestLoop()
    {
        if (_selectDestCoroutine != null)
        {
            // 모든 타일 Raging 끄고 상태 초기화
            GridManagement.Instance.RageDone();
            _ragingNow.Clear();

            // 연출 종료
            LocalState.Instance.PlayerObDic[PhotonNetwork.LocalPlayer.ActorNumber]
                .GetComponentInChildren<Animator>().SetTrigger("Trig_Think_Done");

            // 타일 색상/상태 원복
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
                // 안내 UI
                if (alim) alim.SetActive(true);

                // 이전 hover 색 원복 (선택된 타일은 유지)
                if (hoveredTile != null && hoveredTile != selectedTile)
                    hoveredTile.GetComponent<SpriteRenderer>().color = hoveredTile.defaultColor;

                // 현재 hover 타일 색 강조
                if (currentTile != selectedTile)
                    hoveredObj.GetComponent<SpriteRenderer>().color = Color.yellow;

                distance = currentTile.cost;
                if (alim)
                    alim.GetComponent<TextMeshProUGUI>().text = $"이동까지 {distance} 행동 소모";

                hoveredTile = currentTile;

                // ✅ hover 타일만 Raging 유지 (카드 모드와 동일 방식)
                UpdateRageForHoveredTile(currentTile);

                // 클릭 처리
                if (Input.GetMouseButtonDown(0))
                {
                    if (currentTile.canMove == false)
                    {
                        if (alim) alim.GetComponent<TextMeshProUGUI>().text = $"해당 위치로는 이동할 수 없다";
                        return;
                    }

                    destinationIndex = currentTile.tileIndex;

                    // 코루틴/연출 정리 먼저
                    StopSelectDestLoop();

                    // 액션 제출
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
                    if (alim) alim.SetActive(false);
                }
            }
            else
            {
                // 이동 불가 타일 위면 Raging 끄기
                UpdateRageForHoveredTile(null);
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
            // Raging 대상 비우기
            UpdateRageForHoveredTile(null);
        }
    }

    /// <summary>
    /// hover 중인 타일 1개만 Raging 유지되도록 prev/next 차이만 반영
    /// </summary>
    private void UpdateRageForHoveredTile(EachTile tile)
    {
        _scratch.Clear();
        if (tile != null && tile.canMove)
            _scratch.Add(tile.tileIndex);

        GridManagement.Instance.RageApplyDiff(_ragingNow, _scratch);

        // 집합 스왑(할당 없이 유지)
        var t = _ragingNow; _ragingNow = _scratch; _scratch = t;
    }

    /* 필요시 사용하던 이전 코드 참고용
    IEnumerator ExitMoveModeAfterFrame() { ... }
    */
}
