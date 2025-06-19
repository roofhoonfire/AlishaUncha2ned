using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public class CardChooseTile : MonoBehaviour
{
    public static CardChooseTile Instance;
    // Start is called before the first frame update
    private GameObject myChara;        // 캐릭터 오브젝트
    public float angle;                // 마우스와 캐릭터 간 각도

    private Coroutine _selectTileCoroutine;
    private bool isActive = false;
    private Vector3Int playerCoord;
    private List<int> prevHighlighted = new List<int>();

    public List<Vector3Int> debugYong;


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

    public void SetActive(bool active, ActionData action)
    {


        if (active == isActive) return; // 중복 코루틴 시작 방지

        isActive = active;

        if (isActive) StartSelectTileLoop(action);
        else StopSelectTileLoop();

    }

    private void StartSelectTileLoop(ActionData action)
    {

        if (_selectTileCoroutine == null)
            _selectTileCoroutine = StartCoroutine(SelectTileLoop(action));


    }

   private IEnumerator SelectTileLoop(ActionData action)
    {
        int actor = PhotonNetwork.LocalPlayer.ActorNumber;
        playerCoord = GridManagement.Instance.GetCoordFromIndex(LocalRenderingStatic.localRenderingDatas[actor].curpos);
        myChara = LocalState.Instance?.PlayerObDic[actor];
        AlertDialogue.Instance.StartDialogue(action, actor, HookType.Activate, 0,DialogueType.TileChoose);


        // ★ tileType이 -1이면 바로 종료
        if (action.tileType == -1)
        {
            Overmind.Instance.SendTile(PhotonNetwork.LocalPlayer.ActorNumber, new List<int>());
            isActive = false;

            _selectTileCoroutine = null; // 안전하게 핸들 초기화

            yield break;
        }
        if (action.tileType >= 11)
        {
            GridManagement.Instance.HighlightReachableTilesFrom(
                GridManagement.Instance.coordToIndex[playerCoord],
                action.tileType, // tileType >= 11이면 LinearSkill 처리됨
                Color.cyan,
                "LinearSkill"
            );
        }
        else if (action.tileType != 0) {
            GridManagement.Instance.HighlightReachableTilesFrom(
        GridManagement.Instance.coordToIndex[playerCoord],
        action.tileType,     // 스킬 사거리
        Color.cyan ,    // 스킬 선택 범위 색
        "Skill"
        );
        }
        
        while (isActive)
        {

            SelectTile(action.tileType, action.zoneIndex);

            yield return null;
            if (Input.GetMouseButtonDown(0))
            {
                GridManagement.Instance.ResetAllTiles();
                StopSelectTileLoop();
            }
        }
    }
    private void StopSelectTileLoop()
    {
        if (_selectTileCoroutine != null)
        {
            StopCoroutine(_selectTileCoroutine);
            _selectTileCoroutine = null;
            //여기서 마스터 클라이언트한테 넘겨주면 된다

            Overmind.Instance.SendTile(PhotonNetwork.LocalPlayer.ActorNumber, CoordsToIndices(debugYong));
            isActive = false;

        }


        //
    }
    private void SelectTile (int tiletype, int zoneIndex)
    {
        if (myChara == null) return;

        CalculateAndLogAngleWithMouseToPlayer();

        if (tiletype == 0)
        {
            debugYong = HexSkill.GetSkillTargets(
                SkillTileDatabase.skillShapes[zoneIndex],
                DegreeToDirection(angle),
                playerCoord
            );
        }
        else if (tiletype > 0 && tiletype<11)//베이가 w의 경우
        {
            GameObject hoveredTIle = GridManagement.Instance.GetTileUnderMouse();
            if (hoveredTIle == null) return;

            EachTile tile = hoveredTIle.GetComponent<EachTile>();
            if (tile == null || tile.canMove == false) return;

            int HoverdedIndex = tile.tileIndex;
    
            debugYong = HexSkill.GetSkillAreaByClickedTile(
                SkillTileDatabase.skillShapes[zoneIndex],
                HoverdedIndex
            );

        }
        else if (tiletype > 10)
        {
            GameObject hoveredTile = GridManagement.Instance.GetTileUnderMouse();
            if (hoveredTile == null) return;

            EachTile tile = hoveredTile.GetComponent<EachTile>();
            if (tile == null || tile.canMove == false) return;

            Vector3Int targetCoord = GridManagement.Instance.GetCoordFromIndex(tile.tileIndex);

            // HexSkill에서 계산 요청
            debugYong = HexSkill.GetTilesBetweenPlayerAndTarget(playerCoord, targetCoord);


        }
        HighlightTiles(debugYong);
    }
 
    private void CalculateAndLogAngleWithMouseToPlayer()
    {
        if (myChara == null) return;

        Vector2 mousePos = Input.mousePosition; // 마우스의 스크린 좌표
        Vector2 plPos = Camera.main.WorldToScreenPoint(myChara.transform.position); // 플레이어의 스크린 좌표
        Vector2 dir = (plPos - mousePos).normalized; // 마우스 → 플레이어 방향 벡터
         angle = Vector2.SignedAngle(Vector2.up, dir); // 위쪽과 이루는 각도

        //    Debug.Log($"마우스와 Player가 이루는 각도: {angle}도");
    }
 

    private Vector3Int DegreeToDirection(float deg)
    {
        if (deg >= 60f && deg < 120f) return new Vector3Int(1, -1, 0);
        if (deg >= 120f && deg <= 180f) return new Vector3Int(0, -1, 1);
        if (deg >= 0f && deg < 60f) return new Vector3Int(-1, 0, 1);
        if (deg < 0f && deg >= -60f) return new Vector3Int(0, 1, -1);
        if (deg < -60f && deg >= -120f) return new Vector3Int(-1, 1, 0);
        if (deg < -120f && deg >= -180f) return new Vector3Int(1, 0, -1);
        throw new System.ArgumentOutOfRangeException(nameof(deg), deg, "지원되지 않는 각도입니다.");
    }

    private void HighlightTiles(List<Vector3Int> coordsToHighlight)
    {
        foreach (var kvp in GridManagement.Instance.tileObjects)
        {
            int tileIndex = kvp.Key;
            GameObject tileObj = kvp.Value;

            EachTile tile = tileObj.GetComponent<EachTile>();
            Vector3Int tileCoord = GridManagement.Instance.GetCoordFromIndex(tileIndex);

            if (coordsToHighlight != null && coordsToHighlight.Contains(tileCoord))
            {
                tileObj.GetComponent<SpriteRenderer>().color = Color.yellow;
            }
            else
            {
                tileObj.GetComponent<SpriteRenderer>().color = tile.defaultColor;
            }
        }
    }
    private List<int> CoordsToIndices(List<Vector3Int> coords)
    {
        // coords가 null이면 빈 리스트 리턴
        if (coords == null) //디버그용이 널 즉 tiletype이 -1인경우
            return new List<int>();

        var indices = new List<int>(coords.Count);
        foreach (var coord in coords)
        {
            int idx = GridManagement.Instance.GetIndexFromCoord(coord);
            if (idx >= 0)
                indices.Add(idx);
        }
        return indices;
    }

}
