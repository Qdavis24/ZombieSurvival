using System;
using System.Threading.Tasks;
using Godot;
using ZombieSurvival.scripts.inventory_system;

namespace ZombieSurvival.scripts.player.weapons;

public partial class WeaponManager : Node
{
    private const float MagazineUpgradeMultiplier = 2f;
    private const int PierceUpgradeHitCount = 3;
    private const float FireRateUpgradeMultiplier = 1.5f;

    [Signal]
    public delegate void AmmoChangedEventHandler(int currentAmmo, int reserve);

    [Signal]
    public delegate void ReloadFailedEventHandler();

    private sealed class WeaponSlot
    {
        public PackedScene Scene;
        public bool Unlocked;
        public int CurrentAmmo;
        public ItemType AmmoType;
        public ItemType WeaponType;
        public bool MagSizeUpgraded;
        public bool PierceUpgraded;
        public bool FireRateUpgraded;

        public WeaponSlot(PackedScene scene, int currentAmmo, ItemType ammoType, ItemType weaponType)
        {
            Scene = scene;
            CurrentAmmo = currentAmmo;
            AmmoType = ammoType;
            WeaponType = weaponType;
        }
    }

    public enum HandActionState
    {
        Idle,
        SwappingWeapon,
        ThrowingGrenade,
        UsingPerk
    }

    public enum WeaponUpgradeStatus
    {
        Available,
        Locked,
        Owned,
        NotEnoughPoints,
        Invalid
    }

    [Export] private Node3D _weaponSocket;
    [Export] private PackedScene[] _weaponScenes;
    [Export] private int[] _startingReserveAmmo;
    private WeaponSlot[] _weaponSlots;
    private int _currentWeaponIndex = 0;
    private bool _isRocketLoaded = true;
    private bool _crosshairHiddenByWeaponState = false;
    private HandActionState _handActionState = HandActionState.Idle;

    [Export] private PlayerController _playerController;
    [Export] private Camera _camera;

    [Export] private float _defaultHipFov = 90f;
    [Export] private float _fovLerpSpeed = 80f;

    private HitResolver _hitResolver;

    private WeaponBase _current;
    private float _reloadSpeedMultiplier = 1f;
    public bool IsHandsBusy => _handActionState != HandActionState.Idle;

    public override void _Ready()
    {
        _hitResolver = GetTree().CurrentScene.GetNodeOrNull<HitResolver>("HitResolver");
        _playerController.Inventory.ItemAdded += OnPlayerInventoryItemAdded;
        _playerController.Inventory.ItemRemoved += OnPlayerInventoryItemRemoved;

        if (_weaponScenes != null && _weaponScenes.Length > 0)
        {
            _weaponSlots = new WeaponSlot[_weaponScenes.Length];

            for (int i = 0; i < _weaponScenes.Length; i++)
            {
                var scene = _weaponScenes[i];
                if (scene == null)
                    continue;

                var previewWeapon = scene.Instantiate<WeaponBase>();
                int magazineSize = previewWeapon.MagazineSize;
                int reserveAmmo = (_startingReserveAmmo != null && i < _startingReserveAmmo.Length)
                    ? _startingReserveAmmo[i]
                    : 0;
                ItemType ammoType = previewWeapon.AmmoType;
                ItemType weaponType = previewWeapon.WeaponType;

                _weaponSlots[i] = new WeaponSlot(scene, magazineSize, ammoType, weaponType)
                {
                    // Unlocked = i == 0 // final version just pistol unlocked
                    Unlocked = i == 0
                };

                _playerController.Inventory.AddItem(ammoType, reserveAmmo);

                previewWeapon.QueueFree();
            }

            _currentWeaponIndex = 0;
            Equip(_currentWeaponIndex);
        }
    }

    private void OnPlayerInventoryItemAdded(ItemType itemType, int amount)
    {
        if (_weaponSlots == null) return;
        switch (itemType.GetGroup())
        {
            case (ItemGroup.Ammo):
                if (_weaponSlots[_currentWeaponIndex].AmmoType == itemType)
                    RefreshHudAmmo();
                break;
            case (ItemGroup.Weapon):
                for (int i = 0; i < _weaponSlots.Length; i++)
                {
                    if (_weaponSlots[i].WeaponType == itemType)
                        _weaponSlots[i].Unlocked = true;
                }

                break;
        }
    }

    private void OnPlayerInventoryItemRemoved(ItemType itemType, int amount)
    {
        if (_weaponSlots == null) return;
        if (_weaponSlots[_currentWeaponIndex].AmmoType == itemType)
            RefreshHudAmmo();
    }

    private void Equip(int weaponIndex)
    {
        if (_weaponSlots == null || weaponIndex < 0 || weaponIndex >= _weaponSlots.Length)
            return;

        var slot = _weaponSlots[weaponIndex];
        if (slot == null || slot.Scene == null)
            return;

        if (_current != null)
        {
            _current.Fired -= _camera.OnWeaponFired;
            _current.Fired -= _playerController.OnWeaponFired;
            _current.AmmoChanged -= OnCurrentWeaponAmmoChanged;
            _current.ReloadFailed -= OnCurrentWeaponReloadFailed;
        }

        _current?.QueueFree();

        _current = slot.Scene.Instantiate<WeaponBase>();
        _current.ReloadSpeedMultiplier = _reloadSpeedMultiplier;
        if (_current is RpgWeapon rpg)
        {
            rpg.SetRocketLoaded(_isRocketLoaded);
        }

        _weaponSocket.AddChild(_current);
        _current.Initialize(_camera, _hitResolver, slot.CurrentAmmo);
        ApplySlotUpgrades(slot, _current);
        _current.SetAmmoSource(
            needed =>
            {
                var available = _playerController.Inventory.GetAmount(slot.AmmoType);
                var granted = Math.Min(needed, available);
                if (granted > 0)
                    _playerController.Inventory.ConsumeItem(slot.AmmoType, granted);
                return granted;
            },
            () => _playerController.Inventory.GetAmount(slot.AmmoType)
        );        
        _current.Fired += _camera.OnWeaponFired;
        _current.Fired += _playerController.OnWeaponFired;
        _current.AmmoChanged += OnCurrentWeaponAmmoChanged;
        _current.ReloadFailed += OnCurrentWeaponReloadFailed;

        RefreshHudAmmo();
        CallDeferred(nameof(RefreshHudAmmo));
    }

    private void ApplySlotUpgrades(WeaponSlot slot, WeaponBase weapon)
    {
        if (slot == null || weapon == null)
            return;

        weapon.SetMagazineSizeMultiplier(slot.MagSizeUpgraded ? MagazineUpgradeMultiplier : 1f);
        weapon.SetPierceHitCount(slot.PierceUpgraded ? PierceUpgradeHitCount : 1);
        weapon.SetFireRateMultiplier(slot.FireRateUpgraded ? FireRateUpgradeMultiplier : 1f);
    }

    public WeaponUpgradeStatus GetUpgradeStatus(WeaponUpgradeTarget target)
    {
        var slot = GetUpgradeSlot(target);
        if (slot == null)
            return WeaponUpgradeStatus.Invalid;

        if (!slot.Unlocked)
            return WeaponUpgradeStatus.Locked;

        return IsFullyUpgraded(slot)
            ? WeaponUpgradeStatus.Owned
            : WeaponUpgradeStatus.Available;
    }

    public bool TryUpgradeWeapon(WeaponUpgradeTarget target)
    {
        if (GetUpgradeStatus(target) != WeaponUpgradeStatus.Available)
            return false;

        var slotIndex = GetUpgradeSlotIndex(target);
        if (slotIndex < 0)
            return false;

        var slot = _weaponSlots[slotIndex];
        slot.MagSizeUpgraded = true;
        slot.PierceUpgraded = true;
        slot.FireRateUpgraded = true;

        if (slotIndex == _currentWeaponIndex)
        {
            ApplySlotUpgrades(slot, _current);
            RefreshHudAmmo();
        }

        return true;
    }

    private WeaponSlot GetUpgradeSlot(WeaponUpgradeTarget target)
    {
        var slotIndex = GetUpgradeSlotIndex(target);
        return slotIndex >= 0 ? _weaponSlots[slotIndex] : null;
    }

    private int GetUpgradeSlotIndex(WeaponUpgradeTarget target)
    {
        if (_weaponSlots == null || _weaponSlots.Length == 0)
            return -1;

        if (target == WeaponUpgradeTarget.Pistol)
            return _weaponSlots[0] != null ? 0 : -1;

        var weaponType = target switch
        {
            WeaponUpgradeTarget.Shotgun => ItemType.Shotgun,
            WeaponUpgradeTarget.Rifle => ItemType.Rifle,
            _ => (ItemType)(-1)
        };

        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            var slot = _weaponSlots[i];
            if (slot != null && slot.WeaponType == weaponType)
                return i;
        }

        return -1;
    }

    private static bool IsFullyUpgraded(WeaponSlot slot)
    {
        return slot.MagSizeUpgraded && slot.PierceUpgraded && slot.FireRateUpgraded;
    }

    private void TryHandleUpgradeInput()
    {
        if (_current == null || _weaponSlots == null || _currentWeaponIndex < 0 || _currentWeaponIndex >= _weaponSlots.Length)
            return;

        var slot = _weaponSlots[_currentWeaponIndex];
        if (slot == null)
            return;

        if (Input.IsActionJustPressed("upgrade_mag_size") && !slot.MagSizeUpgraded)
        {
            slot.MagSizeUpgraded = true;
            _current.SetMagazineSizeMultiplier(MagazineUpgradeMultiplier);
            RefreshHudAmmo();
        }

        if (Input.IsActionJustPressed("upgrade_pierce") && !slot.PierceUpgraded)
        {
            slot.PierceUpgraded = true;
            _current.SetPierceHitCount(PierceUpgradeHitCount);
        }

        if (Input.IsActionJustPressed("upgrade_fire_rate") && !slot.FireRateUpgraded)
        {
            slot.FireRateUpgraded = true;
            _current.SetFireRateMultiplier(FireRateUpgradeMultiplier);
        }
    }

    public void SetReloadSpeedMultiplier(float reloadSpeedMultiplier)
    {
        _reloadSpeedMultiplier = Mathf.Max(0.01f, reloadSpeedMultiplier);

        if (_current != null)
            _current.ReloadSpeedMultiplier = _reloadSpeedMultiplier;
    }

    private void OnCurrentWeaponAmmoChanged(int currentAmmo)
    {
        if (_weaponSlots != null && _currentWeaponIndex >= 0 && _currentWeaponIndex < _weaponSlots.Length)
        {
            var slot = _weaponSlots[_currentWeaponIndex];
            if (slot != null)
            {
                if (_current is RpgWeapon)
                {
                    // Update rocket loaded state
                    _isRocketLoaded = currentAmmo != 0;
                }

                slot.CurrentAmmo = currentAmmo;
            }
        }

        RefreshHudAmmo();
    }

    private void OnCurrentWeaponReloadFailed()
    {
        EmitSignal(SignalName.ReloadFailed);
    }

    private void RefreshHudAmmo()
    {
        if (_current == null || _weaponSlots == null)
            return;

        var reserve = _playerController.Inventory.GetAmount(_weaponSlots[_currentWeaponIndex].AmmoType);
        EmitSignal(SignalName.AmmoChanged, _current.CurrentAmmo, reserve);
    }


    private void UpdateCrosshairVisibility(bool isMovingForward)
    {
        bool shouldHideCrosshair = isMovingForward || IsHandsBusy || (_current?.IsReloading ?? false) || (_current?.IsAiming ?? false);

        if (shouldHideCrosshair)
        {
            if (!_crosshairHiddenByWeaponState)
            {
                _playerController.HideCrosshair();
                _crosshairHiddenByWeaponState = true;
            }
        }
        else if (_crosshairHiddenByWeaponState)
        {
            _playerController.ShowCrosshair();
            _crosshairHiddenByWeaponState = false;
        }
    }

    private AnimationPlayer GetWeaponAnimationPlayer(WeaponBase weapon)
    {
        return weapon?.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
    }

    private void DisconnectCurrentWeapon()
    {
        if (_current == null)
            return;

        _current.Fired -= _camera.OnWeaponFired;
        _current.Fired -= _playerController.OnWeaponFired;
        _current.AmmoChanged -= OnCurrentWeaponAmmoChanged;
        _current.ReloadFailed -= OnCurrentWeaponReloadFailed;
    }

    private async Task StowCurrentWeapon()
    {
        if (_current == null)
            return;

        var currentAnim = GetWeaponAnimationPlayer(_current);
        if (currentAnim != null && currentAnim.HasAnimation("transition_swap"))
        {
            _current.SetAimState(false);
            currentAnim.Play("transition_swap");
            await ToSignal(currentAnim, AnimationPlayer.SignalName.AnimationFinished);
        }

        DisconnectCurrentWeapon();
        _current.QueueFree();
        _current = null;
    }

    private async Task RaiseCurrentWeapon()
    {
        if (_current == null)
            return;

        var currentAnim = GetWeaponAnimationPlayer(_current);
        if (currentAnim != null && currentAnim.HasAnimation("transition_swap"))
        {
            currentAnim.Play("transition_swap");
            currentAnim.Seek(currentAnim.CurrentAnimationLength, true);
            currentAnim.Play("transition_swap", customSpeed: -1.0f, fromEnd: true);
            await ToSignal(currentAnim, AnimationPlayer.SignalName.AnimationFinished);
        }
    }

    public async Task PlayTemporaryHandAction(HandActionState actionState, Func<Task> action)
    {
        if (IsHandsBusy)
            return;

        _handActionState = actionState;

        try
        {
            int previousWeaponIndex = _currentWeaponIndex;

            await StowCurrentWeapon();
            try
            {
                await action();
            }
            finally
            {
                Equip(previousWeaponIndex);
                await RaiseCurrentWeapon();
            }
        }
        finally
        {
            _handActionState = HandActionState.Idle;
        }
    }

    private async void SwapToWeaponIndex(int newIndex)
    {
        if (IsHandsBusy) return;
        if (_weaponSlots == null || _weaponSlots.Length == 0) return;
        if (newIndex < 0 || newIndex >= _weaponSlots.Length) return;
        if (_weaponSlots[newIndex] == null || !_weaponSlots[newIndex].Unlocked) return;
        if (newIndex == _currentWeaponIndex) return;

        _handActionState = HandActionState.SwappingWeapon;

        try
        {
            await StowCurrentWeapon();

            _currentWeaponIndex = newIndex;
            Equip(_currentWeaponIndex);
            await RaiseCurrentWeapon();
        }
        finally
        {
            _handActionState = HandActionState.Idle;
        }

    }

    private int FindNextUnlockedWeaponIndex(int direction)
    {
        if (_weaponSlots == null || _weaponSlots.Length == 0)
            return -1;

        int index = _currentWeaponIndex;
        for (int i = 0; i < _weaponSlots.Length; i++)
        {
            index += direction;
            if (index >= _weaponSlots.Length)
                index = 0;
            else if (index < 0)
                index = _weaponSlots.Length - 1;

            if (_weaponSlots[index] != null && _weaponSlots[index].Unlocked)
                return index;
        }

        return _currentWeaponIndex;
    }

    private void TryHandleWeaponSwapInput()
    {
        if (IsHandsBusy || _weaponSlots == null || _weaponSlots.Length == 0)
            return;

        if (Input.IsActionJustPressed("weapon_swap_down") || Input.IsActionJustPressed("weapon_swap_right"))
        {
            int nextIndex = FindNextUnlockedWeaponIndex(1);
            SwapToWeaponIndex(nextIndex);
        }

        if (Input.IsActionJustPressed("weapon_swap_up") || Input.IsActionJustPressed("weapon_swap_left"))
        {
            int nextIndex = FindNextUnlockedWeaponIndex(-1);
            SwapToWeaponIndex(nextIndex);
        }
    }

    public override void _Process(double delta)
    {
        bool isMovingForward = Input.IsActionPressed("move_forward");
        UpdateCrosshairVisibility(isMovingForward);

        TryHandleWeaponSwapInput();

        if (!IsHandsBusy)
        {
            if (Input.IsActionJustPressed("weapon1"))
                SwapToWeaponIndex(0);
            if (Input.IsActionJustPressed("weapon2"))
                SwapToWeaponIndex(1);
            if (Input.IsActionJustPressed("weapon3"))
                SwapToWeaponIndex(2);
            if (Input.IsActionJustPressed("weapon4"))
                SwapToWeaponIndex(3);

            if (IsHandsBusy)
                return;

            TryHandleUpgradeInput();

            bool aimHeld = Input.IsActionPressed("aim");
            _current?.SetAimState(aimHeld);
            float targetFov = _current != null ? _current.GetTargetFov() : _defaultHipFov;
            _camera.Fov = Mathf.MoveToward(_camera.Fov, targetFov, (float)(_fovLerpSpeed * delta));

            _current?.SetMovementState(isMovingForward);
            _camera.SetMovementState(isMovingForward);

            if (Input.IsActionJustPressed("reload"))
            {
                var slot = _weaponSlots?[_currentWeaponIndex];
                if (_current != null && slot != null)
                    _current.TryReload();
            }

            bool triggerHeld = Input.IsActionPressed("fire");
            _current?.TryFire(triggerHeld);
        }
    }
}
