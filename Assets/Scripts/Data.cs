using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class Data : MonoBehaviour
{
   public static void SaveProfile(ProfileData profile)
    {
        try
        {

        string path = Application.persistentDataPath + "/profile.dt";

            if (File.Exists(path)) File.Delete(path);

        FileStream file = File.Create(path);

        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(file, profile);
        file.Close();
        }
        catch
        {
            Debug.Log("Error: Could not save profile to file.");
        }
    }

    public static ProfileData LoadProfile()
    {
            ProfileData ret = new ProfileData();
        try
        {
            string path = Application.persistentDataPath + "/profile.dt";

            if (File.Exists(path))
            {
                FileStream file = File.Open(path, FileMode.Open);
                BinaryFormatter bf = new BinaryFormatter();
                ret = (ProfileData)bf.Deserialize(file);
            }
        }
        catch
        {
            Debug.Log("Error: Could not load profile from file.");
        }

        return ret;
    }
}
 