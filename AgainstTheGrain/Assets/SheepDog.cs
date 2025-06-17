using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SheepDog : AnimalUnit
{

    [Header("SheepDog Settings")]
    public int moveIncrease = 1;
    public int range = 2;
    public int durationAmt = 1;
    public string buffName = "SheepDog";

    public Sprite downSprite;
    public Sprite upSprite;

    //List of animals to buff and unbuff
    private List<AnimalUnit> animalsNearby = new List<AnimalUnit>();

    //bark somewhere here
    public override void moveToTile(Vector3Int position)
    {
        //update the logic for the surrounding animals
        clearBuff();

        //do the normal move logic
        base.moveToTile(position);

        //update the list of nearby animals
        getAnimalsNearby();

        //update the stats of new nearby units
        buffNearbyAnimals();
    }

    public override void Refresh()
    {
        sprite.sprite = upSprite;
        base.Refresh();
    }

    public override void Deactivate()
    {
        sprite.sprite = downSprite;
        base.Deactivate();
    }

    //reset the current units movement capability
    private void clearBuff()
    {
        foreach (AnimalUnit animal in animalsNearby)
        {
            animal.ClearBuffByName(buffName);
        }
    }

    //updates the movement capabilities of all nearby units if able
    private void buffNearbyAnimals()
    {
        

        Buff distBuff = new Buff(BuffType.Movement, 1, moveIncrease, buffName);
        foreach (AnimalUnit animal in animalsNearby)
        {
            //only buff active units
            if (animal.active)
            {
                animal.buffUnit(distBuff);
            }
        }
    }

    public void getAnimalsNearby()
    {
        //reset the current list to refill
        animalsNearby.Clear();

        //get all the applicable animals in the area
        List<Unit> units = TilemapManager.instance.getUnitsNearby(GetGridPosition(), range, true);
        foreach (Unit unit in units)
        {
            AnimalUnit animal = unit.GetComponent<AnimalUnit>();
            if (animal != null)
            {
                //apply buff to the animal
                animalsNearby.Add(animal);
            }
        }
    }
}
