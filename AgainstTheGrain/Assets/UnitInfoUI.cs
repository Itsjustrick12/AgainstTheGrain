using TMPro;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;

public class TileInfoUI : MonoBehaviour
{
    //UI Element References
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI cropText;

    public Image unitSprite;
    public Image cropSprite;

    public GameObject UnitPictureUI;
    public GameObject HealthAmountUI;
    public GameObject UnfedUI;

    public GameObject CropStageUI;
    public GameObject CropTextUI;

    public void DisplayUnitInfo(Unit unit)
    {

        AnimalUnit animal = unit.GetComponent<AnimalUnit>();
        if (animal != null)
        {
            DisplayUnitInfo(animal);
            return;
        }

        healthText.text = unit.GetHealth().ToString();
        unitSprite.sprite = unit.sprite.sprite;

        
        HealthAmountUI.SetActive(true);
        UnfedUI.SetActive(false);
        

        UnitPictureUI.SetActive(true);
        
        CropStageUI.SetActive(false);
        CropTextUI.SetActive(false);
    }

    public void DisplayUnitInfo(AnimalUnit unit)
    {

        healthText.text = unit.GetHealth().ToString();
        unitSprite.sprite = unit.sprite.sprite;
        if (!unit.isFed)
        {
            UnfedUI.SetActive(true);
            HealthAmountUI.SetActive(false);
        }
        else
        {
            HealthAmountUI.SetActive(true);
            UnfedUI.SetActive(false);
        }
        UnitPictureUI.SetActive(true);
        CropStageUI.SetActive(false);
        CropTextUI.SetActive(false);
    }

    public void DisplayCropInfo(CropObject cropObj)
    {
        cropText.text = (cropObj.stage + "/" + (cropObj.crop.numStages-1));
        cropSprite.sprite = cropObj.sprite.sprite;
        UnitPictureUI.SetActive(false);
        HealthAmountUI.SetActive(false);
        CropStageUI.SetActive(true);
        CropTextUI.SetActive(true);
        UnfedUI.SetActive(false);
    }

    public void HideInfo()
    {
        UnitPictureUI.SetActive(false);
        HealthAmountUI.SetActive(false);
        CropStageUI.SetActive(false);
        CropTextUI.SetActive(false);
        UnfedUI.SetActive(false);
    }
}
