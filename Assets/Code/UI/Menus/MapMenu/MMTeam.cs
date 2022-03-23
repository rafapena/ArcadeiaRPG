using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.AI;
using UnityEditor.Tilemaps;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Linq;
using UnityEditor;

public class MMTeam : MM_Super
{
    private enum Selections { None, SelectFirstPlayer, SelectSecondPlayer }

    public MenuFrame Relations;
    public TextMeshProUGUI RelationsCharacterName;
    public GridLayoutGroup RelationsList;
    public PlayerSelectionList OneAvatar;
    public PlayerSelectionList CurrentPartyList;
    public PlayerSelectionList ReservePartyList;
    public TextMeshProUGUI SwapLabel;

    private Selections Selection;
    private bool ListsSetup;

    private bool SelectedFirstFromReserves;
    private int ToSwapWithIndex;
    private Battler ToSwapWithTeammate;
    private ListSelectable ToSwapWithButton;

    public override void Open()
    {
        base.Open();
        Relations.Activate();
        SetupLists();
        // Manual startup - assumes the default selected button is the first entry in the Current Party List
        CurrentPartyList.ResetSelected();
        HoverOverCurrentPlayer();
    }

    public override void Close()
    {
        base.Close();
        Relations.Deactivate();
    }

    public override void GoBack()
    {
        switch (Selection)
        {
            case Selections.SelectFirstPlayer:
                Selection = Selections.None;
                MenuManager.GoToMain();
                break;
            case Selections.SelectSecondPlayer:
                UndoFirstSelection(true);
                break;
        }
    }

    protected override void ReturnToInitialSetup()
    {
        SwapLabel.gameObject.SetActive(false);
        OneAvatar.ClearSelections();
        CurrentPartyList.ClearSelections();
        ReservePartyList.ClearSelections();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Team List --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SetupLists()
    {
        Selection = Selections.SelectFirstPlayer;
        OneAvatar.Refresh(MenuManager.PartyInfo.Players.Take(1).ToList());
        CurrentPartyList.Refresh(MenuManager.PartyInfo.Players.Skip(1).ToList());
        ReservePartyList.Refresh(MenuManager.PartyInfo.AllPlayers.Skip(MenuManager.PartyInfo.Players.Count).ToList());
        ListsSetup = true;
    }

    public void HoverOverCurrentPlayer()
    {
        if (!ListsSetup) return;
        CurrentPartyList.SetSelected();
        HoverOverPlayer(CurrentPartyList.SelectedObject as BattlePlayer);
    }

    public void HoverOverReservePlayer()
    {
        if (!ListsSetup) return;
        ReservePartyList.SetSelected();
        HoverOverPlayer(ReservePartyList.SelectedObject as BattlePlayer);
    }

    private void HoverOverPlayer(BattlePlayer p)
    {
        Relations.Activate();
        RelationsCharacterName.text = p.Name.ToUpper();
        int i = 0;
        int i0 = 0;
        for (; i < MenuManager.PartyInfo.Players.Count; i++)
        {
            int playerId = MenuManager.PartyInfo.Players[i].Id;
            if (p.Id == playerId) continue;
            Transform entry = RelationsList.transform.GetChild(i0);
            RelationBar relBar = entry.GetChild(1).GetComponent<RelationBar>();
            entry.gameObject.SetActive(true);
            relBar.Setup(p.Relations[playerId]);
            i0++;
        }
        for (; i0 < RelationsList.transform.childCount; i0++)
        {
            Transform entry = RelationsList.transform.GetChild(i0);
            entry.gameObject.SetActive(false);
        }
    }

    public void DeselectPlayer()
    {
        Relations.Deactivate();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// -- Selecting players --
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void SelectCurrentPlayer()
    {
        if (!ListsSetup) return;
        else if (Selection == Selections.SelectFirstPlayer)
        {
            SelectedFirstFromReserves = false;
            SelectFirstPlayer(CurrentPartyList, ReservePartyList);
        }
        else if (Selection == Selections.SelectSecondPlayer) SelectSecondPlayer(false);
    }

    public void SelectReservePlayer()
    {
        if (!ListsSetup) return;
        else if (Selection == Selections.SelectFirstPlayer)
        {
            SelectedFirstFromReserves = true;
            SelectFirstPlayer(ReservePartyList, CurrentPartyList);
        }
        else if (Selection == Selections.SelectSecondPlayer) SelectSecondPlayer(true);
    }

    private void SelectFirstPlayer(PlayerSelectionList psl, PlayerSelectionList landingPsl)
    {
        Selection = Selections.SelectSecondPlayer;
        ToSwapWithIndex = psl.SelectedIndex;
        ToSwapWithTeammate = psl.SelectedObject;
        ToSwapWithButton = psl.SelectedButton;
        psl.SelectedButton.KeepSelected();
        SwapLabel.gameObject.SetActive(true);
        SwapLabel.text = "SWAP " + psl.SelectedObject.Name.ToUpper() + " WITH...";
        EventSystem.current.SetSelectedGameObject(landingPsl.transform.GetChild(0).gameObject);
    }

    private void UndoFirstSelection(bool moveToPrevious)
    {
        Selection = Selections.SelectFirstPlayer;
        SwapLabel.gameObject.SetActive(false);
        if (moveToPrevious) EventSystem.current.SetSelectedGameObject(ToSwapWithButton.gameObject);
        ToSwapWithTeammate = null;
        ToSwapWithButton.ClearHighlights();
    }

    private void SelectSecondPlayer(bool selectedSecondFromReserves)
    {
        int firstSelectedIndex = SelectedFirstFromReserves ? (MenuManager.PartyInfo.Players.Count + ToSwapWithIndex) : (1 + ToSwapWithIndex);
        int secondSelectedIndex = selectedSecondFromReserves ? (MenuManager.PartyInfo.Players.Count + ReservePartyList.SelectedIndex) : (1 + CurrentPartyList.SelectedIndex);
        UndoFirstSelection(false);
        
        // Swap teammates
        BattlePlayer temp = MenuManager.PartyInfo.AllPlayers[firstSelectedIndex];
        MenuManager.PartyInfo.AllPlayers[firstSelectedIndex] = MenuManager.PartyInfo.AllPlayers[secondSelectedIndex];
        MenuManager.PartyInfo.AllPlayers[secondSelectedIndex] = temp;

        MenuManager.PartyInfo.UpdateActivePlayers();
        SetupLists();
        if (selectedSecondFromReserves) HoverOverReservePlayer();
        else HoverOverCurrentPlayer();
    }
}