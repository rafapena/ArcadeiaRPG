using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ObtainingWeapons : MonoBehaviour
{
    private struct PlayerEquipBoost
    {
        public bool IsNeeded =>
            WeakestWeapon == null ||
            Weapon.Power > WeakestWeapon.Power ||
            Weapon.Range > WeakestWeapon.Range ||
            Weapon.CriticalRateBoost > WeakestWeapon.CriticalRateBoost;
        public BattlePlayer Player;
        public Weapon WeakestWeapon;
    }

    public MenuFrame InfoFrame;
    public Transform PartyList;
    public GameObject EquipToWhomFrame;
    public TextMeshProUGUI EquipToWhomMessage;
    public GameObject SelectEquipmentFrame;
    public InventoryToolSelectionList SelectEquipmentList;
    public GameObject ListBlocker;

    private PlayerParty Party;
    private static Weapon Weapon;
    private List<PlayerEquipBoost> NeededPlayers = new List<PlayerEquipBoost>();

    public int AmountToEquip { get; set; }
    private int SelectedPlayerIndex;
    private int EquippingProgress;
    private float DoneTimer;
    private const float DONE_TIME = 1f;

    public bool NooneNeeded => NeededPlayers.Count == 0;

    public bool IsDone => EquippingProgress == 4;

    // Update is called once per frame
    void Update()
    {
        if (EquippingProgress == 1 && InputMaster.GoingBack) EquippingProgress = 4;
        else if (EquippingProgress == 2 && InputMaster.GoingBack) CloseSwapEquipment();
        else if (EquippingProgress == 3 && Time.unscaledTime > DoneTimer) EquippingProgress = 4;
    }

    public void Initialize(PlayerParty party)
    {
        Party = party;
        Deactivate();
    }

    public void Deactivate()
    {
        SelectedPlayerIndex = -1;
        AmountToEquip = 0;
        EquippingProgress = 0;
        ListBlocker.SetActive(false);
        EquipToWhomFrame.SetActive(false);
        SelectEquipmentFrame.SetActive(false);
    }

    public void Refresh(Weapon wp)
    {
        Weapon = wp;
        InfoFrame.Activate();
        NeededPlayers.Clear();
        IEnumerable<BattlePlayer> players = Party.AllPlayers.Where(x => wp.CanEquipWith(x.Class));
        int i = 0;
        foreach (BattlePlayer p in players)
        {
            Transform entry = PartyList.GetChild(i++);
            bool alreadyEquipped = p.Weapons.Find(x => x.Id == wp.Id);
            entry.gameObject.SetActive(true);
            entry.GetChild(0).GetComponent<Image>().sprite = p.FaceImage;
            SetEntryAsEquipped(entry, alreadyEquipped);
            if (!alreadyEquipped)
            {
                PlayerEquipBoost peb;
                peb.Player = p;
                peb.WeakestWeapon = (p.Weapons.Count == 0 || p.Accessories.Count == BattleMaster.MAX_NUMBER_OF_EQUIPS) ? null : GetWeakestLink(p, wp);
                if (peb.IsNeeded) NeededPlayers.Add(peb);
                SetInfoFrameData(entry, wp, peb);
            }
        }
        for (; i < PartyList.childCount; i++) PartyList.GetChild(i).gameObject.SetActive(false);
    }

    public void RefreshWithOnlyNeededEntries()
    {
        InfoFrame.Activate();
        int i = 0;
        foreach (PlayerEquipBoost peb in NeededPlayers)
        {
            Transform entry = PartyList.GetChild(i++);
            entry.GetChild(0).GetComponent<Image>().sprite = peb.Player.FaceImage;
            SetEntryAsEquipped(entry, false);
            SetInfoFrameData(entry, Weapon, peb);
        }
        for (; i < PartyList.childCount; i++) PartyList.GetChild(i).gameObject.SetActive(false);
    }

    private void SetInfoFrameData(Transform t, Weapon wp, PlayerEquipBoost peb)
    {
        MenuMaster.SetNumberBoost(t.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>(), wp.Power - (peb.WeakestWeapon?.Power ?? 0), "+0");
        MenuMaster.SetNumberBoost(t.GetChild(2).GetChild(0).GetComponent<TextMeshProUGUI>(), wp.Range - (peb.WeakestWeapon?.Range ?? 0), "+0", "%");
        MenuMaster.SetNumberBoost(t.GetChild(3).GetChild(0).GetComponent<TextMeshProUGUI>(), wp.CriticalRateBoost - (peb.WeakestWeapon?.CriticalRateBoost ?? 0), "+0", "%");
    }

    private void SetEntryAsEquipped(Transform entry, bool equipped)
    {
        entry.GetChild(1).gameObject.SetActive(!equipped);
        entry.GetChild(2).gameObject.SetActive(!equipped);
        entry.GetChild(3).gameObject.SetActive(!equipped);
        entry.GetChild(4).gameObject.SetActive(equipped);
    }

    private Weapon GetWeakestLink(BattlePlayer p, Weapon wp)
    {
        switch (wp.WeaponType)
        {
            case BattleMaster.WeaponTypes.Staff:
                switch (wp.Element)
                {
                    case BattleMaster.Elements.Thunder: return GetWeakestLink(p, wp, 1, 1, 1);
                    case BattleMaster.Elements.Ice: return GetWeakestLink(p, wp, 1, 1, 1);
                    default: return GetWeakestLink(p, wp, 1, 0, 0);
                }
            case BattleMaster.WeaponTypes.Blade: return GetWeakestLink(p, wp, 1, 1, 1);
            case BattleMaster.WeaponTypes.Gun: return GetWeakestLink(p, wp, 1, 1, 1);
            default: return GetWeakestLink(p, wp, 1, 0, 0);
        }
    }

    private Weapon GetWeakestLink(BattlePlayer player, Weapon wp, int p, int r, int c)
    {
        IEnumerable<Weapon> weapons = player.Weapons.Where(x => x.WeaponType == wp.WeaponType);
        return weapons.Any() ? weapons.OrderBy(x => x.GetValue(p, r, c)).First() : null;
    }

    public void OpenEquipCharacterSelection()
    {
        EquippingProgress = 1;
        EquipToWhomFrame.SetActive(true);
        if (AmountToEquip > NeededPlayers.Count) AmountToEquip = NeededPlayers.Count;
        DisplayAmountToEquip();
        RefreshWithOnlyNeededEntries();
        EventSystem.current.SetSelectedGameObject(PartyList.transform.GetChild(0).gameObject);
    }

    private void DisplayAmountToEquip()
    {
        EquipToWhomMessage.text = "Equip " + Weapon.Name + " to whom?" + (AmountToEquip > 1 ? (" (" + AmountToEquip + ")") : "");
    }

    public void SelectCharacterEquip()
    {
        int index = EventSystem.current.currentSelectedGameObject.GetComponent<ListSelectable>().Index;
        if (EquippingProgress == 0 || EquippingProgress == 3 || PartyList.GetChild(index).GetChild(4).gameObject.activeSelf) return;
        PlayerEquipBoost peb = NeededPlayers[index];
        SelectedPlayerIndex = index;
        if (peb.Player.MaxEquipment && !peb.WeakestWeapon) OpenSwapEquipment(peb.Player);
        else EquipWeapon(index);
    }

    private void OpenSwapEquipment(BattlePlayer player)
    {
        EquippingProgress = 2;
        SelectEquipmentFrame.SetActive(true);
        SelectEquipmentList.Refresh(player.Equipment);
        SelectEquipmentList.Selecting = true;
        ListBlocker.SetActive(true);
        EventSystem.current.SetSelectedGameObject(SelectEquipmentList.transform.GetChild(0).gameObject);
    }

    public void CloseSwapEquipment()
    {
        EquippingProgress = 1;
        SelectEquipmentFrame.SetActive(false);
        SelectEquipmentList.Selecting = false;
        ListBlocker.SetActive(false);
        EventSystem.current.SetSelectedGameObject(PartyList.transform.GetChild(0).gameObject);
    }

    public void EquipWeapon()
    {
        SelectEquipmentFrame.SetActive(false);
        SetEntryAsEquipped(PartyList.GetChild(SelectedPlayerIndex), true);
        PlayerEquipBoost peb = NeededPlayers[SelectedPlayerIndex];
        IToolEquippable tool = SelectEquipmentList.SelectedObject as IToolEquippable;
        peb.Player.Unequip(tool);
        Party.Inventory.Add(tool);
        peb.Player.Equip(Weapon);
        Party.Inventory.Remove(Weapon);
        SelectEquipmentList.Selecting = false;
        CheckDone();
    }

    private void EquipWeapon(int index)
    {
        SetEntryAsEquipped(PartyList.GetChild(index), true);
        PlayerEquipBoost peb = NeededPlayers[index];
        if (peb.WeakestWeapon)
        {
            peb.Player.Unequip(peb.WeakestWeapon);
            Party.Inventory.Add(peb.WeakestWeapon);
        }
        peb.Player.Equip(Weapon);
        Party.Inventory.Remove(Weapon);
        CheckDone();
    }

    private void CheckDone()
    {
        AmountToEquip--;
        DisplayAmountToEquip();
        if (AmountToEquip == 0)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EquippingProgress = 3;
            DoneTimer = Time.unscaledTime + DONE_TIME;
            return;
        }
        else if (SelectedPlayerIndex >= 0) EventSystem.current.SetSelectedGameObject(PartyList.transform.GetChild(0).gameObject);
        ListBlocker.SetActive(false);
        EquippingProgress = 1;
    }
}
