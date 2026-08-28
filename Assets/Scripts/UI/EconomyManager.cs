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
    
    [SerializeField] private int factions = 3;
    [SerializeField] private int currencies = 10;
    [SerializeField] private int[][] currency;
    [SerializeField] private string[][] currencyName;

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
        currency = new int[factions][currencies];
        currencyName = new string[factions][currencies];
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

    public int FindCurrency(int faction, string name)
    {
        for (int i = 0; i < currencies; i++)
        {
            if (name == currencyName[faction][i])
            {
                return i;
            }
        }

        //currency is not found so it returns -1
        return -1;

    }

    //returns the currency amount
    public int GetCurrencyAmount(int faction, string name)
    {
        //finds the currency number
        int number = FindCurrency(faction, name);

        return GetCurrencyAmount(faction, number);

    }

    public int GetCurrencyAmount(int faction, int number)
    {
        //returns -1 if the currency doesn't exist
        if (number < 0 || number >= currencies)
        {
            return -1;
        }
        
        //returns the currency amount
        return currency[faction][findCurrency];

    }

    //returns the name of a currency at a certain position
    public int GetCurrencyName(int faction, int number)
    {
        //returns -1 if the currency doesn't exist
        if (number == -1 || number >= currencies)
        {
            return -1;
        }

        return currencyName[faction][number];

    }

    //checks to see if the faction has enough currency
    public bool CanAfford(int faction, string name, int amount)
    {
        //finds the currency number
        int number = FindCurrency(faction, name);

        return CanAfford(faction, number, amount);

    }

    public bool CanAfford(int faction, int number, int amount)
    {
        //returns false if the currency doesn't exist
        if(number == -1 || number >= currencies)
        {
            return false;
        }

        if (amt <= currency[faction][number])
        {
            return true;
        }
        return false;

    }

    public void SetCurrency(int faction, string name, int amount)
    {
        int number = FindCurrency(faction, name);

        SetCurrency(faction, number, amount);

    }

    public void SetCurrency(int faction, int number, int amount)
    {
        if (number >= 0 && number < currencies)
        {
            //Clamp so coins can never be negative
            currency[faction][number] = Mathf.Max(0, amount);
            OnCurrencyUpdate?.Invoke(faction, number, currency[faction][number]);
        }

    }

    //sets the currency name
    public void SetCurrencyName(int faction, int number, string name)
    {
        //checks that name and number are correct
        if (name != null && number >= 0 && number < currencies)
        {
            currencyName[faction][number] = name;
        }

    }

    //adds currency, leaving the coinsound for now, but it would be nice for a sound for adding other currencies(energy, crops)
    public void AddCurrency(int faction, string name, int amount)
    {
        int number = FindCurrency(faction, name);
        AddCurrency(faction, number, amount);

    }

    public void AddCurrency(int faction, int number, int amount)
    {
        SoundManager.Instance.PlaySound(coinSound);
        SetCoins(coins + amt);

    }

    //attempts to buy the item, also added the can afford as a failsafe
    public bool AttemptToBuy(int faction, string name, int amount)
    {
        int number = FindCurrency(faction, name);
        return AttemptToBuy(faction, number, amount);

    }

    public bool AttemptToBuy(int faction, int number, int amount)
    {
        if (!CanAfford(faction, name, amount))
        {
            return false;
        }

        SetCoins(coins - cost);
        return true;

    }

}