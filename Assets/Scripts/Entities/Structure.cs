using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
public class Structure : Entity
{
    public int ID;
    public virtual void Interact()
    {
        if (isActive && IsInteractable())
        {
            Debug.Log("Tried to Intreact with simple structure");
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        StructureInfo info = StructureDatabase.Instance.GetStructureInfo(ID);
        actions = info.actions;
        maxHealth = info.baseHealth;
        currentHealth = info.baseHealth;
    }

    public override void Awake()
    {
        base.Awake();
    }

}
