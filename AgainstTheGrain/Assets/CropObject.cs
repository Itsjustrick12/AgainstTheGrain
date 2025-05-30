using System.Runtime.ConstrainedExecution;
using Unity.VisualScripting;
using UnityEngine;

public class CropObject : MonoBehaviour
{
    public SpriteRenderer sprite;
    public TileData tile;
    public Vector3Int pos;

    public Crop crop;
    public bool harvestable = false;
    public bool watered = false;
    public int growthDays = 0;
    public int stage = 0;

    TilemapManager tileManager;
    public void Initialize(Crop newCrop, Vector3Int position, TileData tileData)
    {
        tile = tileData;
        Initialize(newCrop, position);

    }
    public void Initialize(Crop newCrop, Vector3Int position)
    {
        tileManager = TilemapManager.instance;
        //get a reference to the scriptable object
        crop = newCrop;
        pos = position;
        transform.position = position + new Vector3(0.5f, 0.5f, 0);
        sprite.sprite = crop.sprites[0];

    }

    public void Water()
    {
        watered = true;
        if (tileManager.getPlaceHolderAt(pos) == TileType.Dirt)
        {
            tileManager.SetTileByType(TileType.Soil, pos);
        }
    }

    public void Harvest()
    {
        if (harvestable)
        {
            //update feed amt
            GameManager.instance.AddFeed(1);
            //add a count to the gamemanager as a type of currency
            DestroyCrop();
        }
    }

    //grow the crop if conditions are fufilled
    public void NewDay()
    {
        Debug.Log("New Day Processing");
        tileManager.SetTileByType(TileType.Dirt, pos);

        //check if growing conditions are fufilled
        if (watered)
        {
            growthDays++;
            //check if can update stage
            if (stage < crop.growthDays.Length-1 && crop.growthDays[stage] < growthDays)
            {
                stage++;
                sprite.sprite = crop.sprites[stage];
            }
            if (stage >= crop.growthDays.Length - 1)
            {
                harvestable = true;
                Debug.Log("Plant can now be harvested");
            }

            watered = false;
        }
    }

    
    public void DestroyCrop()
    {
        tile.RemoveCrop();
        Destroy(this.gameObject);
    }
}
