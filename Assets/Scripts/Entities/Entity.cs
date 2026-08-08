using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

//This is the base script that is used for all things that can occupy tiles in the game
//This includes units, obstacles, interactable objects, and crops

public enum EntityType
{
    Animal,
    Crop,
    Enemy,
    Farmer,
    Structure,
    None
}

[RequireComponent(typeof(SpriteRenderer))]
public class Entity : MonoBehaviour, IBuffable
{
    //all other class storages
    protected GameManager gameManager;
    protected AIManager aiManager;
    protected TileHelper tileHelper;
    protected SpriteRenderer sprite;
    //Stores the location of where this entity actually is
    private Vector3Int gridPos;

    protected bool isActive = true;
    private bool isInitialized = false;

    //near constant color used for dimming entities when they are deactivated
    public static readonly Color DimColor = new Color(0.4f, 0.4f, 0.4f);

    [Header("Stats")]
    //stores the entity's max hitpoints
    [SerializeField] protected int maxHealth = 10;
    //stores the entity's type
    [SerializeField] protected EntityType type = EntityType.None;
    //stores the entity's hitpoints
    [SerializeField] protected int currentHealth = 10;
    //Determines where or not something can pathfind through the tile this entity is on
    [SerializeField] private bool isObstacle;
    //Determines if this entity can be clicked on or affected in any way
    [SerializeField] private bool isInteractable;
    private Vector3 offset = new Vector3(0.5f, 0.5f, 0);
    //holds the animator
    [SerializeField] public Animator animator;
    [SerializeField] public int team = 0;

    //Hidden logic for determining what a unit is able to do, define by the unit database
    protected List<EntityAction> actions = new();

    public static event Action<Entity, Vector3Int> OnEntityDestroyed;

    //For managing buffs
    protected List<Buff> activeBuffs = new List<Buff>();

    public virtual void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        tileHelper = FindFirstObjectByType<TileHelper>();
    }

    public void InitializeActions(List<EntityAction> newActions)
    {
        actions = newActions;
    }

    public List<EntityAction> GetAvailableActions()
    {
        //Return all the actions that are currently possible given the Unit's information (and generally position)
        return actions.Where(action => action.IsPossible(this)).ToList();
    }

    public List<EntityAction> GetAllActions()
    {
        //Return all the actions that are currently possible given the Unit's information (and generally position)
        return actions;
    }

    public void SetGridPos(Vector3Int pos)
    {
        gridPos = pos;
        transform.position = pos+offset;
    }

    public Vector3Int GetGridPos()
    {
        return gridPos;
    }
    
    public void SetCurrentHealth(int healthValue)
    { 
        if(healthValue > maxHealth)
        {
            healthValue = maxHealth;
        }
        currentHealth = healthValue;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetHealth()
    {
        return currentHealth;
    }

    public void SetHealth(int healthValue)
    { 
        if(healthValue > maxHealth)
        {
            healthValue = maxHealth;
        }
        currentHealth = healthValue;
    }

    public bool GetInitialized()
    {
        return Initialized;
    }

    public void SetMaxHealth(int healthValue)
    { 
        if(healthValue > 0)
        {
            healthValue = maxHealth;
        }
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsInteractable()
    {
        return isInteractable;
    }

    public void SetIsInteractable(bool temp)
    {
        isInteractable = temp;
    }

    public bool IsObstacle()
    {
        return isObstacle;
    }

    public void SetIsObstacle(bool obstacle)
    {
        isObstacle = obstacle;
    }
    public void HideSprite()
    {
        sprite.enabled = false;
    }
    public void ShowSprite()
    {
        sprite.enabled = true;
    }

    public Sprite GetSprite()
    {
        return sprite.sprite;
    }

    public void SetSprite(Sprite temp)
    {
        sprite.sprite = temp;
    }

    public int GetTeam()
    {
        return team;
    }

    public void SetTeam(int i)
    {
        if(i >= 0)
        {
            team = i;
        }
        else
        {
            team = 0;
        }
    }

    public virtual EntityType GetType()
    {
        return type;
    }

    public void SetType(EntityType temp)
    {
        type = temp;
    }

    public virtual void Die()
    {

        //Remove from hierarchy (needed for the check of how many units there are
        transform.SetParent(null);

        //Now game state is accurate
        OnEntityDestroyed(this, gridPos);

        //Destroy entity after the check to allow it to happen
        Destroy(gameObject);

    }

    public void UpdateTransform(Vector3Int pos)
    {
        //Update the Transform to refelct the gameobject visually
        this.gameObject.transform.position = pos + (new Vector3(0.5f, 0.5f, 0f));
    }

    public virtual void DestroyEntity()
    {
        //Remove this entity from the field by updating the tile data it belongs to
        TileManager tM = FindFirstObjectByType<TileManager>();
        tM.GetTileDataAt(GetGridPos()).occupyingEntity = null;
        Destroy(this.gameObject);
    
    }

    public virtual void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        aiManager = FindFirstObjectByType<AIManager>();
        Initialize();
    }

    //Used to update stats based on database
    public virtual void Initialize()
    {
        isInitialized = true;
    }

    public bool IsActive()
    {
        return isActive;
    }

    public bool IsSameTeam(Entity entity)
    {
        return team == entity.GetTeam();
    }

    public virtual void Activate()
    {
        sprite.color = Color.white;
        isActive = true;
    }

    public void AddBuff(Buff buff)
    {
        activeBuffs.Add(buff);
        buff.Apply(this);
    }

    public void ClearBuffs()
    {
        activeBuffs.Clear();
    }

    public virtual void Deactivate()
    {
        Deactivate(0.7f);
    }

    public virtual void Deactivate(float tim)
    {
        isActive = false;
        StartCoroutine(DeactivateHelper(tim));
    }

    private IEnumerator DeactivateHelper(float tim)
    {
        yield return new WaitForSeconds(tim);
        sprite.color = DimColor;
    }

    //Is called by the buff class itself who manages the duration of itself
    public void RemoveBuff(Buff buff)
    {
        if (!activeBuffs.Contains(buff))
        {
            //if the buff isn't here, dont do anything
            return;
        }
        activeBuffs.Remove(buff);
    }

    public virtual void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }
}
