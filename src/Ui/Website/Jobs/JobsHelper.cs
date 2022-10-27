namespace CrossBusExplorer.Website.Jobs;

public static class JobsHelper
{
    public static int GetProgress(long totalCount, long purgedCount) =>
        (int)((float)purgedCount / totalCount * 100);
}