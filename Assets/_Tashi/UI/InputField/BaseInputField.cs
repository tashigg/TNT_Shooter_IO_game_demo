using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseInputField : SaiMonoBehaviour
{
    [Header("Base InputField")]
    [SerializeField] protected InputField inputField;

    protected override void Start()
    {
        base.Start();
        this.AddOnChangeEvent();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadButton();
    }

    protected virtual void LoadButton()
    {
        if (this.inputField != null) return;
        this.inputField = GetComponent<InputField>();
        Debug.LogWarning(transform.name + ": LoadButton", gameObject);
    }

    protected virtual void AddOnChangeEvent()
    {
        this.inputField.onValueChanged.AddListener(this.onChanged);
    }

    protected abstract void onChanged(string value);
}
