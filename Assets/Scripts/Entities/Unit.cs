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
    [SerializeField] protected int strength = 1;
    [SerializeField] protected int attackRange = 1;
    [SerializeField] protected int movementRange = 3;
    public bool isFed = false;
    public bool isEnemy = false;
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

    public void SetAttackRange(int temp)
    {
        if(temp > 0)
        {
            attackRange = temp;
        }
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


    public int GetAttackRange()
    {
        return attackRange;
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

    public bool IsSameTeamAs(Unit diffUnit)
    {
        if (diffUnit == null)
        {
            return false;
        }

        //Check if same side, either both enemies or both not
        if ((isEnemy && diffUnit.isEnemy) || (!isEnemy && !diffUnit.isEnemy))
        {
            return true;
        }
        //Otherwise, different teams
        return false;
    }

    public bool IsSameTeamAs(Entity entity)
    {
        if (entity == null)
        {
            Debug.LogError("UNIT.The passed entity is null, can't compare teams");
            return false;
        }

        //Cast entity to unit
        Unit unitCheck = entity as Unit;


        return IsSameTeamAs(unitCheck);
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

    public override void TakeDamage(int damage)
    {
        SoundManager.Instance.PlayEntitySound(this, SoundType.HURT);

        if (activeBuffs.Count <= 0)
        {
            base.TakeDamage(damage);
            return;
        }
        else
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
            int newDamage = Mathf.Max(0, (int)(damage - (baseIncrease * multiplier)));
            base.TakeDamage(newDamage);
        }
        if (isEnemy)
        {
            OnEnemyHit?.Invoke();
        }
    }

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
        if (activeBuffs.Count <= 0)
        {
            base.TakeDamage(damage);
        }
        else
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
            base.TakeDamage(damage);
        }
    }

    //does the damaged knockback animation for the unit
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

    public void KnockbackHelper(Unit otherUnit, int distance)
    {
        StartCoroutine(Knockback(otherUnit, distance));
    }

    //forcefully moves this unit back distance spaces
    public IEnumerator Knockback(Unit otherUnit, int distance)
    {
        //gets positions for logic
        Vector3Int startPos = GetGridPos();
        Vector3Int currentPos = startPos;
        Vector3Int otherPos = otherUnit.GetGridPos();
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
}

