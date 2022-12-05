using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class CellType
//{
//    public const byte Empty = 0;
//    public const byte Surface = 1;
//    public const byte In = 2;
//    public const byte Tri = 3; // placeholder
//}

public enum CellType
{
    Undefined = 0,
    Surface, 
    In,
    Tri,
    Out
}

// https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
//The following implementation uses the Fisher-Yates algorithm AKA the Knuth Shuffle.It runs in O(n) time and shuffles in place, so is better performing than the 'sort by random' technique, although it is more lines of code. See here for some comparative performance measurements. I have used System.Random, which is fine for non-cryptographic purposes.*
static class RandomExtensions
{
    public static void Shuffle<T>(this System.Random rng, T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }
}

public class HarmonicGrid
{
    public int xMax;
    public int yMax;
    public int zMax;
    public int cellCount;
    public CellType[] cellType; // 0 == empty (default), 1 == surface, 2 = fill
    public float[] cellWeight; // default to 0, see CellType


    public List<int>[] triangleCells; // index = triangle list

    public List<int> surfaceCellIndices;


    // all fill cells have 8 neighbours to sum on processing (never overrides surface)
    public List<int> fillCellIndices;
    //public int[] fillCellIndicesArr;


    //System.Random rng = new System.Random();
    //public void RandomizeFillCellIndices()
    //{
    //    rng.Shuffle(fillCellIndicesArr);
    //}


    //// neighbour lists, index in list has no meaning, just sum all values in sub cell list (NOTE: includes self)
    //// no such thing a neighbour1
    //public List<int> neighbour1 = new List<int>();

    public int GetCellIndex(Vector3 pos)
    {
        return GetCellIndex((int)pos.x, (int)pos.y, (int)pos.z);
    }

        public int GetCellIndex(int x, int y, int z)
    {
        x = x < 0 ? 0 : (x >= xMax ? xMax - 1 : x);
        y = y < 0 ? 0 : (y >= yMax ? yMax - 1 : y);
        z = z < 0 ? 0 : (z >= zMax ? zMax - 1 : z);

        // https://stackoverflow.com/questions/7367770/how-to-flatten-or-index-3d-array-in-1d-array
        return (z * xMax * yMax) + (y * xMax) + x; // x + xDim * (y + yDim * z); 
    }

    // cell pos is not from the center point
    public Vector3Int GetCellPos(int cellIndex)
    {
        var z = cellIndex / (xMax * yMax);
        cellIndex -= (z * xMax * yMax);
        var y = cellIndex / xMax;
        var x = cellIndex % xMax;
        return new Vector3Int(x, y, z);
    }

    public Vector3 GetCellCenterPos(int cellIndex)
    {
        var z = cellIndex / (xMax * yMax);
        cellIndex -= (z * xMax * yMax);
        var y = cellIndex / xMax;
        var x = cellIndex % xMax;
        return new Vector3(x+ 0.5f, y+ 0.5f, z+0.5f); 
    }

    public HarmonicGrid(int xDim, int yDim, int zDim, int triCount)
    {
 
        this.xMax = xDim;
        this.yMax = yDim;
        this.zMax = zDim;

        cellCount = xDim * yDim * zDim;
        cellType = new CellType[cellCount];
        cellWeight = new float[cellCount];
        surfaceCellIndices = new List<int>(cellCount);
        fillCellIndices = new List<int>(cellCount);

        triangleCells = new List<int>[triCount];

        var center = GetCellIndex(1, 1, 1);
         xInc = GetCellIndex(2, 1, 1) - center;
         yInc = GetCellIndex(1, 2, 1) - center;
        zInc = GetCellIndex(1, 1, 2) - center;
    }

    // number of cells for the next cord e.g x = 1, y = 12, z ~= 144
    public int xInc;
    public int yInc;
    public int zInc;

    //public void SetWeight(int x, int y, int z, float weight)
    //{
    //    SetWeight(GetCellIndex(x,y,z), weight);
    //}

    public void SetWeight(int cellIndex, float weight)
    {
        cellWeight[cellIndex] = weight;
    }


    public bool IsCellValid(int cell)
    {
        return cell >= 0 && cell < cellCount;
    }

    public float CalcFillWeight()
    {
        var sum = 0.0f;
        for (var i=0; i<fillCellIndices.Count; i++)
        {
            sum += GetWeight(i);
        }
        return sum;/// fillCellIndices.Count;
    }

    public float GetWeight(int cellIndex)
    {
        return cellWeight[cellIndex];
    }

    public float GetWeight(int x, int y, int z)
    {
        var cell = GetCellIndex(x, y, z);
        return GetWeight(cell);
    }

    //public void SetCellType(int x, int y, int z, byte type)
    //{
    //    SetWeight(GetCellIndex(x, y, z), type);
    //}

    public void SetCellType(int cellIndex, CellType type) {
      
        cellType[cellIndex] = type;
    }

    public CellType GetCellType(int cellIndex)
    { 
        return cellType[cellIndex];
    }

    // https://en.wikipedia.org/wiki/Trilinear_interpolation
    public float TriLinear(
    float c000, float c100,
    float c010, float c110, 
    float c001, float c101,
    float c011, float c111,

    float xd, float yd, float zd)
    {
        var c00 = c000 * (1.0f - xd) + c100 * xd;
        var c01 = c001 * (1.0f - xd) + c101 * xd;
        var c10 = c010 * (1.0f - xd) + c110 * xd;
        var c11 = c011 * (1.0f - xd) + c111 * xd;

        var c0 = c00 * (1.0f - yd) + c10 * yd;
        var c1 = c01 * (1.0f - yd) + c11 * yd;

        return c0 * (1 - zd) + c1 * zd;
    }

    public float CalcHarmonicWeight(Vector3 pos)
    {
        // Note: pos -= 0.5f for cente position? appears correct in editor
        pos -= new Vector3(0.5f, 0.5f, 0.5f);
        var cell = GetCellIndex(pos);
        var iCellMin = GetCellPos(cell);
        var x = iCellMin.x;
        var y = iCellMin.y;
        var z = iCellMin.z;

        var remander = new Vector3(pos.x - iCellMin.x, pos.y - iCellMin.y, pos.z - iCellMin.z);
        // TODO: Trilinear interpolation

        return TriLinear(
            GetWeight(x, y, z), GetWeight(x + 1, y, z),
            GetWeight(x, y + 1, z), GetWeight(x + 1, y + 1, z),

            GetWeight(x, y, z + 1), GetWeight(x + 1, y, z + 1),
            GetWeight(x, y + 1, z + 1), GetWeight(x + 1, y + 1, z + 1),

            remander.x, remander.y, remander.z
        );

      //  return GetWeight(cell);
    }

}
