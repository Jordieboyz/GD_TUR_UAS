using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;


// Local Libraries
using Utils;

/*------------------------------------------------------------------
    PlayerPrefs wrapper class
        Acces PlayerPrefs variables with their respective property name 
--------------------------------------------------------------------*/
public class StaticSettings : PPSettingsBase<StaticSettings>
{
    // Global PlayerPrefs Fields
    private static string PlayerName  { get => __get<string>(); } 
    private static float MasterVolume { get => __get<float>();  } 
    private static float VFXVolume    { get => __get<float>();  } 

    public StaticSettings(){
        if(!PlayerPrefs.HasKey(nameof(PlayerName))){
            __set("Player", nameof(PlayerName));
        }
        if(!PlayerPrefs.HasKey(nameof(MasterVolume))){
            __set(5f, nameof(MasterVolume));
        }
        if(!PlayerPrefs.HasKey(nameof(VFXVolume))){
            __set(5f, nameof(VFXVolume));
        }
    }
}

public class StaticGameSettings : PPSettingsBase<StaticGameSettings>
{
    // Global PlayerPrefs Fields
    private static float PlayersAmount  { get => __get<float>(); } 
    private static bool RandomMap { get => __get<bool>();  } 

    public StaticGameSettings(){
        if(!PlayerPrefs.HasKey(nameof(PlayersAmount))){
            __set(3f, nameof(PlayersAmount));
        }
        if(!PlayerPrefs.HasKey(nameof(RandomMap))){
            __set(true, nameof(RandomMap));
        }
    }
}

/*------------------------------------------------------------------
    Base class PlayerPrefs wrapper class
        Generic getters and setters to acces PlayerPrefs variables
--------------------------------------------------------------------*/

public class PPSettingsBase<U> where U : PPSettingsBase<U>
{
    private static BindingFlags defFlags = BindingFlags.Static | BindingFlags.NonPublic ;
    /*--------------------------------------------------------------------------------------
        Public Section
            Get(string)     : Retrieve the value of a field dynamically based on the type in PlayerPrefs

            Set(T, string)  : Store value of type T dynamically on the key `name` if
                              the property already exists as a PlayerPrefs variable
    ----------------------------------------------------------------------------------------*/
    public static T Get<T>(string key)
    {
        return Static.GetPropertyOfClass<U>(key, defFlags) != null ? __get<T>(key) : default;
    }

    
    public static void Set(object value, string key)
    {
        if (Static.GetPropertyOfClass<U>(key,  defFlags) != null){
            __set(value, key);
        };
    }


    /*--------------------------------------------------------------------------------------
        Protected Section
            _Get()          : Dynamically retrieve the value of type T, which is stored on
                              the `key`. The key will always be the name of the calling member

            _Set(T, string) : Dynamically store value of type T on the `key`. The key will 
                              always be the name of the calling member.
    ----------------------------------------------------------------------------------------*/
    protected static T __get<T>([CallerMemberName] string key = null){
        return typeof(T) switch
        {
            _ when typeof(T) == typeof(bool) => (T)(object)(PlayerPrefs.GetInt(key) == 1),
            _ when typeof(T) == typeof(int) => (T)(object)PlayerPrefs.GetInt(key),
            _ when typeof(T) == typeof(float) => (T)(object)PlayerPrefs.GetFloat(key),
            _ when typeof(T) == typeof(string) => (T)(object)PlayerPrefs.GetString(key),
            _ => default
        };
    }

    protected static void __set<T>(T value, [CallerMemberName] string key = null){
        switch (value){
            case bool boolValue:
                PlayerPrefs.SetInt(key, boolValue ? 1 : 0); break;
            case int intValue:
                PlayerPrefs.SetInt(key, intValue); break;
            case float floatValue:
                PlayerPrefs.SetFloat(key, floatValue); break;
            case string stringValue:
                PlayerPrefs.SetString(key, stringValue); break;
            default: break;
        }
    }  
}
