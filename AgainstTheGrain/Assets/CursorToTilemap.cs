
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public enum ActionType
{
    None,
    Attack,
    Move,
    Wait,
    Plant,
    Water,
    Farm
}

public class CursorToTilemap : MonoBehaviour
{
    [SerializeField] TilemapManager manager;

    public Tilemap placeholder;
    public Tilemap display;

    public Tilemap hover;
    public Tile hoverTile;

    private Vector3Int currentTile;
    private List<Tuple<Vector3Int, bool>> modifiedTiles;

    SpriteRenderer hoverSprite;
    public Transform hoverTransform;
    public Vector3Int fromTile;

    private Unit selectedUnit;
    public bool holdingUnit = false;

    //[SerializeField] public GameObject chickenPrefab;

    GameManager GM;
    CropManager cropManager;

    public PlayerOptions optionsMenu;
    public bool makingDecision = false;
    ActionType currAction = ActionType.None;


    //for input modularity
    ATGInput inputActions;

    //for displaying unit info
    private TileInfoUI infoUI;

    public void Start()
    {
        GM = GameManager.instance;
        cropManager = FindAnyObjectByType<CropManager>();
        manager = TilemapManager.instance;

        hoverSprite = GetComponentInChildren<SpriteRenderer>();
        hoverTransform = GetComponentInChildren<Transform>();

        placeholder = manager.placeholderMap;
        display = manager.displayMap;

        modifiedTiles = new List<Tuple<Vector3Int, bool>>();
        infoUI = FindFirstObjectByType<TileInfoUI>();
    }



    private void Update()
    {
        if (makingDecision)
        {
            return;
        }

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int mousePos = Simplify(mousePosition);
        Vector3Int tilePos = placeholder.WorldToCell(mousePosition);

        //Highlight the tile the mouse is currently over
        if (tilePos != currentTile && GM.isPlayerTurn && manager.getTileData(tilePos) != null)
        {
            //take highlight off previous tile
            DeselectLast();
            //select new tile
            currentTile = tilePos;

            if ((!holdingUnit && currAction == ActionType.None) ||
                //for finding enemy tiles
                (currAction == ActionType.Attack && CanMoveToTile(tilePos)) ||
                //for finding plant tiles
                ((currAction == ActionType.Farm || currAction == ActionType.Water || currAction == ActionType.Plant) && CanMoveToTile(tilePos)) ||

                (currAction == ActionType.None && CanMoveToTile(tilePos)))
            {
                SelectCurrent(tilePos);
                //offset for display map
                hoverTransform.position = tilePos + new Vector3(0.5f, 0.90f, 0f);

            }

            //display the unit that is being hovered over
            Unit unitCheck = manager.getTileData(tilePos).occupant;
            CropObject cropCheck = manager.getTileData(tilePos).crop;
            if (unitCheck != null)
            {
                infoUI.DisplayUnitInfo(unitCheck);
            }
            else if (cropCheck != null)
            {
                infoUI.DisplayCropInfo(cropCheck);
            }
            else
            {
                infoUI.HideInfo();
            }
        }



        //get the tile at the current mouse position
        //TileData unitCheck = manager.getTileData(tilePos);

        //if (Input.GetKeyDown(KeyCode.Mouse0))
        //{
        //    TileData temp = manager.getTileData(tilePos);
        //    if (temp != null) {
        //        Debug.Log("This is tile: [" + tilePos.x + "," + tilePos.y + "]");
        //        temp.PrintDetails();
        //    }
        //    else
        //    {
        //        Debug.Log("No Tile Here");            }
        //}

        //show the movement locations




        //if (Input.GetKeyDown(KeyCode.Mouse1))
        //{
        //    if (holdingUnit)
        //    {
        //        if (CanMoveToTile(tilePos))
        //        {
        //            Debug.Log("Can move to tile");
        //        }
        //        else
        //        {
        //            Debug.Log("Cannot go there");
        //        }
        //        return;
        //    }

        //    TileData temp = manager.getTileData(tilePos);
        //    if (temp.GetOccupant() == null)
        //    {
        //        GameManager.instance.SpawnUnitAtPos(chickenPrefab, tilePos);

        //    }
        //}

        //if (Input.GetKeyDown(KeyCode.Alpha5))
        //{


        //    manager.ToggleUnwalkable(tilePos);
        //}

        //if (Input.GetKey(KeyCode.Alpha1))
        //{
        //    manager.SetTile(mousePos, manager.dirtTile);
        //}
        //else if (Input.GetKey(KeyCode.Alpha2))
        //{
        //    manager.SetTile(mousePos, manager.grassTile);
        //}
        //else if (Input.GetKey(KeyCode.Alpha3))
        //{
        //    manager.SetTile(mousePos, manager.soilTile);
        //}
        //else if (Input.GetKeyDown(KeyCode.Alpha4))
        //{
        //    manager.ToggleUnwalkable(mousePos);
        //}
        //if (Input.GetKeyDown(KeyCode.F))
        //{
        //    TilemapManager.instance.NearestPlayerUnit(tilePos);
        //}

    }

    //chopped up from initial movement logic, split into two steps
    //used when object is selected
    //bool represents successful placement
    public bool PlaceDown()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int mousePos = Simplify(mousePosition);
        Vector3Int tilePos = placeholder.WorldToCell(mousePosition);

        //get the tile at the current mouse position
        TileData unitCheck = manager.getTileData(tilePos);
        //if a unit is selected
        //can place on self or empty spot
        if ((unitCheck.occupant != null && tilePos == selectedUnit.GetGridPosition())
            || (unitCheck.occupant == null && selectedUnit != null && CanMoveToTile(tilePos)))
        {

            selectedUnit.moveToTile(tilePos);
            HideHoverUnit(selectedUnit);

            AnimalUnit animal = selectedUnit.GetComponent<AnimalUnit>();
            //feed the animal by paying it's cost if it isnt fed yet
            if (animal != null && !animal.isFed)
            {
                GM.UseFeed(animal.feedNeed);
                animal.isFed = true;
            }

            clearTiles();
            return true;
        }

        return false;

    }

    public void PickUp()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int mousePos = Simplify(mousePosition);
        Vector3Int tilePos = placeholder.WorldToCell(mousePosition);

        //get the tile at the current mouse position
        TileData unitCheck = manager.getTileData(tilePos);
        //if a unit is selected
        if (unitCheck != null && unitCheck.occupant != null)
        {
            if (unitCheck.occupant.active && !unitCheck.occupant.isEnemy)
            {

                selectedUnit.Refresh();
                selectedUnit.sprite.color = new Color(1, 1, 1, 0);

                fromTile = tilePos;

                ShowHoverUnit(selectedUnit);
                optionsMenu.HideOptions();
            }
        }

    }

    public void SelectUnit(Vector3Int location)
    {

        TileData unitCheck = manager.getTileData(location);
        if (unitCheck != null)
        {
            Unit tempUnit = unitCheck.occupant;
            if (GM.isPlayerTurn)
            {
                selectedUnit = unitCheck.occupant;
                if (tempUnit != null)
                {

                    if (modifiedTiles.Count > 0 && !holdingUnit)
                    {
                        clearTiles();
                    }
                    if (tempUnit.active && !holdingUnit || (tempUnit.isEnemy))
                    {
                        modifiedTiles = manager.ShowMoveableTiles(location, tempUnit.getEffectiveMoveAmt());
                    }
                }
            }
        }
    }

    public void HandleSelect(InputAction.CallbackContext a)
    {

        Vector3Int mouseTile = getMouseTile();
        if (selectedUnit == null && !holdingUnit)
        {

            SelectUnit(getMouseTile());

            //if the unit is an unfed animal, dont do anything
            if (selectedUnit != null)
            {
                AnimalUnit animal = selectedUnit.GetComponent<AnimalUnit>();
                if (animal != null && !animal.isFed && !GM.CanFeedAnimal(animal.feedNeed))
                {
                    //dont do anything, you dont have feed for the animal
                    selectedUnit = null;
                    return;
                }
            }
            PickUp();
        }
        else if (currAction == ActionType.Attack && CanMoveToTile(mouseTile))
        {
            //attack the enemy at the spot
            Unit enemy = manager.getTileData(mouseTile).occupant;
            if (enemy != null)
            {
                //attack the enemy based on the selected unit
                enemy.TakeDamage(selectedUnit.getEffectiveDamage());
            }

            if (enemy != null && enemy.GetHealth() > 0)
            {
                infoUI.DisplayUnitInfo(enemy);
            }
            else
            {
                infoUI.HideInfo();
            }

            //check if the last enemy was killed for victory
            GM.CheckEndState();

        }
        else if (currAction == ActionType.Water && CanMoveToTile(mouseTile))
        {
            CropObject cropObj = manager.getTileData(mouseTile).crop;
            cropObj.Water();

        }
        else if (currAction == ActionType.Farm && CanMoveToTile(mouseTile))
        {
            CropObject cropObj = manager.getTileData(mouseTile).crop;
            cropObj.Harvest();

        }
        else if (currAction == ActionType.Plant && CanMoveToTile(mouseTile))
        {
            //plant a new crop
            cropManager.PlantCropOnTile(mouseTile);
        }
        else if (holdingUnit)
        {
            Unit temp = selectedUnit;

            if (!PlaceDown())
            {
                HideHoverUnit(selectedUnit);
                //deselect the unit
                Deselect();
            }
            else
            {
                //display the options panel and have the player decide their action
                optionsMenu.ShowOptions(temp.GetGridPosition(), temp);
            }
        }



        //after action is taken, deactivate the unit
        if (currAction != ActionType.None && CanMoveToTile(mouseTile))
        {
            //clear the highlight tiles
            clearTiles();

            //deselect the unit
            selectedUnit.Deactivate();
            selectedUnit = null;

            //end the turn
            currAction = ActionType.None;
        }
        GM.CheckEndState();
        GM.CheckAutoEndPlayerTurn();


    }

    public void HighlightNearbyEnemies()
    {
        modifiedTiles = manager.GetEnemiesNearby(selectedUnit.GetGridPosition(), true);
    }

    public void DetectFarmableTiles()
    {
        modifiedTiles = manager.GetCropsNearby(selectedUnit.GetGridPosition(), true);
    }

    public void DetectDirtTiles()
    {
        modifiedTiles = manager.GetDirtNearby(selectedUnit.GetGridPosition(), true);
    }

    //called once the player detemines what they want to do
    public void HandleAction(ActionType action)
    {
        makingDecision = false;

        if (action == ActionType.Attack)
        {
            HighlightNearbyEnemies();
            currAction = ActionType.Attack;
        }
        //limit selection to Crop Tiles
        else if (action == ActionType.Water)
        {
            DetectFarmableTiles();
            currAction = action;
        }
        else if (action == ActionType.Farm)
        {
            modifiedTiles = manager.GetHarvestableCropsNearby(selectedUnit.GetGridPosition(), true);
            currAction = ActionType.Farm;
        }
        else if (action == ActionType.Plant)
        {
            DetectDirtTiles();
            currAction = action;
        }
        else if (action == ActionType.Wait)
        {
            if (selectedUnit != null)
            {

                selectedUnit.Deactivate();
                GameManager.instance.CheckAutoEndPlayerTurn();
            }
            selectedUnit = null;
        }




    }

    public void Deselect()
    {
        clearTiles();

        selectedUnit = null;
    }

    public void HandleDeselect(InputAction.CallbackContext a)
    {
        if (currAction == ActionType.None)
        {

            if (holdingUnit)
            {
                HideHoverUnit(selectedUnit);
            }
            Deselect();
        }
        infoUI.HideInfo();


    }

    public Vector3Int getMouseTile()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int mousePos = Simplify(mousePosition);
        Vector3Int tilePos = placeholder.WorldToCell(mousePosition);
        return tilePos;
    }

    public void GetPlayerAction()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int mousePos = Simplify(mousePosition);
        Vector3Int tilePos = placeholder.WorldToCell(mousePosition);
        TileData unitCheck = manager.getTileData(tilePos);
        Unit temp = unitCheck.occupant;
        if (temp != null && temp.active && !temp.isEnemy)
        {
            //get the tile at the current mouse position
            optionsMenu.ShowOptions(tilePos, temp);
        }


    }


    //Needed for restricting movement of unit to only availible tiles
    private bool CanMoveToTile(Vector3Int tile)
    {
        
        Tuple<Vector3Int, bool> temp = new Tuple<Vector3Int, bool>(tile, true);
        return modifiedTiles.Contains(temp);
    }

    private Vector3Int Simplify(Vector3 pos)
    {
        int xPos = Mathf.FloorToInt(pos.x);
        int yPos = Mathf.FloorToInt(pos.y);
        return new(xPos, yPos, 0);
    }

    public void SelectCurrent(Vector3Int curr)
    {
        if (placeholder.HasTile(curr))
        {
            hover.SetTile(curr, hoverTile);
            Unit temp = manager.getTileData(curr).occupant;
            
        }
    }

    public void DeselectLast()
    {
        if (placeholder.HasTile(currentTile))
        {
            hover.SetTile(currentTile, null);
            optionsMenu.HideOptions();
            
        }
    }

    public void clearTiles()
    {

        foreach (Tuple<Vector3Int, bool> tile in modifiedTiles)
        {
            manager.infoMap.SetTile(tile.Item1, null);
        }
    }

    private void ShowHoverUnit(Unit unit)
    {
        hoverSprite.sprite = unit.GetComponent<SpriteRenderer>().sprite;
        holdingUnit = true;
    }

    private void HideHoverUnit(Unit unit)
    {
        unit.sprite.color = Color.white;
        hoverSprite.sprite = null;
        holdingUnit = false;
    }

    private void OnEnable()
    {
        inputActions = new ATGInput();
        inputActions.Enable();
        inputActions.GameMappings.Select.performed += HandleSelect;
        inputActions.GameMappings.Deselect.performed += HandleDeselect;
    }

    private void OnDisable()
    {
        inputActions.GameMappings.Select.performed -= HandleSelect;
        inputActions.GameMappings.Deselect.performed -= HandleDeselect;
        inputActions.Disable();
    }
}
