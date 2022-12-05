using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// https://toqoz.fyi/thousands-of-meshes.html
public class PointRenderer : MonoBehaviour
{
    public Material material;

    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;

    public Mesh mesh;

    [HideInInspector]
    public List<Vector3> pos = new List<Vector3>();

    [HideInInspector]
    public List<float> weight = new List<float>();

    public float cellSize = 0.5f;
    bool beginCalled;

    public void Begin()
    {
        beginCalled = true;
        pos.Clear();
        weight.Clear();
    }

    public void Add(Vector3 pos, float weight)
    {
        this.pos.Add(pos);
        this.weight.Add(weight);
    }

    public void End()
    {
        Draw(pos, weight, cellSize);// 25);
    }

    // Mesh Properties struct to be read from the GPU.
    // Size() is a convenience funciton which returns the stride of the struct.
    private struct MeshProperties
    {
        public Matrix4x4 mat;
        public Vector4 color;

        public static int Size()
        {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4;      // color;
        }
    }

    public void Draw(List<Vector3> pos, List<float> w, float scale)
    {
        Dispose();
        int count = pos.Count;
        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)count;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        Vector3 vScale = new Vector3(scale, scale, scale);
        // Initialize buffer with the given population.
        MeshProperties[] properties = new MeshProperties[count];
        for (int i = 0; i < count; i++)
        {
            MeshProperties props = new MeshProperties();
            Vector3 position = pos[i];
            Quaternion rotation = Quaternion.identity;
           // Vector3 scale = Vector3.one;

            props.mat = Matrix4x4.TRS(position, rotation, vScale);
            props.color = new Color(1, w[i], 0, 1);

            properties[i] = props;
        }

        meshPropertiesBuffer = new ComputeBuffer(count, MeshProperties.Size());
        meshPropertiesBuffer.SetData(properties);
        material.SetBuffer("_Properties", meshPropertiesBuffer);
    }
     
    private void Update()
    {
        if (beginCalled == false)
        {
            return;
        }
        var bounds = new Bounds(transform.position, Vector3.one * 1000);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer);
    }

    private void Awake()
    {
        //Draw(new List<Vector3>(new Vector3[] { Vector3.zero}), new List<float>(new float[] { 1}), 0.1f);
    }

    void Dispose()
    {
        // Release gracefully.
        if (meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if (argsBuffer != null)
        {
            argsBuffer.Release();
        }
        argsBuffer = null;
    }
    private void OnDisable()
    {
        Dispose();
    }
}
