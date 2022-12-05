using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

// MeshCage\orig\MeshCage\Assets
public class LinearGPUSkinning 
{
    ComputeShader cs;
    ComputeBuffer verticesBuffer;
    ComputeBuffer boneWeightBuffer;
    ComputeBuffer boneBuffer;
    ComputeBuffer verticesOutBuffer;

    Vector3[] v;
    BoneWeight[] w;
    Transform[] bones;

    Vector3[] vOut;
    Transform transform;
    Matrix4x4[] boneMatrices;
    protected Matrix4x4[] bindposes;

    public  void Setup(Transform transform, Mesh mesh,  Transform[] bones)
    {
        this.transform = transform;
        vOut = mesh.vertices;
        v = mesh.vertices;
        w = mesh.boneWeights;
        this.bones = bones;
        this.bindposes = mesh.bindposes;

        cs = Resources.Load<ComputeShader>("LinearSkinning");
        verticesBuffer = new ComputeBuffer(v.Length, Marshal.SizeOf(typeof(Vector3)));
        verticesBuffer.SetData(v);
        boneWeightBuffer = new ComputeBuffer(v.Length, Marshal.SizeOf(typeof(BoneWeight)));
        boneWeightBuffer.SetData(w);
        boneBuffer = new ComputeBuffer(bones.Length, Marshal.SizeOf(typeof(Matrix4x4)));
        verticesOutBuffer = new ComputeBuffer(v.Length, Marshal.SizeOf(typeof(Vector3)));

        boneMatrices = new Matrix4x4[bones.Length];
    }
    
    public  void ApplySkinning()
    {
        var invBaseRot = Quaternion.Inverse(transform.rotation);
        for (var j = 0; j < bones.Length; j++)
        {
            boneMatrices[j] = transform.worldToLocalMatrix * bones[j].localToWorldMatrix * bindposes[j];
        }

        boneBuffer.SetData(boneMatrices);
        
        var kernel = cs.FindKernel("CSMain");
        cs.SetBuffer(kernel, "verticesBuffer", verticesBuffer);
        cs.SetBuffer(kernel, "boneWeightBuffer", boneWeightBuffer);
        cs.SetBuffer(kernel, "verticesOutBuffer", verticesOutBuffer);
        cs.SetBuffer(kernel, "boneBuffer", boneBuffer);
        cs.SetInt("vertCount", v.Length);
        cs.Dispatch(kernel, v.Length / 64 + 1, 1, 1);
        
        verticesOutBuffer.GetData(vOut);
    }

    public  void Dispose()
    {
        verticesBuffer.Dispose();
        boneWeightBuffer.Dispose();
        boneBuffer.Dispose();
        verticesOutBuffer.Dispose();
    }

}