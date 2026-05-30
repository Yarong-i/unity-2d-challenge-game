using UnityEngine;

public class WallBounceSurface2D : MonoBehaviour
{
    public bool forceBounce = true;
    public bool sideOnly = true;
    public float sideNormalMinAbsX = 0.45f;
    public float sideNormalMaxAbsY = 0.55f;
    public bool allowTopBounce;
    public bool allowBottomBounce;
    public float bounceMultiplierOverride = -1f;
    public float upwardBoostOverride = -1f;
    public float detachSpeedOverride = -1f;

    public bool IsBounceAllowedForNormal(Vector2 normal)
    {
        Vector2 normalizedNormal = normal.normalized;
        if (!allowTopBounce && normalizedNormal.y >= sideNormalMaxAbsY)
            return false;

        if (!allowBottomBounce && normalizedNormal.y <= -sideNormalMaxAbsY)
            return false;

        if (!sideOnly)
            return true;

        return Mathf.Abs(normalizedNormal.x) >= sideNormalMinAbsX &&
            Mathf.Abs(normalizedNormal.y) <= sideNormalMaxAbsY;
    }

    public bool IsTopContact(Vector2 normal)
    {
        return normal.normalized.y >= sideNormalMaxAbsY;
    }

    public bool IsBottomContact(Vector2 normal)
    {
        return normal.normalized.y <= -sideNormalMaxAbsY;
    }
}
