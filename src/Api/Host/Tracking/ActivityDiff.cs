namespace CrossBusExplorer.Host.Tracking;

public static class ActivityDiff
{
    /// <param name="ownAccessUntil">
    /// AccessedAt values up to this time are attributed to the tracker itself and ignored.
    /// </param>
    public static IReadOnlyList<ActivityChange> Compute(
        IReadOnlyDictionary<string, EntitySnapshot> previous,
        IEnumerable<EntitySnapshot> current,
        IReadOnlyDictionary<string, DateTimeOffset> ownAccessUntil)
    {
        var changes = new List<ActivityChange>();

        foreach (var entity in current)
        {
            // New entities only establish a baseline.
            if (!previous.TryGetValue(entity.Path, out var before))
            {
                continue;
            }

            var activeDelta = entity.ActiveCount - before.ActiveCount;
            var deadLetterDelta = entity.DeadLetterCount - before.DeadLetterCount;
            var accessed = entity.AccessedAt > before.AccessedAt &&
                           (!ownAccessUntil.TryGetValue(entity.Path, out var own) ||
                            entity.AccessedAt > own);

            if (activeDelta != 0 || deadLetterDelta != 0 || accessed)
            {
                changes.Add(new ActivityChange(entity, activeDelta, deadLetterDelta));
            }
        }

        return changes;
    }
}
