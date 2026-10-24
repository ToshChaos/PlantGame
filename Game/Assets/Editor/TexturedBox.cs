using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Editor
{
    /// <summary>
    /// A box mesh with world-scale UVs (one texture tile per `tile` metres) so tiled materials do not stretch on big slabs.
    /// Submesh 0 = top face (topMat), submesh 1 = sides + bottom (sideMat). Has a BoxCollider, layer Ground. The mesh is kept in the scene.
    /// </summary>
    public static class TexturedBox
    {
        public static GameObject Create(string name, Vector3 pos, Vector3 size, Material topMat, Material sideMat, float tile = 3f)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var h = size * 0.5f;
            var verts = new System.Collections.Generic.List<Vector3>();
            var norms = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var top = new System.Collections.Generic.List<int>();
            var side = new System.Collections.Generic.List<int>();

            void Face(Vector3 n, Vector3 right, Vector3 up, float w, float hgt, System.Collections.Generic.List<int> tris)
            {
                var c = Vector3.Scale(n, h);
                var r = right * (w * 0.5f); var u = up * (hgt * 0.5f);
                int i = verts.Count;
                verts.Add(c - r - u); verts.Add(c + r - u); verts.Add(c + r + u); verts.Add(c - r + u);
                for (int k = 0; k < 4; k++) norms.Add(n);
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(w / tile, 0)); uvs.Add(new Vector2(w / tile, hgt / tile)); uvs.Add(new Vector2(0, hgt / tile));
                tris.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }
            // winding checked against the normal below; flip if needed
            Face(Vector3.up, Vector3.right, Vector3.forward, size.x, size.z, top);
            Face(Vector3.down, Vector3.right, Vector3.back, size.x, size.z, side);
            Face(Vector3.forward, Vector3.left, Vector3.up, size.x, size.y, side);
            Face(Vector3.back, Vector3.right, Vector3.up, size.x, size.y, side);
            Face(Vector3.right, Vector3.forward, Vector3.up, size.z, size.y, side);
            Face(Vector3.left, Vector3.back, Vector3.up, size.z, size.y, side);

            var mesh = new Mesh { name = name + "_Mesh" };
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(top, 0); mesh.SetTriangles(side, 1);
            // make every triangle face along its stored normal
            Fix(mesh, 0); Fix(mesh, 1);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { topMat, sideMat != null ? sideMat : topMat };
            go.AddComponent<BoxCollider>().size = size;
            int ground = LayerMask.NameToLayer(Layers.GroundName);
            if (ground >= 0) go.layer = ground;
            return go;
        }

        static void Fix(Mesh m, int sub)
        {
            var t = m.GetTriangles(sub); var v = m.vertices; var n = m.normals;
            for (int i = 0; i < t.Length; i += 3)
            {
                var cross = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                if (Vector3.Dot(cross, n[t[i]]) < 0f) { (t[i + 1], t[i + 2]) = (t[i + 2], t[i + 1]); }
            }
            m.SetTriangles(t, sub);
        }
    }
}
