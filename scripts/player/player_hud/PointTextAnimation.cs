using Godot;
using System;

public partial class PointTextAnimation : Label
{
    [Export] public float RiseDistance = 60f;
    [Export] public float Duration = 0.7f;
    [Export] public float HorizontalSpread = 30f;
    [Export] public float SpawnSpreadX = 2f;
    [Export] public float SpawnSpreadY = 14f;
    [Export] public float MinSeparation = 24f;
    [Export] public int MaxPlacementAttempts = 8;
    [Export] public int AllowOverlapWhenSiblingCountAtLeast = 8;

    public void SetAmount(int amount)
    {
        Text = $"+{amount}";
        Modulate = new Color(Modulate, 1f); // ensure fully visible

        StartAnimation();
    }

    private void StartAnimation()
    {
        Vector2 basePos = Position;
        Vector2 startPos = FindNonOverlappingStartPosition(basePos);

        // Random horizontal drift for the actual shoot-out motion
        float randomX = (float)GD.RandRange(-HorizontalSpread, HorizontalSpread);
        Vector2 endPos = startPos + new Vector2(randomX, -RiseDistance);

        Position = startPos;

        var tween = CreateTween();
        tween.SetParallel(true);

        // Move upward + sideways
        tween.TweenProperty(this, "position", endPos, Duration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        // Fade out
        tween.TweenProperty(this, "modulate:a", 0f, Duration);

        // Small pop at start
        Scale = Vector2.One;
        tween.TweenProperty(this, "scale", new Vector2(1.2f, 1.2f), 0.1f)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);

        tween.Chain().TweenProperty(this, "scale", Vector2.One, 0.15f);

        tween.Finished += () => QueueFree();
    }

    private Vector2 FindNonOverlappingStartPosition(Vector2 basePos)
    {
        if (GetParent() == null)
            return basePos;

        int siblingPopupCount = 0;
        foreach (Node child in GetParent().GetChildren())
        {
            if (child is PointTextAnimation)
                siblingPopupCount++;
        }

        // If a ton of popups are already active, stop fighting overlap and just let them spray.
        if (siblingPopupCount >= AllowOverlapWhenSiblingCountAtLeast)
            return basePos + new Vector2(
                (float)GD.RandRange(-SpawnSpreadX, SpawnSpreadX),
                (float)GD.RandRange(-SpawnSpreadY, SpawnSpreadY)
            );

        Vector2 bestCandidate = basePos;
        float bestDistance = -1f;

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector2 candidate = basePos + new Vector2(
                (float)GD.RandRange(-SpawnSpreadX, SpawnSpreadX),
                (float)GD.RandRange(-SpawnSpreadY, SpawnSpreadY)
            );

            float nearestDistance = DistanceToNearestPopup(candidate);
            if (nearestDistance >= MinSeparation)
                return candidate;

            if (nearestDistance > bestDistance)
            {
                bestDistance = nearestDistance;
                bestCandidate = candidate;
            }
        }

        return bestCandidate;
    }

    private float DistanceToNearestPopup(Vector2 candidate)
    {
        if (GetParent() == null)
            return float.MaxValue;

        float nearestDistance = float.MaxValue;

        foreach (Node child in GetParent().GetChildren())
        {
            if (child == this)
                continue;

            if (child is not PointTextAnimation other)
                continue;

            float distance = candidate.DistanceTo(other.Position);
            if (distance < nearestDistance)
                nearestDistance = distance;
        }

        return nearestDistance;
    }
}