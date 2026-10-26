using UnityEngine;

namespace PlantGame.UI
{
    /// <summary>Procedural 64 px sprites (no art files): filled disc, ring, diamond outline. Cached.</summary>
    public static class UiSprites
    {
        const int N = 64;
        static Sprite _coconut, _disc, _ring, _drop, _leaf, _bamboo, _bean, _diamondThick, _diamondThin, _ringThin, _ringSlim, _puffball, _roundMushroom, _succulent, _heart, _melon;

        public static Sprite Disc => _disc != null ? _disc : _disc = Make((x, y) => Mathf.Clamp01(N * 0.5f - 1f - Dist(x, y)));
        public static Sprite Ring => _ring != null ? _ring : _ring = Make((x, y) =>
        {
            float d = Dist(x, y);
            return Mathf.Max(Mathf.Clamp01(1f - (Mathf.Abs(d - 24f) - 2.5f) / 2f), d < 24f ? 0.16f : 0f);   // bold outline + faint fill
        });
        /// <summary>Thick diamond: the "this is the right thing" marker (the thin one below is the faded one).</summary>
        public static Sprite DiamondThick => _diamondThick != null ? _diamondThick : _diamondThick = Make((x, y) =>
        {
            float d = (Mathf.Abs(x - (N - 1) * 0.5f) + Mathf.Abs(y - (N - 1) * 0.5f));
            return Mathf.Max(Mathf.Clamp01(1f - (Mathf.Abs(d - 25f) - 4.5f) / 2f), d < 25f ? 0.18f : 0f);
        });
        /// <summary>Hairline diamond (about 2 px) for pickups that are still far away.</summary>
        public static Sprite DiamondThin => _diamondThin != null ? _diamondThin : _diamondThin = Make((x, y) =>
        {
            float d = (Mathf.Abs(x - (N - 1) * 0.5f) + Mathf.Abs(y - (N - 1) * 0.5f));
            return Mathf.Clamp01(1.5f - Mathf.Abs(d - 27f));
        });
        /// <summary>Slim ring (about half the medium one) for the soil ring of the right seed.</summary>
        public static Sprite RingSlim => _ringSlim != null ? _ringSlim : _ringSlim = Make((x, y) =>
        {
            float d = Dist(x, y);
            return Mathf.Max(Mathf.Clamp01(1f - (Mathf.Abs(d - 24f) - 1.2f) / 1.5f), d < 24f ? 0.12f : 0f);
        });
        public static Sprite RingThin => _ringThin != null ? _ringThin : _ringThin = Make((x, y) => Mathf.Clamp01(1.5f - Mathf.Abs(Dist(x, y) - 25f)));

        /// <summary>Teardrop: pointed top, round belly (HUD water charges).</summary>
        public static Sprite Drop => _drop != null ? _drop : _drop = Make((x, y) =>
        {
            float u = (x - (N - 1) * 0.5f) / (N * 0.5f), v = (y - (N - 1) * 0.5f) / (N * 0.5f);   // -1..1, y up
            float belly = 0.56f - Mathf.Sqrt(u * u + (v + 0.28f) * (v + 0.28f));
            float t = Mathf.Clamp01((0.92f - v) / 1.05f);                                      // 0 at the tip, 1 at the belly line
            float wedge = (v >= -0.28f && v <= 0.92f) ? 0.56f * Mathf.Pow(t, 1.15f) - Mathf.Abs(u) : -1f;
            return Mathf.Clamp01(Mathf.Max(belly, wedge) * 14f);
        });

        /// <summary>Pointed leaf blade running bottom-left to top-right (hotbar icon of the parachute leaf).</summary>
        public static Sprite Leaf => _leaf != null ? _leaf : _leaf = Make((x, y) =>
        {
            float u = (x - (N - 1) * 0.5f) / (N * 0.5f), v = (y - (N - 1) * 0.5f) / (N * 0.5f);
            float a = (u + v) * 0.7071f, b = (v - u) * 0.7071f;              // along the blade, across it
            float t = (a + 0.96f) / 1.92f;
            if (t <= 0f || t >= 1f) return 0f;
            float hw = 0.40f * Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.85f)), 0.8f);
            return Mathf.Clamp01((hw - Mathf.Abs(b)) * 14f);
        });


        /// <summary>Hotbar icon of an item (T33). Null = no special sprite, callers fall back to Disc.</summary>
        public static Sprite ForItem(string id)
        {
            switch (id)
            {
                case "parachute_leaf": return Leaf;
                case "vine_seed": return Bamboo;
                case "beanstalk_seed": return Bean;
                case "shelf_seed": return Puffball;
                case "dew_seed": return Succulent;
                case "heart_seed": return Heart;
                case "heat_seed": return Melon;
                case "coconut": return Coconut;
                default: return null;
            }
        }

        /// <summary>Bamboo piece running bottom-left to top-right: two joints (thin dark gaps with a slight bulge).</summary>
        public static Sprite Bamboo => _bamboo != null ? _bamboo : _bamboo = MakeShaded((x, y) =>
        {
            float u = (x - (N - 1) * 0.5f) / (N * 0.5f), v = (y - (N - 1) * 0.5f) / (N * 0.5f);
            float a = (u + v) * 0.7071f, b = (v - u) * 0.7071f;
            if (Mathf.Abs(a) > 0.86f) return Vector2.zero;
            float near = Mathf.Min(Mathf.Abs(a + 0.29f), Mathf.Abs(a - 0.29f));
            float hw = 0.15f + 0.05f * Mathf.Clamp01(1f - near / 0.07f);
            float end = Mathf.Clamp01((0.86f - Mathf.Abs(a)) * 20f);
            float al = Mathf.Clamp01((hw - Mathf.Abs(b)) * 14f) * end;
            float shade = (near < 0.018f ? 0.55f : 1f) * (1f - 0.25f * Mathf.Clamp01(b / hw));   // joint rings darker, lower edge lit
            return new Vector2(al, shade);
        });

        /// <summary>Kidney bean: a bent oval with the notch on the inner side.</summary>
        public static Sprite Bean => _bean != null ? _bean : _bean = MakeShaded((x, y) =>
        {
            float u = (x - (N - 1) * 0.5f) / (N * 0.5f), v = (y - (N - 1) * 0.5f) / (N * 0.5f);
            float a = (u + v) * 0.7071f, b = (v - u) * 0.7071f;
            float b2 = b + 0.32f * a * a - 0.05f;
            float d = Mathf.Sqrt(a * a / (0.66f * 0.66f) + b2 * b2 / (0.36f * 0.36f));
            d = Mathf.Max(d, 1.35f - Vector2.Distance(new Vector2(a, b), new Vector2(0.02f, 0.5f)) / 0.2f);   // notch on the inner (upper) side
            float al = Mathf.Clamp01((1f - d) * 8f);
            float shade = 1f - 0.22f * Mathf.Clamp01(b2 / 0.36f + 0.2f);
            return new Vector2(al, shade);
        });

        /// <summary>Puffball: a round cap on a short, thick stalk, with a few darker dimples on the cap.</summary>
        public static Sprite Puffball => _puffball != null ? _puffball : _puffball = MakeShaded((x, y) =>
        {
            float u = (x - (N - 1) * 0.5f) / (N * 0.5f), v = (y - (N - 1) * 0.5f) / (N * 0.5f);
            float cap = Mathf.Clamp01((0.56f - Mathf.Sqrt(u * u + (v - 0.14f) * (v - 0.14f))) * 14f);
            float stalk = (v < 0.0f && v > -0.78f) ? Mathf.Clamp01((0.17f + 0.07f * Mathf.Clamp01(-v - 0.45f) * 4f - Mathf.Abs(u)) * 14f) * Mathf.Clamp01((v + 0.78f) * 14f) : 0f;
            float al = Mathf.Max(cap, stalk);
            float shade = 1f - 0.2f * Mathf.Clamp01((0.12f - v) * 1.2f) * (cap > 0.5f ? 1f : 0f);
            foreach (var s in new[] { new Vector2(-0.18f, 0.32f), new Vector2(0.14f, 0.4f), new Vector2(0.2f, 0.12f), new Vector2(-0.12f, 0.08f) })
                if (Vector2.Distance(new Vector2(u, v), s) < 0.055f) shade *= 0.72f;
            if (cap < 0.5f) shade = 0.85f;   // stalk a touch darker than the cap
            return new Vector2(al, shade);
        });

        /// <summary>Slider handle: a fat round mushroom (near-circular cap, short stalk, a few dimples).</summary>
        public static Sprite RoundMushroom => _roundMushroom != null ? _roundMushroom : _roundMushroom = MakeShaded((x, y) =>
        {
            float u = (x - (N - 1) * 0.5f) / (N * 0.5f), v = (y - (N - 1) * 0.5f) / (N * 0.5f);
            float cap = Mathf.Clamp01((0.7f - Mathf.Sqrt(u * u + (v - 0.18f) * (v - 0.18f))) * 14f);
            float stalk = (v < -0.3f && v > -0.85f) ? Mathf.Clamp01((0.2f - Mathf.Abs(u)) * 14f) * Mathf.Clamp01((v + 0.85f) * 14f) : 0f;
            float al = Mathf.Max(cap, stalk);
            float shade = cap > 0.5f ? 1f - 0.22f * Mathf.Clamp01((0.0f - v) * 1.2f) : 0.82f;
            foreach (var d in new[] { new Vector2(-0.25f, 0.42f), new Vector2(0.18f, 0.5f), new Vector2(0.3f, 0.12f), new Vector2(-0.1f, 0.15f) })
                if (Vector2.Distance(new Vector2(u, v), d) < 0.07f) shade *= 0.74f;
            return new Vector2(al, shade);
        });

        /// <summary>Succulent seen from above: two rings of fat pointed leaves around a bud, like a flower.</summary>
        public static Sprite Succulent => _succulent != null ? _succulent : _succulent = MakeShaded((x, y) =>
        {
            float u = (x - (N - 1) * 0.5f) / (N * 0.5f), v = (y - (N - 1) * 0.5f) / (N * 0.5f);
            float r = Mathf.Sqrt(u * u + v * v), th = Mathf.Atan2(v, u);
            float outer = 0.36f + 0.56f * Mathf.Pow(Mathf.Abs(Mathf.Cos(4f * th)), 0.55f);          // 8 fat leaves
            float inner = 0.2f + 0.36f * Mathf.Pow(Mathf.Abs(Mathf.Cos(3f * th + 0.6f)), 0.55f);    // 6 shorter ones on top
            float alOuter = Mathf.Clamp01((outer - r) * 12f), alInner = Mathf.Clamp01((inner - r) * 12f);
            float rim = Mathf.Clamp01((inner - r) * 12f) - Mathf.Clamp01((inner - r - 0.05f) * 12f);   // thin edge line round the inner leaves
            float shade = alInner > 0.5f ? (r < 0.12f ? 1f : 0.95f) - 0.2f * rim : 0.72f;
            return new Vector2(alOuter, shade);
        });

        /// <summary>Plump heart with a lit upper-left lobe (hotbar icon of the Heart Seed, tinted red).</summary>
        public static Sprite Heart => _heart != null ? _heart : _heart = MakeShaded((x, y) =>
        {
            float u = (x - (N - 1) * 0.5f) / (N * 0.5f) * 1.25f, v = (y - (N - 1) * 0.5f) / (N * 0.5f) * 1.25f + 0.1f;
            float f = Mathf.Pow(u * u + v * v - 1f, 3f) - u * u * v * v * v;   // implicit heart curve, inside < 0
            float al = Mathf.Clamp01(-f * 14f);
            float shade = 0.78f + 0.22f * Mathf.Clamp01(0.5f - (u * 0.5f - v * 0.5f) * 0.8f);
            return new Vector2(al, shade);
        });

        /// <summary>Golden melon: a round fruit with curved rib lines, a bright highlight and a short stem on top (Heat Seed).</summary>
        public static Sprite Melon => _melon != null ? _melon : _melon = MakeShaded((x, y) =>
        {
            float u = (x - (N - 1) * 0.5f) / (N * 0.5f), v = (y - (N - 1) * 0.5f) / (N * 0.5f) + 0.08f;
            const float R = 0.72f;
            float d = Mathf.Sqrt(u * u + v * v);
            float body = Mathf.Clamp01((R - d) * 10f);
            // stem: a short dark nub rising from the top
            float stem = Mathf.Abs(u - 0.02f * v) < 0.07f && v > R - 0.1f && v < R + 0.17f ? 1f : 0f;
            // leaf: a small blade leaning right from the stem base
            float lu = u - 0.16f, lv = v - (R + 0.02f);
            float leaf = (lu * lu) / 0.012f + (lv * lv) / 0.0025f < 1f && lu > -0.02f ? 1f : 0f;
            if (body <= 0f && stem <= 0f && leaf <= 0f) return Vector2.zero;
            if (body <= 0f) return new Vector2(1f, stem > 0f ? 0.45f : 0.7f);
            float phi = Mathf.Asin(Mathf.Clamp(u / R, -1f, 1f));
            float rib = Mathf.Abs(Mathf.Repeat(phi / 0.5236f + 0.5f, 1f) - 0.5f);   // meridian every 30 degrees
            float lines = rib < 0.06f ? 0.72f : 1f;
            float light = 0.82f + 0.2f * Mathf.Clamp01((-u * 0.6f + v * 0.8f) * 1.2f) - 0.12f * Mathf.Clamp01(d / R);   // lit from the upper left
            float hl = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(-0.25f, 0.28f)) / 0.14f) * 0.25f;
            return new Vector2(body, Mathf.Clamp01(light * lines + hl));
        });

        /// <summary>Coconut: a round hairy husk with a lit upper left, fibre streaks and the three dark "eyes" near the top.</summary>
        public static Sprite Coconut => _coconut != null ? _coconut : _coconut = MakeShaded((x, y) =>
        {
            float u = (x - (N - 1) * 0.5f) / (N * 0.5f), v = (y - (N - 1) * 0.5f) / (N * 0.5f);
            const float R = 0.7f;
            float d = Mathf.Sqrt(u * u + v * v);
            float body = Mathf.Clamp01((R - d) * 10f);
            if (body <= 0f) return Vector2.zero;
            float fibre = 0.5f + 0.5f * Mathf.Sin((u * 38f + v * 9f) + Mathf.Sin(v * 17f) * 2f);   // short hairy streaks
            float light = 0.78f + 0.25f * Mathf.Clamp01((-u * 0.6f + v * 0.8f) * 1.2f) - 0.15f * Mathf.Clamp01(d / R);
            light *= 0.86f + 0.14f * fibre;
            float eyes = 1f;
            Vector2[] e = { new Vector2(-0.14f, 0.3f), new Vector2(0.14f, 0.3f), new Vector2(0f, 0.14f) };
            foreach (var c in e) eyes = Mathf.Min(eyes, Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), c) / 0.075f) * 0.7f + 0.3f);
            float hl = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(-0.28f, 0.3f)) / 0.14f) * 0.18f;
            return new Vector2(body, Mathf.Clamp01(light * eyes + hl));
        });

        static float Dist(int x, int y) => Vector2.Distance(new Vector2(x, y), new Vector2((N - 1) * 0.5f, (N - 1) * 0.5f));

        /// <summary>(alpha, shade) per pixel: shade darkens the white so one tint still shows detail.</summary>
        static Sprite MakeShaded(System.Func<int, int, Vector2> f)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { var p = f(x, y); px[y * N + x] = new Color(p.y, p.y, p.y, p.x); }
            tex.SetPixels(px); tex.Apply();
            var s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }

        static Sprite Make(System.Func<int, int, float> alpha)
        {
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) px[y * N + x] = new Color(1, 1, 1, alpha(x, y));
            tex.SetPixels(px); tex.Apply();
            var s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N);   // 64 px = 1 world unit
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }
    }
}
