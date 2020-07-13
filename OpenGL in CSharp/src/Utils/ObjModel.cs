﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Assimp;
using Microsoft.SqlServer.Server;
using OpenTK;

namespace OpenGL_in_CSharp
{
    /// <summary>
    /// Simple class to keep all relevant data related to an .obj 3D model
    /// </summary>
    public class ObjModel
    {
        public List<Vector3> Vertices = new List<Vector3>();
        public List<Vector2> TextureCoordinates = new List<Vector2>();
        public List<Vector3> Normals = new List<Vector3>();

        public float MaxX { set; get; }
        public float MaxY { set; get; }
        public float MaxZ { set; get; }

        public float MinX { set; get; }
        public float MinY { set; get; }
        public float MinZ { set; get; }

        public List<uint> Indices { set; get; } = new List<uint>();

        public float[] VerticesFloat;
        public float[] TextureCoordinatesFloat;
        public float[] NormalsFloat;
        public float[] Tangents;
        public float[] BiTangents;

        public ObjModel() { }

        public static ObjModel LoadWithTangents(string objFile)
        {
            Scene scene = new AssimpContext().ImportFile(objFile, PostProcessSteps.GenerateSmoothNormals
                | PostProcessSteps.CalculateTangentSpace
                | PostProcessSteps.Triangulate);

            if (!scene.HasMeshes)
            {
                throw new MissingFieldException("No meshes found!");
            }
            // this whole class is only made for single mesh files
            if (scene.MeshCount > 1)
            {
                throw new MissingFieldException("Found multiple meshes!");
            }

            var model = InitFromScene(scene);
            model.DetermineBorders(scene.Meshes[0].Vertices);

            return model;
        }

        public static async Task<ObjModel> LoadWithTangentsAsync(string objFile)
        {
            Scene scene = await Task.Run(() => new AssimpContext().ImportFile(objFile, PostProcessSteps.GenerateSmoothNormals
                | PostProcessSteps.CalculateTangentSpace
                | PostProcessSteps.Triangulate
                ));

            if (!scene.HasMeshes)
            {
                throw new MissingFieldException("No meshes found!");
            }
            // this whole class is only made for single mesh files
            if (scene.MeshCount > 1)
            {
                throw new MissingFieldException("Found multiple meshes!");
            }

            var model = InitFromScene(scene);
            model.DetermineBorders(scene.Meshes[0].Vertices);

            return model;
        }

        private static ObjModel InitFromScene(Scene scene)
        {
            ObjModel model = new ObjModel
            {
                VerticesFloat = new float[scene.Meshes[0].VertexCount * 3],
                TextureCoordinatesFloat = new float[scene.Meshes[0].VertexCount * 2],
                NormalsFloat = new float[scene.Meshes[0].VertexCount * 3],
                Tangents = new float[scene.Meshes[0].Tangents.Count * 3],
                BiTangents = new float[scene.Meshes[0].BiTangents.Count * 3]
            };

            // fills in the arrays
            for (int i = 0; i < scene.Meshes[0].VertexCount; ++i)
            {
                model.VerticesFloat[3 * i] = scene.Meshes[0].Vertices[i].X;
                model.VerticesFloat[3 * i + 1] = scene.Meshes[0].Vertices[i].Y;
                model.VerticesFloat[3 * i + 2] = scene.Meshes[0].Vertices[i].Z;

                model.NormalsFloat[3 * i] = scene.Meshes[0].Normals[i].X;
                model.NormalsFloat[3 * i + 1] = scene.Meshes[0].Normals[i].Y;
                model.NormalsFloat[3 * i + 2] = scene.Meshes[0].Normals[i].Z;

                model.TextureCoordinatesFloat[2 * i] = scene.Meshes[0].TextureCoordinateChannels[0][i].X;
                model.TextureCoordinatesFloat[2 * i + 1] = scene.Meshes[0].TextureCoordinateChannels[0][i].Y;

                model.Tangents[3 * i] = scene.Meshes[0].Tangents[i].X;
                model.Tangents[3 * i + 1] = scene.Meshes[0].Tangents[i].Y;
                model.Tangents[3 * i + 2] = scene.Meshes[0].Tangents[i].Z;

                model.BiTangents[3 * i] = scene.Meshes[0].BiTangents[i].X;
                model.BiTangents[3 * i + 1] = scene.Meshes[0].BiTangents[i].Y;
                model.BiTangents[3 * i + 2] = scene.Meshes[0].BiTangents[i].Z;
            }

            return model;
        }

        private void DetermineBorders(List<Vector3D> vertices)
        {
            foreach (var vec in vertices)
            {
                if (vec.X > MaxX)
                {
                    MaxX = vec.X;
                }
                if (vec.Y > MaxY)
                {
                    MaxY = vec.Y;
                }
                if (vec.Z > MaxZ)
                {
                    MaxZ = vec.Z;
                }
                ///
                if (vec.X < MinX)
                {
                    MinX = vec.X;
                }
                if (vec.Y < MinY)
                {
                    MinY = vec.Y;
                }
                if (vec.Z < MinZ)
                {
                    MinZ = vec.Z;
                }
            }
        }
    }

    public static class ObjParser
    {
        public static ObjModel ParseObjFile(string path)
        {
            bool parsedAllCoords = false;
            ObjModel result = new ObjModel();

            foreach (string line in File.ReadAllLines(path))
            {
                string[] parts = line.Split(' ');
                switch(parts[0])
                {
                    case "v":
                        ParseVector3(ref result.Vertices, ref parts);
                        
                        if (result.Vertices.Last().X > result.MaxX)
                        {
                                result.MaxX = result.Vertices.Last().X;
                        }
                        if ( result.Vertices.Last().Y > result.MaxY)
                        {
                            result.MaxY = result.Vertices.Last().Y;
                        }
                        if ( result.Vertices.Last().Z > result.MaxZ)
                        {
                            result.MaxZ = result.Vertices.Last().Z;
                        }
                        ///
                        if ( result.Vertices.Last().X < result.MinX)
                        {
                            result.MinX = result.Vertices.Last().X;
                        }
                        if (result.Vertices.Last().Y < result.MinY)
                        {
                            result.MinY = result.Vertices.Last().Y;
                        }
                        if ( result.Vertices.Last().Z < result.MinZ)
                        {
                            result.MinZ = result.Vertices.Last().Z;
                        }
                        break;
                    case "vt":
                        ParseTextureCoords(result, ref parts);
                        break;
                    case "vn":
                        ParseVector3(ref result.Normals, ref parts);
                        break;
                    case "f":
                        if (!parsedAllCoords)
                        {
                            result.VerticesFloat = new float[result.Vertices.Count * 3]; // 3 flaots for each
                            result.TextureCoordinatesFloat = new float[result.Vertices.Count * 2]; // 2 floats for each
                            result.NormalsFloat = new float[result.Vertices.Count * 3]; //3 for each
                            parsedAllCoords = true;
                        }
                        ParseFaces(result, ref parts);
                        break;
                }
            }

            return result;
        }

        private static void ParseVector3(ref List<Vector3> lst, ref string[] parts)
        {
            lst.Add(new Vector3(
                float.Parse(parts[1]),
                float.Parse(parts[2]),
                float.Parse(parts[3])
                ));
        }




        private static void ParseTextureCoords(ObjModel model, ref string[] parts)
        {
            model.TextureCoordinates.Add(new Vector2(
                float.Parse(parts[1]),
                float.Parse(parts[2])
                ));
        }


        /// <summary>
        /// The face states which vertices (and their textures, normals) form a certain geometry primitive
        /// The primitive is most often a triangle, this parser can only parse triangles so far
        /// 
        /// This is how a face of a triangle (3 vertices) looks like in an .obj file:
        /// "f v/t/n v/t/n v/t/n"
        /// v - vertex index, t - texture index, n - normal index
        /// </summary>
        private static void ParseFaces(ObjModel model, ref string[] parts)
        {
            for (int i = 1; i < 4; i++)
            {
                string[] indices = parts[i].Split('/');

                // Blender starts indexing from 1 (why though?)
                int vertIndex = int.Parse(indices[0]) - 1;
                int texIndex = int.Parse(indices[1]) - 1;
                int normalIndex = int.Parse(indices[2]) - 1;
                model.Indices.Add((uint)vertIndex);

                model.VerticesFloat[vertIndex * 3] = model.Vertices[vertIndex].X;
                model.VerticesFloat[vertIndex * 3 + 1] = model.Vertices[vertIndex].Y;
                model.VerticesFloat[vertIndex * 3 + 2] = model.Vertices[vertIndex].Z;

                model.TextureCoordinatesFloat[vertIndex * 2] = model.TextureCoordinates[texIndex].X;
                model.TextureCoordinatesFloat[vertIndex * 2 + 1] = model.TextureCoordinates[texIndex].Y;

                model.NormalsFloat[vertIndex * 3] = model.Normals[normalIndex].X;
                model.NormalsFloat[vertIndex * 3 + 1] = model.Normals[normalIndex].Y;
                model.NormalsFloat[vertIndex * 3 + 2] = model.Normals[normalIndex].Z;
            }


        }
    }
}
 