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
    protected TileManager tileManager;
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
    //stores the entity's type
    [SerializeField] protected EntityType type = EntityType.None;
    //stores the entity's max hitpoints
    [SerializeField] protected int maxHealth = 10;
    //stores the entity's hitpoints
    [SerializeField] protected int currentHealth = 10;
    //the damage of the entity's attacks(0 if it can't)
    [SerializeField] protected int strength = 0;
    //the range of it's actions
    [SerializeField] protected int attackRange = 1;
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
        tileManager = FindFirstObjectByType<TileManager>();
        tileHelper = FindFirstObjectByType<TileHelper>();
    }

    public void InitializeActions(List<EntityAction> newActions)
    {
        actions = newActions;
    }

    //This is dumb change this later
    public bool CanAttack()
    {
        foreach (var action in actions)
        {
            if (action.GetName() == "Attack")
            {
                return true;
            }
        }
        return false;
    }

    public int GetAttackRange()
    {
        return attackRange;
    }

    public void SetAttackRange(int temp)
    {
        if(temp > 0)
        {
            attackRange = temp;
        }
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

    public virtual EntityType GetEntityType()
    {
        return type;
    }

    public void SetEntityType(EntityType temp)
    {
        type = temp;
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
        return isInitialized;
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

    public int GetStrength()
    {
        if (activeBuffs.Count == 0)
            return strength;
        
        int baseIncrease = 0;
        float multiplier = 1;

        
        //loop through all buffs to check for strengh buffs
        foreach (Buff buff in activeBuffs)
        {
            //check for strength buffs
            StrengthBuff sBuff = buff as StrengthBuff;
            if (sBuff != null)
            {
                baseIncrease += sBuff.baseIncrease;
                multiplier *= sBuff.multiplier;
            }
        }

        //return the calculated stat after base increases and multiplier
        return (int)((strength + baseIncrease) * multiplier);
    }

    public void SetStrength(int strengthValue)
    {
        
        strength = strengthValue;
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

    //this entity takes "damage" damage (pre buffs)
    public void TakeDamage(int damage)
    {
        SoundManager.Instance.PlayEntitySound(this, SoundType.HURT);

        //if there are any buffs to account for
        if (activeBuffs.Count > 0)
        {
            //calculate buff defense if any
            int baseIncrease = 0;
            float multiplier = 1;
            foreach (Buff buff in activeBuffs)
            {
                //check for strength buffs
                DefenseBuff dBuff = buff as DefenseBuff;
                if (dBuff != null)
                {
                    baseIncrease += dBuff.baseIncrease;
                    multiplier *= dBuff.multiplier;
                }
            }

            //calculate reduction
            damage = Mathf.Max(0, (int)(damage - (baseIncrease * multiplier)));
        }

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }

    }

    //shows the number for damage, at position with x difference between entities
    public void ShowNumber(int damage, Vector3Int position, int x)
    {
        Debug.Log("showNumber");
        GameObject prefab = Resources.Load<GameObject>("FloatingNum");

        if (prefab == null)
        {
            Debug.LogError("prefab not found");
            return;
        }

        GameObject canvas = GameObject.Find("HUD");
        GameObject obj = Instantiate(prefab, canvas.transform, false);

        FloatingNumber fn = obj.GetComponent<FloatingNumber>();
        StartCoroutine(fn.SetNum(x, damage, position));
    }

    //entity takes damage from position
    public void TakeDamage(int damage, Vector3Int position)
    {
        if (isEnemy)
        {
            OnEnemyHit?.Invoke();
        }
        SoundManager.Instance.PlayEntitySound(this, SoundType.HURT);
        int x = 0;
        int y = 0;
        //choose directions for the hitback
        if (position.x < GetGridPos().x)
        {
            x = GetStrength();
        }
        else if (position.x > GetGridPos().x)
        {
            x = -1 * GetStrength();
        }
        if (position.y < GetGridPos().y)
        {
            y = GetStrength();
        }
        else if (position.y > GetGridPos().y)
        {
            y = -1 * GetStrength();
        }
        StartCoroutine(Knockback(x, y));
        
    }

    //does the damaged knockback animation for the entity
    public IEnumerator Knockback(int x, int y)
    {
        Renderer rend = GetComponent<Renderer>();
        Color og = rend.material.color;
        float speed = strength * .02f;
        if (speed > .005f) speed = .01f;
        float elapsed = 0f;
        float duration = 1f;
        float time = 0;

        rend.material.color = Color.red;
        while (time < 360)
        {
            time += 60;
            transform.position += new Vector3(Mathf.Sin((time / 360f) * 2f * Mathf.PI) * speed * x, Mathf.Sin((time / 360f) * 2f * Mathf.PI) * speed * y, 0);

            elapsed += Time.deltaTime;
            yield return new WaitForSeconds(duration / 60f);
        }
        rend.material.color = og;
    }

    public void KnockbackHelper(Entity otherEntity, int distance)
    {
        StartCoroutine(Knockback(otherEntity, distance));
    }

    //forcefully moves this unit back distance spaces
    public IEnumerator Knockback(Entity otherEntity, int distance)
    {
        //gets positions for logic
        Vector3Int startPos = GetGridPos();
        Vector3Int currentPos = startPos;
        Vector3Int otherPos = otherEntity.GetGridPos();
        bool takeDamage = false;

        //sets the x and y for the knockback based on the difference between this unit and the other unit
        Vector3Int diff = startPos - otherPos;
        Vector3Int knockback = new Vector3Int(
            diff.x == 0 ? 0 : diff.x > 0 ? 1 : -1,
            diff.y == 0 ? 0 : diff.y > 0 ? 1 : -1,
            0
        );

        // VISUAL MOVE LOOP, LOOP OVER ALL TILES IN PATH
        Vector3 cellOffset = new Vector3(
            tileManager.entitiesMap.cellSize.x,
            tileManager.entitiesMap.cellSize.y, 0) * 0.5f;

        for (int i = 0; i < distance; i++)
        {
            //find the next position of the unit
            Vector3Int nextPos = currentPos + knockback;
            Vector3 startWorld = transform.position;
            Vector3 endWorld = tileManager.entitiesMap.CellToWorld(nextPos) + cellOffset;

            //if it's a border tile don't take damage and exit
            if(tileManager.GetTileDataAt(nextPos) == null)
            {
                break;
            }

            //if there's an occupant take damage and exit
            if(tileManager.GetTileDataAt(nextPos).HasOccupant())
            {
                takeDamage = true;
                break;
            }

            float elapsed = 0f;
            while (elapsed < tileManager.stepDuration)
            {
                transform.position = Vector3.Lerp(startWorld, endWorld, elapsed / tileManager.stepDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = endWorld;

            //set next position for the loop
            currentPos = nextPos;
        }

        // LOGICAL MOVE, ACTUALLY MOVE TO GRID SPACE
        if(startPos != currentPos)
        {
            tileManager.MoveEntity(startPos, currentPos);
        }

        //logic for ram knockback
        if(takeDamage && otherUnit.ID == 10)
        {
            //ShowNumber(5, otherUnit.GetGridPos(), distance);
            this.TakeDamage(5, otherUnit.GetGridPos());
        }
    }
}
