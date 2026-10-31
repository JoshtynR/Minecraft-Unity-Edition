# Live video settings

```bash
python3 Tools/SettingsChecks/run.py --mcs /path/to/mcs --mono /path/to/mono
```

Compiles production SettingsManager, UISlider, and the actual streaming request
entry point against Unity/world doubles. Checks immediate gamma and cached
dictionary updates, loaded/new dimensions, stationary view changes, forced
shrink refreshes, debounce, active-worker coalescing, saving/transfer blockers,
invalid values, music volume, and deferred/explicit persistence.

Brightness updates the existing lightmap's gamma input; it does not dirty chunk
meshes or recalculate gameplay light levels. The active world's lighting update
already notices changed gamma, including while time scale is zero.

Render distance updates world values immediately and requests a forced streaming
pass 150 ms after the most recent change. Force bypasses the ready-area shortcut
so reducing the view also reaches the normal dirty-save/eviction path. The
existing generation queue, worker ownership and upload budgets remain in use.
The request waits for a created world/player and completion of saves/transfers.
Newly activated worlds receive current settings on the next SettingsManager
update; cached dimensions receive them at the time of the edit.

Menu synchronization uses SetValueWithoutNotify, preventing scene/panel loading
from masquerading as a slider edit. PlayerPrefs saves are deferred until 500 ms
after the most recent edit; Done, application pause/quit, and manager teardown
flush pending changes. Existing GUI-scale/FOV application remains on Apply.

These checks do not run Unity's renderer, disk-backed chunk eviction, or actual
generation tasks. In Play Mode/builds, test brightness with the options panel
open, both distance directions while standing still, changing distance during
generation, Escape/reopen, and an Overworld/Nether round trip. New terrain still
takes time to generate; dynamic settings are not an instant-generation promise.
