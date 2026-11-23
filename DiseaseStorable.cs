using SnowyCraftingCore;
using UnityEngine;

namespace LethalDiseases;

public abstract class DiseaseStorable : FluidStorable
{
    ScanNodeProperties scanNode = null!;

    public virtual void Awake()
    {
        scanNode = gameObject.GetComponentInChildren<ScanNodeProperties>();
    }

    public override void Update()
    {
        base.Update();

        scanNode.subText = IsFilled ? Disease.GetNameFromString(StoredFluid) : "";
    }
}