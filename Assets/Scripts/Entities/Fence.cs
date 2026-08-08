using UnityEngine;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
public class Fence : Structure
{
    [SerializeField] private Sprite[] fenceSprites;
    public override void Interact()
    {
        Debug.Log("Tried to interact with Fence");
    }

    private Dictionary<Tuple<bool, bool, bool, bool>, int> convertToSprite;

    public override void Initialize()
    {
        StructureInfo info = StructureDatabase.Instance.GetStructureInfo(ID);
        base.Initialize();
        SetUp();
    }

    public void SetUp()
    {
        convertToSprite = new()
        {
            {new (false, true, false, true), 0 },
            {new (false, false, true, true), 1 },
            {new (false, true, true, false), 2 },
            {new (false, false, false, false), 3 },
            {new (true, false, false, true), 4 },
            {new (true, false, true, false), 5 },
            {new (false, true, false, false), 6 },
            {new (true, true, false, false), 7 },
            {new (false, false, false, true), 8 },
            {new (false, false, true, false), 9 },
            {new (true, false, false, false), 10 },
            {new (true, true, true, true), 11 }

        };
        UpdateSprite();
        UpdateNeighbors();
    }

    public void UpdateSprite()
    {
        Vector3Int pos = GetGridPos();
        bool up = tileManager.GetEntityOnTile(pos + Vector3Int.up) is Fence;
        bool down = tileManager.GetEntityOnTile(pos + Vector3Int.down) is Fence;
        bool left = tileManager.GetEntityOnTile(pos + Vector3Int.left) is Fence;
        bool right = tileManager.GetEntityOnTile(pos + Vector3Int.right) is Fence;

        Tuple<bool, bool, bool, bool> key = new Tuple<bool, bool, bool, bool>(up, down, left, right);

        if (convertToSprite.TryGetValue(key, out int index))
        {
            sprite.sprite = fenceSprites[index];
        }
        else
        {
            Debug.LogWarning($"No sprite mapped for fence combination: up={up} down={down} left={left} right={right}");
            sprite.sprite = fenceSprites[3];
        }
    }

    public void UpdateNeighbors()
    {
        //remove from tile then update neighbors
        //tileManager.GetTileDataAt(GetGridPos()).ClearOccupant();
        
        Vector3Int pos = GetGridPos();
        Vector3Int[] neighbors = new Vector3Int[]
        {
        pos + Vector3Int.up,
        pos + Vector3Int.down,
        pos + Vector3Int.left,
        pos + Vector3Int.right
        };

        foreach (Vector3Int neighborPos in neighbors)
        {
            Fence fence = tileManager.GetEntityOnTile(neighborPos) as Fence;
            if (fence != null) { fence.UpdateSprite(); }
        }
    }

    public override void Die()
    {
        UpdateNeighbors(); // notify neighbors before removal
        base.Die();
    }

    public override void DestroyEntity()
    {
        UpdateNeighbors(); // notify neighbors before removal
        base.DestroyEntity();
    }
}
