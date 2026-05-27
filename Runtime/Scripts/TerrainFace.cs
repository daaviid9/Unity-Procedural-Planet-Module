using UnityEngine;
using Unity.Collections;
using Unity.Jobs;

namespace ProceduralPlanet
{
    public class TerrainFace
    {
        private readonly ShapeGenerator shapeGenerator;
        private readonly Mesh mesh;
        private readonly Vector3 localup;
        private readonly Vector3 axisA;
        private readonly Vector3 axisB;

        private int resolution;
        private NativeArray<Vector3> verticesNative;
        private NativeArray<Vector3> outVerticesNative;
        private NativeArray<Vector2> outUVsNative;
        private Vector3[] vertices;
        private Vector3[] normals;
        private Vector2[] uvs;
        private int[] triangles;

        public Vector3 localup_for_lod => localup;
        public int resolution_property => resolution;

        public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localup)
        {
            this.shapeGenerator = shapeGenerator;
            this.mesh = mesh;
            this.resolution = resolution;
            this.localup = localup;

            axisA = new Vector3(localup.y, localup.z, localup.x);
            axisB = Vector3.Cross(localup, axisA);

            if (this.mesh != null)
            {
                this.mesh.MarkDynamic();
            }
        }

        public void Release()
        {
            if (verticesNative.IsCreated) verticesNative.Dispose();
            if (outVerticesNative.IsCreated) outVerticesNative.Dispose();
            if (outUVsNative.IsCreated) outUVsNative.Dispose();
        }

        public void UpdateResolution(int resolution)
        {
            this.resolution = resolution;
        }

        public void ConstructMesh(NativeArray<NoiseLayerStruct> shapeLayers, NativeArray<BiomeStruct> biomes, NoiseLayerStruct tempNoiseSettings, float blendAmount, float planetRadius)
        {
            int vertexCount = resolution * resolution;
            int triangleCount = (resolution - 1) * (resolution - 1) * 6;

            PrepareNativeArrays(vertexCount);
            PrepareManagedBuffers(vertexCount, triangleCount);
            FillUnitSpherePoints();

            TerrainJob job = new TerrainJob
            {
                vertices = verticesNative,
                shapeLayers = shapeLayers,
                biomes = biomes,
                planetRadius = planetRadius,
                blendAmount = blendAmount,
                temperatureNoiseSettings = tempNoiseSettings,
                outVertices = outVerticesNative,
                outUVs = outUVsNative
            };

            JobHandle handle = job.Schedule(vertexCount, 64);
            handle.Complete();

            outVerticesNative.CopyTo(vertices);
            UpdateNormals();

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;

            UpdateMinMaxHeight();
        }

        private void PrepareNativeArrays(int vertexCount)
        {
            if (!outVerticesNative.IsCreated || outVerticesNative.Length != vertexCount)
            {
                Release();
                verticesNative = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
                outVerticesNative = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
                outUVsNative = new NativeArray<Vector2>(vertexCount, Allocator.Persistent);
            }
        }

        private void PrepareManagedBuffers(int vertexCount, int triangleCount)
        {
            if (vertices == null || vertices.Length != vertexCount)
            {
                vertices = new Vector3[vertexCount];
                normals = new Vector3[vertexCount];
                uvs = new Vector2[vertexCount];
            }

            if (triangles == null || triangles.Length != triangleCount)
            {
                triangles = new int[triangleCount];
                BuildTriangles();
            }
        }

        private void FillUnitSpherePoints()
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = x + y * resolution;
                    Vector2 percent = new Vector2(x, y) / (resolution - 1);
                    Vector3 pointOnUnitCube = localup + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                    verticesNative[i] = pointOnUnitCube.normalized;
                }
            }
        }

        private void BuildTriangles()
        {
            int triIndex = 0;
            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int i = x + y * resolution;
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + resolution + 1;
                    triangles[triIndex + 2] = i + resolution;
                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }

        private void UpdateNormals()
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                normals[i] = vertices[i].normalized;
            }
        }

        private void UpdateMinMaxHeight()
        {
            foreach (Vector2 uv in outUVsNative)
            {
                float unscaledElevation = uv.x;
                shapeGenerator.minElevationHeight = Mathf.Min(shapeGenerator.minElevationHeight, unscaledElevation);
                shapeGenerator.maxElevationHeight = Mathf.Max(shapeGenerator.maxElevationHeight, unscaledElevation);
            }
        }

        public void UpdateUVs(float min, float max)
        {
            if (!outUVsNative.IsCreated)
            {
                return;
            }

            if (uvs == null || uvs.Length != outUVsNative.Length)
            {
                uvs = new Vector2[outUVsNative.Length];
            }

            float heightRange = max - min;
            if (Mathf.Approximately(heightRange, 0f))
            {
                heightRange = 1f;
            }

            for (int i = 0; i < outUVsNative.Length; i++)
            {
                float unscaledElevation = outUVsNative[i].x;
                float biomePercent = outUVsNative[i].y;
                float heightPercent = Mathf.Clamp01((unscaledElevation - min) / heightRange);
                uvs[i] = new Vector2(heightPercent, biomePercent);
            }

            mesh.uv = uvs;
        }
    }
}
