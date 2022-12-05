using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HarmonicWeight 
{
    public HarmonicGrid grid;
    int[] tris;
    int triCount;
    List<List<int>> triCellLists = new List<List<int>>();
    public Vector3[] vertices; // cage

    public Vector3[] deformVerts;
    HarmonicMeshAsset harmonicMeshAsset;
    public BlurGPU gpu;

    public HarmonicWeight(HarmonicGrid grid, int[] tris, Vector3[] vertices, Transform deformMeshTransfrom, Mesh deformMesh, HarmonicMeshAsset harmonicMeshAsset)
    {
        this.grid = grid;
        this.tris = tris;
        this.vertices = vertices;
        triCount = tris.Length / 3;
        this.harmonicMeshAsset = harmonicMeshAsset;

        gpu = new BlurGPU();
        gpu.Setup(grid);

       // Debug.LogError("BW: " + deformMesh.boneWeights.Length);
        deformVerts = deformMesh.vertices;
        for (var i=0;i<deformVerts.Length; i++)
        {
            deformVerts[i] = deformMeshTransfrom.TransformPoint(deformVerts[i]);
        }
        SetupSkinning();
    }

    public void Dispose()
    {
        gpu.Dispose();
    }

    public void TestSingleCageVertSurfaceWeight(int cageVertIndex)
    {
        ClearPrevApplyWeights();

        for (var i = 0; i < triCount; i++)
        {
            var triIndex = i * 3;
            if (tris[triIndex] == cageVertIndex || tris[triIndex + 1] == cageVertIndex || tris[triIndex + 2] == cageVertIndex)
            {
                var triCells = grid.triangleCells[i];
                Apply(triCells, i, cageVertIndex);
            }
        }

    }
 
    public void ApplyWeight(int cageVertIndex, int iterations)
    {
        ClearPrevApplyWeights();

        // apply weights to surface
        for (var i=0; i<triCount; i++)
        {
            var triIndex = i * 3;
            if (tris[triIndex] == cageVertIndex || tris[triIndex+1] == cageVertIndex || tris[triIndex+2] == cageVertIndex)
            {
                var triCells = grid.triangleCells[i];
                triCellLists.Add(triCells);
                Apply(triCells, i, cageVertIndex);
            }
        }

        // smooth
        // var prevFillWeight = 0.0f;

        if (harmonicMeshAsset.useInitBlur_experiment)
        {
            ApplyInitBlur(GetControlPointCellPos(cageVertIndex));
        }

        if (true)
        {
         
            gpu.Eval(iterations);
 
        }
        else
        {
            for (var i = 0; i < iterations; i++)
            {
                Diffuse();

                // weight change doesn't work on more complex meshes
                // might want to use max change per cell?

                //if (cageVertIndex == 10)
                //{
                //    var fillWeight = grid.CalcFillWeight();
                //    var change = fillWeight - prevFillWeight;
                //    prevFillWeight = fillWeight;
                //    Debug.Log(i.ToString("000") + ": " + change);
                //}
            }
        }
     
        
       

        // calc deform vert weights
        SetSkinWeights(cageVertIndex); 
    }
     
    void Apply(List<int> cells, int tri, int w1VertIndex)
    {
        var triIndex = tri * 3;

        Vector3 w1;
        Vector3 w0A;
        Vector3 w0B;

        var a = tris[triIndex + 0];
        var b = tris[triIndex + 1];
        var c = tris[triIndex + 2];
        if (w1VertIndex == a)
        {
            w1 = vertices[a];
            w0A = vertices[b];
            w0B = vertices[c];
        } else if (w1VertIndex == b)
        {
            w1 = vertices[b];
            w0A = vertices[c];
            w0B = vertices[a];
        }
        else
        {
            w1 = vertices[c];
            w0A = vertices[a];
            w0B = vertices[b];
        }
         
        foreach (var cell in cells)
        {
            var cellPos = grid.GetCellCenterPos(cell);
            var weight = MathUtil.BaryCentricWeight(w1, w0A, w0B, cellPos);

            // can only increase e.g small triangle => single cell => max weight
            var curWeight = grid.GetWeight(cell);
            if (weight > curWeight)
            {
                grid.SetWeight(cell, weight);
            } 
        }
    }

    public void ClearPrevApplyWeights()
    {
        // clear surface
        foreach (var list in triCellLists)
        {
            foreach (var cell in list)
            {
                grid.SetWeight(cell, 0);
            }
        }
        triCellLists.Clear();

        // clear fill
        var count = grid.fillCellIndices.Count;
        for (var i = 0; i < count; i++)
        {
            grid.SetWeight(grid.fillCellIndices[i], 0);
        }
    }

    public void Diffuse()
    {
        // single smooth pass
        var mult = 1.0f / 6.0f;
        var count = grid.fillCellIndices.Count;

      

        for (var i = 0; i < count; i++)
        {
            var cell = grid.fillCellIndices[i];
            var iCell = grid.GetCellPos(cell);

            var w =
              (
              //grid.GetWeight(cell) +  don't include self
              grid.GetWeight(cell + grid.xInc) +
              grid.GetWeight(cell + grid.yInc) +
              grid.GetWeight(cell + grid.zInc) +
              grid.GetWeight(cell - grid.xInc) +
              grid.GetWeight(cell - grid.yInc) +
              grid.GetWeight(cell - grid.zInc)
               ) * mult;

            grid.SetWeight(cell, w);
        }
    }

    class TempWeight
    {
        public int cageVertIndex;
        public float weight;
    }
    List<TempWeight>[] tempSkinWeights;

    void SetupSkinning()
    {
        tempSkinWeights = new List<TempWeight>[deformVerts.Length];
        for (var i = 0; i < deformVerts.Length; i++)
        {
            tempSkinWeights[i] = new List<TempWeight>();
        }

        harmonicMeshAsset.cageVertCount = this.vertices.Length;
        harmonicMeshAsset.weights = new HarmonicMeshVertWeights[deformVerts.Length];
        //harmonicMeshAsset.vertLinearSkinningWeights = new float[deformVerts.Length];
    }

    public void SetSkinWeights(int cageVertIndex)
    { 
        for (var i=0; i<deformVerts.Length; i++)
        {
            var weight = grid.CalcHarmonicWeight(deformVerts[i]); 
            tempSkinWeights[i].Add(new TempWeight { cageVertIndex = cageVertIndex, weight = weight }); 
        } 
    }

    void SaveSkinWeights()
    {
        //var sumTotal = 0.0f;
        for (var i = 0; i < deformVerts.Length; i++)
        {
            if (harmonicMeshAsset.vertLinearSkinningWeights[i] == 1)
            {
                // full linear, ignore weight
                //continue;
            }
            var weights = tempSkinWeights[i];
            var res = weights.OrderByDescending(o => o.weight).ToList();
             
            var topCount = Mathf.Min(harmonicMeshAsset.topCount, res.Count);  
            var vh = new HarmonicMeshVertWeights();
            vh.cageVertIndex = new int[topCount];
            vh.weight = new float[topCount];
             
            // normalize / get sum
            var sum = 0.0f;
            for (var j = 0; j < topCount; j++)
            {
                sum += res[j].weight;
            }

            for (var j=0; j< topCount; j++)
            {
                vh.cageVertIndex[j] = res[j].cageVertIndex;

                if (harmonicMeshAsset.normalize)
                {
                    vh.weight[j] = res[j].weight / sum; // normalize
                }
                else
                {
                    vh.weight[j] = res[j].weight;//  / sum; // normalize
                }
          
            }

            // NOTE: sum should be ~1
            //Debug.LogError(sum);
            harmonicMeshAsset.weights[i] = vh;
            //harmonicMeshAsset.sum[i] = sum;
            //sumTotal += sum;
        }

       // harmonicMeshAsset.sumAverage = sumTotal / deformVerts.Length;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(harmonicMeshAsset);
#endif
    }

    public Vector3Int GetControlPointCellPos(int cageVertIndex)
    {
        var cellPos = vertices[cageVertIndex];
        var controlPoint = new Vector3Int((int)cellPos.x, (int)cellPos.y, (int)cellPos.z);

        // validate
        var cell = grid.GetCellIndex(controlPoint.x, controlPoint.y, controlPoint.z);
        var cellType = grid.GetCellType(cell);
        Debug.Assert(cellType == CellType.Surface);

        return controlPoint;
    }

    public void Process(int iterations)
    {
        for (var i=0; i< vertices.Length; i++)
        {

            ApplyWeight(i, iterations);
        }

        // after processing all verts
        SaveSkinWeights();

        //ClearPrevApplyWeights();
    }


    #region Init blur

    bool AtDistance(int x, int y, int z, int distance)
    {
        distance = Mathf.Abs(distance);
        return Mathf.Abs(x) == distance || Mathf.Abs(y) == distance || Mathf.Abs(z) == distance;
    }

    float CalcBoxAverageSurfaceWeight(Vector3Int controlPoint, int distance)
    {
        var sum = 0.0f;
        var count = 0;
        for (var x=-distance; x<=distance; x++)
        {
            for (var y = -distance; y <= distance; y++)
            {
                for (var z = -distance; z <= distance; z++)
                {
                    // only on outer edges
                    if (AtDistance(x ,y, z , distance))
                    {
                        var cell = grid.GetCellIndex(controlPoint.x+x, controlPoint.y+y, controlPoint.z+z);
                        if (grid.IsCellValid(cell) == false)
                        {
                            //Debug.LogError("Invalid: " + x + "," + y + "," +z);
                            // outside grid  
                            continue;
                        }
                       // Debug.LogError(grid.GetCellType(cell));
                        if (grid.GetCellType(cell) == CellType.Surface ) {
                            sum += grid.GetWeight(cell);
                            count++;
                        }
                    }
                }
            }
        }
        if (count == 0)
        {
            return 0;
        }
        return sum / count;
    }

    void SetFillWeight1(Vector3Int controlPoint, float weight)
    {
        var distance = 1;
        for (var x = -distance; x <= distance; x++)
        {
            for (var y = -distance; y <= distance; y++)
            {
                for (var z = -distance; z <= distance; z++)
                {
                    // only on outer edges
                    if (AtDistance(x, y, z, distance))
                    {
                        var cell = grid.GetCellIndex(controlPoint.x + x, controlPoint.y + y, controlPoint.z + z);
                        if (grid.IsCellValid(cell) == false)
                        {
                            // outside grid  
                            continue;
                        }
                        if (grid.GetCellType(cell) == CellType.In)
                        {
                            // shouldn't have fill on both sides of same cell: TODO: check
                            grid.SetWeight(cell, weight);
                        }
                    }
                }
            }
        } 
    }

    bool HasFillWeightNeigbour(Vector3Int controlPoint)
    {
        var distance = 1;
        for (var x = -distance; x <= distance; x++)
        {
            for (var y = -distance; y <= distance; y++)
            {
                for (var z = -distance; z <= distance; z++)
                {
                    if (AtDistance(x, y, z, distance))
                    {
                        var cell = grid.GetCellIndex(controlPoint.x + x, controlPoint.y + y, controlPoint.z + z);
                        if (grid.IsCellValid(cell) == false)
                        {
                            continue;
                        }
                        if (grid.GetCellType(cell) == CellType.In && grid.GetWeight(cell) > 0)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    void SetDistanceFillWeight(Vector3Int controlPoint, float weight, int distance)
    { 
        for (var x = -distance; x <= distance; x++)
        {
            for (var y = -distance; y <= distance; y++)
            {
                for (var z = -distance; z <= distance; z++)
                {
                    // only on outer edges
                    if (AtDistance(x, y, z, distance))
                    {
                        var cell = grid.GetCellIndex(controlPoint.x + x, controlPoint.y + y, controlPoint.z + z);
                        if (grid.IsCellValid(cell) == false)
                        {
                            // outside grid  
                            continue;
                        }
                        //Debug.LogError("Vaild: " + x + "," + y + "," + z );
                        if (grid.GetCellType(cell) == CellType.In)
                        {
                            // shouldn't have fill on both sides of same cell: TODO: check
                            if (HasFillWeightNeigbour(controlPoint))
                            {
                                grid.SetWeight(cell, weight);
                            } 
                        }
                    }
                }
            }
        }
    }

    // Expand out from max weight control point
    // 
    public void ApplyInitBlur(Vector3Int controlPoint)
    {
        // first pass sets all fill type neighbours
        var w1 = CalcBoxAverageSurfaceWeight(controlPoint, 1);
        SetFillWeight1(controlPoint, w1);

        SetDistanceFillWeight(controlPoint, 1, 2);

        //return;
        var dist = 2;
        var surfaceAvg = CalcBoxAverageSurfaceWeight(controlPoint, dist);
        while (surfaceAvg > 0)
        {
            SetDistanceFillWeight(controlPoint, surfaceAvg, dist);
            dist++;
            surfaceAvg = CalcBoxAverageSurfaceWeight(controlPoint, dist);
        }
    }

    #endregion

}
