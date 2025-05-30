using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TabOrder : MonoBehaviour
{
    public Button[] buttons;

    public int selectedChoice = -1;

    ATGInput inputActions;

    public Sprite selectedSprite;
    public Sprite buttonSprite;
    int numChoices = 0;

    public void Awake()
    {
        inputActions = new ATGInput();
        numChoices = buttons.Length;
    }

    public void ExecuteAction(InputAction.CallbackContext a)
    {
        if (selectedChoice != -1)
        {

            buttons[selectedChoice].onClick.Invoke();
        }
    }
    public void NavigateUp(InputAction.CallbackContext a)
    {
        DeselectButton();

        if (selectedChoice == -1)
        {
            selectedChoice = 0;
        }
        //navigate up
        else if (selectedChoice <= 0)
        {
            selectedChoice = numChoices - 1;
        }
        else
        {
            selectedChoice--;
        }
        SelectButton();
    }

    public void NavigateDown(InputAction.CallbackContext a)
    {
        DeselectButton();
        //navigate down
        if (selectedChoice == -1)
        {
            selectedChoice = 0;
        }
        else if (selectedChoice >= numChoices - 1)
        {
            selectedChoice = 0;
        }
        else
        {
            selectedChoice++;
        }
        SelectButton();
    }

    public void DeselectButton()
    {
        if (selectedChoice >= 0 && selectedChoice < buttons.Length)
            buttons[selectedChoice].image.sprite = buttonSprite;

    }

    public void SelectButton()
    {
        buttons[selectedChoice].image.sprite = selectedSprite;
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
