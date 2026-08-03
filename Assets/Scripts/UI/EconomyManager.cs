using System;
using System.Collections.Generic;
using UnityEngine;

public class EconomyManager : MonoBehaviour
{
    //Needed for global references
    public static EconomyManager Instance { get; private set; }

    [Header("Other")]
    public GameManager gameManager;

    [Header("Currencies")]
    /*
        currencies are going to be divided into a grid system like so

        int currency[faction][currency #]
        and
        string currencyName[faction][currency #]
        
        this way we can store multiple factions and their multiple currencies in one place
    */
    
    [SerializeField] private int currency[3][10];
    [SerializeField] private string currencyName[3][10];

    //Used whenever a currency is updated to update the UI
    //int(faction), int(currency #), int(new value)
    public static event Action<int, int, int> OnCurrencyUpdate;

    [Header("Sounds")]
    public AudioClip coinSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        //we're grabbing the game manager becuase it's gonna have which faction has which currencies
        gameManager = FindFirstObjectByType<GameManager>();

        //geting the currency names
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                currency[i][j] = 0;
                currencyName[i][j] = gameManager.GetCurrencyName(i,j);
            }
        }

        Instance = this;
    }

    //returns the currency amount
    public int GetCurrencyAmount(int faction, string name or int number???)
    {
        return currency[faction][number];
    }

    //returns the name of a currency at a certain position
    public int GetCurrencyName(int faction, int number)
    {
        return currencyName[faction][number];
    }

    //checks to see if the faction has enough currency
    public bool CanAfford(int faction, string name or int number???, int amount)
    {
        if (amt <= currency[faction][number])
        {
            return true;
        }
        return false;
    }

    public void SetCurrency(int faction, string name or int number???, int amt)
    {
        //Clamp so coins can never be negative
        currency[faction][number] = Mathf.Max(0, amt);
        OnCurrencyUpdate?.Invoke(faction, number, currency[faction][number]);
    }

    //sets the currency name
    public void SetCurrencyName(int faction, int number, string name)
    {
        if (name != null)
        {
            currencyName[faction][number] = name;
        }
        else
        {
            currencyName[faction][number] = "";
        }
    }

    //adds currency, leaving the coinsound for now, but it would be nice for a sound for adding other currencies(energy, crops)
    public void AddCurrency(int faction, string name or int number???, int amount)
    {
        SoundManager.Instance.PlaySound(coinSound);
        SetCoins(coins + amt);
    }

    //attempts to buy the item, also added the can afford as a failsafe
    public bool AttemptToBuy(int faction, string name or int number???, int amount)
    {
        if (!CanAfford(faction, name, amount))
            return false;

        SetCoins(coins - cost);
        return true;

    }

}