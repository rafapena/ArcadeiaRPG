﻿using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class OptionsFrame : MonoBehaviour
{
    public MenuFrame MainFrame;
    public Slider MusicSlider;
    public Slider SFXSlider;
    private bool HoveredOverMusic;
    private bool HoveredOverSFX;
    public GameObject TextSpeedList;
    public GameObject DifficultyList;
    private const float SLIDER_CHANGE_RATE = 0.01f;

    private void Update()
    {
        UpdateSlider(HoveredOverMusic, MusicSlider);
        UpdateSlider(HoveredOverSFX, SFXSlider);
    }

    public void Activate()
    {
        MainFrame.Activate();
        EventSystem.current.SetSelectedGameObject(transform.GetChild(1).gameObject);
        MusicSlider.value = PlayerPrefs.GetFloat(GameplayMaster.MASTER_MUSIC_VOLUME);
        SFXSlider.value = PlayerPrefs.GetFloat(GameplayMaster.MASTER_SFX_VOLUME);
        SelectTextSpeed(PlayerPrefs.GetInt(GameplayMaster.TEXT_DELAY_INDEX));
        if (DifficultyList.activeSelf) SelectDifficulty((int)GameplayMaster.Difficulty);
    }

    public void Deactivate()
    {
        MainFrame.Deactivate();
    }

    public void HoverOverMusic()
    {
        HoveredOverMusic = true;
    }

    public void HoverOverSFX()
    {
        HoveredOverSFX = true;
    }

    public void HoverAwayFromMusic()
    {
        HoveredOverMusic = false;
    }

    public void HoverAwayFromSFX()
    {
        HoveredOverSFX = false;
    }

    public void ChangeMusic(Slider slider)
    {
        PlayerPrefs.SetFloat(GameplayMaster.MASTER_MUSIC_VOLUME, slider.value);
    }

    public  void ChangeSFX(Slider slider)
    {
        PlayerPrefs.SetFloat(GameplayMaster.MASTER_SFX_VOLUME, slider.value);
    }

    private void UpdateSlider(bool highlighted, Slider slider)
    {
        if (!highlighted) return;
        else if (Input.GetKey(KeyCode.LeftArrow)) slider.value -= SLIDER_CHANGE_RATE;
        else if (Input.GetKey(KeyCode.RightArrow)) slider.value += SLIDER_CHANGE_RATE;
    }

    public void SelectTextSpeed(int index)
    {
        for (int i = 1; i < TextSpeedList.transform.childCount; i++) TextSpeedList.transform.GetChild(i).GetComponent<ListSelectable>().ClearHighlights();
        TextSpeedList.transform.GetChild(index + 1).GetComponent<ListSelectable>().KeepHighlighted();
        GameplayMaster.SetTextSpeed(index);
    }

    public void SelectDifficulty(int index)
    {
        for (int i = 1; i < DifficultyList.transform.childCount; i++) DifficultyList.transform.GetChild(i).GetComponent<ListSelectable>().ClearHighlights();
        DifficultyList.transform.GetChild(index + 1).GetComponent<ListSelectable>().KeepHighlighted();
        GameplayMaster.Difficulty = (GameplayMaster.Difficulties)index;
    }

    public void RestoreToDefaults()
    {
        MusicSlider.value = ResourcesMaster.DEFAULT_MASTER_MUSIC_VOLUME;
        SFXSlider.value = ResourcesMaster.DEFAULT_MASTER_SFX_VOLUME;
        SelectTextSpeed(ResourcesMaster.DEFAULT_TEXT_DELAY_INDEX);
    }
}
