using UnityEngine;

public partial class World
{
    [Header("Survival")]
    [Tooltip("Starvation stops at 5 hearts on Easy, half a heart on Normal, and can reach zero on Hard.")]
    public SurvivalDifficulty survivalDifficulty = SurvivalDifficulty.Normal;
    [Tooltip("Recover half a heart every four seconds at 18+ food, costing 3 exhaustion per heal.")]
    public bool naturalRegeneration = true;
}
