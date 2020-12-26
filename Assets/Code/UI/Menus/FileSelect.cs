using UnityEngine;

public class FileSelect : MonoBehaviour
{
    public enum FileMode { Save, Load }
    public static FileMode FileSelectMode;

    public MenuFrame MainFrame;
    public FileSelectionList FilesList;

    private void Start()
    {
        MainFrame.Activate();
    }

    public void SelectFile()
    {

    }

    public void SaveGame()
    {

    }

    public void LoadGame()
    {

    }
}
