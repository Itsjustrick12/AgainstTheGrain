using UnityEngine;

public class CropManager : MonoBehaviour
{
    TilemapManager tileManager;
    public Crop tempCrop;
    //crop object prefab to instantiate
    public GameObject cropOBJ;
    //for finding crop refrences later
    public GameObject cropContainer;
    public void PlantCropOnTile(Vector3Int position)
    {
        tileManager = TilemapManager.instance;
        TileData tile  = tileManager.getTileData(position);

        //set up the crop object with the right crop
        GameObject obj = GameObject.Instantiate(cropOBJ);
        obj.transform.parent = cropContainer.transform;
        CropObject cropObject = obj.GetComponent<CropObject>();
        cropObject.Initialize(tempCrop, position,tile);

        tile.SetCrop(cropObject);
    }

    public void UpdateCrops()
    {
        foreach (Transform child in cropContainer.transform)
        {
            GameObject tempObj = child.gameObject;
            CropObject tempCrop = tempObj.GetComponent<CropObject>();
            tempCrop.NewDay();
        }
    }
}
