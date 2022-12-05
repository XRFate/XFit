using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarmonicVoxel 
{
    public HarmonicGrid grid;
    public float scale = 2.0f; // *2 scale => increase pos on add (adds two points per 1 unit) but half on set (back to world)

    public List<Vector3> rawTest = new List<Vector3>();

    public int triIndex;

    Vector3 triMin;
    Vector3 triMax;

    public HarmonicVoxel(HarmonicGrid grid)
    {
        this.grid = grid;
    }

    public void BakeIndices()
    {
        grid.surfaceCellIndices.Clear();
       
        for (var i=0; i<grid.cellCount; i++)
        {
            var type = grid.GetCellType(i);
            if (type == CellType.Surface)
            {
                grid.surfaceCellIndices.Add(i);
            }
            else if (type == CellType.In)
            {
                grid.fillCellIndices.Add(i);
            } else if (type == CellType.Tri)
            {
                throw new System.Exception("Shouldn't contain tris");
            }
        }

        //grid.fillCellIndicesArr = grid.fillCellIndices.ToArray();
    }

    public void AddTri(Vector3 a, Vector3 b, Vector3 c, int triIndex)
    {
         triMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
         triMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        a *= scale;
        b *= scale;
        c *= scale;
        this.triIndex = triIndex; 
        var ab = Vector3.Distance(a, b);
        var bc = Vector3.Distance(b, c);
        var ac = Vector3.Distance(a, c);

        if (ab > bc)
        {
            if (ab > ac)
            {
                // ab == max
                VoxelTri(c, a, b);
            }
            else
            {
                // ac == max
                VoxelTri(b, a, c);
            }
        }
        else if (bc > ac)
        {
            // bc == max
            VoxelTri(a, b, c);
        }
        else
        {
            // ac == max
            VoxelTri(b, a, c);
        }

        BakeTriList();
    }
    
    // converts tri types to surface types and stores cells used by tri
    void BakeTriList()
    {
        var min = new Vector3Int((int)triMin.x, (int)triMin.y, (int)triMin.z);
        var max = new Vector3Int((int)triMax.x, (int)triMax.y, (int)triMax.z);
        var triCells = new List<int>();

        // note: needs to include max
        for (var x=min.x; x<=max.x; x++)
        {
            for (var y = min.y; y <= max.y; y++)
            {
                for (var z = min.z; z <= max.z; z++)
                {
                    var cell = grid.GetCellIndex(x, y, z);
                    var type = grid.GetCellType(cell);
                    if (type == CellType.Tri)
                    {
                        triCells.Add(cell);
                        grid.SetCellType(cell, CellType.Surface);
                    }
                }
            }
        }

        grid.triangleCells[triIndex] = triCells;
    }

    void SetSurfaceCell(Vector3 pos)
    {
        pos = pos / scale;
        var cell = grid.GetCellIndex(pos);
        grid.SetCellType(cell, CellType.Tri);//  Surface);
        //Debug.LogError(pos);
        rawTest.Add(pos);

        // cell pos
        triMin = Vector3.Min(triMin, pos);
        triMax = Vector3.Max(triMax, pos);
    }

    void VoxelTri(Vector3 start, Vector3 left, Vector3 right)
    { 
        //var c = Gizmos.color;
        var end = MathUtil.ProjectPointOnLine(left, (right - left).normalized, start);
 
        SetSurfaceCell(left);
        SetSurfaceCell(right);

        var height = Vector3.Distance(start, end);
        DrawRHTri(start, end, left, height);
        DrawRHTri(start, end, right, height);

        DrawLine(start, end, height); // shared between both sides
        DrawLine(start, left, Vector3.Distance(start, left)); // extra
        DrawLine(start, right, Vector3.Distance(start, right)); // extra

        //Gizmos.DrawLine(start, left);
        //Gizmos.DrawLine(start, right);
        //Gizmos.DrawLine(left, right);

        //Gizmos.color = c;
    }

    int GetCellCount(float dist)
    {
        return (int)(dist);// / cellSize);
    }
     
    void DrawRHTri(Vector3 start, Vector3 end, Vector3 corner, float height)
    {
        var width = Vector3.Distance(end, corner);
        var slope = width / height;
        var fwd = (corner - end).normalized; // outward direction, cell units = 1

        // draw slope points
        var heightCells = GetCellCount(height); // (int)height;
        var w = 0.0f;
        for (var i = 0; i <= heightCells; i++)
        {
            var t = i / height;

            // mid line
            var midPoint = Vector3.Lerp(start, end, t);

            // start at 1 e.g two triangles, don't draw the same mid line points
            var xCount = GetCellCount(w);// (int)w;
            for (var x = 1; x <= xCount; x++)
            {
                var pos = midPoint + (fwd * x);
                SetSurfaceCell(pos);
            }

            w += slope;
        } 

        DrawLine(end, corner, width);

    }

    void DrawLine(Vector3 start, Vector3 end, float dist)
    {
        var endCells = GetCellCount(dist);// (int)dist;
        
        for (var i = 0; i <= endCells; i++)
        {
            var t = i / dist;
            SetSurfaceCell(Vector3.Lerp(start, end, t));
        }
    }

    bool Consume(CellType consumeType, int endIndex, ref int cell)
    {
        for (var i = cell; i < endIndex; i++)
        {
            var type = grid.GetCellType(i);
            if (type != consumeType)
            {
                cell = i;
                return false;
            }
        }
        return true;
    }

    //void FillRow( int cell, int endIndex)
    //{
    //    if (Consume(CellType.Undefined, endIndex, ref cell)) return;
    //    if (Consume(CellType.Surface, endIndex, ref cell)) return;
    //    var startIndex = cell;
    //    if (Consume(CellType.Undefined, endIndex, ref cell)) return;

    //    for (var i=startIndex; i<cell; i++)
    //    {
    //        grid.SetCellType(i, CellType.In);
    //    }

    //    FillRow(cell, endIndex);
    //}
    
    //public void FillOrig()
    //{ 
    //    // process in line paths
    //    var rowSize = grid.xMax;
    //    var cell = 0;
    //    var rowCount = grid.yMax * grid.zMax;
        
    //    for (var j=0; j<rowCount; j++)
    //    { 
    //        FillRow( cell, cell + rowSize);
    //        cell += rowSize;
    //    }
       
    //}


    void CheckFillCell( Stack<int> cellsToProcess, int cell) {
        // check cell range 
        if (cell >= 0 && cell < grid.cellCount && grid.GetCellType(cell) == CellType.Undefined)
        {
            cellsToProcess.Push(cell);
        }
    }

    public void Fill()
    {
        var cellsToProcess = new Stack<int>();
        cellsToProcess.Push(0); 

        while (cellsToProcess.Count > 0)
        {
            var cell = cellsToProcess.Pop(); 
            if (grid.GetCellType(cell) != CellType.Undefined)
            {
                continue;
            }
            grid.SetCellType(cell, CellType.Out);

            CheckFillCell(cellsToProcess, cell + grid.xInc);
            CheckFillCell(cellsToProcess, cell - grid.xInc);
            CheckFillCell(cellsToProcess, cell + grid.yInc);
            CheckFillCell(cellsToProcess, cell - grid.yInc);
            CheckFillCell(cellsToProcess, cell + grid.zInc);
            CheckFillCell(cellsToProcess, cell - grid.zInc);
        }
        
        for ( var i = 0; i < grid.cellCount; i++)
        {
            if ( grid.GetCellType(i) == CellType.Undefined)
            {
                grid.SetCellType(i, CellType.In);
            }
        } 
    }

}
