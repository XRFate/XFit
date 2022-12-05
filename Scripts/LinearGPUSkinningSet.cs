using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class LinearGPUSkinningSet 
{
    Mesh deformMesh;

    ComputeShader cs;

    ComputeBuffer verticesBuffer; // rest pos
    ComputeBuffer boneWeightBuffer;
    ComputeBuffer boneBuffer;
    ComputeBuffer verticesOutBuffer;

    public Vector3[] outVertices;
    public Vector3[] outNormals;

    #region Linear Skinning
    Vector3[] v;
    BoneWeight[] w;
    Transform[] bones;

    Vector3[] vOut;
    Transform transform;
    Matrix4x4[] boneMatrices;
    protected Matrix4x4[] bindposes;
    #endregion

    string prefix;

    int mainKernel;

    public void Setup(ComputeShader cs, Transform transform, Transform[] bones, Mesh mesh, string prefix)
    {
        this.deformMesh = mesh;
        this.cs = cs;// Resources.Load<ComputeShader>("MeshDeform");
        outVertices = mesh.vertices; // not using values, just sizing array
        outNormals = mesh.normals;
        this.prefix = prefix;
        verticesOutBuffer = new ComputeBuffer(outVertices.Length, Marshal.SizeOf(typeof(Vector3)));
        //normals = new ComputeBuffer(outNormals.Length, Marshal.SizeOf(typeof(Vector3)));

        mainKernel = cs.FindKernel("CSMain");

       
        this.transform = transform;
        vOut = mesh.vertices;
        v = mesh.vertices;
        w = mesh.boneWeights;
        this.bones = bones;
        this.bindposes = mesh.bindposes;

        //  cs = Resources.Load<ComputeShader>("LinearSkinning");
        verticesBuffer = new ComputeBuffer(v.Length, Marshal.SizeOf(typeof(Vector3)));
        verticesBuffer.SetData(v);
        boneWeightBuffer = new ComputeBuffer(v.Length, Marshal.SizeOf(typeof(BoneWeight)));
        boneWeightBuffer.SetData(w);
        boneBuffer = new ComputeBuffer(bones.Length, Marshal.SizeOf(typeof(Matrix4x4)));
        //verticesOutBuffer = new ComputeBuffer(v.Length, Marshal.SizeOf(typeof(Vector3))); 
        boneMatrices = new Matrix4x4[bones.Length];

        //var kernel = cs.FindKernel("CSMain");


        // set out data array
        verticesOutBuffer.SetData(outVertices);

    }



    void SetBuffers()
    {

        cs.SetBuffer(mainKernel, prefix+"verticesBuffer", verticesBuffer);
        cs.SetBuffer(mainKernel, prefix+"boneWeightBuffer", boneWeightBuffer);
        cs.SetBuffer(mainKernel, prefix+"verticesOutBuffer", verticesOutBuffer);
        cs.SetBuffer(mainKernel, prefix+"boneBuffer", boneBuffer);
        cs.SetInt(prefix+"vertCount", v.Length);
    }

    public void Eval()
    {
        // need to set per update e.g multiple meshes using the same compute shader
        SetBuffers();

        var invBaseRot = Quaternion.Inverse(transform.rotation);
        for (var j = 0; j < bones.Length; j++)
        {
            boneMatrices[j] = transform.worldToLocalMatrix * bones[j].localToWorldMatrix * bindposes[j];
        }
        boneBuffer.SetData(boneMatrices);

        //cs.Dispatch(mainKernel, outVertices.Length / 64 + 1, 1, 1);

    }

    public void Getoutput()
    {

        verticesOutBuffer.GetData(outVertices);

        var top = new Vector3[10];
        for (var i = 0; i < 10; i++)
        {
            top[i] = outVertices[i];
        }
    }

    public void Dispose()
    {
    
        verticesOutBuffer.Dispose();
        verticesBuffer.Dispose();
        boneWeightBuffer.Dispose();
        boneBuffer.Dispose();
    }
}
