using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class BlurGPU 
{
    ComputeShader cs;
    ComputeBuffer fillIndices;
    ComputeBuffer cellWeights;
    HarmonicGrid grid;
    int kernel;

    public void Setup(HarmonicGrid grid)
    {
        this.grid = grid;
        cs = Resources.Load<ComputeShader>("Blur3D");
        fillIndices = new ComputeBuffer(grid.fillCellIndices.Count, Marshal.SizeOf(typeof(int)));
        cellWeights = new ComputeBuffer(grid.cellCount, Marshal.SizeOf(typeof(float)));

        kernel = cs.FindKernel("CSMain"); 
        cs.SetBuffer(kernel, "fillIndices", fillIndices);
        cs.SetBuffer(kernel, "cellWeights", cellWeights);
        fillIndices.SetData(grid.fillCellIndices.ToArray());

        cs.SetInt("fillCount", grid.fillCellIndices.Count);
        cs.SetInt("xSpacing", grid.xInc);
        cs.SetInt("ySpacing", grid.yInc);
        cs.SetInt("zSpacing", grid.zInc);
    }

    public void Eval(int iterations)
    {

        cellWeights.SetData(grid.cellWeight);
        for (var i = 0; i < iterations; i++)
        {

            cs.Dispatch(kernel, grid.fillCellIndices.Count / 64 + 1, 1, 1);

        }
        cellWeights.GetData(grid.cellWeight);



        //for (var i = 0; i < iterations; i++)
        //{
        //    cellWeights.SetData(grid.cellWeight);
        //    cs.SetBuffer(kernel, "fillIndices", fillIndices);
        //    cs.SetBuffer(kernel, "cellWeights", cellWeights);
        //    cs.Dispatch(kernel, grid.fillCellIndices.Count / 64 + 1, 1, 1);
        //    cellWeights.GetData(grid.cellWeight);
        //}
      
    }
    
    public void Dispose()
    {
        fillIndices.Dispose();
        cellWeights.Dispose(); 
    }
}
