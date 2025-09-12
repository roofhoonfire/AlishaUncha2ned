using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class CineManager : MonoBehaviour
{
    // Start is called before the first frame update
    public PlayableDirector _rumbleDirector;
    public PlayableDirector _GameStartDirector;


    public static CineManager Instance;

    [SerializeField] private CineContext ctx; // ← 인스펙터에 연결




    private void Awake()
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
    public System.Collections.IEnumerator PlayRumbleAndWait(int actorNum, LocalRenderingData data1, LocalRenderingData data2)
    {

        if (ctx != null)
        {
            ctx.actorNum = actorNum;
            ctx.data1 = data1;
            ctx.data2 = data2;
        }
        else
        {
            Debug.LogWarning("[RumbleCine] RumbleContext가 연결되지 않았음.");
        }

        _rumbleDirector.time = 0;
        _rumbleDirector.Evaluate();
        _rumbleDirector.Play();

        // 재생이 끝날 때까지 대기
        yield return new WaitWhile(() => _rumbleDirector.state == PlayState.Playing);

        // TODO: 여기서 다음 연출/로직 진행




        Debug.Log("자자 럼블 액션 시퀀스 잘봣니?");

        yield return StartCoroutine(LocalRenderingManager.Instance.Rendering_After_Anim_Pack(data1, data2));

    }
    public System.Collections.IEnumerator PlayGameStartAndWait()
    {

       
        _GameStartDirector.time = 0;
        _GameStartDirector.Evaluate();
        _GameStartDirector.Play();

        // 재생이 끝날 때까지 대기
        yield return new WaitWhile(() => _GameStartDirector.state == PlayState.Playing);

        // TODO: 여기서 다음 연출/로직 진행




        Debug.Log("자자 겜시작 시퀀스 잘봣니?");
        Overmind.Instance.Submit_GameStart_SyncDone();

    }

}
