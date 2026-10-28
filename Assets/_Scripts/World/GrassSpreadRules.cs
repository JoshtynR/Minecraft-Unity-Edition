// Beta 1.7.3 BlockGrass light/opacity thresholds. Keeping the predicates
// separate makes the random-tick behavior testable without a running world.
public static class GrassSpreadRules
{
    public static bool ShouldDecay(int lightAbove,int opacityAbove)
        => lightAbove<4&&opacityAbove>2;

    public static bool CanSpreadFrom(int lightAbove)
        => lightAbove>=9;

    public static bool CanBecomeGrass(BlockType candidate,int lightAbove,int opacityAbove)
        => candidate==BlockType.Dirt&&lightAbove>=4&&opacityAbove<=2;
}
