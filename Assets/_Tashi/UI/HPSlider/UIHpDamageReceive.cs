using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHpDamageReceive : SaiMonoBehaviour
{
    public Text hpText;
    public Image hpImage;
    public DamageReceiver damageReceiver;

    private void FixedUpdate()
    {
        this.ShowHP();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadHPText();
        this.LoadHPImage();
        this.LoadDamageReceiver();
    }

    protected virtual void LoadHPText()
    {
        if (this.hpText != null) return;
        this.hpText = GetComponentInChildren<Text>();
        Debug.LogWarning(transform.name + ": LoadHPText", gameObject);
    }

    protected virtual void LoadHPImage()
    {
        if (this.hpImage != null) return;
        this.hpImage = transform.Find("Slider").Find("HpFill").GetComponent<Image>();
        Debug.LogWarning(transform.name + ": LoadHPImage", gameObject);
    }

    protected virtual void LoadDamageReceiver()
    {
        if (this.damageReceiver != null) return;
        this.damageReceiver = transform.parent.GetComponent<DamageReceiver>();
        Debug.LogWarning(transform.name + ": LoadDamageReceiver", gameObject);
    }

    protected virtual void ShowHP()
    {
        int hp = this.damageReceiver.HP;
        int max = this.damageReceiver.HPMax;
        this.hpText.text = $"{hp}/{max}";
        float fill = (float)hp / (float)max;
        this.hpImage.fillAmount = fill;
    }
}
