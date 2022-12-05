 
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class MeshDeformGPU
{
    HarmonicMeshAsset harmonicMeshAsset;

    ComputeShader cs;

    ComputeBuffer cageVertsBuffer;
    ComputeBuffer cageNormalsBuffer;
    ComputeBuffer cageIndicesBuffer;
    ComputeBuffer cageWeightsBuffer; 
    ComputeBuffer linearWeightBuffer;

    public LinearGPUSkinningSet deformSkinning;
    
    int mainKernel;
    
    public void Setup(Transform transform, Transform[] bones, Mesh deformMesh, HarmonicMeshAsset harmonicMeshAsset, int cageVertCount)
    {
        cs = Resources.Load<ComputeShader>("MeshDeform");
        deformSkinning = new LinearGPUSkinningSet();
        deformSkinning.Setup(cs, transform, bones, deformMesh, "deform_");
        
        this.harmonicMeshAsset = harmonicMeshAsset;

        var weightCount = harmonicMeshAsset.weights.Length * harmonicMeshAsset.topCount;
        cageVertsBuffer = new ComputeBuffer(cageVertCount, Marshal.SizeOf(typeof(Vector3)));
        cageNormalsBuffer = new ComputeBuffer(cageVertCount, Marshal.SizeOf(typeof(Vector3)));
        cageIndicesBuffer = new ComputeBuffer(weightCount, Marshal.SizeOf(typeof(int)));
        cageWeightsBuffer = new ComputeBuffer(weightCount, Marshal.SizeOf(typeof(float)));
        linearWeightBuffer = new ComputeBuffer(weightCount, Marshal.SizeOf(typeof(float)));

        mainKernel = cs.FindKernel("CSMain");

        var cageIndicesData = new int[weightCount];
        var cageWeightsData = new float[weightCount];
        var index = 0;
        for (var i=0; i< harmonicMeshAsset.weights.Length; i++)
        {
            var weight = harmonicMeshAsset.weights[i];
            for (var j=0; j< harmonicMeshAsset.topCount; j++)
            {
                if (j >= weight.cageVertIndex.Length)
                {
                    // e.g top32 but only 8 verts (cube)
                    cageIndicesData[index] = 0;
                    cageWeightsData[index] = 0;
                } else
                {
                    cageIndicesData[index] = weight.cageVertIndex[j];
                    cageWeightsData[index] = weight.weight[j];
                }
               
                index++;
            } 
        }

        cageIndicesBuffer.SetData(cageIndicesData);
        cageWeightsBuffer.SetData(cageWeightsData);
    }
    
    void SetBuffers()
    {
        cs.SetBuffer(mainKernel, "linearWeightBuffer", linearWeightBuffer); 
        cs.SetBuffer(mainKernel, "cageVertsBuffer", cageVertsBuffer);
        cs.SetBuffer(mainKernel, "cageNormalsBuffer", cageNormalsBuffer);
        cs.SetBuffer(mainKernel, "cageIndicesBuffer", cageIndicesBuffer);
        cs.SetBuffer(mainKernel, "cageWeightsBuffer", cageWeightsBuffer);
        cs.SetInt("cageVertsPerVert", harmonicMeshAsset.topCount);
    }

    public void Eval(Vector3[] cageVerts, Vector3[] cageNormals)
    {
        deformSkinning.Eval();
        SetBuffers();

        this.linearWeightBuffer.SetData(harmonicMeshAsset.vertLinearSkinningWeights);
        this.cageVertsBuffer.SetData(cageVerts);
        this.cageNormalsBuffer.SetData(  cageNormals);

        cs.Dispatch(mainKernel, deformSkinning.outVertices.Length / 64 + 1, 1, 1);
        deformSkinning.Getoutput();
    }

    public void Dispose()
    {
        cageVertsBuffer.Dispose();
        cageNormalsBuffer.Dispose();
        cageIndicesBuffer.Dispose();
         cageWeightsBuffer.Dispose();
        deformSkinning.Dispose();
        linearWeightBuffer.Dispose();
    }
}
