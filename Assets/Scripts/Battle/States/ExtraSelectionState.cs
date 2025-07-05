using Newtonsoft.Json;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public class ExtraSelectionState : MonoBehaviour
{
   public static ExtraSelectionState Instance;
   private Coroutine _ExtraSelectCoroutine;
    private bool isActive = false;
    private int playerIndex;
    public List<Vector3Int> theHilighted;

    private string json;
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

    public void SetActive(bool active, ExtraSelection e)
    {


        if (active == isActive) return; // 중복 코루틴 시작 방지

        isActive = active;

        if (isActive) Start_Extra_Select_Loop(e);
        else Stop_Extra_Select_Loop();

    }

    private void Start_Extra_Select_Loop(ExtraSelection e)
    {
        if (_ExtraSelectCoroutine== null)
        {
            switch (e)
            {

                case ExtraSelection.Kawari:
                    _ExtraSelectCoroutine = StartCoroutine(KawariLoop());
                    
                    
                    break;


            }


        }
    }
    private IEnumerator KawariLoop()
    {
        int OpActorNum = Overmind.Instance.GetOtherPlayerNumber(PhotonNetwork.LocalPlayer.ActorNumber);
        playerIndex = LocalRenderingStatic.localRenderingDatas[OpActorNum].curpos;
        GridManagement.Instance.HighlightReachableTilesFrom(playerIndex, 1, Color.cyan, "Kawari");

        while (isActive)
        {

            Select_Kawari();
            yield return null;
            if (Input.GetMouseButton(0))
            {
                GridManagement.Instance.ResetAllTiles();
                json = JsonConvert.SerializeObject(GridManagement.Instance.coordToIndex[theHilighted[0]]);
                Stop_Extra_Select_Loop() ;
            }
        }
        //타일 선택쿠!
    }
    private void Select_Kawari()
    {

        GameObject hoveredTIle = GridManagement.Instance.GetTileUnderMouse();
        if (hoveredTIle == null) return;

        EachTile tile = hoveredTIle.GetComponent<EachTile>();
        if (tile == null || tile.canMove == false) return;
        int HoverdedIndex = tile.tileIndex;

        //조악한 투척이 단일 타일이므로
        theHilighted = HexSkill.GetSkillAreaByClickedTile(
            SkillTileDatabase.skillShapes[4],
            HoverdedIndex
        );
        CardChooseTile.Instance.HighlightTiles(theHilighted);
    }

    private void Stop_Extra_Select_Loop()
    {


        if (_ExtraSelectCoroutine != null)
        {


            StopCoroutine( _ExtraSelectCoroutine ); 
            _ExtraSelectCoroutine = null;

            //여기서 제이슨 오버마인드한테 보내는 코드 ㅎ ㅎ 
           
            Overmind.Instance.Send_Extra_Selection(json);

            isActive  = false;  
        }
    }

}
