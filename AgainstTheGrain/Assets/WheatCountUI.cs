using TMPro;
using UnityEngine;

public class WheatCountUI : MonoBehaviour
{
    //shows the number passed
    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void updateUICount(int amt)
    {
        
        text.text = amt.ToString();
    }
}
