using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "NewResourceDatabase", menuName = "AgainstTheGrain/Databases/ResourceDatabase")]
public class ResourceDatabase : ScriptableObject
{
    private static ResourceDatabase instance;
    [Header("Basic Resource Prefab")]
    public GameObject resourcePrefab;
    public static ResourceDatabase Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<ResourceDatabase>("ResourceDatabase");
                if (instance == null)
                {
                    Debug.LogError("ResourceDatabase not found in Resources!");
                }
                else
                {
                    instance.BuildLookup();
                }
            }
            return instance;
        }
    }

    public List<ResourceInfo> resources = new List<ResourceInfo>();

    private Dictionary<int, ResourceInfo> lookup;

    private void BuildLookup()
    {
        lookup = new Dictionary<int, ResourceInfo>();

        foreach (var resource in resources)
        {
            if (!lookup.ContainsKey(resource.id))
            {
                lookup.Add(resource.id, resource);
            }
            else
            {
                Debug.LogError($"Duplicate Resource ID detected: {resource.id}");
            }
        }
    }

    public ResourceInfo GetResourceInfo(int id)
    {
        if (lookup == null)
            BuildLookup();

        if (lookup.TryGetValue(id, out ResourceInfo resource))
            return resource;

        Debug.LogError("No resource exists with id: " + id);
        return null;
    }

    public TileBase GetSeedTile(int id)
    {
        ResourceInfo resource = GetResourceInfo(id);
        return resource != null ? resource.tile : null;
    }

    public Sprite GetIcon(int id)
    {
        ResourceInfo resource = GetResourceInfo(id);
        return resource != null ? resource.sprite : null;
    }

    public int GetNumStages(int id)
    {
        ResourceInfo resource = GetResourceInfo(id);
        return resource != null ? resource.numStages : 0;
    }

    public int GetSellValue(int id)
    {
        ResourceInfo resource = GetResourceInfo(id);
        return resource != null ? resource.sellValue : 0;
    }

    public int GetIDFromTile(TileBase tile)
    {
        foreach (var resource in resources)
        {
            if (resource.tile == tile)
                return resource.id;
        }

        return -1;
    }

    public ResourceInfo GetResourceInfoFromTile(TileBase tile)
    {
        foreach (var resource in resources)
        {
            if (resource.tile == tile)
                return resource;
        }

        return null;
    }

    public int GetNumResources()
    {
        return resources.Count;
    }
}
