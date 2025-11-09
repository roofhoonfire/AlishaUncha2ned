using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public class CardChooseTile : MonoBehaviour
{
    [Header("Aim Line")]
    [SerializeField] private bool showAimLine = true;
    [Tooltip("라인 프리팹(선택). 비우면 런타임에 자동 생성")]
    [SerializeField] private LineRenderer linePrefab;

    [Tooltip("라인 Z 오프셋(타일 위로 띄우기)")]
    [SerializeField] private float lineZOffset = -0.1f;

    [Tooltip("기본 두께")]
    [SerializeField] private float lineBaseWidth = 0.035f;

    [Tooltip("두께 펄스 진폭")]
    [SerializeField] private float linePulseAmp = 0.015f;

    [Tooltip("두께 펄스 속도")]
    [SerializeField] private float linePulseSpeed = 6f;

    [Tooltip("캐릭터에서 라인 시작 위치(있으면 사용)")]
    [SerializeField] private string charAnchorName = "charpoint";

    [Tooltip("정렬 레이어/오더(선택)")]
    [SerializeField] private string lineSortingLayer = "Default";
    [SerializeField] private int lineSortingOrder = 50;

    private LineRenderer _line;        // 생성·캐싱
    private Transform _charAnchor;
    public static CardChooseTile Instance;
    // Start is called before the first frame update
    private GameObject myChara;        // 캐릭터 오브젝트
    public float angle;                // 마우스와 캐릭터 간 각도

    private Coroutine _selectTileCoroutine;
    private bool isActive = false;
    private Vector3Int playerCoord;
    private List<int> prevHighlighted = new List<int>();

    public List<Vector3Int> debugYong;
    public string mouseSide;

    //레이징용
    private HashSet<int> _ragingNow = new HashSet<int>();


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




        //마우스 라인 쿠쿠
//  SetupAimLine();


        _ragingNow.Clear();
        GridManagement.Instance.ClearAllRageTriggers();

      //  AlertDialogue.Instance.StartDialogue(action, actor, HookType.Activate, 0,DialogueType.TileChoose);
        
        //타일추스 애니메
        var tile_chooser_anim = myChara.GetComponentInChildren<Animator>();
        if (tile_chooser_anim == null)
        {
            Debug.LogError("[타일추져] 공격자 Animator 없음");
            isActive = false;

            _selectTileCoroutine = null; // 안전하게 핸들 초기화

            yield break;
        }
        if(action.cardcode!="c7858")
             tile_chooser_anim.SetTrigger("Trig_TileChoose");
      //  GridManagement.Instance.RageOn();


        // ★ tileType이 -1이면 바로 종료
        if (action.tileType == -1)
        {
            Overmind.Instance.SendTileandDirec(PhotonNetwork.LocalPlayer.ActorNumber, new List<int>(), null);
            isActive = false;

            _selectTileCoroutine = null; // 안전하게 핸들 초기화

            GridManagement.Instance.RageDone();
            _ragingNow.Clear();

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
          //  UpdateAimLine();

            SelectTile(action.tileType, action.zoneIndex);

            yield return null;
            if (Input.GetMouseButtonDown(0))
            {

                //캐릭터 좌우 판정

                var cam = Camera.main;
                if (cam != null && myChara != null)
                {
                    Vector3 charScreen = cam.WorldToScreenPoint(myChara.transform.position);

                    if (charScreen.z > 0f)
                    {
                        // 캐릭터가 카메라 앞에 있을 때: 스크린 x 기준
                        mouseSide = (Input.mousePosition.x >= charScreen.x) ? "right" : "left";
                    }
                    else
                    {
                        // 캐릭터가 카메라 뒤에 있거나 투영이 이상할 때: 그라운드 평면에 레이캐스트로 대체
                        var plane = new Plane(Vector3.up, myChara.transform.position); // y-수평인 평면(그리드 평면)
                        var ray = cam.ScreenPointToRay(Input.mousePosition);
                        if (plane.Raycast(ray, out float dist))
                        {
                            Vector3 hit = ray.GetPoint(dist);
                            Vector3 toHit = hit - myChara.transform.position;
                            float side = Vector3.Dot(toHit, cam.transform.right); // 카메라의 '오른쪽' 기준
                            mouseSide = (side >= 0f) ? "right" : "left";
                        }
                        else
                        {
                            mouseSide = "right"; // 레이 미스면 디폴트
                        }
                    }
                }
                //
               

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

        //    TeardownAimLine();


            GridManagement.Instance.RageDone();
            _ragingNow.Clear();
            Overmind.Instance.SendTileandDirec(PhotonNetwork.LocalPlayer.ActorNumber, CoordsToIndices(debugYong), mouseSide);
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
        UpdateRageForDebugYong();

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

    public void HighlightTiles(List<Vector3Int> coordsToHighlight)
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


    private void SetupAimLine()
    {
        if (!showAimLine) return;
        if (myChara == null) return;

        // 시작점 앵커 캐싱(없으면 캐릭터 자체)
        _charAnchor = myChara.transform.Find(charAnchorName) ?? myChara.transform;

        if (_line == null)
        {
            if (linePrefab != null) {

                _line = Instantiate(linePrefab, transform); // 관리 스크립트 아래에 생성
                Debug.Log("임마이거 줄 잘 만들엇다");
            }
            else
                _line = CreateRuntimeLine();

            // 정렬
            _line.sortingLayerName = lineSortingLayer;
            _line.sortingOrder = lineSortingOrder;
        }

        _line.enabled = true;
        _line.positionCount = 2;
    }

    private void UpdateAimLine()
    {
        if (!showAimLine || _line == null || !_line.enabled) return;
        if (Camera.main == null || _charAnchor == null) return;

        Vector3 start = _charAnchor.position;
        start.z += lineZOffset;

        Vector3 end = GetMouseWorldOnPlane(start.y); // 캐릭터 높이(y) 기준 평면
        end.z += lineZOffset;

        _line.SetPosition(0, start);
        _line.SetPosition(1, end);

        // 두께 펄스
        float w = lineBaseWidth + Mathf.Sin(Time.unscaledTime * linePulseSpeed) * linePulseAmp;
        _line.startWidth = w;
        _line.endWidth = w * 0.9f;
    }

    private void TeardownAimLine()
    {
        Debug.Log("자 줄 없앱니다 ");
        if (_line != null)
            _line.enabled = false;
        _charAnchor = null;
    }

    private LineRenderer CreateRuntimeLine()
    {
        var go = new GameObject("~AimLine");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.startWidth = lineBaseWidth;
        lr.endWidth = lineBaseWidth;

        // 기본 머티리얼(없으면 Sprites/Default)
        var shader = Shader.Find("Sprites/Default");
        lr.material = new Material(shader);

        // 기본 색(원하면 인스펙터에서 프리팹으로 대체)
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.cyan, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.8f, 1f) }
        );
        lr.colorGradient = grad;
        return lr;
    }

    private Vector3 GetMouseWorldOnPlane(float y)
    {
        var cam = Camera.main;
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, new Vector3(0f, y, 0f)); // y=캐릭터 높이 평면
        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);

        // 폴백(직접 z까지 투영) — 정밀도 떨어질 수 있음
        var wp = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(cam.transform.position.y - y)));
        return new Vector3(wp.x, y, wp.z);
    }

    private void UpdateRageForDebugYong()
    {
        // debugYong(List<Vector3Int>) → 인덱스 Set
        var next = new HashSet<int>();
        if (debugYong != null)
        {
            foreach (var c in debugYong)
            {
                int idx = GridManagement.Instance.GetIndexFromCoord(c);
                if (idx >= 0) next.Add(idx);
            }
        }

        // 차이만 반영해 트리거 (재발사/깜빡임 방지)
        GridManagement.Instance.RageApplyDiff(_ragingNow, next);

        _ragingNow = next;
    }




}
