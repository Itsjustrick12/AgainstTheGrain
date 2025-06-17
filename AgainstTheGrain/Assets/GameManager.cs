using System;
using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

//Contains the main logic for controlling turn order and larger scale turn mechanics
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    CursorToTilemap inputManager;
    CropManager cropManager;

    //containers for unit objects
    public GameObject playerUnits;
    public GameObject enemyUnits;

    public GameObject[] unitPrefabs;
    public TileBase[] unitTiles;

    public Tilemap unitMap;

    public bool isPlayerTurn = true;
    public bool gameOver = false;

    //used for controlling the animal units
    public int feedCount = 0;

    public GameObject pauseMenu;
    public GameObject victoryScreen;
    public GameObject defeatScreen;
    public bool paused = false;

    public ATGInput inputActions;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        inputActions = new ATGInput();
        Time.timeScale = 1;
        
    }

    public void Start()
    {
        pauseMenu.SetActive(false);
        SpawnStartingUnits();
        inputManager = FindFirstObjectByType<CursorToTilemap>();
        cropManager = FindFirstObjectByType<CropManager>();
        StartPlayerTurn();
    }

    private IEnumerator StartComputerTurn()
    {

        isPlayerTurn = false;

        //clear the info map to hide any player input info
        inputManager.clearTiles();
        inputManager.DeselectLast();

        

        //loop through and allow each enemy to process it's turn
        for (int i = 0; i < enemyUnits.transform.childCount; i++)
        {
            yield return CheckEndState();
            EnemyUnit temp = enemyUnits.transform.GetChild(i).GetComponent<EnemyUnit>();
            temp.Refresh();

            //wait for selected enemy to finish its turn
            
            yield return temp.ProcessTurn();
           
        }
        StartPlayerTurn();
        

    }

    public bool CheckEndState()
    {
        if (CheckNoEnemyUnits())
        {
            gameOver = true;
            ShowVictoryScreen();
            return true;
        }
        else if (CheckNoPlayerUnits() || CheckOnlyUnfedAnimals()) {
            gameOver = true;
            ShowDefeatScreen();
            return true;
        }
        return false;
    }

    //Refresh all units and relinquish control to the player
    public void StartPlayerTurn()
    {

        CheckEndState();

        isPlayerTurn = true;
        Debug.Log("Call to update crops");
        cropManager.UpdateCrops();
        
        //refresh every player unit
        foreach (Transform child in playerUnits.transform)
        {
            //get the gameobject first, then a reference to the unit class
            GameObject childObj = child.gameObject;

            //Refresh every unit
            Unit childUnit = childObj.GetComponent<Unit>();

            if (childUnit != false)
            {
                childUnit.Refresh();
            }
        }
    }

    public bool CheckNoPlayerUnits()
    {
        if (playerUnits.transform.childCount <= 0)
        {
            ShowDefeatScreen();
            return true;
        }
        return false;
    }

    private bool CheckOnlyUnfedAnimals()
    {
        foreach (Transform child in playerUnits.transform)
        {
            //get the gameobject first, then a reference to the unit class
            GameObject childObj = child.gameObject;

            //Refresh every unit
            Unit childUnit = childObj.GetComponent<Unit>();

            if (childUnit != null)
            {
                if (childUnit.isUseableUnit())
                {
                    return false;
                }
            }
        }
        ShowDefeatScreen();
        return true;
    }

    public bool CheckNoEnemyUnits()
    {
        if (enemyUnits.transform.childCount <= 0)
        {
            return true;
        }
        return false;
    }

    public int GetNumEnemies()
    {
        return enemyUnits.transform.childCount;
    }

    public int GetNumActivePlayers()
    {
        int count = 0;
        foreach (Transform child in playerUnits.transform)
        {
            //get the gameobject first, then a reference to the unit class
            GameObject childObj = child.gameObject;

            //Refresh every unit
            Unit childUnit = childObj.GetComponent<Unit>();

            if (childUnit != false)
            {
                if (childUnit.isUseableUnit())
                {
                    count += 1; 
                }
            }
        }
        return count;
    }

    //See if the player no longer has anything to do
    public void CheckAutoEndPlayerTurn()
    {
        //refresh every player unit
        foreach (Transform child in playerUnits.transform)
        {
            //get the gameobject first, then a reference to the unit class
            GameObject childObj = child.gameObject;

            Unit childUnit = childObj.GetComponent<Unit>();

            if (childUnit != false)
            {
                //if any unit is still active, dont end the turn automatically
                if (childUnit.active && (childUnit.isUseableUnit()))
                {
                    Debug.Log("Some units have not been utilized, continue player turn");
                    return;
                }
            }
        }
        Debug.Log("All Units have been utilized! Starting enemy turn");


        StartCoroutine(StartComputerTurn());
        
    }

    public void SpawnUnitAtPos(GameObject unitPrefab, Vector3Int pos)
    {

        TileData temp = TilemapManager.instance.getTileData(pos);
        GameObject newUnit = Instantiate<GameObject>(unitPrefab);
        newUnit.transform.position = pos + new Vector3(0.5f, 0.5f, 0);

        //sets the unit to be in the container in the inspector, important for checking all units

        if (!newUnit.GetComponent<Unit>().isEnemy)
        {

            newUnit.transform.SetParent(GameObject.Find("PlayerUnits").transform);
        }
        else
        {
            newUnit.transform.SetParent(GameObject.Find("EnemyUnits").transform);
        }
        temp.SetOccupant(newUnit.GetComponent<Unit>());

        newUnit.GetComponent<Unit>().tile = temp;
    }

    private void SpawnStartingUnits()
    {
        Vector3Int pos;
        for (int i = -16; i < 16; i++)
        {
            for (int j = -16; j < 16; j++)
            {
                pos = new Vector3Int(i, j, 0);
                if (unitMap.HasTile(pos))
                {

                    TileBase tile = unitMap.GetTile(pos);
                    if (tile != null)
                    {
                        for (int k = 0; k < unitTiles.Length; k++)
                        {
                            if (unitTiles[k] == tile)
                            {
                                //spawn the corresponding tile
                                SpawnUnitAtPos(unitPrefabs[k], pos);
                                //remove visual tile
                                unitMap.SetTile(pos, null);
                                //exit the loop
                                break;
                            }
                        }
                    }
                    
                }
            }
        }
    }

    public bool CanFeedAnimal(int feedCost)
    {
        if (feedCost <= feedCount)
        {
            return true;
        }
        return false;
    }

    public void AddFeed(int amt)
    {
        feedCount += amt;
    }

    public void UseFeed(int amt)
    {
        feedCount -= amt;
    }

    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0;
        paused = true;
    }

    public void UnPauseGame()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1;
        paused = false;
    }

    public void ShowDefeatScreen()
    {
        StopAllCoroutines();
        defeatScreen.SetActive(true);
        victoryScreen.SetActive(false);
        pauseMenu.SetActive(false);
        Time.timeScale = 0;
        paused = true;
    }

    public void ShowVictoryScreen()
    {
        StopAllCoroutines();
        defeatScreen.SetActive(false);
        victoryScreen.SetActive(true);
        pauseMenu.SetActive(false);
        Time.timeScale = 0;
        paused = true;
    }

    private void TogglePause(InputAction.CallbackContext a)
    {
        if (!gameOver)
        {

            if (paused)
            {
                UnPauseGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void EndPlayerTurn(InputAction.CallbackContext a)
    {
        isPlayerTurn = false;
        foreach (Transform child in playerUnits.transform)
        {
            //get the gameobject first, then a reference to the unit class
            GameObject childObj = child.gameObject;

            //deactivate every unit
            Unit childUnit = childObj.GetComponent<Unit>();

            if (childUnit != false)
            {
                childUnit.Deactivate();
            }
        }
        StartCoroutine(StartComputerTurn());
    }

    private void OnEnable()
    {

        inputActions.Enable();
        inputActions.GameMappings.Pause.performed += TogglePause;
        inputActions.GameMappings.EndTurn.performed += EndPlayerTurn;
    }

    private void OnDisable()
    {
        inputActions.GameMappings.Pause.performed -= TogglePause;
        inputActions.GameMappings.EndTurn.performed -= EndPlayerTurn;
        inputActions.Disable();
    }
}
