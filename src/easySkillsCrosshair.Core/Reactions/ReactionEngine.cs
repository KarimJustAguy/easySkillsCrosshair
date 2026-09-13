using easySkillsCrosshair.Core.Crosshair;

namespace easySkillsCrosshair.Core.Reactions;

/// <summary>
/// Turns raw fire/aim/move input into a <see cref="CrosshairRenderState"/>. Pure logic with an
/// explicit clock, so bloom timing is unit-testable without any Win32 input source.
/// Bloom is 1 while fire is held and decays linearly to 0 over the recover time after release.
/// </summary>
public sealed class ReactionEngine
{
    private readonly HashSet<int> _heldMoveKeys = [];
    private bool _isFiring;
    private bool _isAiming;
    private DateTime _fireReleasedAtUtc = DateTime.MinValue;

    public event EventHandler? InputChanged;

    public void SetFiring(bool isDown, DateTime nowUtc)
    {
        if (_isFiring == isDown) return;
        _isFiring = isDown;
        if (!isDown) _fireReleasedAtUtc = nowUtc;
        InputChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetAiming(bool isDown)
    {
        if (_isAiming == isDown) return;
        _isAiming = isDown;
        InputChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetMoveKey(int virtualKey, bool isDown)
    {
        var changed = isDown ? _heldMoveKeys.Add(virtualKey) : _heldMoveKeys.Remove(virtualKey);
        if (changed) InputChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Drops all held state, e.g. when the app itself takes focus.</summary>
    public void Reset()
    {
        // Called on every raw-input message while the app is focused — stay silent when idle.
        if (!_isFiring && !_isAiming && _heldMoveKeys.Count == 0 && _fireReleasedAtUtc == DateTime.MinValue)
        {
            return;
        }

        _isFiring = false;
        _isAiming = false;
        _heldMoveKeys.Clear();
        _fireReleasedAtUtc = DateTime.MinValue;
        InputChanged?.Invoke(this, EventArgs.Empty);
    }

    public CrosshairRenderState ComputeState(DynamicReactionSettings settings, DateTime nowUtc)
    {
        double bloom = 0;
        if (settings.BloomOnFire)
        {
            if (_isFiring)
            {
                bloom = 1;
            }
            else if (settings.BloomRecoverTime > TimeSpan.Zero)
            {
                var sinceRelease = nowUtc - _fireReleasedAtUtc;
                bloom = Math.Clamp(1 - sinceRelease / settings.BloomRecoverTime, 0, 1);
            }
        }

        return new CrosshairRenderState(
            bloom,
            IsHidden: settings.HideOnAim && _isAiming,
            ForceTStyle: settings.TShapeOnMove && _heldMoveKeys.Count > 0);
    }

    /// <summary>True while a bloom is still decaying — the renderer must keep animating.</summary>
    public bool IsAnimating(DynamicReactionSettings settings, DateTime nowUtc)
    {
        var state = ComputeState(settings, nowUtc);
        return settings.BloomOnFire && !_isFiring && state.BloomFactor > 0;
    }
}
