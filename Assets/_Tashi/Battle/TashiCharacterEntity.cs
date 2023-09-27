using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Photon.Pun;

[RequireComponent(typeof(TashiCharacterMovement))]
[DisallowMultipleComponent]
public class TashiCharacterEntity : SaiMonoBehaviour
{
    public const float DISCONNECT_WHEN_NOT_RESPAWN_DURATION = 60;
    public const int MAX_EQUIPPABLE_WEAPON_AMOUNT = 10;

    public enum ViewMode
    {
        TopDown,
        ThirdPerson,
    }

    [System.Serializable]
    public class ViewModeSettings
    {
        public Vector3 targetOffsets = Vector3.zero;
        public float zoomDistance = 3f;
        public float minZoomDistance = 3f;
        public float maxZoomDistance = 3f;
        public float xRotation = 45f;
        public float minXRotation = 45f;
        public float maxXRotation = 45f;
        public float yRotation = 0f;
        public float fov = 60f;
        public float nearClipPlane = 0.3f;
        public float farClipPlane = 1000f;
    }

    public ViewMode viewMode;
    public ViewModeSettings topDownViewModeSettings;
    public ViewModeSettings thirdPersionViewModeSettings;
    public bool doNotLockCursor;
    public Transform damageLaunchTransform;
    public Transform effectTransform;
    public Transform characterModelTransform;
    public GameObject[] localPlayerObjects;
    public float dashDuration = 1.5f;
    public float dashMoveSpeedMultiplier = 1.5f;
    public float blockMoveSpeedMultiplier = 0.75f;
    public float returnToMoveDirectionDelay = 1f;
    public float endActionDelay = 0.75f;
    [Header("UI")]
    public Transform hpBarContainer;
    public Image hpFillImage;
    public Text hpText;
    public Image armorFillImage;
    public Text armorText;
    public Text nameText;
    public Text levelText;
    public GameObject attackSignalObject;
    public GameObject attackSignalObjectForTeamA;
    public GameObject attackSignalObjectForTeamB;
    [Header("Effect")]
    public GameObject invincibleEffect;
    public CharacterCtrl charCtrl;

    #region Sync Vars
    private SyncHpRpcComponent syncHp = null;
    public int Hp
    {
        get { return syncHp.Value; }
        set
        {
            if (!LobbyManager.Instance._isLobbyHost)
                return;

            if (value <= 0)
            {
                value = 0;
                if (!IsDeadMarked)
                {
                    DeathTime = Time.unscaledTime;
                    IsDeadMarked = true;
                }
            }

            if (value > TotalHp)
                value = TotalHp;

            syncHp.Value = value;
        }
    }

    private SyncArmorRpcComponent syncArmor = null;
    public int Armor
    {
        get { return syncArmor.Value; }
        set
        {
            if (!LobbyManager.Instance._isLobbyHost)
                return;

            if (value <= 0)
                value = 0;

            if (value > TotalArmor)
                value = TotalArmor;

            syncArmor.Value = value;
        }
    }

    private SyncExpRpcComponent syncExp = null;
    public virtual int Exp
    {
        get { return syncExp.Value; }
        set
        {
            if (!LobbyManager.Instance._isLobbyHost)
                return;

            var gameplayManager = GameplayManager.Singleton;
            while (true)
            {
                if (Level == gameplayManager.maxLevel)
                    break;

                var currentExp = gameplayManager.GetExp(Level);
                if (value < currentExp)
                    break;
                var remainExp = value - currentExp;
                value = remainExp;
                ++Level;
                StatPoint += gameplayManager.addingStatPoint;
            }

            syncExp.Value = value;
        }
    }

    private SyncLevelRpcComponent syncLevel = null;
    public int Level { get { return syncLevel.Value; } set { syncLevel.Value = value; } }

    private SyncStatPointRpcComponent syncStatPoint = null;
    public int StatPoint { get { return syncStatPoint.Value; } set { syncStatPoint.Value = value; } }

    private SyncWatchAdsCountRpcComponent syncWatchAdsCount = null;
    public byte WatchAdsCount { get { return syncWatchAdsCount.Value; } set { syncWatchAdsCount.Value = value; } }

    private SyncSelectCharacterRpcComponent syncSelectCharacter = null;
    public int SelectCharacter { get { return syncSelectCharacter.Value; } set { syncSelectCharacter.Value = value; } }

    private SyncSelectHeadRpcComponent syncSelectHead = null;
    public int SelectHead { get { return syncSelectHead.Value; } set { syncSelectHead.Value = value; } }

    private SyncSelectWeaponsRpcComponent syncSelectWeapons = null;
    public int[] SelectWeapons { get { return syncSelectWeapons.Value; } set { syncSelectWeapons.Value = value; } }

    private SyncSelectCustomEquipmentsRpcComponent syncSelectCustomEquipments = null;
    public int[] SelectCustomEquipments { get { return syncSelectCustomEquipments.Value; } set { syncSelectCustomEquipments.Value = value; } }

    private SyncSelectWeaponIndexRpcComponent syncSelectWeaponIndex = null;
    public int SelectWeaponIndex { get { return syncSelectWeaponIndex.Value; } set { syncSelectWeaponIndex.Value = value; } }

    private SyncIsInvincibleRpcComponent syncIsInvincible = null;
    public bool IsInvincible { get { return syncIsInvincible.Value; } set { syncIsInvincible.Value = value; } }

    private SyncAttributeAmountsRpcComponent syncAttributeAmounts = null;
    public AttributeAmounts AttributeAmounts { get { return syncAttributeAmounts.Value; } set { syncAttributeAmounts.Value = value; } }

    private SyncExtraRpcComponent syncExtra = null;
    public string Extra { get { return syncExtra.Value; } set { syncExtra.Value = value; } }

    #endregion


    private void LateUpdate()
    {
        UpdateAnimation();
    }

    protected virtual void Update()
    {
        UpdateInput();
    }

    private void FixedUpdate()
    {
        if (!previousPosition.HasValue)
            previousPosition = CacheTransform.position;
        var currentMove = CacheTransform.position - previousPosition.Value;
        currentVelocity = currentMove / Time.deltaTime;
        previousPosition = CacheTransform.position;

        if (!LobbyManager.Instance.IsReadyToPlay()) return;

        UpdateMovements();
    }
    public virtual bool IsDead()
    {
        return this.charCtrl.charNetwork.isDead.Value;
    }

    public virtual bool IsBot
    {
        get { return false; }
    }

    [Header("Weapons")]
    public System.Action onDead;
    public readonly HashSet<TashiPickupEntity> PickableEntities = new HashSet<TashiPickupEntity>();
    public readonly TashiEquippedWeapon[] equippedWeapons = new TashiEquippedWeapon[MAX_EQUIPPABLE_WEAPON_AMOUNT];

    protected ViewMode dirtyViewMode;
    protected Camera targetCamera;
    protected Vector3 cameraForward;
    protected Vector3 cameraRight;
    protected Coroutine attackRoutine;
    protected Coroutine reloadRoutine;
    [SerializeField] protected CharacterModel characterModel;
    protected CharacterData characterData;
    protected HeadData headData;
    protected Dictionary<int, CustomEquipmentData> customEquipmentDict = new Dictionary<int, CustomEquipmentData>();
    protected int defaultWeaponIndex = -1;
    protected bool isMobileInput;
    protected Vector3 inputMove;
    protected Vector3 inputDirection;
    [SerializeField] protected bool inputAttack;
    [SerializeField] protected bool inputJump;
    [SerializeField] protected Vector3 dashInputMove;
    [SerializeField] protected float dashingTime;
    protected Vector3? previousPosition;
    protected Vector3 currentVelocity;
    protected float lastActionTime;
    protected Coroutine endActionDelayCoroutine;

    public float StartReloadTime { get; protected set; }
    public float ReloadDuration { get; protected set; }
    public bool IsReady { get; protected set; }
    public bool IsDeadMarked { get; protected set; }
    public bool IsGrounded { get { return CacheCharacterMovement.IsGrounded; } }
    public bool IsPlayingAttackAnim { get; protected set; }
    public bool IsReloading { get; protected set; }
    public bool IsDashing { get; protected set; }
    public bool HasAttackInterruptReload { get; protected set; }
    public float DeathTime { get; protected set; }
    public float InvincibleTime { get; protected set; }

    public float FinishReloadTimeRate
    {
        get { return (Time.unscaledTime - StartReloadTime) / ReloadDuration; }
    }

    public TashiEquippedWeapon CurrentEquippedWeapon
    {
        get
        {
            try
            { return equippedWeapons[SelectWeaponIndex]; }
            catch
            { return TashiEquippedWeapon.Empty; }
        }
    }

    public TashiWeaponData WeaponData
    {
        get
        {
            try
            { return CurrentEquippedWeapon.WeaponData; }
            catch
            { return null; }
        }
    }

    private bool isHidding;
    public bool IsHidding
    {
        get { return isHidding; }
        set
        {
            isHidding = value;
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                renderer.enabled = !isHidding;
            var canvases = GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
                canvas.enabled = !isHidding;
            var projectors = GetComponentsInChildren<Projector>();
            foreach (var projector in projectors)
                projector.enabled = !isHidding;
        }
    }

    public Transform CacheTransform { get; private set; }
    public TashiCharacterMovement CacheCharacterMovement { get; private set; }
    protected bool refreshingSumAddStats = true;
    protected CharacterStats sumAddStats = new CharacterStats();

    public bool canControl = true;
    public virtual CharacterStats SumAddStats
    {
        get
        {
            if (refreshingSumAddStats)
            {
                var addStats = new CharacterStats();
                if (headData != null)
                    addStats += headData.stats;
                if (characterData != null)
                    addStats += characterData.stats;
                if (WeaponData != null)
                    addStats += WeaponData.stats;
                if (customEquipmentDict != null)
                {
                    foreach (var value in customEquipmentDict.Values)
                    {
                        addStats += value.stats;
                    }
                }
                if (AttributeAmounts != null)
                {
                    foreach (var kv in AttributeAmounts.Dict)
                    {
                        CharacterAttributes attribute;
                        if (TashiGameplayManager.Singleton.Attributes.TryGetValue(kv.Key, out attribute))
                            addStats += attribute.stats * kv.Value;
                    }
                }
                sumAddStats = addStats;
                refreshingSumAddStats = false;
            }
            return sumAddStats;
        }
    }

    public int TotalHp
    {
        get
        {
            var total = TashiGameplayManager.Singleton.baseMaxHp + SumAddStats.addMaxHp;
            return total;
        }
    }

    public int TotalArmor
    {
        get
        {
            var total = TashiGameplayManager.Singleton.baseMaxArmor;
            return total;
        }
    }

    public int TotalMoveSpeed
    {
        get
        {
            var total = TashiGameplayManager.Singleton.baseMoveSpeed;
            return total;
        }
    }

    public float TotalWeaponDamageRate
    {
        get
        {
            var total = TashiGameplayManager.Singleton.baseWeaponDamageRate + SumAddStats.addWeaponDamageRate;

            var maxValue = TashiGameplayManager.Singleton.maxWeaponDamageRate;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public float TotalReduceDamageRate
    {
        get
        {
            var total = TashiGameplayManager.Singleton.baseReduceDamageRate + SumAddStats.addReduceDamageRate;

            var maxValue = TashiGameplayManager.Singleton.maxReduceDamageRate;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public float TotalBlockReduceDamageRate
    {
        get
        {
            var total = TashiGameplayManager.Singleton.baseBlockReduceDamageRate + SumAddStats.addBlockReduceDamageRate;

            var maxValue = TashiGameplayManager.Singleton.maxBlockReduceDamageRate;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public float TotalArmorReduceDamage
    {
        get
        {
            var total = TashiGameplayManager.Singleton.baseArmorReduceDamage + SumAddStats.addArmorReduceDamage;

            var maxValue = TashiGameplayManager.Singleton.maxArmorReduceDamage;
            if (total < maxValue)
                return total;
            else
                return maxValue;
        }
    }

    public float TotalExpRate
    {
        get
        {
            var total = 1 + SumAddStats.addExpRate;
            return total;
        }
    }

    public float TotalScoreRate
    {
        get
        {
            var total = 1 + SumAddStats.addScoreRate;
            return total;
        }
    }

    public float TotalHpRecoveryRate
    {
        get
        {
            var total = 1 + SumAddStats.addHpRecoveryRate;
            return total;
        }
    }

    public float TotalArmorRecoveryRate
    {
        get
        {
            var total = 1 + SumAddStats.addArmorRecoveryRate;
            return total;
        }
    }

    public float TotalDamageRateLeechHp
    {
        get
        {
            var total = SumAddStats.addDamageRateLeechHp;
            return total;
        }
    }

    public virtual int RewardExp
    {
        get { return TashiGameplayManager.Singleton.GetRewardExp(Level); }
    }

    public virtual int KillScore
    {
        get { return TashiGameplayManager.Singleton.GetKillScore(Level); }
    }

    protected virtual void Init()
    {
        if (!LobbyManager.Instance._isLobbyHost)
            return;

        Hp = 0;
        Armor = 0;
        Exp = 0;
        Level = 1;
        StatPoint = 0;
        WatchAdsCount = 0;
        SelectCharacter = 0;
        SelectHead = 0;
        SelectWeapons = new int[0];
        SelectCustomEquipments = new int[0];
        SelectWeaponIndex = -1;
        IsInvincible = false;
        AttributeAmounts = new AttributeAmounts();
        Extra = string.Empty;
    }

    protected override void Awake()
    {
        base.Awake();
        gameObject.layer = TashiGameInstance.Singleton.characterLayer;
        CacheTransform = transform;
        CacheCharacterMovement = gameObject.GetOrAddComponent<TashiCharacterMovement>();

        if (damageLaunchTransform == null)
            damageLaunchTransform = CacheTransform;
        if (effectTransform == null)
            effectTransform = CacheTransform;
        if (characterModelTransform == null)
            characterModelTransform = CacheTransform;
        foreach (var localPlayerObject in localPlayerObjects)
        {
            localPlayerObject.SetActive(false);
        }
        DeathTime = Time.unscaledTime;
    }


    protected virtual void UpdateInputDirection_TopDown(bool canAttack)
    {
        if (viewMode != ViewMode.TopDown) return;
        doNotLockCursor = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (isMobileInput)
        {
            inputDirection = Vector3.zero;
            inputDirection += InputManager.GetAxis("Mouse Y", false) * cameraForward;
            inputDirection += InputManager.GetAxis("Mouse X", false) * cameraRight;
            if (canAttack)
                inputAttack = inputDirection.magnitude != 0;
        }
        else
        {
            inputDirection = (InputManager.MousePosition() - Camera.main.WorldToScreenPoint(CacheTransform.position)).normalized;
            inputDirection = new Vector3(inputDirection.x, 0, inputDirection.y);
            if (canAttack)
                inputAttack = InputManager.GetButton("Fire1");
        }
    }

    protected virtual void UpdateInputDirection_ThirdPerson(bool canAttack)
    {
        if (viewMode != ViewMode.ThirdPerson)
            return;
        if (isMobileInput || doNotLockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (isMobileInput)
        {
            inputDirection = Vector3.zero;
            inputDirection += InputManager.GetAxis("Mouse Y", false) * cameraForward;
            inputDirection += InputManager.GetAxis("Mouse X", false) * cameraRight;
            if (canAttack)
                inputAttack = InputManager.GetButton("Fire1");
        }
        else
        {
            inputDirection = (InputManager.MousePosition() - targetCamera.WorldToScreenPoint(CacheTransform.position)).normalized;
            inputDirection = new Vector3(inputDirection.x, 0, inputDirection.y);
            if (canAttack)
                inputAttack = InputManager.GetButton("Fire1");
        }

        if (inputAttack) lastActionTime = Time.unscaledTime;
    }

    protected virtual void UpdateAnimation()
    {
        if (characterModel == null) return;

        var animator = characterModel.CacheAnimator;
        if (animator == null) return;

        if (this.IsDead())
        {
            animator.SetBool("IsDead", true);
            animator.SetFloat("JumpSpeed", 0);
            animator.SetFloat("MoveSpeed", 0);
            animator.SetBool("IsGround", true);
            animator.SetBool("IsDash", false);
            animator.SetBool("IsBlock", false);
        }
        else
        {
            var velocity = currentVelocity;
            var xzMagnitude = new Vector3(velocity.x, 0, velocity.z).magnitude;
            var ySpeed = velocity.y;
            animator.SetBool("IsDead", false);
            animator.SetFloat("JumpSpeed", ySpeed);
            animator.SetFloat("MoveSpeed", xzMagnitude);
            animator.SetBool("IsGround", Mathf.Abs(ySpeed) < 0.5f);
            animator.SetBool("IsDash", IsDashing);
        }

        animator.SetBool("IsIdle", !animator.GetBool("IsDead") && !animator.GetBool("DoAction") && animator.GetBool("IsGround"));

        this.ApplyAttackAnimation();
    }

    protected virtual void ApplyAttackAnimation()
    {
        bool doAction = true;
        if (this.charCtrl.charNetwork.attackActionId.Value < 1) doAction = false;

        this.characterModel.CacheAnimator.SetBool("DoAction", doAction);
        this.characterModel.CacheAnimator.SetInteger("ActionID", this.charCtrl.charNetwork.attackActionId.Value);
        this.characterModel.CacheAnimator.Play(0, 1, 0);
    }

    protected virtual bool IsAttacking()
    {
        return this.charCtrl.charNetwork.attackActionId.Value == 1;
    }

    protected virtual void UpdateInput()
    {
        if (!this.charCtrl.charNetwork.IsOwner) return;
        if (!this.canControl) return;

        var fields = FindObjectsOfType<InputField>();
        foreach (var field in fields)
        {
            if (field.isFocused)
            {
                canControl = false;
                break;
            }
        }

        isMobileInput = Application.isMobilePlatform;
#if UNITY_EDITOR
        isMobileInput = GameInstance.Singleton.showJoystickInEditor;
#endif
        InputManager.useMobileInputOnNonMobile = isMobileInput;

        var canAttack = isMobileInput || !EventSystem.current.IsPointerOverGameObject();
        inputMove = Vector3.zero;
        inputDirection = Vector3.zero;
        inputAttack = false;
        if (canControl)
        {
            cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            cameraForward = cameraForward.normalized;
            cameraRight = Camera.main.transform.right;
            cameraRight.y = 0;
            cameraRight = cameraRight.normalized;
            inputMove = Vector3.zero;
            if (!IsDead())
            {
                inputMove += cameraForward * InputManager.GetAxis("Vertical", false);
                inputMove += cameraRight * InputManager.GetAxis("Horizontal", false);
            }


            // Jump
            if (!IsDead() && !inputJump)
                inputJump = InputManager.GetButtonDown("Jump") && IsGrounded && !IsDashing;

            if (!IsDashing)
            {
                UpdateInputDirection_TopDown(canAttack);
                UpdateInputDirection_ThirdPerson(canAttack);
                if (!IsDead())
                {
                    if (InputManager.GetButtonDown("Reload"))
                        Reload();
                    if (TashiGameplayManager.Singleton.autoReload &&
                        CurrentEquippedWeapon.currentAmmo == 0 &&
                        CurrentEquippedWeapon.CanReload())
                        Reload();
                    IsDashing = InputManager.GetButtonDown("Dash") && IsGrounded;
                }
                if (IsDashing)
                {
                    if (isMobileInput)
                        dashInputMove = inputMove.normalized;
                    else
                        dashInputMove = new Vector3(CacheTransform.forward.x, 0f, CacheTransform.forward.z).normalized;
                    inputAttack = false;
                    dashingTime = Time.unscaledTime;
                }
            }
        }
    }

    protected virtual float GetMoveSpeed()
    {
        return TotalMoveSpeed * TashiGameplayManager.REAL_MOVE_SPEED_RATE;
    }

    protected virtual void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude > 1)
            direction = direction.normalized;
        direction.y = 0;

        var targetSpeed = GetMoveSpeed() * (IsDashing ? dashMoveSpeedMultiplier : 1f);
        CacheCharacterMovement.UpdateMovement(Time.deltaTime, targetSpeed, direction, inputJump);
    }

    protected virtual void UpdateMovements()
    {
        if (!this.charCtrl.charNetwork.IsOwner) return;
        //if (!this.charCtrl.charEnitiy.canControl) return;

        Move(inputMove);

        // Turn character to move direction
        if (inputDirection.magnitude <= 0 && inputMove.magnitude > 0 || viewMode == ViewMode.ThirdPerson)
            inputDirection = inputMove;
        if (characterModel && characterModel.CacheAnimator && (characterModel.CacheAnimator.GetBool("DoAction") || Time.unscaledTime - lastActionTime <= returnToMoveDirectionDelay) && viewMode == ViewMode.ThirdPerson)
            inputDirection = cameraForward;

        Rotate(inputDirection);

        if (this.inputAttack)
            Attack();
        else
            StopAttack();

        inputJump = false;
    }

    protected void Rotate(Vector3 direction)
    {
        if (direction.sqrMagnitude != 0)
            CacheTransform.rotation = Quaternion.LookRotation(direction);
    }

    public void GetDamageLaunchTransform(bool isLeftHandWeapon, out Transform launchTransform)
    {
        if (characterModel == null || !characterModel.TryGetDamageLaunchTransform(isLeftHandWeapon, out launchTransform))
            launchTransform = damageLaunchTransform;
    }

    protected void Attack()
    {
        if (this.charCtrl.charNetwork.IsOwner)
        {
            // If attacking while reloading, determines that it is reload interrupting
            if (IsReloading && FinishReloadTimeRate > 0.8f)
                HasAttackInterruptReload = true;
        }

        if (IsPlayingAttackAnim || IsReloading) return;

        if (this.charCtrl.charNetwork.attackActionId.Value < 0 && this.charCtrl.charNetwork.IsOwner)
        {
            this.charCtrl.charNetwork.attackActionId.Value = 1;
        }
    }

    protected void StopAttack()
    {
        if (this.charCtrl.charNetwork.attackActionId.Value >= 0 && this.charCtrl.charNetwork.IsOwner)
            this.charCtrl.charNetwork.attackActionId.Value = -1;
    }

    protected void Reload()
    {
        if (IsPlayingAttackAnim || IsReloading || !CurrentEquippedWeapon.CanReload())
            return;
    }

    IEnumerator DelayEndAction(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        characterModel.CacheAnimator.SetBool("DoAction", false);
    }

    IEnumerator ReloadRoutine()
    {
        HasAttackInterruptReload = false;
        if (!IsReloading && CurrentEquippedWeapon.CanReload())
        {
            IsReloading = true;
            if (WeaponData != null)
            {
                ReloadDuration = WeaponData.reloadDuration;
                StartReloadTime = Time.unscaledTime;
                if (WeaponData.clipOutFx != null && AudioManager.Singleton != null)
                    AudioSource.PlayClipAtPoint(WeaponData.clipOutFx, CacheTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);
                yield return new WaitForSeconds(ReloadDuration);
                if (LobbyManager.Instance._isLobbyHost)
                {
                    var equippedWeapon = CurrentEquippedWeapon;
                    equippedWeapon.Reload();
                    equippedWeapons[SelectWeaponIndex] = equippedWeapon;
                    //photonView.AllRPC(RpcUpdateEquippedWeaponsAmmo, SelectWeaponIndex, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
                }
                if (WeaponData.clipInFx != null && AudioManager.Singleton != null)
                    AudioSource.PlayClipAtPoint(WeaponData.clipInFx, CacheTransform.position, AudioManager.Singleton.sfxVolumeSetting.Level);
            }
            // If player still attacking, random new attacking action id
            //if (LobbyManager.Instance._isLobbyHost && AttackingActionId >= 0 && WeaponData != null)
            //    AttackingActionId = WeaponData.GetRandomAttackAnimation().actionId;
            yield return new WaitForEndOfFrame();
            IsReloading = false;
            if (this.charCtrl.charNetwork.IsOwner)
            {
                // If weapon is reload one ammo at a time (like as shotgun), automatically reload more bullets
                // When there is no attack interrupt while reload
                if (WeaponData != null && WeaponData.reloadOneAmmoAtATime && CurrentEquippedWeapon.CanReload())
                {
                    if (!HasAttackInterruptReload)
                        Reload();
                    else
                        Attack();
                }
            }
        }
    }


    public void KilledTarget(TashiCharacterEntity target)
    {
        if (!LobbyManager.Instance._isLobbyHost)
            return;

        var gameplayManager = TashiGameplayManager.Singleton;
        var targetLevel = target.Level;
        var maxLevel = gameplayManager.maxLevel;
        Exp += Mathf.CeilToInt(target.RewardExp * TotalExpRate);
        var increaseScore = Mathf.CeilToInt(target.KillScore * TotalScoreRate);
        //syncScore.Value += increaseScore;
        //GameNetworkManager.Singleton.OnScoreIncrease(this, increaseScore);
        foreach (var rewardCurrency in gameplayManager.rewardCurrencies)
        {
            var currencyId = rewardCurrency.currencyId;
            var amount = rewardCurrency.amount.Calculate(targetLevel, maxLevel);
            //photonView.TargetRPC(RpcTargetRewardCurrency, photonView.Owner, currencyId, amount);
        }
        var increaseKill = 1;
        //syncKillCount.Value += increaseKill;
        //GameNetworkManager.Singleton.OnKillIncrease(this, increaseKill);
        //GameNetworkManager.Singleton.SendKillNotify(PlayerName, target.PlayerName, WeaponData == null ? string.Empty : WeaponData.GetId());
    }

    public void Heal(int amount)
    {
        if (!LobbyManager.Instance._isLobbyHost)
            return;

        if (Hp <= 0)
            return;

        Hp += amount;
    }

    public virtual float GetAttackRange()
    {
        if (WeaponData == null || WeaponData.damagePrefab == null)
            return 0;
        return WeaponData.damagePrefab.GetAttackRange();
    }

    public virtual Vector3 GetSpawnPosition()
    {
        return TashiGameplayManager.Singleton.GetCharacterSpawnPosition(this);
    }

    public void UpdateCharacterModelHiddingState()
    {
        if (characterModel == null)
            return;
        var renderers = characterModel.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
            renderer.enabled = !IsHidding;
    }

    protected void InterruptAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            IsPlayingAttackAnim = false;
        }
    }

    protected void InterruptReload()
    {
        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
            IsReloading = false;
        }
    }

    public virtual void OnSpawn() { }

    public void OnUpdateSelectCharacter(int selectCharacter)
    {
        refreshingSumAddStats = true;
        if (characterModel != null)
            Destroy(characterModel.gameObject);
        characterData = GameInstance.GetCharacter(selectCharacter);
        if (characterData == null || characterData.modelObject == null)
            return;
        characterModel = Instantiate(characterData.modelObject, characterModelTransform);
        characterModel.transform.localPosition = Vector3.zero;
        characterModel.transform.localEulerAngles = Vector3.zero;
        characterModel.transform.localScale = Vector3.one;
        if (headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        if (WeaponData != null)
            characterModel.SetWeaponModel(WeaponData.rightHandObject, WeaponData.leftHandObject, WeaponData.shieldObject);
        if (customEquipmentDict != null)
        {
            characterModel.ClearCustomModels();
            foreach (var value in customEquipmentDict.Values)
            {
                characterModel.SetCustomModel(value.containerIndex, value.modelObject);
            }
        }
        characterModel.gameObject.SetActive(true);
        UpdateCharacterModelHiddingState();
    }

    public void OnUpdateSelectHead(int selectHead)
    {
        refreshingSumAddStats = true;
        headData = GameInstance.GetHead(selectHead);
        if (characterModel != null && headData != null)
            characterModel.SetHeadModel(headData.modelObject);
        UpdateCharacterModelHiddingState();
    }

    public void OnUpdateSelectWeapons(int[] selectWeapons)
    {
        refreshingSumAddStats = true;
        // Changes weapon list, equip first weapon equipped position
        var minEquipPos = int.MaxValue;
        for (var i = 0; i < selectWeapons.Length; ++i)
        {
            var weaponData = GameInstance.GetWeapon(selectWeapons[i]);

            if (weaponData == null)
                continue;

            var equipPos = weaponData.equipPosition;
            if (minEquipPos > equipPos)
            {
                defaultWeaponIndex = equipPos;
                minEquipPos = equipPos;
            }

            var equippedWeapon = new TashiEquippedWeapon();
            equippedWeapon.defaultId = weaponData.GetHashId();
            equippedWeapon.weaponId = weaponData.GetHashId();
            equippedWeapon.SetMaxAmmo();
            equippedWeapons[equipPos] = equippedWeapon;
            //if (PhotonNetwork.IsMasterClient)
            //    photonView.AllRPC(RpcUpdateEquippedWeapons, equipPos, equippedWeapon.defaultId, equippedWeapon.weaponId, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
        }
        SelectWeaponIndex = defaultWeaponIndex;
    }

    public void OnUpdateSelectCustomEquipments(int[] selectCustomEquipments)
    {
        refreshingSumAddStats = true;
        if (characterModel != null)
            characterModel.ClearCustomModels();
        customEquipmentDict.Clear();
        if (selectCustomEquipments != null)
        {
            for (var i = 0; i < selectCustomEquipments.Length; ++i)
            {
                var customEquipmentData = GameInstance.GetCustomEquipment(selectCustomEquipments[i]);
                if (customEquipmentData != null &&
                    !customEquipmentDict.ContainsKey(customEquipmentData.containerIndex))
                {
                    customEquipmentDict[customEquipmentData.containerIndex] = customEquipmentData;
                    if (characterModel != null)
                        characterModel.SetCustomModel(customEquipmentData.containerIndex, customEquipmentData.modelObject);
                }
            }
        }
        UpdateCharacterModelHiddingState();
    }

    public void OnUpdateSelectWeaponIndex(int selectWeaponIndex)
    {
        refreshingSumAddStats = true;
        if (selectWeaponIndex < 0 || selectWeaponIndex >= equippedWeapons.Length)
            return;
        if (characterModel != null && WeaponData != null)
            characterModel.SetWeaponModel(WeaponData.rightHandObject, WeaponData.leftHandObject, WeaponData.shieldObject);
        UpdateCharacterModelHiddingState();
    }

    public void OnUpdateAttributeAmounts()
    {
        refreshingSumAddStats = true;
    }

    public bool ServerFillWeaponAmmo(WeaponData weaponData, int ammoAmount)
    {
        if (!LobbyManager.Instance._isLobbyHost) return false;
        if (weaponData == null || weaponData.equipPosition < 0 || weaponData.equipPosition >= equippedWeapons.Length)
            return false;
        var equipPosition = weaponData.equipPosition;
        var equippedWeapon = equippedWeapons[equipPosition];
        var updated = false;
        if (equippedWeapon.weaponId == weaponData.GetHashId())
        {
            updated = equippedWeapon.AddReserveAmmo(ammoAmount);
            if (updated)
            {
                equippedWeapons[equipPosition] = equippedWeapon;
                //photonView.AllRPC(RpcUpdateEquippedWeaponsAmmo, equipPosition, equippedWeapon.currentAmmo, equippedWeapon.currentReserveAmmo);
            }
        }
        return updated;
    }

    public virtual string PlayerId()
    {
        return LobbyManager.Instance.PlayerId;
    }

    public virtual bool IsMine()
    {
        return this.charCtrl.charNetwork.IsOwner;
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadCharacterModel();
    }

    protected virtual void LoadCharacterModel()
    {
        if (this.characterModel != null) return;
        this.characterModel = GetComponentInChildren<CharacterModel>();
        Debug.LogWarning(transform.name + ": LoadCharacterModel", gameObject);
    }
}
