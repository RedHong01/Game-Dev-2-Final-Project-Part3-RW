using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameResetWithBackups : MonoBehaviour
{
    public List<GameObject> gameObjectsToBackup;
    private List<GameObject> backupObjects;

    public Button resetButton;

    private void Start()
    {
        CreateBackups();
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetGame);
    }

    private void CreateBackups()
    {
        backupObjects = new List<GameObject>();
        foreach (GameObject obj in gameObjectsToBackup)
        {
            var backup = Instantiate(obj, obj.transform.position, obj.transform.rotation);
            backup.SetActive(false);
            backupObjects.Add(backup);
        }
    }

    public void ResetGame()
    {
        foreach (GameObject obj in gameObjectsToBackup)
        {
            obj.SetActive(false);
        }

        foreach (GameObject backup in backupObjects)
        {
            backup.SetActive(true);
        }

        gameObjectsToBackup.Clear();
        gameObjectsToBackup.AddRange(backupObjects);
        CreateBackups();
    }
}