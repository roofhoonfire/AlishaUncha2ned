using UnityEngine;

public class DisableOnEnter : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 다음 스테이트에 진입하면 오브젝트 비활성화
        animator.gameObject.SetActive(false);
    }
}
