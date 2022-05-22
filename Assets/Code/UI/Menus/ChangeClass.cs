using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using UnityEditor.Tilemaps;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;

public class ChangeClass : MonoBehaviour
{
    private enum Selections { StartingUp, SelectingCharacter, SelectingClass, ChangingClass, ChangedClass }
    private Selections Selection;

    public MenuFrame Title;
    public Item ScrollToUse;
    public MenuFrame PartyListFrame;
    public PlayerSelectionList PartyList;
    public Image SelectedCharacter;
    public MenuFrame ClassListFrame;
    public Transform ClassList;
    public GameObject ClassListBlocker;
    public GameObject ConfirmChange;

    // Character class changed
    public Image CharacterUpdatedImage;
    public MenuFrame CharacterUpdatedFrame;
    public MenuFrame CharacterBubbleFrame;
    public TextMeshProUGUI CharacterMessage;

    // Info frame
    public MenuFrame ClassInfoFrame;
    public TextMeshProUGUI ClassName;
    public TextMeshProUGUI ClassDescription;
    public Transform ClassMaxHPStat;
    public StatsList ClassStatsList;
    public Image ClassCanWield1;
    public Image ClassCanWield2;
    public Sprite EmptyStar;
    public Sprite HalfStar;
    public Sprite WholeStar;

    // Info for globals
    [HideInInspector] public PlayerParty PartyInfo;
    private float DoneTimer;
    private const float SELECT_CHARACTER_TIMER = 1.5f;
    private const float CHANGING_CLASS_TIMER = 6f;
    private const float CHANGED_CLASS_TIMER = 2f;

    // Classes list details
    private List<BattlerClass> ClassOptions;
    private int SelectedClassListIndex;
    private ListSelectable SelectedClassListButton;
    private BattlerClass SelectedClassListObject;
    private Stats NewClassStats;

    private bool PassedTime => Time.unscaledTime > DoneTimer;

    void Start()
    {
        PartyInfo = GameplayMaster.Party;
        DoneTimer = Time.unscaledTime + SELECT_CHARACTER_TIMER;
        Selection = Selections.StartingUp;
        ClassListBlocker.SetActive(false);
        ConfirmChange.SetActive(false);
        SelectedCharacter.gameObject.SetActive(false);
        ClassOptions = new List<BattlerClass>();
        NewClassStats = new Stats { };
    }

    void Update()
    {
        switch (Selection)
        {
            case Selections.StartingUp:
                if (PassedTime) SetupCharacterSelection();
                break;
            case Selections.SelectingCharacter:
                if (InputMaster.GoingBack) SceneMaster.CloseChangeClassMenu(PartyInfo);
                break;
            case Selections.SelectingClass:
                if (InputMaster.GoingBack)
                {
                    if (ConfirmChange.activeSelf) UndoSelectClass();
                    else UndoSelectCharacter();
                }
                break;
            case Selections.ChangingClass:
                if (PassedTime) SetupChangedClass();
                break;
            case Selections.ChangedClass:
                if (PassedTime && (InputMaster.ProceedInMenu || InputMaster.GoingBack)) SceneMaster.CloseChangeClassMenu(PartyInfo);
                break;
        }
    }

    private void SetupCharacterSelection()
    {
        Selection = Selections.SelectingCharacter;
        Title.Activate();
        PartyListFrame.Activate();
        PartyList.Refresh(PartyInfo.AllPlayers);
        EventSystem.current.SetSelectedGameObject(PartyList.transform.GetChild(0).gameObject);
    }

    public void SelectCharacter()
    {
        if (!MenuMaster.ReadyToSelectInMenu) return;
        PartyListFrame.Deactivate();
        PartyList.SetSelected();
        PartyList.UnhighlightAll();
        PartyList.SelectedButton.KeepSelected();
        SetupSelectClass();
    }

    private void UndoSelectCharacter()
    {
        Selection = Selections.SelectingCharacter;
        EventSystem.current.SetSelectedGameObject(PartyList.SelectedButton.gameObject);
        PartyListFrame.Activate();
        PartyList.ClearSelections();
        ClassListFrame.Deactivate();
        ClassInfoFrame.Deactivate();
    }

    private void SetupSelectClass()
    {
        Selection = Selections.SelectingClass;
        //SelectedCharacter.sprite = PartyList.SelectedObject.MainImage;
        ClassListFrame.Activate();
        RefreshClassList();
        EventSystem.current.SetSelectedGameObject(ClassList.GetChild(0).gameObject);
    }

    private void RefreshClassList()
    {
        ClassOptions.Clear();
        BattlerClass bc = PartyList.SelectedObject.Class;
        if (bc.IsBaseClass)
        {
            if (bc.UpgradedClass1) ClassOptions.Add(bc.UpgradedClass1);
            if (bc.UpgradedClass2) ClassOptions.Add(bc.UpgradedClass2);
        }
        else if (bc.IsAdvancedClass)
        {
            BattlePlayer p = PartyList.SelectedObject as BattlePlayer;
            ClassOptions.AddRange(p.ClassSet.FindAll(x => x.Id != p.Class.Id));
        }

        int i = 0;
        foreach (BattlerClass option in ClassOptions)
        {
            ClassList.GetChild(i).GetComponent<ListSelectable>().Index = i;
            ClassList.GetChild(i).gameObject.SetActive(true);
            ClassList.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().text = ClassOptions[i].Name;
            i++;
        }
        for (; i < ClassList.childCount; i++) ClassList.GetChild(i).gameObject.SetActive(false);
    }

    public void HoverOverClass()
    {
        SelectedClassListIndex = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>().Index;
        SelectedClassListButton = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>();
        SelectedClassListObject = ClassOptions[SelectedClassListIndex];
        RefreshClassInfo(PartyList.SelectedObject as BattlePlayer, SelectedClassListObject);
    }

    private void RefreshClassInfo(BattlePlayer bp, BattlerClass bc)
    {
        ClassInfoFrame.Activate();
        ClassName.text = bc.Name.ToUpper();
        ClassDescription.text = bc.Description;
        SetCanWieldInfo(ClassCanWield1, bc.UsableWeapon1Type);
        SetCanWieldInfo(ClassCanWield2, bc.UsableWeapon2Type);
        NewClassStats.Set(bc.BaseStats);
        NewClassStats.ConvertFromBaseToActual(PartyInfo.Level, bp.NaturalStats);
        ClassStatsList.Setup(bp, NewClassStats);

        GetStarInfo(bc.BaseStats.MaxHP, 0);
        GetStarInfo(bc.BaseStats.Atk, 1);
        GetStarInfo(bc.BaseStats.Def, 2);
        GetStarInfo(bc.BaseStats.Map, 3);
        GetStarInfo(bc.BaseStats.Mar, 4);
        GetStarInfo(bc.BaseStats.Spd, 5);
        GetStarInfo(bc.BaseStats.Tec, 6);
        GetStarInfo(bc.BaseStats.Rec, 7);
    }

    private void SetCanWieldInfo(Image classCanWield, BattleMaster.WeaponTypes usuableWeaponType)
    {
        bool usable = usuableWeaponType != BattleMaster.WeaponTypes.None;
        classCanWield.gameObject.SetActive(usable);
        if (usable) classCanWield.sprite = UIMaster.WeaponImages[usuableWeaponType];
    }

    private void GetStarInfo(int stat, int index)
    {
        Transform statEntryStars = ClassStatsList.transform.GetChild(index).GetChild(2);
        for (int i = 0; i < statEntryStars.childCount; i++)
        {
            Image starImg = statEntryStars.GetChild(i).GetComponent<Image>();
            if (stat >= 10) starImg.sprite = WholeStar;
            else if (stat > 3) starImg.sprite = HalfStar;
            else starImg.sprite = EmptyStar;
            stat -= 10;
        }
    }

    public void SelectClass()
    {
        ClassListBlocker.SetActive(true);
        ConfirmChange.SetActive(true);
        ConfirmChange.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "CHANGE " + PartyList.SelectedObject.Name.ToUpper() + " INTO A " + SelectedClassListObject.Name.ToUpper() + "?";
        SelectedClassListButton.KeepSelected();
        EventSystem.current.SetSelectedGameObject(ConfirmChange.transform.GetChild(2).gameObject);
    }

    public void UndoSelectClass()
    {
        ClassListBlocker.SetActive(false);
        ConfirmChange.SetActive(false);
        SelectedClassListButton.ClearHighlights();
        EventSystem.current.SetSelectedGameObject(SelectedClassListButton.gameObject);
    }

    public void ChangeClassStart()
    {
        Selection = Selections.ChangingClass;
        DoneTimer = Time.unscaledTime + CHANGING_CLASS_TIMER;
        Title.Deactivate();
        ClassListBlocker.SetActive(false);
        ConfirmChange.SetActive(false);
        ClassListFrame.Deactivate();
        ClassInfoFrame.Deactivate();
        (PartyList.SelectedObject as BattlePlayer).ChangeClass(SelectedClassListObject);
        EventSystem.current.SetSelectedGameObject(null);
        HandleEquipment();
    }

    private void HandleEquipment()
    {
        BattlePlayer p = PartyList.SelectedObject as BattlePlayer;
        foreach (Weapon wp in p.Weapons.FindAll(x => !x.CanEquipWith(p.Class))) PartyInfo.Inventory.Add(wp);
        foreach (Accessory ac in p.Accessories.FindAll(x => !x.CanEquipWith(p.Class))) PartyInfo.Inventory.Add(ac);
        p.Weapons.RemoveAll(x => !x.CanEquipWith(p.Class));
        p.Accessories.RemoveAll(x => !x.CanEquipWith(p.Class));
        if (p.Weapons.Count == 0)
        {
            IEnumerable<Weapon> equippableWeapons = PartyInfo.Inventory.Weapons.Where(x => x.CanEquipWith(p.Class)).OrderByDescending(x => x.Power);
            if (equippableWeapons != null && equippableWeapons.Any())
            {
                Weapon strongestInInventory = equippableWeapons.First();
                p.Equip(strongestInInventory);
                PartyInfo.Inventory.Remove(strongestInInventory);
            }
        }
    }

    private void SetupChangedClass()
    {
        Selection = Selections.ChangedClass;
        DoneTimer = Time.unscaledTime + CHANGED_CLASS_TIMER;
        //CharacterUpdatedImage.sprite = PartyList.SelectedObject.MainImage;
        CharacterUpdatedFrame.Activate();
        CharacterBubbleFrame.Activate();
        CharacterMessage.text = "I CHANGED YAY!";
    }
}