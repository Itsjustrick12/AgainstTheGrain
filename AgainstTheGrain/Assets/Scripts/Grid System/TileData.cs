using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;


public class TileData
{
    public TileType type = TileType.None;
    public int moveCost = 1;
    public bool canWalk = true;
    public Unit occupant = null;
    public CropObject crop = null;

    public TileData(TileType t, int cost, bool walkable) {
        type = t;
        moveCost = cost;
        canWalk = walkable;
        occupant = null;
    }

    public TileData(TileType t, int cost, bool walkable, Unit occupuyingUnit) {
        type = t;
        moveCost = cost;
        canWalk = walkable;
        occupant = occupuyingUnit;
    }

    public Unit GetOccupant()
    {
        return occupant;
    }

    public CropObject GetCrop()
    {
        return crop;
    }

    public void SetCrop(CropObject newCrop)
    {
        if (crop == null)
        {

            crop = newCrop;
            occupant = null;
            
            canWalk = false;
        }
        else
        {

            crop = null;
            canWalk = true;
        }
    }

    public void RemoveCrop()
    {
        if (crop != null)
        {

            crop = null;
            canWalk = true;
        }
    }

    public void SetOccupant(Unit newOccupant)
    {
        if (occupant == null)
        {

            occupant = newOccupant;
            canWalk = false;
        }
        else
        {

            occupant = null;
            canWalk = true;
        }
    }

    public void RemoveOccupant()
    {
        if (occupant != null)
        {

            occupant = null;
            canWalk = true;
        }
    }

    public void PrintDetails()
    {
        if (occupant != null)
        {
                Debug.Log("There is a " + occupant.unitName + " here on this tile!");
                occupant.PrintInfo();
        }
        else
        {
            if (type == TileType.Grass)
            {
                Debug.Log("There is a grass tile here!");
            }
            else if (type == TileType.Soil)
            {
                Debug.Log("There is a soil tile here!");
            }
            else if (type == TileType.Dirt)
            {
                Debug.Log("There is a dirt tile here!");
            }
            else
            {
                Debug.Log("There is a tile with a different type here!");
            }
        }
    }

    
};

