/// <summary>
/// Single boundary between Unity's horizontal world coordinates and the
/// canonical Minecraft Beta 1.7.3 coordinate space.
///
/// Beta generation always runs in Beta coordinates. The X reflection is
/// applied globally here instead of transposing blocks independently inside
/// each chunk.
/// </summary>
public static class BetaCoordinateSpace
{
    public static int UnityChunkToBetaChunkX(int unityChunkX) => -unityChunkX - 1;
    public static int UnityChunkToBetaChunkZ(int unityChunkZ) => unityChunkZ;

    public static int BetaLocalToUnityLocalX(int betaLocalX) => 15 - betaLocalX;
    public static int BetaLocalToUnityLocalZ(int betaLocalZ) => betaLocalZ;

    public static int BetaChunkToWorldX(int betaChunkX) => betaChunkX * 16;
    public static int BetaChunkToWorldZ(int betaChunkZ) => betaChunkZ * 16;
}
