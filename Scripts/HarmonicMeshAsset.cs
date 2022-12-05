using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class HarmonicMeshVertWeights
{
    public int[] cageVertIndex;
    public float[] weight;
}

[CreateAssetMenu()]
public class HarmonicMeshAsset : ScriptableObject
{
    public bool normalize = true;

    // didn't seem to have much effect
    [HideInInspector]
     public bool useInitBlur_experiment = false;

    public int topCount = 32;

    public HarmonicMeshVertWeights[] weights; // index = skinned mesh vert index

    //public float[] sum;
    //public float sumAverage;
    public float[] vertLinearSkinningWeights;

    public int cageVertCount;
}
