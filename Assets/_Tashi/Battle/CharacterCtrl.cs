using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCtrl : SaiMonoBehaviour
{
    public CharacterNetwork charNetwork;
    public TashiCharacterEntity charEnitiy;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadCharNetwork();
        this.LoadTashiCharacterEntity();
    }

    protected virtual void LoadCharNetwork()
    {
        if (this.charNetwork != null) return;
        this.charNetwork = GetComponent<CharacterNetwork>();
        Debug.LogWarning(transform.name + ": LoadCharNetwork", gameObject);
    }

    protected virtual void LoadTashiCharacterEntity()
    {
        if (this.charEnitiy != null) return;
        this.charEnitiy = GetComponent<TashiCharacterEntity>();
        this.charEnitiy.charCtrl = this;
        Debug.LogWarning(transform.name + ": LoadTashiCharacterEntity", gameObject);
    }
}
