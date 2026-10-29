public static class RenderRebuildPriority
{
    // Nearby edits win regardless of view direction; otherwise visible chunks
    // win. Within either category, prefer the shortest squared distance.
    public static bool IsBetter(float distance, bool visible, float bestDistance, bool bestVisible)
    {
        int rank=distance<=256?0:visible?1:2;
        int bestRank=bestDistance<=256?0:bestVisible?1:2;
        return rank<bestRank || rank==bestRank&&distance<bestDistance;
    }
}
