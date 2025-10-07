using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EachJujuInfo : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI playText;

    [Header("Icon (이 오브젝트의 Image 연결)")]
    public Image iconImage;   // 인스펙터에 연결. 비워두면 GetComponent<Image>()로 자동 할당

    public string jujuCode;

    public void ApplyJujuData(Juju data)
    {
        if (nameText) nameText.text = data.jujuName;
        if (playText) playText.text = data.Text;
        jujuCode = data.jujuCode;

        // 아이콘 타깃 확보
        if (iconImage == null) iconImage = GetComponent<Image>();

        if (iconImage != null)
        {
            iconImage.sprite = data.sprite;
            iconImage.enabled = (data.sprite != null); // 스프라이트 없으면 감춤
            iconImage.preserveAspect = true;           // 비율 유지(원하면)
            // 필요 시 단순 타입으로 고정
            if (iconImage.type != Image.Type.Simple && iconImage.type != Image.Type.Filled)
                iconImage.type = Image.Type.Simple;
        }
    }
}
