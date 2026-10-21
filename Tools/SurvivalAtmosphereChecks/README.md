# Survival and overworld atmosphere

Run `python3 Tools/SurvivalAtmosphereChecks/run.py --mcs <compiler> --mono <runtime>`.
This compiles the production food/fall rules, player adapter, world clock and
sky component against Unity/Mirror doubles. It covers food timing, cancellation,
nutrition, inventory revision rejection, bowl returns, fall thresholds, camera
baseline recovery, clock resume, fog scope and sky resource ownership.

Motion also compiles the production BetaPlayerMotion and MinecraftWalkAnimator.
Checks cover the reference first camera/limb tick, identical travel at 30/50/60/
144/240 update rates, settling, teleport reset, jump pitch, opposite leg phases
and forward hand bob versus inverse camera bob. The old walking clip is kept as
an asset; runtime limb rotations now follow ModelBiped's displacement formulas.
The Animator continues to supply existing crouch translations.

The existing fluid, startup/save, first-person, hunger, inventory, memory and
Nether checks cover their integration points. Unity and shader rendering still
need an editor/build smoke test:

- Walk/sprint, stop, jump, swim and fly; walking bob should ease out in the air,
  with vertical-velocity camera pitch during jumps. Flight disables bob. Check
  crouching, held items and remote players' limb poses. Enable/disable view bob
  and confirm the camera returns to its resting position.
- Hold right-click with food below full hunger; releasing or changing slots
  cancels. Eating takes 1.6 seconds, slows movement and returns bowls for stew.
- Fall three blocks safely, then four blocks for one damage point; test water
  landings, flight, respawn and portal travel without carried fall distance.
- Observe fog approaching the render boundary, then enter water/lava and return
  to air. The hand and HUD should not inherit world fog.
- Set the World clock to 6000/noon or 18000/midnight, observe sky/light changes,
  then save/reopen and check time resumes. Multiplayer clients share server time.
- Check moving clouds and opposite sun/moon in the overworld, and their absence
  in the Nether. The resource config accepts sun/cloud textures and a 4x2 moon
  phase sheet. Existing GUI refresh binds environment textures when present;
  generated pixel textures provide a fallback when absent.

Food replenishes the project's existing hunger/saturation system; this is the
same deliberate post-Beta survival extension as sprinting and hunger. These
changes do not alter seed generation or block/item asset assignments.
