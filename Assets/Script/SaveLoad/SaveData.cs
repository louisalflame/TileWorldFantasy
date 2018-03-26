using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveData  {
    private const string MAP_KEY = "MAP_KEY";

    public void SaveMap(float[] map)
    { 
        var data = new SaveDataUnit()
        {
            Map = map,
        };

        var mapJson = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(MAP_KEY, mapJson); 

        var m = PlayerPrefs.GetString(MAP_KEY);
        var mm = JsonUtility.FromJson<SaveDataUnit>(m);

        Debug.Log(mm.Map.Length);
    }

    public class SaveDataUnit
    {
        public float[] Map;
    }
}
