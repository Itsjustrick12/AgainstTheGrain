using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Composites;
using UnityEngine.UI;


public class PlayerOptions : MonoBehaviour
{

    public Canvas uiCanvas;

    public CursorToTilemap inputManager;
    public TilemapManager tileManger;

    [SerializeField]
    public List<OptionBox> optionList;

    //move to the tile beside it on the right
    public int offsetX = 60;
    public int offsetY = 0;

    public int selectedChoice = -1;
    public int numChoices;

    //for input modularity
    ATGInput inputActions;

    public GameObject buttonContainer;

    public Sprite OptionBox;
    public Sprite SelectedOptionBox;
    public GameObject selectionArrow;

    public Vector3Int actionLocation;

    public void Awake()
    {
        inputActions = new ATGInput();
        tileManger = TilemapManager.instance;
        PopulateOptions();
    }
    public void ExecuteAction(InputAction.CallbackContext a)
    {
        Debug.Log("Current Choice: " + selectedChoice);
        ReportAction();
    }
    public void NavigateUp(InputAction.CallbackContext a)
    {
        DeselectButton();
        //navigate up
        if (selectedChoice <= 0)
        {
            selectedChoice = numChoices - 1;
        } 
        else
        {
            selectedChoice--;
        }
        Debug.Log("Current Choice: " + selectedChoice);
        SelectButton();
    }

    public void NavigateDown(InputAction.CallbackContext a)
    {
        DeselectButton();
        //navigate down
        if (selectedChoice >= numChoices - 1)
        {
            selectedChoice = 0;
        }
        else
        {
            selectedChoice++;
        }
        Debug.Log("Current Choice: " + selectedChoice);
        SelectButton();
    }

    public void DeselectButton()
    {
        optionList[selectedChoice].GetComponent<Image>().sprite = OptionBox;
        
    }

    public void SelectButton()
    {
        selectionArrow.GetComponent<RectTransform>().anchoredPosition = new Vector2(selectionArrow.GetComponent<RectTransform>().anchoredPosition.x, -24 * selectedChoice);
        optionList[selectedChoice].GetComponent<Image>().sprite = SelectedOptionBox;
    }

    public void ShowOptions(Vector3Int tileLocation, Unit player)
    {
        gameObject.SetActive(true);

        //only show buttons with fufilled conidtions
        //ex dont show attack if nothing to attack
        selectedChoice = 0;
        actionLocation = tileLocation;
        DetermineValidOptions(player.type);
        inputManager.makingDecision = true;

        //initalize the navigation
        numChoices = optionList.Count;
        SelectButton();


        //place the menu next to the current tile
        Vector2 newPosition;
        Vector3 tilePosition = tileManger.placeholderMap.CellToWorld(tileLocation);
        Vector2 screenPos = Camera.main.WorldToScreenPoint(tilePosition);

        Vector2 tileOffset = new Vector2(offsetX,offsetY);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                uiCanvas.transform as RectTransform,
                screenPos,
                uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera,
                out newPosition);
        


        GetComponent<RectTransform>().anchoredPosition = newPosition + tileOffset;
    }

    public void ReportAction()
    {
        ActionType type = optionList[selectedChoice].GetComponent<OptionBox>().action;
        //dont select attack if theres nothing to attack
        
        inputManager.HandleAction(type);
        HideOptions();
    }

    public void PopulateOptions()
    {
        List<OptionBox> temp = new List<OptionBox>();
        //reset to have all options availible
        foreach (Transform child in buttonContainer.transform)
        {
            temp.Add(child.GetComponent<OptionBox>());
            child.GetComponent<Image>().sprite = OptionBox;
        }
        optionList = temp;
        EventSystem.current.SetSelectedGameObject(null);
    }

    //hide any buttons that have actions that cant be taken
    public void DetermineValidOptions(UnitType playerType)
    {

        PopulateOptions();

        tileManger = TilemapManager.instance;
        foreach (OptionBox box in optionList)
        {
            //do checks to see if the button should be hidden
            bool active = true;
            ActionType type = box.action;


            //dont select attack if theres nothing to attack
            if (type == ActionType.Attack && tileManger.GetEnemiesNearby(actionLocation, false).Count == 0)
            {
                active = false;
            }
            //hide if no plants nearby
            else if ((type == ActionType.Water) && tileManger.GetCropsNearby(actionLocation, false).Count == 0)
            {
                active = false;
            }
            else if ((type == ActionType.Farm) && tileManger.GetHarvestableCropsNearby(actionLocation, false).Count == 0)
            {
                active = false;
            }
            else if (type == ActionType.Plant && tileManger.GetDirtNearby(actionLocation, false).Count == 0)
            {
                active = false;
            }
            else if (type == ActionType.Wait)
            {
                active = true;
            }

            //disable certain options based on unit type
            if (playerType == UnitType.Farmer && type == ActionType.Attack)
            {
                active = false;
            }
            else if ((playerType == UnitType.Animal || playerType == UnitType.Animal) 
                && ((type == ActionType.Plant || type == ActionType.Water || type == ActionType.Farm))){
                active = false;
            }

            //update the gameobject to show or not show
            box.gameObject.SetActive(active);
        }

        //update the options list to only have active options
        optionList = optionList.FindAll(op => op.gameObject.activeSelf);
    }

    public void HideOptions()
    {
        if (selectedChoice > 0)
        {

            DeselectButton();
        }
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {

        inputActions.Enable();
        inputActions.GameMappings.Up.performed += NavigateUp;
        inputActions.GameMappings.Down.performed += NavigateDown;
        inputActions.GameMappings.Select.performed += ExecuteAction;
        
    }

    private void OnDisable()
    {
        inputActions.GameMappings.Up.performed -= NavigateUp;
        inputActions.GameMappings.Down.performed -= NavigateDown;
        inputActions.GameMappings.Select.performed -= ExecuteAction;
        inputActions.Disable();
    }
}
