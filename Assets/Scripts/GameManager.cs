using NUnit.Framework;
using PixelCrushers.DialogueSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
public class GameManager : MonoBehaviour
{

    [Header("General Settings")]
    public static GameManager Instance;
    public TileManager tileManager;
    //Needed for pause logic
    private UnitInteractionSystem interactionSystem;
    public ActionMenu actionMenu;
    public AIManager aiManager;

    //Transforms for sorting
    public Transform friendlyUnits;
    public Transform enemyUnits;
    public Transform resourceContainer;
    public Transform structureContainter;
    CameraController camera;

    public Tilemap entityMap;
    private AgainstTheGrainInput input;

    public static event Action StartPlayerTurn;
    public static event Action StartEnemyTurn;
    //fires after animation finishes
    public static event Action EnemyAnimDone;
    public static event Action EndEnemyTurn;

    public GameObject pauseScreen;
    public GameObject winScreen;
    public GameObject loseScreen;
    
    public bool isPlayerTurn = true;
    public bool isPaused = false;

    public MapSize mapSize = MapSize.SMALL;

    private int currUnit = 0;

    public TurnChangeUI turnChangeUI;
    public bool isGameOver = false;
    public bool skipTurnAnimations = false;

    //Set which resources are availible to play with
    public List<int> resourceIDs = new List<int>();
    public List<int> entityIDs = new List<int>();


    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        tileManager = FindFirstObjectByType<TileManager>();
        interactionSystem = FindFirstObjectByType<UnitInteractionSystem>();
        aiManager = FindFirstObjectByType<AIManager>();
        camera = FindFirstObjectByType<CameraController>();

        if (turnChangeUI == null)
        {
            turnChangeUI = FindFirstObjectByType<TurnChangeUI>();
        }
    }

    public void Start()
    {
        //actionMenu = FindFirstObjectByType<ActionMenu>();
        isPlayerTurn = true;
        SpawnStartingUnits();
        interactionSystem.DisableInputs();
        
        //turn off the info panel
        interactionSystem.infoPanel.HidePanel();
        //clear the optionsmap
        interactionSystem.optionsMap.ClearAllTiles();

        SoundManager.Instance.PlayMusic(MusicTrack.BATTLE);
        PlayPlayerTurnAnimation();
    }

    public void BeginEnemyTurn()
    {
        isPlayerTurn = false;
        StartEnemyTurn?.Invoke();
        if (skipTurnAnimations)
        {
            StartCoroutine(EnemyTurnRoutine());
            return;
        }

        TurnChangeUI.TurnAnimationEnd.AddListener(OnEnemyTurnAnimDone);
        turnChangeUI.PlayEnemyTurn();
    }

    public void BeginPlayerTurn()
    {
        isPlayerTurn = true;
        interactionSystem.EnableInputs();

        // Call this whenever a turn/day ends
        //Debug.Log("Turn advanced!");
        List<Unit> friendlies = GetAllFriendlyUnits();
        List<Structure> structures = GetAllStructures();
        //Reactivate the friendly units
        foreach (Unit unit in friendlies)
        {
            unit.Activate();
        }
        //Reactivate Structures
        foreach (Structure structure in structures)
        {
            structure.Activate();
        }
        camera.FocusOnNextUnit();
        StartPlayerTurn?.Invoke();
    }

    public void SpawnStartingUnits()
    {
        //loop over a placeholder tilemap for placing units 
        int size = GameConstants.MapSizeToInt(mapSize);
        for (int i = -size / 2; i < size / 2; i++)
        {
            for (int j = -size / 2; j < size / 2; j++)
            {
                Vector3Int tilePos = new Vector3Int(i, j, 0);
                if (entityMap.HasTile(tilePos))
                {
                    TileBase temp = entityMap.GetTile(tilePos);
                    UnitInfo unitInfo = UnitDatabase.Instance.GetUnitInfoFromTile(temp);
                    ResourceInfo resourceInfo = ResourceDatabase.Instance.GetResourceInfoFromTile(temp);
                    StructureInfo structInfo = StructureDatabase.Instance.GetStructureInfoFromTile(temp);

                    if (unitInfo != null)
                    {
                        SpawnUnitOnTile(unitInfo, tilePos);
                    }
                    else if (resourceInfo != null)
                    {
                        SpawnresourceOnTile(resourceInfo, tilePos);
                    }
                    else if (structInfo != null)
                    {
                        SpawnStructureOnTile(structInfo, tilePos);
                    }

                    entityMap.SetTile(tilePos, null);
                }
            }
        }
    }

    public void SpawnUnitOnTile(UnitInfo unitInfo, Vector3Int pos)
    {
        TileData data = tileManager.GetTileDataAt(pos);
        if (data == null || data.HasOccupant())
        {
            //Dont place anything if somethings already here
            return;
        }
        //spawn the new object
        GameObject obj = Instantiate(unitInfo.prefab);
        if (unitInfo.isEnemy)
        {
            obj.transform.parent = enemyUnits;
        }
        else
        {
            obj.transform.parent = friendlyUnits;
        }
        //Update the position
        Unit unitRef  = obj.GetComponent<Unit>();
        if (unitRef != null) {

            tileManager.PlaceEntityOnTile(pos, unitRef);
            unitRef.UpdateTransform(pos);
        }

    }

    public void SpawnStructureOnTile(StructureInfo structInfo, Vector3Int pos)
    {
        TileData data = tileManager.GetTileDataAt(pos);
        if (data == null || data.HasOccupant())
        {
            //Dont place anything if somethings already here
            return;
        }
        //spawn the new object
        GameObject obj = Instantiate(structInfo.prefab);
        obj.transform.SetParent(structureContainter);
        //Update the position
        Structure structRef = obj.GetComponent<Structure>();
        if (structRef != null)
        {

            tileManager.PlaceEntityOnTile(pos, structRef);
            structRef.UpdateTransform(pos);
        }

    }

    public void SpawnResourceOnTile(ResourceInfo resourceInfo, Vector3Int pos)
    {
        TileData data = tileManager.GetTileDataAt(pos);
        if (data == null || data.HasOccupant() || data.type == TileType.Grass)
        {
            //Dont place anything if somethings already here
            return;
        }
        //spawn the new object
        GameObject obj = Instantiate(ResourceDatabase.Instance.resourcePrefab);
        if (obj != null)
        {
            obj.transform.parent = resourceContainer;
        }
        //Update the position
        Resource resourceRef = obj.GetComponent<Resource>();
        if (resourceRef != null)
        {
            //Assign the resource to mirror here
            resourceRef.Initialize(resourceInfo);
            //Assign to the tiledata
            tileManager.PlaceEntityOnTile(pos, resourceRef);
            resourceRef.UpdateTransform(pos);
        }

    }

    private IEnumerator EnemyTurnRoutine()
    {
        isPlayerTurn = false;
        interactionSystem.DisableInputs();

        yield return StartCoroutine(WaitForDialogue());

        //grabs units from team 2 (enemy)
        List<Unit> tempunits = Units(2);

        //sort based on units that are closest to opposing units first to prevent poor team execution
        //Basically, if you're already close, do your turn first before others so they make smarter decisions
        tempunits = tempunits.OrderBy(unit => GetDistanceToClosestUnit(unit)).ToList();


        foreach (Unit unit in tempunits)
        {
            //Focus on each unit with the camera
            camera.FocusOnTilePosition(unit.GetGridPos(),0.25f);
            yield return new WaitForSeconds(0.25f); // pause between each enemy
            yield return StartCoroutine(unit.DoTurn());
            yield return new WaitForSeconds(0.5f); // pause between each enemy
            if (isGameOver)
            {
                yield break;
            }
        }

        yield return new WaitForSeconds(0.5f);
        EndEnemyTurn?.Invoke();
        yield return StartCoroutine(WaitForDialogue());

        PlayPlayerTurnAnimation();

    }

    //Checks to see if a dialogue section has started to allow the dialogue to happen before continuing
    private IEnumerator WaitForDialogue()
    {
        // Wait a frame first to let the conversation actually start
        yield return null;
        while (DialogueManager.IsConversationActive)
        {
            yield return null;
        }
    }

    private void PlayPlayerTurnAnimation()
    {
        if (skipTurnAnimations)
        {
            BeginPlayerTurn();
            return;
        }

        TurnChangeUI.TurnAnimationEnd.AddListener(OnPlayerTurnAnimDone);
        turnChangeUI.PlayPlayerTurn();
    }


    //Uses the transform containers to return all friendly units
    public List<Unit> GetAllEntities(int team)
    {
        return new List<Unit>(Entities[team].GetComponentsInChildren<Entity>());
    }

    public List<Structure> GetAllStructures()
    {
        return new List<Structure>(structureContainter.GetComponentsInChildren<Structure>());
    }

    public List<Resource> GetAllResources()
    {
        return new List<Resource>(resourceContainer.GetComponentsInChildren<Resource>());
    }

    public List<int> GetLevelEntities()
    {
        //return the list of entities flagged for this level
        return entityIDs;
    }

    private void OnEnable()
    {
        input = new AgainstTheGrainInput();
        Entity.OnEntityDestroyed += CheckEndState;
        input.Gameplay.Pause.performed += TogglePause;
        input.Enable();

    }

    private void OnDisable()
    {
        Entity.OnEntityDestroyed -= CheckEndState;
        input.Gameplay.Pause.performed -= TogglePause;
        input.Disable();
    }

    private void OnEnemyTurnAnimDone()
    {
        TurnChangeUI.TurnAnimationEnd.RemoveListener(OnEnemyTurnAnimDone);
        // Now safe to begin player turn AFTER animation finishes
        EnemyAnimDone?.Invoke();
        StartCoroutine(EnemyTurnRoutine());
    }

    private void OnPlayerTurnAnimDone()
    {
        TurnChangeUI.TurnAnimationEnd.RemoveListener(OnPlayerTurnAnimDone);
        BeginPlayerTurn(); // logic runs only after animation ends
    }

    public void TogglePause(InputAction.CallbackContext context)
    {
        if (!isPaused)
        {
            PauseGame();
        }
        else
        {
            UnPauseGame();
        }
    }

    public bool IsFriendlyDefeated()
    {
        //determine whether or not the friendly units can still play
        //Determine if there's any units that can still attack
        List<Unit> units = GetAllFriendlyUnits();
        //basic check, is there any friendly units?
        if (units.Count == 0) return true;

        //Deteremine whether or not theres a unit that can attack
        //foreach (Unit unit in units)
        //{
        //    if (unit.CanAttack())
        //    {
        //        return false;
        //    }
        //}
        return false;
    }

    public bool IsEnemyDefeated()
    {
        //determine whether or not the friendly units can still play
        //Determine if there's any units that can still attack
        List<Unit> units = GetAllEnemyUnits();
        //basic check, is there any friendly units?
        if (units.Count == 0) return true;

        //Deteremine whether or not theres a unit that can attack
        foreach (Unit unit in units)
        {
            if (unit.CanAttack())
            {
                return false;
            }
        }
        return true;
    }

    int GetDistanceToClosestUnit(Unit enemy)
    {

        List<Unit> friendlies = GetAllFriendlyUnits();

        int bestDist = int.MaxValue;
        //Find the closest unit
        foreach (Unit friendly in friendlies)
        {
            int dist = Mathf.Abs(enemy.GetGridPos().x - friendly.GetGridPos().x)
                     + Mathf.Abs(enemy.GetGridPos().y - friendly.GetGridPos().y);
            if (dist < bestDist)
            {
                bestDist = dist;
            }
        }

        return bestDist;
    }

    public void CheckEndState()
    {
        if (IsEnemyDefeated())
        {
            isGameOver = true;
            Debug.Log("You win!");
            SoundManager.Instance.PlayMusic(MusicTrack.VICTORY);

            StartCoroutine(ShowWinScreen());
        }
        else if (IsFriendlyDefeated())
        {
            isGameOver = true;
            Debug.Log("You Lose!");
            SoundManager.Instance.PlayMusic(MusicTrack.GAME_OVER);
            StartCoroutine(GameOver());
        }
    }

    public void CheckEndState(Entity entity)
    {
        CheckEndState();
    }

    //Display lose Screen
    public IEnumerator GameOver()
    {
        //probably do other things later but just want to test functionality
        yield return new WaitForSeconds(0.5f);
        loseScreen.SetActive(true);
    }

    public IEnumerator ShowWinScreen()
    {
        yield return new WaitForSeconds(0.5f);
        winScreen.SetActive(true);
    }

    public void PauseGame()
    {
        isPaused = true;
        interactionSystem.DisableInputs();
        actionMenu.TurnOffInput();
        pauseScreen.SetActive(true);
    }

    public void UnPauseGame()
    {
        isPaused = false;
        interactionSystem.EnableInputs();
        actionMenu.TurnOnInput();
        pauseScreen.SetActive(false);
    }

    public void HideAllUIScreens()
    {
        loseScreen.SetActive(false);
        winScreen.SetActive(false);
        pauseScreen.SetActive(false);
    }

    //Used to determine next unit to seek out
    public Unit GetNextActiveUnit()
    {
        List<Unit> units = GetAllFriendlyUnits();

        if (units.Count == 0) return null;

        int attempts = 0;

        while (attempts < units.Count)
        {
            //Wrap around once the current unit check goes beyond the count
            //Logic is necessary to progress to next unit if multiple active
            currUnit = (currUnit + 1) % units.Count;

            Unit unit = units[currUnit];

            // Optional: skip units that can't move / are inactive
            if (!unit.IsActive()) // <-- replace with your actual condition if different
            {
                attempts++;
                continue;
            }

            return unit;
        }

        Debug.Log("No active units found.");
        return null;
    }

    public List<int> GetResourceIDs()
    {
        return resourceIDs;
    }


}
