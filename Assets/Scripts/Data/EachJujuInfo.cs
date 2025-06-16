using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EachJujuInfo : MonoBehaviour
{

    
    public TextMeshProUGUI nameText;

    public TextMeshProUGUI playText;

    public string jujuCode;



    public void ApplyJujuData(Juju data)
    {
        nameText.text = data.jujuName;
        playText.text = data.Text;
        jujuCode = data.jujuCode;
    }
}
