using UnityEngine;
using static UnityEditorInternal.ReorderableList;

public enum BuffType
{
    Movement,
    Damage,
    None
}

public class Buff
{
    public BuffType type;
    public int duration; // # of turns
    public int strength;
    public string name = "Default Buff Name";

    public Buff(BuffType bType, int numTurns, int amt)
    {
        type = bType;
        duration = numTurns;
        strength = amt;
        name = "Default Buff Name";
    }

    public Buff(BuffType bType, int numTurns, int amt, string buffName  )
    {
        type = bType;
        duration = numTurns;
        strength = amt;
        name = buffName;
    }

    public void increment()
    {
        duration--;
    }

    public bool isExpired()
    {
        if (duration <= 0)
        {
            return true;
        }
        return false;
    } 
}
