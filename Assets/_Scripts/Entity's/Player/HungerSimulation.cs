using System;

public enum SurvivalDifficulty { Easy, Normal, Hard }

// Classic pre-combat Java FoodStats rules; one Tick is 1/20 second.
public sealed class HungerSimulation
{
    public int Health = 20, Food = 20;
    public float Saturation = 5, Exhaustion;
    public int FoodTimer;
    public bool CanSprint { get { return Food > 6 && Health > 0; } }

    public void AddExhaustion(float amount)
    {
        if (float.IsNaN(amount) || amount <= 0) return;
        Exhaustion = Math.Min(40, Exhaustion + amount);
    }

    public void AddGroundMovement(float distance, bool sprinting, bool flying)
    {
        if (flying || Health <= 0 || float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0) return;
        // Java rounds traveled meters to centimeters before charging exhaustion.
        int centimeters = (int)Math.Floor(distance * 100 + 0.5);
        AddExhaustion((sprinting && CanSprint ? 0.099999994f : 0.01f) * centimeters * 0.01f);
    }

    public void AddJump(bool sprinting, bool flying)
    {
        if (!flying && Health > 0) AddExhaustion(sprinting && CanSprint ? 0.8f : 0.2f);
    }

    public void Tick(SurvivalDifficulty difficulty, bool naturalRegeneration)
    {
        Health = SurvivalHudValues.ClampPoints(Health);
        Food = SurvivalHudValues.ClampPoints(Food);
        if (Health == 0) { FoodTimer = 0; return; }
        if (Exhaustion > 4)
        {
            Exhaustion -= 4;
            if (Saturation > 0) Saturation = Math.Max(0, Saturation - 1);
            else Food = Math.Max(0, Food - 1);
        }
        if (naturalRegeneration && Food >= 18 && Health < 20)
        {
            if (++FoodTimer >= 80)
            {
                Health++;
                AddExhaustion(3);
                FoodTimer = 0;
            }
        }
        else if (Food == 0)
        {
            if (++FoodTimer >= 80)
            {
                int floor = difficulty == SurvivalDifficulty.Easy ? 10 : difficulty == SurvivalDifficulty.Normal ? 1 : 0;
                if (Health > floor) Health--;
                FoodTimer = 0;
            }
        }
        else FoodTimer = 0;
    }
}
