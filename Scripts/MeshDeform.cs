using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MeshDeform : MonoBehaviour
{
    public MeshRenderer deformTarget;
    Mesh deformMesh;
    Vector3[] deformVerts;
    public HarmonicMeshAsset harmonicMeshAsset;
    public SkinnedMeshRenderer cageSkinnedMeshRenderer;
    Mesh bakedMesh;

    bool gpu = true;
    MeshDeformGPU meshDeformGPU;

    public bool useCageMeshNormals; // e.g. toon shading

    public void Start()
    {
        bakedMesh = new Mesh();

        var deformMeshFilter = deformTarget.GetComponent<MeshFilter>();
        deformMesh = Instantiate(deformMeshFilter.sharedMesh);
        deformVerts = deformMesh.vertices;
        deformMesh.MarkDynamic();
        deformMeshFilter.mesh = deformMesh;

        meshDeformGPU = new MeshDeformGPU();
        meshDeformGPU.Setup( this.transform, cageSkinnedMeshRenderer.bones, deformMesh, harmonicMeshAsset, harmonicMeshAsset.cageVertCount);

    }

    private void Update()
    {
        cageSkinnedMeshRenderer.BakeMesh(bakedMesh);
        if (useCageMeshNormals)
        {
            bakedMesh.RecalculateNormals(); //# HACK
        }  
        var bakedCageVerts = bakedMesh.vertices;
        
        // apply deform
        if (gpu)
        {
            //for (var i=0; i< bakedCageVerts.Length; i++)
            //{
            //    bakedCageVerts[i] = cageSkinnedMeshRenderer.transform.TransformPoint(bakedCageVerts[i]);
            //}
            meshDeformGPU.Eval(bakedCageVerts, bakedMesh.normals);
            deformMesh.vertices = meshDeformGPU.deformSkinning.outVertices;
            if (useCageMeshNormals)
            {
                deformMesh.normals = meshDeformGPU.deformSkinning.outNormals;
            }
        } else
        {
            for (var i = 0; i < deformVerts.Length; i++)
            {
                var weights = harmonicMeshAsset.weights[i];
                var pos = Vector3.zero;
                for (var j = 0; j < weights.weight.Length; j++)
                {
                    var cageVertIndex = weights.cageVertIndex[j];
                    var w = weights.weight[j];
                    var cagePos = bakedCageVerts[cageVertIndex];
                    cagePos = cageSkinnedMeshRenderer.transform.TransformPoint(cagePos); // to world space
                    pos += cagePos * w;
                }
                deformVerts[i] = transform.InverseTransformPoint(pos); // local
            }
            deformMesh.vertices = deformVerts;
            deformMesh.RecalculateNormals();
        }
     
    }

    private void OnDestroy()
    {
        meshDeformGPU.Dispose();
    }
}
