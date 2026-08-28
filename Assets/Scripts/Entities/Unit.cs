using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.UI.Image;

public class Unit : Entity
{
    [Header("General Settings")]
    public int ID;
    [SerializeField] protected int movementRange = 3;
    public bool isFed = false;
    public bool canFly = false;

    [Header("For Targeting")]
    //added for targeting
    public List<EntityType> primary = new List<EntityType>();
    //no target uses a negative z value
    public Vector3Int target = new Vector3Int(0,0,-1);
    public int iq = 1;

    //Necessary for animating
    public bool isMoving = false;

    public static event Action OnUnitAttacked;
    public static event Action<int> OnUnitFed;

    public static event Action OnEnemyHit;
    public static event Action OnAnimalDie;
    public static event Action OnFriendlyDie;

    public override void Awake()
    {
        base.Awake();
        
    }

    public override void Initialize()
    {
        base.Initialize();
        UnitInfo info = UnitDatabase.Instance.GetUnitInfo(ID);
        attackRange = info.attackRange;
        strength = info.strength;
        maxHealth = info.baseHealth;
        currentHealth = info.baseHealth;
        actions = info.actions;
        movementRange = info.moveRange;
    }

    public void SetIsFed(bool value)
    {
        isFed = value;
    }

    public bool GetIsFed()
    {
        return isFed;
    }

    public int GetBaseMoveRange()
    {
        return movementRange;
    }

    public int GetMoveRange()
    {
        if (activeBuffs.Count <= 0)
        {

            return movementRange;
        }
        int baseIncrease = 0;
        float multiplier = 1;
        //otherwise, total movement buffs and return
        //loop through all buffs to check for strengh buffs
        foreach (Buff buff in activeBuffs)
        {
            //check for strength buffs
            MovementBuff mBuff = buff as MovementBuff;
            if (mBuff != null)
            {
                baseIncrease += mBuff.baseIncrease;
                multiplier *= mBuff.multiplier;
            }
        }

        //return the calculated stat after base increases and multiplier
        return (int)((movementRange + baseIncrease) * multiplier);
    }

    public void Heal(int amount)
    {
        int after = currentHealth + amount;
        currentHealth = (after) > maxHealth ? maxHealth : after;
    }

    public bool HasAnimator()
    {
        if(animator != null)
        {
            return true;
        }
        return false;
    }

    public void SetCanFly(bool temp)
    { 
        canFly = temp;
    }

    public bool GetCanFly()
    { 
        return canFly;
    }

    public void SetIsEnemy(bool tempIsEnemy)
    {
        isEnemy = tempIsEnemy;
    }

    public bool GetIsEnemy()
    {
        return isEnemy;
    }

    public Vector3Int GetTarget()
    {
        return target;
    }

    public Vector3Int SetAndReturnTarget()
    {
        aiManager.FindTarget(this);
        return GetTarget();
    }

    public List<Vector3Int> GetMovementRange()
    {
        return tileHelper.GetMovementRange(this);
    }

    List<Vector3Int> FindPositions(bool prime)
    {
        List<Vector3Int> temp = new List<Vector3Int>();
        List<Unit> tempUnits;

        //grabs the list depending on team
        if (isEnemy)
        {
            tempUnits = gameManager.GetAllFriendlyUnits();
        }
        else
        {
            tempUnits = gameManager.GetAllEnemyUnits();
        }

        //gets all of the entities to be targeted
        for(int i = 0; i < tempUnits.Count; i++)
        {
            //only grabs an entity if it's a primary target or secondary targeting
            if(primary.Contains(tempUnits[i].GetEntityType()) || !prime)
            {
                temp.Add(tempUnits[i].GetGridPos());
            }
        }

        //only grabs crops if they're a primary target or secondary targeting
        if(primary.Contains(EntityType.Crop) || !prime)
        {
            List<Crop> tempCrops = gameManager.GetAllCrops();
            for(int i = 0; i < tempCrops.Count; i++)
            {
                temp.Add(tempCrops[i].GetGridPos());
            }
        }

        return temp;
    }
    //Animated movement logic
    public IEnumerator Move(List<Vector3Int> path)
    {
        if (path.Count == 0) yield break;
        //Pause the user from interacting with anything during desync period
        isMoving = true;

        Vector3Int startLogicalPos = GetGridPos();

        //Removing starting point to prevent animation from looking weird
        if (startLogicalPos == path[0])
            path.RemoveAt(0);
        //if its the only thing in there now, check again
        if (path.Count == 0)
        {
            isMoving = false;
            yield break;
        }

        //Determine the path the unit needs to take
        if (isEnemy)
            path = TrimPathForEnemy(path);

        //check if trimmed too much again
        if (path.Count == 0)
        {
            isMoving = false;
            yield break;
        }
        //get the final position for the logical move
        Vector3Int destination = path[path.Count - 1];

        // VISUAL MOVE LOOP, LOOP OVER ALL TILES IN PATH
        Vector3 cellOffset = new Vector3(
            tileManager.entitiesMap.cellSize.x,
            tileManager.entitiesMap.cellSize.y, 0) * 0.5f;

        //animate through remaining path
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 startWorld = transform.position;
            Vector3 endWorld = tileManager.entitiesMap.CellToWorld(path[i]) + cellOffset;

            Vector3Int prevPos = (i == 0) ? startLogicalPos : path[i - 1];
            Vector3Int dir = path[i] - prevPos;
            SoundManager.Instance.PlayEntitySound(this, SoundType.WALK);

            if (HasAnimator())
            {
                animator.SetFloat("x position", dir.x);
                if(dir.x < 0)
                {
                    animator.SetFloat("facing", -1f);
                }
                else if(dir.x > 0)
                {
                    animator.SetFloat("facing", 1f);
                }
                animator.SetFloat("y position", dir.y);
                //TODO basically there's a pause where only on move(not on cancel) it pauses, probably a calculation or smth
                animator.SetBool("moving", dir.x != 0 || dir.y != 0);
            }
            else
            {
                Debug.Log("No Animator");
            }

            float elapsed = 0f;
            while (elapsed < tileManager.stepDuration)
            {
                transform.position = Vector3.Lerp(startWorld, endWorld, elapsed / tileManager.stepDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = endWorld;
        }

        // LOGICAL MOVE, ACTUALLY MOVE TO GRID SPACE
        tileManager.MoveEntity(startLogicalPos, destination);

        if (HasAnimator())
        {
            animator.SetBool("moving", false);
        }
        //flag used to allow player input again after move anim
        isMoving = false;
    }

    private List<Vector3Int> TrimPathForEnemy(List<Vector3Int> path)
    {
        // Trim to movement budget
        Debug.Log("Path: " + path);
        int budget = GetMoveRange();
        int trimAt = 0;
        for (int i = 0; i < path.Count; i++)
        {
            int cost = tileManager.GetTileDataAt(path[i]).movementCost;
            if (budget < cost) break;
            budget -= cost;
            trimAt = i + 1;
        }
        //start working with the path that is how far the entity can walk along that path
        path = path.GetRange(0, trimAt);

        //Walk backwards, ensure the ending spot of the path is NOT on a friendly unit even it it is reachable
        while (path.Count > 0)
        {
            Entity occupant = tileManager.GetEntityOnTile(path[path.Count - 1]);
            if (occupant == null) break;
            Unit occupantUnit = occupant as Unit;
            // This checks if the ending tile has a unit of the same team
            if (occupantUnit != null && occupantUnit.isEnemy != isEnemy) break;
            path.RemoveAt(path.Count - 1);
        }
        //Finally return the path for the movement animation
        return path;
    }

    bool Attack()
    {
        if (target.z == -1)
        {
            //Debug.Log("UNIT.No target!");
            return false;
        }


        Vector3Int pos = GetGridPos();
        bool isAdjacent = Mathf.Abs(target.x - pos.x) + Mathf.Abs(target.y - pos.y) <= GetAttackRange();

        if (!isAdjacent)
        {
            //Debug.Log("UNIT.Target isn't adjacent!");
            //Set the target a second time
            return false;
        }

        TileData targetTile = tileManager.GetTileDataAt(target);
        if (targetTile == null || targetTile.occupyingEntity == null)
        {
            //Debug.Log("UNIT.Nothing to attack!");
            return false;
        }

        //Ensure attack never hits friendly unit no matter what was passed in
        Unit targetUnit = targetTile.occupyingEntity as Unit;
        if (targetUnit != null && IsSameTeamAs(targetUnit))
        {
            return false;
        }

        if (targetTile.occupyingEntity as Unit != null)
        {
            ShowNumber(GetStrength(), target, GetGridPos().x - target.x);
            (targetTile.occupyingEntity as Unit).TakeDamage(GetStrength(), GetGridPos());
        }
        else
        {
            targetTile.occupyingEntity.TakeDamage(GetStrength());
        }

        if (targetTile.occupyingEntity == null)
            target = new Vector3Int(0, 0, -1);
        SoundManager.Instance.PlayEntitySound(this, SoundType.ATTACK);
        if(HasAnimator())
        {
            if(targetTile.gridPos.x - GetGridPos().x != 0)
            {
                animator.SetFloat("facing", targetTile.gridPos.x - GetGridPos().x);
            }
            animator.SetTrigger("attack");
        }
        return true;
    }

    public void SetAnimationTrigger(string triggerName)
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerName);
        }
    }

    public IEnumerator DoTurn()
    {
        //If something is already in range, just attack it and don't move
        /*
        Vector3Int inRangeTarget = aiManager.FindTargetInRange(this);
        if (inRangeTarget.z != -1)
        {
            target = inRangeTarget;
            Attack();
            yield break;
        }
        */

        //If nothing is in range, find the closest and most reasonable target
        target = aiManager.FindTarget(this);

        //Sanity check that there is a target there
        TileData data = tileManager.GetTileDataAt(target);
        if (data != null && !data.HasUnit())
        {
           target = aiManager.FindTarget(this);
        }

        //If the target is legit, move to it
        if (target.z != -1)
        {
            //As long as the path is doable, move to the unit
            if (!tileHelper.IsWithinRange(GetGridPos(), target, GetAttackRange()))
            {
                //Depending on IQ use really smart pathfinding to make them move better as a team
                List<Vector3Int> path = tileHelper.TilePath(GetGridPos(), target, this);// tileHelper.TilePath(GetGridPos(), target, this);
                if (path.Count > 0)
                {
                    yield return StartCoroutine(Move(path));
                }
            }
        }

        //If we moved towards our target but did not get close enough to attack it, try to hit something nearby
        Vector3Int inRangeTarget = aiManager.FindTargetInRange(this);
        if (inRangeTarget.z != -1)
        {
            target = inRangeTarget;
        }

        //Attack our current target if we have one
        Attack();
    }

    public override void Die()
    {
        SoundManager.Instance.PlayEntitySound(this, SoundType.DEATH);
        if (!isEnemy)
        {
            if (GetEntityType() == EntityType.Animal)
            {
                OnAnimalDie?.Invoke();
            }
            else if (GetEntityType() == EntityType.Farmer)
            {
                OnFriendlyDie?.Invoke();
            }
        }
        base.Die();
    }

}

