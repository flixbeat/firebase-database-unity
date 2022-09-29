using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Firebase.Database;
using Firebase.Extensions;
using Newtonsoft.Json;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Object = System.Object;

public class DatabaseManager : MonoBehaviour
{

    [Header("Create user")]
    [SerializeField] private TMP_InputField username;
    [SerializeField] private TMP_InputField gold;

    [Header("Get user info")]
    [SerializeField] private TMP_InputField userid;
    [SerializeField] private TextMeshProUGUI unameDisplay;
    [SerializeField] private TextMeshProUGUI goldDisplay;

    [Header("Users list")] 
    [SerializeField] private TMP_InputField allUsers;
    
    [Header("Update user")] 
    [SerializeField] private TMP_InputField userIdUpdate;
    [SerializeField] private TMP_InputField unameUpdate;
    [SerializeField] private TMP_InputField goldUpdate;
    
    [Header("Delete user")] 
    [SerializeField] private TMP_InputField userIdDelete;
    
    private DatabaseReference firebaseDb;
    
    void Start()
    {
        firebaseDb = FirebaseDatabase.DefaultInstance.RootReference;
        DisplayAllUsers();
    }

    // called via button onclick
    public async void CreateUser()
    {
        User user = new User(username.text,int.Parse(gold.text));
        string json = JsonConvert.SerializeObject(user);

        var task = firebaseDb.Child("users").Child($"user_{GetUniqueKey(6)}").SetRawJsonValueAsync(json);
        await task.ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted)
                print($"something went wrong {t.Exception}");
            else if (t.IsCompleted)
            {
                DisplayAllUsers();
                print($"{user.name} has been registered");
            }
        });
    }
    
    // called via button onclick
    public async void GetUserData()
    {
        var task = firebaseDb.Child("users").Child(userid.text).GetValueAsync();
        await task.ContinueWithOnMainThread( t =>
        {
            if(t.IsFaulted)
                print($"something went wrong {t.Exception}");
            else if (t.IsCompleted)
            {
                DataSnapshot snapshot = t.Result;
                var data = (Dictionary<string,Object>) snapshot.Value;
                unameDisplay.text = $"Username: {data["name"]}";
                goldDisplay.text = $"Gold: {data["gold"]}";
            }
        });
    }

    public async void DisplayAllUsers()
    {
        //var task = firebaseDb.Child("users").GetValueAsync();
        
        // here, FirebaseDatabase.DefaultInstance.GetReference is used instead to have access with
        // sorting/filtration/event methods upon getting the data
        // https://firebase.google.com/docs/database/unity/retrieve-data#read_data_once
        
        var task = FirebaseDatabase.DefaultInstance.GetReference("users").GetValueAsync();
        await task.ContinueWithOnMainThread(t =>
        {
            if(t.IsFaulted)
                print($"something went wrong {t.Exception}");
            else if (t.IsCompleted)
            {
                DataSnapshot snapshot = t.Result;
                var data = (Dictionary<string,Object>) snapshot.Value;
                allUsers.text = "";

                if (data == null)
                    return;
                
                foreach (var d in data)
                {
                    allUsers.text += d.Key + "\n";
                    // allUsers.text += ((Dictionary<string,Object>) d.Value)["name"] + "\n";
                }
                    
            }
        });
    }

    // called via button onclick
    public async void UpdateUser()
    {
        var data = new Dictionary<string,Object>()
        {
            {"name",unameUpdate.text},
            {"gold",goldUpdate.text},
        };
        var task = firebaseDb.Child("users").Child(userIdUpdate.text).UpdateChildrenAsync(data);
        await task.ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted)
                print($"something went wrong {t.Exception}");
            else if (t.IsCompleted)
                print($"{userIdUpdate.text} data has been updated");
        });
    }
    
    // called via button onclick
    public async void DeleteUser()
    {
        var task = firebaseDb.Child("users").Child(userIdDelete.text).RemoveValueAsync();
        await task.ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted)
                print($"something went wrong {t.Exception}");
            else if (t.IsCompleted)
            {
                print($"{userIdDelete.text} data has been deleted");
                DisplayAllUsers();
            }
        });
    }
    
    public static string GetUniqueKey(int size = 12)
    {            
        char[] chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray(); 
        
        byte[] data = new byte[4*size];
        using (var crypto = RandomNumberGenerator.Create())
        {
            crypto.GetBytes(data);
        }
        StringBuilder result = new StringBuilder(size);
        for (int i = 0; i < size; i++)
        {
            var rnd = BitConverter.ToUInt32(data, i * 4);
            var idx = rnd % chars.Length;

            result.Append(chars[idx]);
        }

        return result.ToString();
    }
}


public class User
{
    public string name;
    public int gold;

    public User(string name, int gold)
    {
        this.name = name;
        this.gold = gold;
    }
}