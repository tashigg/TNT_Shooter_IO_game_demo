﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class UIMainMenu : MonoBehaviour
{
    public enum PreviewState
    {
        Idle,
        Run,
        Dead,
    }
    public Text textSelectCharacter;
    public Text textSelectHead;
    public InputField inputName;
    public Transform characterModelTransform;
    public bool saveImmediatelyOnSelectItem = true;
    private int selectCharacter = 0;
    private int selectHead = 0;
    private bool readyToUpdate;
    // Showing character / items
    public CharacterModel characterModel;
    public CharacterData characterData;
    public HeadData headData;
    public WeaponData weaponData;
    public PreviewState previewState;

    public int SelectCharacter
    {
        get { return selectCharacter; }
        set
        {
            selectCharacter = value;
            if (selectCharacter < 0)
                selectCharacter = MaxCharacter;
            if (selectCharacter > MaxCharacter)
                selectCharacter = 0;
            UpdateCharacter();
        }
    }

    public int SelectHead
    {
        get { return selectHead; }
        set
        {
            selectHead = value;
            if (selectHead < 0)
                selectHead = MaxHead;
            if (selectHead > MaxHead)
                selectHead = 0;
            UpdateHead();
        }
    }

    public int MaxHead
    {
        get { return GameInstance.AvailableHeads.Count - 1; }
    }

    public int MaxCharacter
    {
        get { return GameInstance.AvailableCharacters.Count - 1; }
    }

    private void Start()
    {
        StartCoroutine(StartRoutine());
        var uis = FindObjectsOfType<UIProductList>(true);
        foreach (var ui in uis)
        {
            ui.onPurchaseSuccess.RemoveListener(UpdateAvailableItems);
            ui.onPurchaseSuccess.AddListener(UpdateAvailableItems);
        }
    }

    private IEnumerator StartRoutine()
    {
        // Wait until player save validated
        while (!GameInstance.Singleton.PlayerSaveValidated)
        {
            yield return null;
        }
        OnClickLoadData();
        readyToUpdate = true;
    }

    private void Update()
    {
        if (!readyToUpdate)
            return;

        textSelectCharacter.text = (SelectCharacter + 1) + "/" + (MaxCharacter + 1);
        textSelectHead.text = (SelectHead + 1) + "/" + (MaxHead + 1);

        if (characterModel != null)
        {
            var animator = characterModel.CacheAnimator;
            switch (previewState)
            {
                case PreviewState.Idle:
                    animator.SetBool("IsDead", false);
                    animator.SetFloat("JumpSpeed", 0);
                    animator.SetFloat("MoveSpeed", 0);
                    animator.SetBool("IsGround", true);
                    animator.SetBool("IsDash", false);
                    animator.SetBool("DoAction", false);
                    animator.SetBool("IsIdle", true);
                    break;
                case PreviewState.Run:
                    animator.SetBool("IsDead", false);
                    animator.SetFloat("JumpSpeed", 0);
                    animator.SetFloat("MoveSpeed", 1);
                    animator.SetBool("IsGround", true);
                    animator.SetBool("IsDash", false);
                    animator.SetBool("DoAction", false);
                    animator.SetBool("IsIdle", false);
                    break;
                case PreviewState.Dead:
                    animator.SetBool("IsDead", true);
                    animator.SetFloat("JumpSpeed", 0);
                    animator.SetFloat("MoveSpeed", 0);
                    animator.SetBool("IsGround", true);
                    animator.SetBool("IsDash", false);
                    animator.SetBool("DoAction", false);
                    animator.SetBool("IsIdle", false);
                    break;
            }
        }

        UpdateWeapon();
    }

    private void UpdateCharacter()
    {
        if (characterModel != null)
            Destroy(characterModel.gameObject);
        characterData = GameInstance.GetAvailableCharacter(SelectCharacter);
        if (characterData == null || characterData.modelObject == null)
            return;
        characterModel = Instantiate(characterData.modelObject, characterModelTransform);
        characterModel.transform.localPosition = Vector3.zero;
        characterModel.transform.localEulerAngles = Vector3.zero;
        characterModel.transform.localScale = Vector3.one;
        if (headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        if (weaponData != null)
        {
            characterModel.SetWeaponModel(weaponData.rightHandObject, weaponData.leftHandObject, weaponData.shieldObject);
            characterModel.CacheAnimator.SetInteger("WeaponAnimId", weaponData.weaponAnimId);
        }
        characterModel.gameObject.SetActive(true);
    }

    private void UpdateHead()
    {
        headData = GameInstance.GetAvailableHead(SelectHead);
        if (characterModel != null && headData != null)
            characterModel.SetHeadModel(headData.modelObject);
    }

    private void UpdateWeapon()
    {
        var savedWeapons = PlayerSave.GetWeapons();
        var minPosition = int.MaxValue;
        foreach (int position in savedWeapons.Keys)
        {
            if (minPosition > position)
                minPosition = position;
        }
        var newWeaponData = GameInstance.GetAvailableWeapon(savedWeapons[minPosition]);
        if (weaponData != newWeaponData)
        {
            weaponData = newWeaponData;
            if (characterModel != null && weaponData != null)
            {
                characterModel.SetWeaponModel(weaponData.rightHandObject, weaponData.leftHandObject, weaponData.shieldObject);
                characterModel.CacheAnimator.SetInteger("WeaponAnimId", weaponData.weaponAnimId);
            }
        }
    }

    public void OnClickBackCharacter()
    {
        --SelectCharacter;
        if (saveImmediatelyOnSelectItem)
            OnClickSaveData();
    }

    public void OnClickNextCharacter()
    {
        ++SelectCharacter;
        if (saveImmediatelyOnSelectItem)
            OnClickSaveData();
    }

    public void OnClickBackHead()
    {
        --SelectHead;
        if (saveImmediatelyOnSelectItem)
            OnClickSaveData();
    }

    public void OnClickNextHead()
    {
        ++SelectHead;
        if (saveImmediatelyOnSelectItem)
            OnClickSaveData();
    }

    public void OnInputNameChanged(string eventInput)
    {
        PlayerSave.SetPlayerName(inputName.text);
    }

    public void OnClickSaveData()
    {
        PlayerSave.SetCharacter(SelectCharacter);
        PlayerSave.SetHead(SelectHead);
        PlayerSave.SetPlayerName(inputName.text);
        PhotonNetwork.LocalPlayer.NickName = PlayerSave.GetPlayerName();
    }

    public void OnClickLoadData()
    {
        inputName.text = PlayerSave.GetPlayerName();
        SelectHead = PlayerSave.GetHead();
        SelectCharacter = PlayerSave.GetCharacter();
    }

    public void UpdateAvailableItems()
    {
        GameInstance.Singleton.UpdateAvailableItems();
    }
}
