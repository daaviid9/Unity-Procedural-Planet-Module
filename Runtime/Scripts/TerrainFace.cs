using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ProceduralPlanet {
    public class TerrainFace {
        ShapeGenerator shapeGenerator;
        Mesh mesh;
        int resolution;
        Vector3 localup;
        Vector3 axisA;
        Vector3 axisB;

        public Vector3 localup_for_lod => localup;

        // Tieto polia budeme držať v pamäti pre Joby
        NativeArray<Vector3> verticesNative;
        NativeArray<Vector3> outVerticesNative;
        NativeArray<Vector2> outUVsNative;

        public int resolution_property => resolution;

        public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localup) {
            this.shapeGenerator = shapeGenerator;
            this.mesh = mesh;
            this.resolution = resolution;
            this.localup = localup;

            axisA = new Vector3(localup.y, localup.z, localup.x);
            axisB = Vector3.Cross(localup, axisA);
        }

        // Musíme uvoľniť pamäť, keď sa objekt zničí
        public void Release() {
            if (verticesNative.IsCreated) verticesNative.Dispose();
            if (outVerticesNative.IsCreated) outVerticesNative.Dispose();
            if (outUVsNative.IsCreated) outUVsNative.Dispose();
        }

        public void UpdateResolution(int resolution) {
            this.resolution = resolution;
        }

        public void ConstructMesh(NativeArray<NoiseLayerStruct> shapeLayers, NativeArray<BiomeStruct> biomes, NoiseLayerStruct tempNoiseSettings, float blendAmount, float planetRadius) {
            int vertexCount = resolution * resolution;
            int triangleCount = (resolution - 1) * (resolution - 1) * 6;

            // 1. Pripravíme NativeArray (ak sa zmenilo rozlíšenie, musíme ich vytvoriť znova)
            PrepareNativeArrays(vertexCount);

            // 2. Naplníme základné body na sfére (tento malý loop môžeme nechať tu alebo dať do Jobu)
            for (int y = 0; y < resolution; y++) {
                for (int x = 0; x < resolution; x++) {
                    int i = x + y * resolution;
                    Vector2 percent = new Vector2(x, y) / (resolution - 1);
                    Vector3 pointOnUnitCube = localup + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                    verticesNative[i] = pointOnUnitCube.normalized;
                }
            }

            // 3. Konfigurácia a spustenie Jobu
            TerrainJob job = new TerrainJob {
                vertices = verticesNative,
                shapeLayers = shapeLayers,
                biomes = biomes,
                planetRadius = planetRadius,
                blendAmount = blendAmount,
                temperatureNoiseSettings = tempNoiseSettings,
                outVertices = outVerticesNative,
                outUVs = outUVsNative
            };

            // Spustíme Job a POČKÁME na dokončenie (Schedule a potom Complete)
            JobHandle handle = job.Schedule(vertexCount, 64);
            handle.Complete();

            // 4. Update meshu z výsledkov Jobu
            Vector3[] vertices = new Vector3[vertexCount];
            int[] triangles = new int[triangleCount];
            outVerticesNative.CopyTo(vertices);

            // Výpočet trojuholníkov (zostáva rovnaký, je to rýchle)
            int triIndex = 0;
            for (int y = 0; y < resolution - 1; y++) {
                for (int x = 0; x < resolution - 1; x++) {
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

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            
            // Rýchly výpočet normál (ten čo sme robili predtým)
            Vector3[] normals = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++) normals[i] = vertices[i].normalized;
            mesh.normals = normals;

            // Uložíme si min/max výšku pre shader
            UpdateMinMaxHeight();
        }

        void PrepareNativeArrays(int vertexCount) {
            if (!outVerticesNative.IsCreated || outVerticesNative.Length != vertexCount) {
                Release();
                verticesNative = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
                outVerticesNative = new NativeArray<Vector3>(vertexCount, Allocator.Persistent);
                outUVsNative = new NativeArray<Vector2>(vertexCount, Allocator.Persistent);
            }
        }

        void UpdateMinMaxHeight() {
            foreach (var uv in outUVsNative) {
                float unscaledElevation = uv.x;
                shapeGenerator.minElevationHeight = Mathf.Min(shapeGenerator.minElevationHeight, unscaledElevation);
                shapeGenerator.maxElevationHeight = Mathf.Max(shapeGenerator.maxElevationHeight, unscaledElevation);
            }
        }

        public void UpdateUVs(float min, float max) {
            Vector2[] uvs = new Vector2[outUVsNative.Length];
            for (int i = 0; i < outUVsNative.Length; i++) {
                float unscaledElevation = outUVsNative[i].x;
                float biomePercent = outUVsNative[i].y;
                float heightPercent = Mathf.Clamp01((unscaledElevation - min) / (max - min));
                uvs[i] = new Vector2(heightPercent, biomePercent);
            }
            mesh.uv = uvs;
        }
    }
}
