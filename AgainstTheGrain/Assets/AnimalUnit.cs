using UnityEngine;

public class AnimalUnit : Unit
{
    [Header("Animal Settings")]
    public bool isFed = false;
    public int feedNeed = 1;

    public override void KillUnit()
    {
        //before destroying gameobject, remove unit from its matched tile
        pos = GetGridPosition();

        //remove self from owning tile
        if (tile != null)
        {
            tile.RemoveOccupant();
        }

        //dont count the difference check if the killed unit is an unactive animal
        int difference = (isFed) ? 1 : 0;

        //check if end game state is reached based on this unit dying
        GameManager GM = GameManager.instance;
        if (isEnemy && GM.GetNumEnemies() <= 1)
        {
            GM.ShowVictoryScreen();
            Destroy(this.gameObject);
        }
        else if (!isEnemy && GM.GetNumActivePlayers() - difference == 0)
        {

            GM.ShowDefeatScreen();
            Destroy(this.gameObject);
        }
        Destroy(this.gameObject);
    }

    public new bool isUseableUnit()
    {
        return isFed;
    }
}
