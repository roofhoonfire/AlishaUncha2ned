using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening; // ← 파일 상단 using들 사이에 추가


public class JujuSelectModeState : MonoBehaviour
{
    // Start is called before the first frame update
    public static JujuSelectModeState Instance;
    public bool isActive = false;
    private Coroutine _selectJujuCoroutine;
    private int actorNum;
  public   GameObject jujuPrefab;
    public RectTransform jujuContentArea; // ScrollView 안의 Content
    public GraphicRaycaster raycaster;


    public List<GameObject> spawnedJuju = new List<GameObject>();
    void Awake()
    {
        actorNum = PhotonNetwork.LocalPlayer.ActorNumber;
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }
    private static void PlaySpawnPop(Transform visualRoot, float upScale = 1.1f, float durUp = 0.08f, float durDown = 0.12f)
    {
        if (visualRoot == null) return;

        // 중복 트윈 방지
        visualRoot.DOKill(true);

        var baseScale = visualRoot.localScale;
        DOTween.Sequence()
            .Append(visualRoot.DOScale(baseScale * upScale, durUp).SetEase(Ease.OutBack))
            .Append(visualRoot.DOScale(baseScale, durDown).SetEase(Ease.InOutQuad));
    }

    public void StopSelectJujuLoop(string jujuCode)
    {
        if (_selectJujuCoroutine != null)
        {
            StopCoroutine(_selectJujuCoroutine);
            _selectJujuCoroutine = null;
        }

       
        
            isActive = false;
        ClearAllJuju();

        if (jujuCode!= null)
        {

            Debug.Log($"{jujuCode}씨발 보낸다 ");
            Overmind.Instance.Submit_Juju(actorNum, jujuCode);


        }
    }
    public void SetActive(bool active, int actorNum, string boundCode, List<string> jujucodes)
    {
        if (active == isActive) return; //이미중복 코루틴 시작 방지
        isActive = active;
        if (isActive) StartSelectJujuLoop(boundCode,jujucodes);
        else StopSelectJujuLoop(null);
    }

    private void StartSelectJujuLoop(string boundCode, List<string> jujucodes)
    {
        if (_selectJujuCoroutine == null) {
            PopulateJuju(jujucodes);

            _selectJujuCoroutine = StartCoroutine(SelectJujuLoop());
        }
    }


    private IEnumerator SelectJujuLoop()
    {
        while(true)
        {
            SelectJuju();
            yield return null;
        }
    }


    void SelectJuju()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(pointerData, results);

            foreach (var result in results)
            {
                GameObject clicked = result.gameObject;

                foreach (var juju in spawnedJuju)
                {
                    if (clicked == juju || clicked.transform.IsChildOf(juju.transform))
                    {
                        //var data = JujuLoader.jujuDataBase[juju.GetComponent<EachJujuInfo>().jujuCode]; // 또는 저장해둔 map 사용
                        //data.Apply();

                        var jujuCode = juju.GetComponent<EachJujuInfo>().jujuCode;
                        StopSelectJujuLoop(jujuCode);
                    }
                }
            }
        }



    }

    void PopulateJuju(List<string> jujucodes)
    {
        spawnedJuju.Clear();

        for (int i = 0; i < jujucodes.Count; i++)
        {
            string selectedCode = jujucodes[i];

            Juju jujuData = JujuLoader.jujuDataBase[selectedCode];

            GameObject jujuObj = Instantiate(jujuPrefab, jujuContentArea);
            spawnedJuju.Add(jujuObj);

            // ★ 비주얼 루트 찾아 팝 연출
            Transform visualRoot = jujuObj.transform.Find("VisualRoot");
            if (visualRoot == null) visualRoot = jujuObj.transform; // 안전빵 폴백
            PlaySpawnPop(visualRoot, 1.1f, 0.08f, 0.12f);

            EachJujuInfo info = jujuObj.GetComponent<EachJujuInfo>();
            if (info != null)
            {
                info.ApplyJujuData(jujuData);
            }
            else
            {
                Debug.LogWarning("EachJujuInfo 스크립트를 찾을 수 없습니다.");
            }
        }
    }


    public void ClearAllJuju()
    {

        //애니메이션 이쁜거 넣기 ㅎ
        // childCount 대신 Transform을 순회하여 안전하게 제거
        for (int i = jujuContentArea.childCount - 1; i >= 0; i--)
        {
            Transform child = jujuContentArea.GetChild(i);
            Destroy(child.gameObject);
        }
    }
}
