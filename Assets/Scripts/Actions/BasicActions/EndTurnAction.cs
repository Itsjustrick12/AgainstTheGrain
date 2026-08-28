using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Actions/EndTurn")]

public class EndTurnAction : EntityAction
{

    public override List<Vector3Int> GetValidTargets(Entity caster)
    {
        List<Vector3Int> valid = new List<Vector3Int>();
        valid.Add(new Vector3Int(0, 0, -1));
        return valid;
    }

    public override bool IsPossible(Entity entity)
    {
        return true;
    }
}
