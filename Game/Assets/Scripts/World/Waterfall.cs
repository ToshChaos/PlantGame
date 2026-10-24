using UnityEngine;

namespace PlantGame.World
{
    /// <summary>Water pouring over an island's rim: a shallow channel on the top and a sheet of falling droplets that drops far below the island. Decorative.</summary>
    public static class Waterfall
    {
        static Material _fx;

        /// <summary>rim = the point on the island's edge (top surface height), outward = horizontal direction away from the island.</summary>
        public static void Build(Transform parent, Vector3 rim, Vector3 outward, float width = 2.6f, bool frozen = false)
        {
            outward.y = 0f; outward.Normalize();
            var root = new GameObject("Waterfall"); root.transform.SetParent(parent, true); root.transform.position = rim;
            if (!frozen) { var wr = root.AddComponent<Audio.LoopEmitter>(); wr.clipName = "waterfall_rush"; wr.volume = 0.55f; wr.minDistance = 4f; wr.maxDistance = 34f; }   // T43: a waterfall roars
            root.transform.rotation = Quaternion.LookRotation(outward);

            // the stream on the island is carved into the terrain by the generator (same water as the ponds); here: foam and the sheet
            var foamSpots = new GameObject("LipFoam"); foamSpots.transform.SetParent(root.transform, false); foamSpots.transform.localPosition = new Vector3(0f, 0.08f, 0.05f);
            if (!frozen) Foam(foamSpots, width);

            // the sheet of streaming water over the edge
            var sheetMat = frozen ? Plants.WaterSource.IceMat() : Kit.Mat("M_WaterfallSheet");   // a frozen fall hangs as a still sheet of ice
            if (sheetMat != null)
            {
                var sheet = new GameObject("Sheet"); sheet.transform.SetParent(root.transform, false); sheet.transform.localPosition = new Vector3(0f, -0.2f, 0.25f);
                sheet.AddComponent<MeshFilter>().sharedMesh = SheetMesh(width * 1.05f, 14f);
                sheet.AddComponent<MeshRenderer>().sharedMaterial = sheetMat;
                sheet.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            if (frozen) return;

            // the fall: stretched droplets under gravity, fading out far below
            var go = new GameObject("Fall"); go.transform.SetParent(root.transform, false); go.transform.localPosition = new Vector3(0f, -0.1f, 0.35f);
            go.transform.localRotation = Quaternion.identity;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(2.4f, 3.2f); main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.6f); main.gravityModifier = 1.0f; main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.9f, 1f, 0.6f), new Color(0.95f, 0.98f, 1f, 0.7f));
            var em = ps.emission; em.rateOverTime = 70f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(width * 0.9f, 0.1f, 0.2f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 1.5f; r.velocityScale = 0.05f; r.sharedMaterial = FxMaterial();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();
        }

        /// <summary>Vertical ribbon hanging from the lip: uv.x across, uv.y 0 at the lip .. 1 at the bottom; follows a gentle outward arc and fans out.</summary>
        static Mesh SheetMesh(float width, float length)
        {
            const int rows = 16, cols = 8;
            var v = new System.Collections.Generic.List<Vector3>(); var uv = new System.Collections.Generic.List<Vector2>(); var t = new System.Collections.Generic.List<int>();
            for (int r = 0; r <= rows; r++)
            {
                float k = r / (float)rows, d = k * length, w = width * (1f + 0.35f * k);
                for (int c = 0; c <= cols; c++)
                {
                    float u = c / (float)cols;
                    v.Add(new Vector3((u - 0.5f) * w, -d, 0.5f * Mathf.Sqrt(d) * 0.6f + 0.02f * d)); uv.Add(new Vector2(u, k));
                }
            }
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++) { int a = r * (cols + 1) + c, b = a + 1, d = a + cols + 1, e = d + 1; t.AddRange(new[] { a, d, b, b, d, e }); }
            var m = new Mesh { name = "WaterfallSheet" }; m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateBounds();
            return m;
        }

        /// <summary>White foam puffs churning along the lip.</summary>
        static void Foam(GameObject go, float width)
        {
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.8f); main.gravityModifier = 0.15f; main.maxParticles = 120;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.55f), new Color(0.85f, 0.95f, 1f, 0.35f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission; em.rateOverTime = 40f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(width * 0.9f, 0.05f, 0.5f); sh.rotation = new Vector3(-60f, 0f, 0f);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = FxMaterial(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();
        }

        static Material FxMaterial()
        {
            if (_fx != null) return _fx;
            return _fx = Core.RuntimeMats.NewParticles("M_WaterfallFx", Color.white);
        }
    }
}
