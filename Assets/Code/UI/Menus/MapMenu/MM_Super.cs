using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public abstract class MM_Super : MonoBehaviour
{
    public MapMenuManager MenuManager;
    public GameObject Legend;
    public GameObject DefaultSelectedButton;
    public MenuFrame MainComponent;

    protected virtual void Start()
    {
        ReturnToInitialSetup();
    }

    protected virtual void Update()
    {
        //
    }

    public virtual void Open()
    {
        SetDefaultSelectedButton();
        MainComponent.Activate();
        Legend.SetActive(true);
    }

    public virtual void Close()
    {
        MainComponent.Deactivate();
        Legend.SetActive(false);
    }

    public abstract void GoBack();

    protected abstract void ReturnToInitialSetup();

    private void SetDefaultSelectedButton()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(DefaultSelectedButton);
    }
}
