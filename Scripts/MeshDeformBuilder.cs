using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HarmonicProcessLevel
{
    CellScale,
    VoxelSurface,
    VoxelFill,
    SingleCageVertSurfaceWeight,

    SingleCageVertInitBlur_Experiment,

    SingleCageVertBlur, 

    Full // apply deform weights to HarmonicMeshAsset
}

[RequireComponent(typeof(MeshRenderer))]
public class MeshDeformBuilder : MonoBehaviour
{
    public HarmonicProcessLevel processLevel;

    HarmonicGrid grid;
    HarmonicVoxel vox;
    HarmonicWeight hWeight;

     PointRenderer pointRenderer;
    [HideInInspector]
    public Vector3Int maxBounds = new Vector3Int(10,10,10);
    
    public int iterations = 50;
    
    public SkinnedMeshRenderer cageMeshRenderer;

     MeshRenderer deformMeshTarget;
    public HarmonicMeshAsset harmonicMeshAsset; // store weights

    public int heightCells = 10;

    public int testCageVertIndex;
    //public Transform container;
    public Transform container;

    public Texture2D weightTexture;

    void ResetScale(Transform target)
    {
        target.position = Vector3.zero;
        target.localScale = Vector3.one;

    }

     
    void SetLinearWeights(Vector2[] uvArr)
    {
        harmonicMeshAsset.vertLinearSkinningWeights = new float[uvArr.Length];
        var w = (float)weightTexture.width;
        var h = (float)weightTexture.height;
        for (var i=0; i< uvArr.Length; i++)
        {
            var uv = uvArr[i];
            var color = weightTexture.GetPixel(Mathf.RoundToInt(uv.x * w), Mathf.RoundToInt(uv.y * h));
            harmonicMeshAsset.vertLinearSkinningWeights[i] = color.g;
        }
    }

    void ScaleToUnitOne()
    {
        ResetScale(container);// this.transform);
       // ResetScale(deformMeshTarget.transform);

     
        var b = cageMeshRenderer.bounds;
        var s = heightCells / b.size.y;
        var scale = new Vector3(s, s, s);
        //this.transform.localScale = scale;
        //transform.localScale = scale;
        container.localScale = scale;

        // start pos at 0.5
        b = cageMeshRenderer.bounds;
        var minOffset = -b.min;
        var padding = 1.5f;
        var posOffset = new Vector3(minOffset.x + padding, minOffset.y + padding, minOffset.z + padding);

        // cells = new Vector3Int(((int)b.extents.x) * 2, ((int)b.extents.y) * 2, ((int)b.extents.z) * 2);


        maxBounds = new Vector3Int(Mathf.RoundToInt((b.extents.x + padding) * 2), Mathf.RoundToInt((b.extents.y + padding) * 2), Mathf.RoundToInt((b.extents.z + padding) * 2));

        

        //this.transform.position = posOffset;
        //this.transform.localScale = scale;

        //deformMeshTarget.transform.position = posOffset;
        //deformMeshTarget.transform.localScale = scale;
        container.position = posOffset;
        container.localScale = scale;
    }


    // before mesh deform start
    void Awake()
    {
        pointRenderer = gameObject.GetComponent<PointRenderer>();
        var start = Time.realtimeSinceStartup;

         

        deformMeshTarget = GetComponent<MeshRenderer>();
        

           // cageMeshRenderer = GetComponent<Renderer>();
           // var meshFilter = GetComponent<MeshFilter>();
           var sharedMesh = cageMeshRenderer.sharedMesh;
        var verts = sharedMesh.vertices;
        var tris = sharedMesh.triangles;
        var triCount = tris.Length / 3;

        ScaleToUnitOne();

        if (processLevel == HarmonicProcessLevel.CellScale)
        {
            return;
        }

        // world space
        for (var i = 0; i < verts.Length; i++)
        {
            // not this transform
            verts[i] = cageMeshRenderer.transform.TransformPoint(verts[i]);
        }

        //  var s = this.transform.localScale; 
        grid = new HarmonicGrid(maxBounds.x + 1, maxBounds.y + 1, maxBounds.z + 1, triCount);

        vox = new HarmonicVoxel(grid);
        for (var i = 0; i < triCount; i++)
        {
            var triIndex = i * 3;
            vox.AddTri(verts[tris[triIndex]], verts[tris[triIndex + 1]], verts[tris[triIndex + 2]], i);
        }

        if (processLevel == HarmonicProcessLevel.VoxelSurface)
        {
            vox.BakeIndices(); // required for draw
            DrawSurface();
            hWeight.Dispose();
            return;
        }

        vox.Fill();

        if (processLevel == HarmonicProcessLevel.VoxelFill)
        {
            cageMeshRenderer.enabled = false; // need to see fill verts
            vox.BakeIndices(); // required for draw
            DrawSurfaceAndFill();
            hWeight.Dispose();
            return;
        }

        vox.BakeIndices();

        var deformMesh = deformMeshTarget.GetComponent<MeshFilter>().sharedMesh;
        SetLinearWeights(deformMesh.uv);
        hWeight = new HarmonicWeight(grid, tris, verts, deformMeshTarget.transform, deformMesh, harmonicMeshAsset);

        if (processLevel == HarmonicProcessLevel.SingleCageVertSurfaceWeight)
        {
            cageMeshRenderer.enabled = false; // need to see fill verts
            hWeight.TestSingleCageVertSurfaceWeight(testCageVertIndex);
            DrawWeights();
            hWeight.Dispose();
            return;
        }

        if (processLevel == HarmonicProcessLevel.SingleCageVertInitBlur_Experiment)
        {
            cageMeshRenderer.enabled = false; // need to see fill verts
            hWeight.TestSingleCageVertSurfaceWeight(testCageVertIndex);

            //var cellPos = hWeight.vertices[testCageVertIndex];
            var controlPoint = hWeight.GetControlPointCellPos(testCageVertIndex);// new Vector3Int((int)cellPos.x, (int)cellPos.y, (int)cellPos.z);
            hWeight.ApplyInitBlur(controlPoint);
            //vox.BakeIndices(); // from ApplyInitBlur

            DrawWeights();
            hWeight.Dispose();
            return;
        }

        if (processLevel == HarmonicProcessLevel.SingleCageVertBlur)
        {
            cageMeshRenderer.enabled = false; // need to see fill verts
            hWeight.TestSingleCageVertSurfaceWeight(testCageVertIndex);
            //for (var i = 0; i < iterations; i++)
            //{
            //    hWeight.Diffuse();
            //}
            hWeight.gpu.Eval(iterations);
            DrawWeights();
            hWeight.Dispose();
            return;
        }

        // hWeight.ApplyWeight(0, iterations);
        hWeight.Process(iterations);


        //DrawSurface();
        ResetScale(container);// this.transform);
                              // ResetScale(this.transform);
                              // ResetScale(deformMeshTarget.transform);

        Debug.Log("Complete: " + Mathf.FloorToInt(Time.realtimeSinceStartup - start));

        var meshDeform = this.gameObject.AddComponent<MeshDeform>();
        meshDeform.harmonicMeshAsset = this.harmonicMeshAsset;
        meshDeform.deformTarget = this.deformMeshTarget;
        meshDeform.cageSkinnedMeshRenderer = cageMeshRenderer;
         
        hWeight.Dispose(); // free GPU

        Destroy(this); // component not required
    }

    private void DrawSurface()
    {
        pointRenderer.Begin();
        foreach (var i in grid.surfaceCellIndices)
        {
            pointRenderer.Add(grid.GetCellCenterPos(i), 1);
        }
        pointRenderer.End();
    }

    private void DrawSurfaceAndFill()
    {
        pointRenderer.Begin();
        foreach (var i in grid.surfaceCellIndices)
        {
            pointRenderer.Add(grid.GetCellCenterPos(i), 1);
        }
        foreach (var i in grid.fillCellIndices)
        {
            pointRenderer.Add(grid.GetCellCenterPos(i), 0);
        }
        pointRenderer.End();
    }

    private void DrawWeights()
    {
        pointRenderer.Begin();
        foreach (var i in grid.surfaceCellIndices)
        {
            pointRenderer.Add(grid.GetCellCenterPos(i), grid.GetWeight(i));
        }

        foreach (var i in grid.fillCellIndices)
        {
            pointRenderer.Add(grid.GetCellCenterPos(i), grid.GetWeight(i));
        }
        pointRenderer.End();
    }


    //private void DrawSurface2()
    //{
    //    if (debugDraw == false)
    //    {
    //        return;
    //    }

    //    pos.Clear();
    //    weight.Clear();

    //    if (true)
    //    {
    //        foreach (var i in grid.surfaceCellIndices)
    //        {
    //            pos.Add(grid.GetCellCenterPos(i));
    //            weight.Add(1.0f);
    //        }
    //        foreach (var i in grid.fillCellIndices)
    //        {
    //            pos.Add(grid.GetCellCenterPos(i));
    //            weight.Add(0.0f);
    //        }

    //        //foreach (var i in grid.triangleCells[triIndex])
    //        //{
    //        //    pos.Add(grid.GetCellCenterPos(i));
    //        //    weight.Add(0.5f);
    //        //}

    //    } else 
    //    { 
    //       foreach (var i in grid.surfaceCellIndices)
    //       {
    //           pos.Add(grid.GetCellCenterPos(i));
    //           weight.Add(grid.GetWeight(i));
    //        }

    //        foreach (var i in grid.fillCellIndices)
    //        {
    //            pos.Add(grid.GetCellCenterPos(i));
    //            weight.Add(grid.GetWeight(i));
    //        }
    //    }




    //    pointRenderer.Draw(pos, weight, 0.5f);// 25); 
    //}

    //private void OnDrawGizmos()
    //{
    //    if (grid == null)
    //    {
    //        return;

    //    }

    //    if (showGrid)
    //    {
    //        foreach (var i in grid.surfaceCellIndices)
    //        {
    //            var pos = grid.GetCellCenterPos(i);
    //            Gizmos.DrawSphere(pos, 0.5f);
    //        }
    //    }

    //    if (showRaw)
    //    {
    //        foreach (var pos in vox.rawTest)
    //        {
    //            Gizmos.DrawSphere(pos, 0.5f);
    //        }
    //    }

    //}

}
