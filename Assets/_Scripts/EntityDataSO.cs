using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName="Entity Data",menuName="Data/Entity Data")]
public sealed class EntityDataSO : ScriptableObject
{
    public List<MobTuning> mobs=new List<MobTuning>();
    public VehicleTuning vehicles=new VehicleTuning();
    [Tooltip("Normalized chest lid progress per second. Default 2 opens in half a second.")]
    [Range(.1f,20)] public float chestLidSpeed=2;
    public void AddMissingMobs()
    {
        if(mobs==null)mobs=new List<MobTuning>();
        foreach(MobType species in Enum.GetValues(typeof(MobType)))
            if(!mobs.Exists(entry=>entry!=null&&entry.species==species))mobs.Add(MobTuning.From(MobDefinitions.Default(species)));
    }
    // Reset every session, including when Enter Play Mode has domain reload off.
    // Runtime uses validated copies; it never rewrites the editable asset.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeRuntime()
    {Apply(Resources.Load<EntityDataSO>("Entity Data"));}
    public static void Apply(EntityDataSO data)
    {
        MobDefinitions.Configure(data==null?null:data.mobs);
        VehicleTuning.Current=(data==null||data.vehicles==null?new VehicleTuning():data.vehicles).Validated();
        ModernChestVisual.LidSpeed=data==null||!VehiclePhysics.Finite(data.chestLidSpeed)?2:Mathf.Clamp(data.chestLidSpeed,.1f,20);
    }
}
