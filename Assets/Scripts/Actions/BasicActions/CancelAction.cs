using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Actions/Cancel")]
public class Cancel : EntityAction
{

    public override List<Vector3Int> GetValidTargets(Entity caster)
    {
        return null;
    }

    public override bool IsPossible(Entity entity)
    {
        return true;
    }
}
