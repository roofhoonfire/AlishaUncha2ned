using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

    void PopulateJuju(List <string> jujucodes)
    {
        spawnedJuju.Clear();

        

        // 최대 3개만 선택
        
        for (int i = 0; i < jujucodes.Count; i++)
        {
            string selectedCode = jujucodes[i];

            //이건 어차피 렌더링 용이기 때문에 굳이 플레이어꺼를 갖다 쓸필요 없음
            Juju jujuData = JujuLoader.jujuDataBase[selectedCode]; 

            GameObject jujuObj = Instantiate(jujuPrefab, jujuContentArea);
            spawnedJuju.Add(jujuObj);
             EachJujuInfo info = jujuObj.GetComponent<EachJujuInfo>();
            if (info != null)
            {
                info.ApplyJujuData(jujuData); //렌더링 코드임
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
