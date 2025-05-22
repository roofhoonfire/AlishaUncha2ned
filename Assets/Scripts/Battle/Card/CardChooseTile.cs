using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
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
        playerCoord = GridManagement.Instance.GetCoordFromIndex(LocalState.Instance.localPlayers[actor].curpos);
        myChara = LocalState.Instance?.PlayerObDic[actor];
        AlertDialogue.Instance.StartDialogue(action, actor, HookType.Activate, DialogueType.TileChoose);
        while (isActive)
        {

            SelectTile(action.tileType, action.zoneIndex);

            yield return null;
            if (Input.GetMouseButtonDown(0))
            {
                ResetAllTileColors();
                StopSelectTileLoop();
            }
        }
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
        else //베이가 w의 경우
        {

        }
        HighlightTiles(debugYong);
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

    private void HighlightTiles(List<Vector3Int> coords)
    {
        var current = CoordsToIndices(coords);

        // Reset previous highlights
        foreach (var idx in prevHighlighted)
        {
            if (!current.Contains(idx) && GridManagement.Instance.tileObjects.TryGetValue(idx, out var go))
                go.GetComponent<SpriteRenderer>().color = Color.white;
        }

        // Apply new highlights
        foreach (var idx in current)
        {
            if (!prevHighlighted.Contains(idx) && GridManagement.Instance.tileObjects.TryGetValue(idx, out var go))
                go.GetComponent<SpriteRenderer>().color = Color.red;
        }

        prevHighlighted = current;
    }
    private List<int> CoordsToIndices(List<Vector3Int> coords)
    {
        var indices = new List<int>(coords.Count);
        foreach (var coord in coords)
        {
            int idx = GridManagement.Instance.GetIndexFromCoord(coord);
            if (idx >= 0)
                indices.Add(idx);
        }
        return indices;
    }
    private void ResetAllTileColors()
    {
        foreach (var kvp in GridManagement.Instance.tileObjects)
        {
            if (kvp.Value.TryGetComponent<SpriteRenderer>(out var sr))
                sr.color = Color.white;
        }
    }
}
