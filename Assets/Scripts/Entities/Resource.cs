using JetBrains.Annotations;
using UnityEngine;
//The resource object that is spawned in the game world and treated like an entity
public class Resource : Entity
{
    //What resource does this entity represent?
    public int id;
    private ResourceInfo refResource;

    [Header("State Variables")]

    //has the resource been interacted with(watering)
    private bool interacted = false;
    //is the resource ready to be harvested?
    private bool isHarvestable = false;
    //What stage is the resource at?
    private int currentStage = 0;

    //Used for multiharvesting
    private bool renewable = false;
    private int onHarvestStage = 0;
    private bool isBarren = false;
    private Sprite barrenSprite;

    //For harvest particle
    [SerializeField] private ParticleSystem harvestParticle;


    public void Initialize(ResourceInfo info)
    {
        refResource = info;
        id = info.id;
        interacted = false;
        currentStage = 0;
        SetIsHarvestable(false);
        sprite.sprite = info.growthStageSprites[0];
        renewable = info.renewable;
        onHarvestStage = info.onHarvestStage;
        barrenSprite = info.barrenSprite;
        maxHealth = info.baseHealth;
        currentHealth = maxHealth;
    }

    public override void Initialize()
    {
        base.Initialize();
        ResourceInfo info = ResourceDatabase.Instance.GetResourceInfo(id);
        if (info == null)
        {
            Debug.LogError("Tried to initialize resource without a valid info in the database scriptable object. Check the resources folder!");
        }
        Initialize(info);
    }

    public void Interact()
    {
        sprite.color = DimColor;
        interacted = true;
    }

    //returns if you can still interact with the resource
    public bool CanInteract()
    {
        return !interacted;
    }

    public void SetIsHarvestable(bool value)
    {
        isHarvestable = value;

        if (value)
        {
            harvestParticle.Play();
        }
        else
        {
            harvestParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void ResetInteract()
    {
        sprite.color = Color.white;
        interacted = false;
    }

    public void ProcessGrowth()
    {
        if (CanGrow())
        {
            //Process the resource's stage
            currentStage = Mathf.Min(currentStage + 1, refResource.numStages-1);

            //If processed through all stages, its now harvestable
            if (refResource != null && refResource.numStages-1 == currentStage)
            {
                //Get the last sprite index
                sprite.sprite = refResource.growthStageSprites[refResource.numStages - 1];
                SetIsHarvestable(true);
                isBarren = false;
            }
            else
            {
                if (refResource.growthStageSprites.Length <= currentStage)
                {
                    Debug.Log("There isn't a sprite for this stage");
                    return;
                }
                // Only swap away from barren sprite once it actually starts regrowing
                if (isBarren && currentStage > onHarvestStage)
                {
                    isBarren = false;
                }
                if (!isBarren)
                {
                    sprite.sprite = refResource.growthStageSprites[currentStage];
                }

            }

        }
        ResetInteract();
    }

    //May get more complicated later
    public bool CanGrow()
    {
        if (interacted)
        {
            return true;
        }
        return false;
    }

    public bool CanBeHarvested()
    {
        return isHarvestable;
    }

    public void Harvest(int team)
    {
        if (CanBeHarvested())
        {

            EconomyManager.Instance.AddCurrency(team, id);
            //If multiharvest, jump to the stage defined by the ResourceInfo, then proceed like normal
            if (renewable)
            {
                currentStage = onHarvestStage;
                sprite.sprite = barrenSprite;
                SetIsHarvestable(false);
                isBarren = true;
            }
            else
            {
                DestroyEntity();
            }
            //TODO Add logic for increasing the player's crop count
            //Remove the entity from it's current TileData and destroy the GameObject
        }

    }

    public bool IsHarvestable()
    {
        return isHarvestable;
    }

    public bool Interacted()
    {
        return interacted;
    }

    //Use the events system to get updates about state when the turn advanced
    private void OnEnable()
    {
        GameManager.StartPlayerTurn += ProcessGrowth;
    }

    private void OnDisable()
    {
        GameManager.StartPlayerTurn -= ProcessGrowth;
    }
}
