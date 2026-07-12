using Language.Lua;
using PixelCrushers.DialogueSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
//using static UnityEditor.PlayerSettings;

public enum InteractionState
{
    Selection,
    Movement,
    ActionSelection,
    DecisionSelection,
    FeedTargeting,
    TargetSelection,
    Execution
}

public enum TileColor
{
    White,
    Red,
    Green,
    Blue,
    Yellow
}

//This is needed to call events and recieve callbacks from UI buttons
[System.Serializable]
public class EntityActionEvent : UnityEvent<EntityAction>
{
}

//This script is responsible for handling the interactions between units
//This includes movement, crop interactions, attacking, and more
public class UnitInteractionSystem : TileCursor
{
    public TileManager tileManager;
    public TileHelper tileHelper;
    [SerializeField] FeedManager feedManager;
    private PickCropUI cropPicker;
    
    public Tilemap optionsMap;
    public Tilemap extensionsMap;

    public Tilemap arrowMap;
    public AIManager aiManager;

    //Used for color coding actionable areas
    public TileBase[] infoTiles;
    public TileBase[] extensionTiles;
    public TileBase[] arrowTiles;
    public TileBase GetInfoTile(TileColor color) => infoTiles[(int)color];
    public TileBase GetExtensionTile(TileColor color) => extensionTiles[(int)color];

    //Stores the location of the current tile selected
    [SerializeField]private ActionMenu actionMenu;

    public InteractionState state;

    //Get reference to the unit/location we are currently interacting with
    public Vector3Int selectedPosition;

    //Needed for reseting movement with cancel action
    public Vector3Int prevLocation;
    public Vector3Int afterLocation;
    public Vector3Int lastLocation;

    public Entity selectedEntity;

    private Vector3 offset = new Vector3(0.5f, 0.75f, 0);

    //Change this later but for now default Unity UI interactions is good
    private AgainstTheGrainInput input;

    [SerializeField] List<Vector3Int> validLocations;
    private EntityAction currAction;

    public bool isInputOn = true;

    public UnitInfoPanel infoPanel;
    private Unit lastHoveredUnit;
    public bool isFeeding = false;
    public bool isMoving = false;

    private Stack<InteractionState> stateHistory = new Stack<InteractionState>();

    public static event System.Action<InteractionState> OnStateChanged;

    //Actions for triggering tutorial statements
    public static event Action OnUnitSelected;
    public static event Action OnUnitMoved;

    //Used to reverse movement path for clean walkback
    private List<Vector3Int> lastMovePath = new List<Vector3Int>();

    public void Awake()
    {
        tileManager = FindFirstObjectByType<TileManager>();
        tileHelper = FindFirstObjectByType<TileHelper>();
        cropPicker = FindFirstObjectByType<PickCropUI>();
        aiManager = FindFirstObjectByType<AIManager>();
        //actionMenu = FindFirstObjectByType<ActionMenu>();
        actionMenu.OnActionSelected.AddListener(SelectAction);
        PushState(InteractionState.Selection);
        BarnUIMenu.OnUnitPurchased.AddListener(OnSelectUnit);
        BarnUIMenu.CancelAction.AddListener(StopAction);
        JobBoardUI.OnFarmerHired.AddListener(OnSelectUnit);
        JobBoardUI.CancelAction.AddListener(StopAction);
        FeedManager.OnFeedingComplete += StopFeeding;
        feedManager = FindFirstObjectByType<FeedManager>();
        validLocations = new List<Vector3Int>();
    }
    //Restrict to only display updated tiles
    protected override void HandleCursor()
    {
        if (DialogueManager.IsConversationActive){
            hoverMap.ClearAllTiles();
            return;
        }
        if (!isInputOn) return;

        Vector3Int tile = GetMouseTile();

        if (showHighlight && tile != currentTile && targetMap.HasTile(tile))
        {
            //Only select valid tiles when there a valid tiles to check
            if ((validLocations.Count==0) || (validLocations.Count > 0 && IsInRange(tile)))
            {
                DeselectLast();
                currentTile = tile;
                SelectCurrent(tile);
            }
        }

        if (state == InteractionState.TargetSelection && currAction.IsAOE())
        {
            Vector3Int hovered = GetCurrentTile();
            extensionsMap.ClearAllTiles();

            if (validLocations.Contains(hovered))
            {
                List<Vector3Int> extensions = currAction.GetExtensionTiles(hovered, selectedEntity.GetGridPos());
                DrawExtensionTiles(extensions);
            }
        }
    }

    public override void Update()
    {
        if (DialogueManager.isConversationActive || !isInputOn)
        {
            return;
        }
        base.Update();
        if(GetCurrentTile() != lastLocation && state == InteractionState.Movement)
        {
            SetArrow();
        }
        lastLocation = GetCurrentTile();
        Vector3Int pos = GetCurrentTile();
        Entity potentialEntity = null;
        TileData data = tileManager.GetTileDataAt(pos);
        List<Vector3Int> hoverLocations;
        // Only run hover logic when in Selection state AND nothing is selected
        if (state == InteractionState.Selection && selectedEntity == null)
        {
            if (data != null)
            {
                potentialEntity = data.GetOccupyingEntity();
            }
            Unit unit = potentialEntity as Unit;
            if (unit != null)
            {
                if (unit != lastHoveredUnit)
                {
                    optionsMap.ClearAllTiles();
                    infoPanel.ShowPanel(unit);
                    if (unit.GetIsEnemy())
                    {
                        hoverLocations = tileHelper.GetInteractionRange(unit);

                        List<EntityAction> actions = unit.GetAllActions();
                        SetOptionsTiles(hoverLocations, actions);
                    }
                    lastHoveredUnit = unit;
                }
            }
            else
            {
                if (lastHoveredUnit != null)
                {
                    optionsMap.ClearAllTiles();
                    infoPanel.HidePanel();
                    lastHoveredUnit = null;
                }
            }
        }

    }

    public void SetArrow()
    {
        arrowMap.ClearAllTiles();

        //Debug.Log("SetArrow");
        List<Vector3Int> path = tileHelper.TilePath(selectedEntity.GetGridPos(), GetCurrentTile(), selectedEntity as Unit);

        //checks the end of the path
        if(tileManager.GetTileDataAt(path[path.Count - 1]).HasOccupant() && path.Count > 1)
        {
            path.RemoveAt(path.Count - 1);
        }

        // Prepend current position so the full path including origin is stored
        List<Vector3Int> fullPath = new List<Vector3Int>();
        fullPath.Add(selectedEntity.GetGridPos());
        fullPath.AddRange(path);
        lastMovePath = fullPath;

        if (path.Count > 1)
        {
            for(int i = 0; i < path.Count; i++)
            {
                arrowMap.SetTile(path[i], arrowTiles[ArrowHelper(i, path)]);
            }
        }

    }

    private int ArrowHelper(int currentTile, List<Vector3Int> path)
    {
        int positionOne = -1;
        int positionTwo = -1;

        // directions:
        // 0 = left
        // 1 = up
        // 2 = right
        // 3 = down

        Vector3Int currentPos = path[currentTile];

        bool hasPrevious = currentTile > 0;
        bool hasNext = currentTile < path.Count - 1;

        //if it's the starting tile
        if(!hasPrevious && hasNext)
        {
            Vector3Int diff = path[currentTile + 1] - currentPos;

            if(diff == Vector3Int.left) return 0; //tail left

            if(diff == Vector3Int.up) return 1; //tail up

            if(diff == Vector3Int.right) return 2; //tail right

            if(diff == Vector3Int.down) return 3; //tail down
        }

        //if it's the ending tile
        if(hasPrevious && !hasNext)
        {
            Vector3Int diff = currentPos - path[currentTile - 1];

            if(diff == Vector3Int.left) return 4; // head left

            if(diff == Vector3Int.up) return 5; // head up

            if(diff == Vector3Int.right) return 6; // head right

            if(diff == Vector3Int.down) return 7; // head down
        }

        //middle tile
        if(hasPrevious && hasNext)
        {
            Vector3Int diffOne = path[currentTile - 1] - currentPos;
            Vector3Int diffTwo = path[currentTile + 1] - currentPos;

            if(diffOne == Vector3Int.left) positionOne = 0;
            else if(diffOne == Vector3Int.up) positionOne = 1;
            else if(diffOne == Vector3Int.right) positionOne = 2;
            else if(diffOne == Vector3Int.down) positionOne = 3;

            if(diffTwo == Vector3Int.left) positionTwo = 0;
            else if(diffTwo == Vector3Int.up) positionTwo = 1;
            else if(diffTwo == Vector3Int.right) positionTwo = 2;
            else if(diffTwo == Vector3Int.down) positionTwo = 3;

            int low = Mathf.Min(positionOne, positionTwo);
            int high = Mathf.Max(positionOne, positionTwo);

            //left + right
            if(low == 0 && high == 2)
                return 8; //horizontal

            //up + down
            if(low == 1 && high == 3)
                return 9; //vertical

            //left + up
            if(low == 0 && high == 1)
                return 10; //corner left-up

            //up + right
            if(low == 1 && high == 2)
                return 11; //corner up-right

            //right + down
            if(low == 2 && high == 3)
                return 12; //corner right-down

            //down + left
            if(low == 0 && high == 3)
                return 13; //corner down-left
        }

        return 0;
    }

    //Modify state with this to keep track of history for undo functionality
    //State should rarely be modified directly to preserve state history for the undo function
    private void PushState(InteractionState newState)
    {
        stateHistory.Push(state);
        state = newState;
        OnStateChanged?.Invoke(state);
    }

    //Attempt to find an entity on the current tile
    public bool AttemptSelection(Vector3Int pos)
    {
         
        TileData data = tileManager.GetTileDataAt(pos);

        if (data == null || !data.HasOccupant())
        {
            return false;
        }

        selectedPosition = pos;
        selectedEntity = data.GetOccupyingEntity();
        if (selectedEntity != null && selectedEntity.IsActive())
        {
            if (selectedEntity.GetEntityType() == EntityType.Farmer)
            {
                OnUnitSelected?.Invoke();
            }
            return true;
        }
        return false;
    }

    //Transitions through the state machine to determine what action to take
    //This is called the second the user clicks their mouse, so the logic has to be really tight
    public void OnSelect(InputAction.CallbackContext context)
    {
        if (DialogueManager.IsConversationActive)
        {
            return;
        }
        Vector3Int pos = GetCurrentTile();
        if (isMoving) return;
        switch (state)
        {
            case InteractionState.Selection:
                //Don't accept inputs while feeding a unit
                if (isFeeding) return;
                //Attempt to set the selected Unit on the tile
                if (AttemptSelection(pos))
                {
                    //If above is true, get reference to entity and switch to movement phase
                    selectedPosition = pos;
                    selectedEntity = tileManager.GetTileDataAt(pos).GetOccupyingEntity();
                    if (selectedEntity != null && selectedEntity is Unit)
                    {
                        Unit unit = selectedEntity as Unit;
                        if (unit.isEnemy)
                        {
                            selectedEntity = null;
                            //selectedPosition = new Vector3Int(0, 0, -1); // reset to your default
                            return;
                        }
                        infoPanel.HidePanel();
                        List<Vector3Int> valid = tileHelper.GetInteractionRange(unit);
                        List<EntityAction> actions = unit.GetAllActions();
                        
                        SetOptionsTiles(valid, actions);

                        //Remove any not moveable or interactable tiles
                        

                        validLocations = valid;
                        SoundManager.Instance.PlayEntitySound(unit, SoundType.SELECT);
                        PushState(InteractionState.Movement);
                        return;
                    }
                    else if (selectedEntity != null && selectedEntity is Structure)
                    {
                        //do structure interaction, skip movement interaction
                        Structure structureCheck = selectedEntity as Structure;
                        List<EntityAction> actions = structureCheck.GetAvailableActions();

                        // Store the SpawnUnitAction as currAction so SelectAction can use it later
                        // when OnPurchaseComplete fires after the player picks a unit in the UI
                        if (actions != null && actions.Count > 0)
                        {
                            currAction = actions[0]; // assumes first action is SpawnUnitAction
                            PushState(InteractionState.ActionSelection);
                            structureCheck.Interact();
                        }
                    }
                }
                break;
            case InteractionState.Movement:
                TileData fromData = tileManager.GetTileDataAt(selectedPosition);
                TileData toData = tileManager.GetTileDataAt(pos);
                //if selected tile, try to place the unit at the current location to see if it works
                if (selectedEntity != null && toData != null)
                {
                    //grab what's at the position before clearing
                    TileBase end = optionsMap.GetTile(pos);

                    //check they if they're the same
                    if (fromData == toData)
                    {
                        //switch to interaction selection phase
                        ShowUnitOptions(toData.GetOccupyingEntity());
                        optionsMap.ClearAllTiles();
                        extensionsMap.ClearAllTiles();
                        //Needed for the cancel action to work
                        prevLocation = selectedPosition;
                        afterLocation = pos;
                        SoundManager.Instance.PlayEntitySound(selectedEntity, SoundType.PLACE);
                    }
                    //Only place if entity can go to new tile
                    else if (IsInRange(pos))
                    {
                        //tileManager.MoveEntity(selectedPosition, pos);
                        prevLocation = selectedPosition;
                        afterLocation = pos;
                        Unit unitCheck = selectedEntity as Unit;
                        if (unitCheck == null) return;
                        
                        //Used to determine what to do at a given location
                        int zFlag = 0;
                        for (int i = 0; i < validLocations.Count; i++)
                        {
                            if (SameExceptZ(validLocations[i], pos)) { zFlag = validLocations[i].z; break; }
                        }

                        // Only move to white tiles that indicate movement
                        if (end == GetInfoTile(TileColor.White) || zFlag == 0)
                        {
                            optionsMap.ClearAllTiles();
                            extensionsMap.ClearAllTiles();
                            StartCoroutine(WaitForMoveAndShowOptions(unitCheck, toData));
                        }
                        else if (end == null || zFlag == 5 || zFlag == -5 || zFlag == -1)
                        {
                            //ignore clicks if the zFlag is indicating a friendly unit or a reach tile
                            return; 
                        }
                        else
                        {
                            //if anything else, do the quick action
                            optionsMap.ClearAllTiles();
                            extensionsMap.ClearAllTiles();
                            StartCoroutine(QuickAction(unitCheck, toData));
                        }

                    }

                    return;
                }
                else
                {
                    //Place the unit back or just dont do anything
                    state = InteractionState.Selection;
                    OnStateChanged?.Invoke(state);

                }
                break;
            case InteractionState.TargetSelection:
                if (AttemptTarget(pos))
                {
                    state = InteractionState.Selection;
                    OnStateChanged?.Invoke(state);
                }
                break;
            case InteractionState.FeedTargeting:
                // Try to feed unit
                TryFeedAtPosition(pos);
                break;
            default:
                break;
        }
    }

    private IEnumerator WaitForMoveAndShowOptions(Unit unit, TileData toData)
    {
        DisableInputs();
        //Debug.Log("WaitForMoveAndShowOptions");
        isMoving = true;
        //grab the path and store incase we undo
        //move along path
        yield return StartCoroutine(unit.Move(lastMovePath));

        yield return new WaitUntil(() => !unit.isMoving);
        isMoving = false;

        arrowMap.ClearAllTiles();
        EnableInputs();
        ShowUnitOptions(unit);
        OnUnitMoved?.Invoke();
    }

    private IEnumerator QuickAction(Unit unit, TileData toData)
    {
        DisableInputs();
        //Debug.Log("QuickAction");
        isMoving = true;
        //grab the path and store incase we undo
        //move along path

        //normalize the ending tile's z flag that was set
        Vector3Int end = new Vector3Int(afterLocation.x, afterLocation.y, 0);
        yield return StartCoroutine(unit.Move(lastMovePath));

        yield return new WaitUntil(() => !unit.isMoving);
        isMoving = false;

        arrowMap.ClearAllTiles();
        EnableInputs();
        //ShowUnitOptions(unit);

        //find the correct action
        string tempAction = "";
        Vector3Int pos = unit.GetGridPos();
        List<EntityAction> actions = unit.GetAllActions();

        //if the final tile has an enemy
        if(tileManager.GetTileDataAt(end).HasEnemyUnit())
        {
            tempAction = "Attack";
        }//if the tile has a crop
        else if (tileManager.GetTileDataAt(end).GetOccupyingEntity() as Crop != null)
        {
            Crop cropCheck = tileManager.GetTileDataAt(end).GetOccupyingEntity() as Crop;
            //if the crop is ready to be harvested
            if (cropCheck.CanBeHarvested())
            {
                tempAction = "Harvest";
            }//if the tile is not ready to be harvested and needs to be watered
            else if (!cropCheck.CanBeHarvested() && !cropCheck.IsWatered())
            {
                tempAction = "Water";
            }
        }

        if(tempAction != "")
        {
            foreach(EntityAction act in actions)
            {
                if(act.GetName() == tempAction)
                {
                    currAction = act;
                }
            }
        }

        if (AttemptTarget(end))
        {
            state = InteractionState.Selection;
            OnStateChanged?.Invoke(state);
        }

    }

    public bool IsInRange(Vector3Int pos)
    {
        for(int i = 0; i < validLocations.Count; i++)
        {
            if (SameExceptZ(validLocations[i],pos))
            {
                // Block pure extension tiles (white, non-actionable)
                return validLocations[i].z != -1;
            }
        }
        return false;
    }

    public bool AttemptTarget(Vector3Int pos)
    {
        if (currAction == null)
        {
            Debug.Log("No Current Action");
            return false;
        }

        
        //check to see that the location is valid
        //Check that the position you clicked is a valid target
        if (validLocations.Count > 0 && XYInList(validLocations, pos))
        {
            //Execute the action
            //Need to remain general
            Debug.Log("Preforming " + currAction.actionName);
            currAction.PerformAt(selectedEntity, pos);
            selectedEntity.Deactivate();
            ResetData();
            return true;
        }
        Debug.Log("Invalid location");
        return false; 
    }
    //Determins if pos is contained in the list ignoring it's z coordinate
    private bool XYInList(List<Vector3Int> list, Vector3Int pos)
    {
        for (int i = 0; i < list.Count; i++)
        {
            //check if theres an x and y match
            if (SameExceptZ(list[i], pos))
            {
                return true;
            }
        }
        return false;
    }
    //Compares the two pos ignoring their z coordinate
    private bool SameExceptZ(Vector3Int a, Vector3Int b)
    {
        return a.x == b.x && a.y == b.y;
    }

    private void ShowUnitOptions(Unit unit)
    {
        if (unit == null)
        {
            //Debug.LogError("THERES NO UNIT TO SHOW OPTIONS FOR");
            return;
        }
        PushState(InteractionState.ActionSelection);
        actionMenu.ShowMenu(unit);
    }
    //used to allow player to undo the steps in the action state machine
    public void UndoAction(InputAction.CallbackContext context)
    {
        UndoAction();
    }
    public void UndoAction()
    {
        Unit unit = selectedEntity as Unit;
        if(unit != null && unit.isMoving)
        {
            return;
        }
        if (stateHistory.Count == 0)
        {
            //can't undo if there's no history
            return;
        }
        //get the previous state type then do the appropriate undo action to get to that state
        InteractionState previous = stateHistory.Pop();
        switch (state)
        {

            case InteractionState.Movement:
                //clear movement tiles and deselect the unit
                optionsMap.ClearAllTiles();
                validLocations.Clear();
                arrowMap.ClearAllTiles();
                selectedEntity = null;
                selectedPosition = new Vector3Int(0, 0, 1);
                break;
            case InteractionState.ActionSelection:
                actionMenu.HideMenu();
                if (afterLocation != prevLocation)
                {
                    //tileManager.MoveEntity(afterLocation, prevLocation);
                    isMoving = true;
                    Debug.Log("UndoAction");
                    tileManager.MoveEntity(afterLocation, prevLocation);
                    isMoving = false;
                    afterLocation = prevLocation;
                }
                //show movement highlights
                if (unit != null)
                {
                    //show movement range
                        validLocations = tileHelper.GetInteractionRange(unit);
                        List<EntityAction> actions = unit.GetAllActions();
                        
                        SetOptionsTiles(validLocations, actions);
                }
                break;
            case InteractionState.DecisionSelection:
                // Close whichever decision UI is open
                if (currAction is PlantAction)
                {
                    cropPicker.OnCropSelected.RemoveListener(OnPlantSelected);
                    cropPicker.CancelPicking(); // whatever your hide method is
                }
                else if (currAction is SpawnUnitAction)
                {
                    // Barn cancel is already handled via CancelAction event
                }
                else if (isFeeding)
                {
                    // Player backed out of crop picker during feed flow
                    feedManager.CancelFeed();
                    cropPicker.CancelPicking(); // safe � CancelPicking guards !picking internally
                    selectedEntity = null;
                    isFeeding = false;
                    ResetData();
                    return; // ResetData sets state to Selection, skip the state = previous line
                }
                currAction = null;
                // Reopen action menu for the unit
                if (unit != null) actionMenu.ShowMenu(unit);
                break;
            case InteractionState.TargetSelection:
                //clear target highlights then go back to action selection
                optionsMap.ClearAllTiles();
                extensionsMap.ClearAllTiles();
                validLocations.Clear();
                currAction = null;
                if (unit != null)
                {
                    actionMenu.ShowMenu(unit);
                }
                break;
            case InteractionState.FeedTargeting:
                //Clean up via the PickCropUI class and ensure all the UIs are closed properly
                cropPicker.OnCropSelected.RemoveListener(OnPlantSelected);
                cropPicker.CancelPicking();
                feedManager.CancelFeed();
                ResetData();
                //Set flag back to false to re-enable functionality
                isFeeding = false;
                break;
            default:
                break;
        }
        state = previous;
        OnStateChanged?.Invoke(state);
    }
    //Regular stop, just cancel the action and move if you need to
    public void StopAction()
    {
        if (afterLocation != prevLocation
            && afterLocation != new Vector3Int(0, 0, -1)
            && prevLocation != new Vector3Int(0, 0, -1))
        {
            Unit unit = tileManager.GetUnitOnTile(afterLocation);
            if (unit != null)
            {
                tileManager.MoveEntity(afterLocation, prevLocation);
            }
        }
        ResetData();
    }
    //desync'd walk back undo ONLY USED by CANCEL
    private IEnumerator StopActionRoutine()
    {
        if (afterLocation != prevLocation
            && afterLocation != new Vector3Int(0, 0, -1)
            && prevLocation != new Vector3Int(0, 0, -1))
        {
            Unit unit = tileManager.GetUnitOnTile(afterLocation);

            if (unit != null)
            {
                DisableInputs();
                isMoving = true;
                //Reverse the arrow path for consistency purposes instead of computing the path again
                List<Vector3Int> reversedPath = new List<Vector3Int>(lastMovePath);
                reversedPath.Reverse();
                yield return StartCoroutine(unit.Move(reversedPath));
                //Wait for animation to finish
                yield return new WaitUntil(() => !unit.isMoving);

                EnableInputs();
            }
        }//reset flag in case their is no move back
        isMoving = false;
        ResetData();
    }

    private void ResetData()
    {
        stateHistory.Clear();
        optionsMap.ClearAllTiles();
        arrowMap.ClearAllTiles();
        extensionsMap.ClearAllTiles();
        validLocations.Clear();

        selectedEntity = null;
        selectedPosition = new Vector3Int(0, 0, -1);
        prevLocation = new Vector3Int(0, 0, -1);
        afterLocation = new Vector3Int(0, 0, -1);
        currAction = null;

        // Also reset state to Selection to prevent stale state issues
        state = InteractionState.Selection;
        OnStateChanged?.Invoke(state);
    }

    public void SelectAction()
    {
        SelectAction(currAction);
    }
    //Once an action is reported from the Action Menu UI script, handle the logic to perform the action
    public void SelectAction(EntityAction action)
    {
        if (isFeeding) return;

        //Check for unique UI ish actions for not doing direct things
        if (action == null)
        {
            Debug.LogError("NO ACTION SELECTED!");
        }

        //once this is called, shift to tile selection based on target tiles
        if (action is WaitAction)
        {
            //Don't do anything, consider the unit moved and don't do anything else
            state = InteractionState.Selection;
            OnStateChanged?.Invoke(state);
            selectedEntity.Deactivate(0f);
            ResetData();
            return;
        }
        else if (action is Cancel)
        {

            //Undo the movement from the previous action and return
            isMoving = true;
            StartCoroutine(StopActionRoutine());
            return;
        }
        else if (action is EndTurnAction)
        {

            //Undo the movement from the previous action and return
            DisableInputs();
            GameManager.Instance.BeginEnemyTurn();
            ResetData();
            return;
        }
        //For planting action, you must first determine which seed to plant via the UI
        if (action is PlantAction)
        {
            currAction = action;
            PushState(InteractionState.DecisionSelection);
            cropPicker.OnCropSelected.AddListener(OnPlantSelected);
            cropPicker.StartPicking(false);
            return;
        }
        if (action is SpawnUnitAction)
        {
            currAction = action;
            PushState(InteractionState.DecisionSelection);
            // Barn UI is already opened via structure interaction,
            // OnPurchaseComplete fires OnDecisionComplete when done
            return;
        }

        currAction = action;
        //Determine where the user needs to click
        GetTargets();
    }

    //Get the availible targets from the current action, then switch to selecting one
    private void GetTargets()
    {
        validLocations = currAction.GetValidTargets(selectedEntity);
        //Highlight the selectable locations
        ColorTiles(validLocations);

        //Otherwise, perform the action on the selected tile
        PushState(InteractionState.TargetSelection);
    }

    //color the spaces for whats there:
    public void ColorTiles(List<Vector3Int> locations)
    {
        foreach (Vector3Int pos in locations)
        {
            //chooses what color tile to be put on the position
            //if the tile has an enemy
            if (tileManager.GetTileDataAt(pos).HasEnemyUnit())
            {
                optionsMap.SetTile(pos, GetInfoTile(TileColor.Red));
            }//if the tile has a crop

            else if (tileManager.GetTileDataAt(pos).GetOccupyingEntity() as Crop != null)
            {
                Crop cropCheck = tileManager.GetTileDataAt(pos).GetOccupyingEntity() as Crop;
                //if the crop is ready to be harvested
                if (cropCheck.CanBeHarvested())
                {
                    optionsMap.SetTile(pos, GetInfoTile(TileColor.Yellow));
                }//if the tile is not ready to be harvested and needs to be watered
                else if (!cropCheck.CanBeHarvested() && !cropCheck.IsWatered())
                {
                    optionsMap.SetTile(pos, GetInfoTile(TileColor.Blue));
                }
            }//if it has nothing on it check if we're planting
            else if(tileManager.GetTileDataAt(pos).GetOccupyingEntity() == null && currAction.GetName() == "Plant")
            {
                optionsMap.SetTile(pos, GetInfoTile(TileColor.Green));
            }
            else
            {
                optionsMap.SetTile(pos, GetInfoTile(TileColor.White));
            }
        }
    }

    public void DrawExtensionTiles(List<Vector3Int> locations)
    {
        foreach (Vector3Int pos in locations)
        {
            if(tileManager.GetTileDataAt(pos) != null)
            {
                if (tileManager.GetTileDataAt(pos).HasEnemyUnit())
                {
                    extensionsMap.SetTile(pos, GetExtensionTile(TileColor.Red));
                }//if the tile has a crop
                else if (tileManager.GetTileDataAt(pos).GetOccupyingEntity() as Crop != null)
                {
                    Crop cropCheck = tileManager.GetTileDataAt(pos).GetOccupyingEntity() as Crop;
                    //if the crop is ready to be harvested
                    if (cropCheck.CanBeHarvested())
                    {
                        extensionsMap.SetTile(pos, GetExtensionTile(TileColor.Yellow));
                    }//if the tile is not ready to be harvested and needs to be watered
                    else if (!cropCheck.CanBeHarvested() && !cropCheck.IsWatered())
                    {
                        extensionsMap.SetTile(pos, GetExtensionTile(TileColor.Blue));
                    }
                }
                else if(tileManager.GetTileDataAt(pos).GetOccupyingEntity() == null && currAction.GetName() == "Plant")
                {
                    extensionsMap.SetTile(pos, GetExtensionTile(TileColor.Green));
                }
                else
                {
                    extensionsMap.SetTile(pos, GetExtensionTile(TileColor.White));
                }
            }
        }
    }

    public void StartFeedTargeting()
    {
        // Reset any prior selection cleanly
        ResetData();

        validLocations = new List<Vector3Int>();
        List<Unit> units = GameManager.Instance.GetAllFriendlyUnits(); ;
        //get position for each friendly unit
        foreach (Unit unit in units)
        {
            if (!unit.isFed)
            {

                validLocations.Add(unit.GetGridPos());
                optionsMap.SetTile(unit.GetGridPos(), GetInfoTile(TileColor.White));
            }
        }

        PushState(InteractionState.FeedTargeting);
        isFeeding = true;
    }

    public void AskEndTurn(InputAction.CallbackContext context)
    {
        if (DialogueManager.IsConversationActive || !GameManager.Instance.isPlayerTurn)
        {
            return;
        }
        if (state == InteractionState.Selection)
        {
            AskEndTurn();
        }
    }

    public void AskEndTurn()
    {
        selectedEntity = null;
        //Add the logic for pulling up end turn
        PushState(InteractionState.ActionSelection);
        actionMenu.ShowDefaultMenu();
    }

    //This method tries to see if there is a Unit that can be fed at the current tile
    // If there is, open the feed picker and switch state
    private void TryFeedAtPosition(Vector3Int pos)
    {

        if(tileManager.GetTileDataAt(pos) == null)
        {
            return;
        }
        Entity entity = tileManager.GetTileDataAt(pos)?.GetOccupyingEntity();
        Unit unit = entity as Unit;

        if (unit == null || unit.isEnemy)
        {
            Debug.Log("No valid unit to feed here");
            return;
        }

        if (unit.GetIsFed())
        {
            SoundManager.Instance.PlaySound(feedManager.thumpSound);
            return;
        }
        //necessary boolean logic to get previous state or selection if there is no history
        state = stateHistory.Count > 0 ? stateHistory.Pop() : InteractionState.Selection;
        feedManager.OpenFeedUI(unit);

        //Show only the newly selected Unit using the selection tiles.
        optionsMap.ClearAllTiles();
        extensionsMap.ClearAllTiles();
        optionsMap.SetTile(unit.GetGridPos(), GetInfoTile(TileColor.White));

        // Push so undo can cancel out of the crop picker during feeding
        PushState(InteractionState.DecisionSelection);
    }

    //Additional picker step needed for the plant action after crop is picked from UI
    private void OnPlantSelected(int cropID)
    {
        //Remove this listner now that the plant has been picked
        cropPicker.OnCropSelected.RemoveListener(OnPlantSelected);
        //actually set the index to plant
        PlantAction plantAct = currAction as PlantAction;
        plantAct.SetCropID(cropID);
        //Debug.Log("PlantAct is linked with ID " + plantAct.cropID);
        //Get the availible targets from the current action, then switch to selecting one
        OnDecisionComplete();
    }

    private void OnSelectUnit(int unitID)
    {
        
        //actually set the index to plant
        SpawnUnitAction spawnAct = currAction as SpawnUnitAction;
        if (spawnAct != null)
        {
            spawnAct.SetSpawnUnit(unitID);
        }
        //Debug.Log("PlantAct is linked with ID " + plantAct.cropID);
        //Get the availible targets from the current action, then switch to selecting one
        OnDecisionComplete();
    }

    //Basically represents the UI intermediary state of needing to do something AFTER an action is selected
    //Currently necessary for planting and unit spawning due to needing to pick said crop or unit
    private void OnDecisionComplete()
    {
        if (stateHistory.Count > 0)
            stateHistory.Pop(); // remove DecisionSelection from history
        //With additional decision made, select where to spawn crop/unit
        GetTargets();
    }

    private void ShowUnitOptions(Entity entity)
    {
        Unit unit = entity as Unit;
        if (unit == null)
        {
            Debug.LogError("This isn't a unit, can't show options");
            return;
        }
        ShowUnitOptions(unit);
    }

    private void ShowFeedOptions(InputAction.CallbackContext context)
    {
        //need economy manager to be able to feed
        if (!EconomyManager.Instance.HasACrop()) return;

        // Already guards, but make this more explicit:
        if (state != InteractionState.Selection) return;
        StartFeedTargeting();
    }

    private void StopFeeding()
    {
        StopAction();
        isFeeding = false;
    }

    private void OnEnable()
    {
        input = new AgainstTheGrainInput();
        input.Enable();
        input.Gameplay.Select.performed += OnSelect;
        input.Gameplay.Feed.performed += ShowFeedOptions;
        input.Gameplay.Cancel.performed += UndoAction;
        input.Gameplay.AdvanceTurn.performed += AskEndTurn;

    }

    private void OnDisable()
    {
        input.Gameplay.Select.performed -= OnSelect;
        input.Gameplay.Feed.performed -= ShowFeedOptions;
        input.Gameplay.Cancel.performed -= UndoAction;
        input.Gameplay.AdvanceTurn.performed -= AskEndTurn;
        input.Disable();

        //prevent bugs on scene transitions
        FeedManager.OnFeedingComplete -= StopFeeding;
    }

    public void DisableInputs()
    {
        isInputOn = false;
        input.Disable();
    }

    public void EnableInputs()
    {
        isInputOn = true;
        input.Enable();
    }

    //checks to see if the provided list has the requested action
    public bool HasAction(List<EntityAction> actions, string name)
    {
        for(int i = 0; i < actions.Count; i++)
        {
            if(actions[i].GetName() == name)
            {
                return true;
            }
        }
        return false;
    }

    public void SetOptionsTiles(List<Vector3Int> valid, List<EntityAction> actions)
    {
        for(int i = 0; i < valid.Count; i++)
        {
            //make a temp tile so we can place it on the optionsMap
            Vector3Int paintLocation = new Vector3Int(valid[i].x, valid[i].y, 0);

            //store a variable for the tile to update based on what kind of tile it is
            TileBase tile = null;
            int zFlag = valid[i].z;

            if (zFlag == 0 || zFlag == 1)
            {
                tile = GetInfoTile(TileColor.White);
            }
            else if ((zFlag == 2 || zFlag == -2) && HasAction(actions, "Attack"))
            {
                tile = zFlag > 0 ? GetInfoTile(TileColor.Red) : GetExtensionTile(TileColor.Red);
            }
            else if ((zFlag == 3 || zFlag == -3) && HasAction(actions, "Water"))
            {
                tile = zFlag > 0 ? GetInfoTile(TileColor.Blue) : GetExtensionTile(TileColor.Blue);
            }
            else if ((zFlag == 4 || zFlag == -4) && HasAction(actions, "Harvest"))
            {

                tile = zFlag > 0 ? GetInfoTile(TileColor.Yellow) : GetExtensionTile(TileColor.Yellow);
            }
            else if ((zFlag == 5 || zFlag == -5))
            {
                //marks plants and units as Green to signify they are allies
                tile = zFlag > 0 ? GetInfoTile(TileColor.Green) : GetExtensionTile(TileColor.Green);
            }
            else
            {
                tile = GetExtensionTile(TileColor.White);
            }
            //draw the tile

            optionsMap.SetTile(paintLocation, tile);
        }
    }
}
