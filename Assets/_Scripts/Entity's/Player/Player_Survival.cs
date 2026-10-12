using Mirror;
using UnityEngine;

public partial class Player
{
    [SyncVar, Range(0, 20)] public float saturation = 5;
    [SyncVar, Range(0, 40)] public float exhaustion;
    private readonly HungerSimulation hungerSimulation = new HungerSimulation();
    private float hungerTickTime;
    private float localWalkDistance, localSprintDistance;
    private Vector3 lastSurvivalPosition;
    private bool survivalPositionKnown;
    private bool serverSprint, serverFlying, serverGrounded;
    private bool movementStateSent, sentSprint, sentFlying, sentGrounded;
    private double lastJumpTime = -1;

    private void LoadHungerState()
    {
        hungerSimulation.Health = SurvivalHudValues.ClampPoints(health);
        hungerSimulation.Food = SurvivalHudValues.ClampPoints(hunger);
        hungerSimulation.Saturation = float.IsNaN(saturation) ? 0 : Mathf.Clamp(saturation, 0, hungerSimulation.Food);
        hungerSimulation.Exhaustion = float.IsNaN(exhaustion) ? 0 : Mathf.Clamp(exhaustion, 0, 40);
    }

    private void StoreHungerState()
    {
        health = hungerSimulation.Health;
        hunger = hungerSimulation.Food;
        saturation = hungerSimulation.Saturation;
        exhaustion = hungerSimulation.Exhaustion;
    }

    private void PublishSurvivalMovement()
    {
        if (!networkStarted || isServer) return;
        if (movementStateSent && sentSprint == isSprinting && sentFlying == isFlying && sentGrounded == isGrounded) return;
        movementStateSent = true;
        sentSprint = isSprinting; sentFlying = isFlying; sentGrounded = isGrounded;
        CmdSurvivalMovement(isSprinting, isFlying, isGrounded);
    }

    [Command]
    private void CmdSurvivalMovement(bool sprinting, bool flying, bool grounded)
    {
        // Movement is already client-authoritative in this project. Report only
        // modes; food costs use displacement observed by the server transform.
        serverSprint = sprinting && hunger > 6;
        serverFlying = flying;
        serverGrounded = grounded;
    }

    protected override void OnJumpPerformed()
    {
        if (isFlying || health <= 0) return;
        if (!networkStarted || isServer) ApplySurvivalJump(isSprinting);
        else if (isLocalPlayer) CmdSurvivalJump(isSprinting);
    }

    [Command]
    private void CmdSurvivalJump(bool sprinting)
    {
        // Same minimum interval as BaseEntity's existing jump cooldown. Clients
        // never supply an exhaustion amount or a new health/food value.
        if (NetworkTime.time - lastJumpTime < 0.45 || serverFlying) return;
        lastJumpTime = NetworkTime.time;
        ApplySurvivalJump(sprinting);
    }

    private void ApplySurvivalJump(bool sprinting)
    {
        LoadHungerState();
        hungerSimulation.AddJump(sprinting, isLocalPlayer ? isFlying : serverFlying);
        StoreHungerState();
    }

    private void AdvanceSurvival(float dt)
    {
        if (world == null || !world.IsWorldCreated) { survivalPositionKnown = false; localWalkDistance = localSprintDistance = 0; return; }
        hungerTickTime += dt;
        if (hungerTickTime + 0.000001f < 0.05f) return;
        var position = transform.position;
        bool local = !networkStarted || isLocalPlayer;
        bool flying = local ? isFlying : serverFlying;
        bool sprint = local ? isSprinting : serverSprint;
        bool grounded = local ? isGrounded : serverGrounded;
        LoadHungerState();
        if (local)
        {
            hungerSimulation.AddGroundMovement(localWalkDistance, false, flying);
            hungerSimulation.AddGroundMovement(localSprintDistance, true, flying);
            localWalkDistance = localSprintDistance = 0;
        }
        else if (survivalPositionKnown && grounded && !flying)
        {
            float distance = new Vector2(position.x - lastSurvivalPosition.x, position.z - lastSurvivalPosition.z).magnitude;
            // Ignore teleports/spawn corrections and cap ordinary network bursts.
            float maximum = Mathf.Max(walkSpeed, sprintSpeed) * hungerTickTime * 3;
            if (distance <= maximum) hungerSimulation.AddGroundMovement(distance, sprint, false);
        }
        lastSurvivalPosition = position;
        survivalPositionKnown = true;
        while (hungerTickTime + 0.000001f >= 0.05f)
        {
            hungerTickTime = Mathf.Max(0, hungerTickTime - 0.05f);
            hungerSimulation.Tick(world.survivalDifficulty, world.naturalRegeneration);
        }
        StoreHungerState();
        if (hunger <= 6 && !isFlying) isSprinting = false;
    }
}
