using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Actions/Wait")]
public class WaitAction : EntityAction
{

    public override List<Vector3Int> GetValidTargets(Entity caster)
    {
        return null;
    }

    public override bool IsPossible(Entity unit)
    {
        return true;
    }
}
