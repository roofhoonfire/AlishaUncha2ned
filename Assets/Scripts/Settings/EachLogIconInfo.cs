using UnityEngine;
using UnityEngine.UI;

public class EachLogIconInfo : MonoBehaviour
{
    [Header("Hook Target (Image가 붙어있거나, 자식에 Image가 있어야 함)")]
    public GameObject Hook;
    public ActionData actionData;
    /// <summary>
    /// Hook 오브젝트에서 Image 컴포넌트를 찾아 반환.
    /// 1) Hook 자체의 Image
    /// 2) Hook 자식들의 Image
    /// 3) 마지막으로 이 아이콘 프리팹 내부에서 "Hook" 이름을 찾아보고 Image 탐색
    /// </summary>
    public Image ResolveHookImage()
    {
        if (Hook != null)
        {
            var img = Hook.GetComponent<Image>();
            if (img != null) return img;

          
        }

       
        return null;
    }
}
