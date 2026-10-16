using UnityEngine;
using System.Collections.Generic;

// 考古生物的程序化占位模型。
// 网格名与正式资源 id 一致：trex_skull、triceratops_skull、stego_plate、ammonite、trilobite、mammoth_tusk。
// 之后把同名 FBX 和 {id}_base.png 放进 Assets/Resources，Load 会优先用正式模型，这些占位就不再出现。
public static class FossilPlaceholders {
  static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
  static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
  static bool logged;

  // 仅这 6 个关卡 id 会生成占位；其它主题缺资源时仍按原逻辑报错。
  public static bool TryCreate(string id, out Mesh mesh, out Texture2D albedo) {
    mesh = null; albedo = null;
    if (!IsFossilId(id)) return false;
    if (!meshCache.TryGetValue(id, out mesh)) {
      var b = new B();
      switch (id) {
        case "trex_skull": BuildTrex(b); break;
        case "triceratops_skull": BuildTriceratops(b); break;
        case "stego_plate": BuildStegoPlate(b); break;
        case "ammonite": BuildAmmonite(b); break;
        case "trilobite": BuildTrilobite(b); break;
        case "mammoth_tusk": BuildTusk(b); break;
      }
      mesh = b.ToMesh(id);
      meshCache[id] = mesh;
      if (!logged) {
        logged = true;
        Debug.Log("化石占位模型已生成。把同名 FBX 与 *_base.png 放进 Assets/Resources 即可替换。");
      }
    }
    if (!texCache.TryGetValue(id, out albedo)) {
      albedo = BakeAlbedo(mesh, id);
      albedo.name = id + "_base";
      texCache[id] = albedo;
    }
    return mesh != null && albedo != null;
  }

  public static bool IsFossilId(string id) {
    return id == "trex_skull" || id == "triceratops_skull" || id == "stego_plate"
      || id == "ammonite" || id == "trilobite" || id == "mammoth_tusk";
  }

  // ---------- 六个化石 ----------

  // 霸王龙头骨：大颅腔、长吻、张开的下颚和一排牙齿
  static void BuildTrex(B b) {
    b.Sphere(new Vector3(0f, 0.78f, -0.2f), new Vector3(1.2f, 1.0f, 1.1f));
    b.Box(new Vector3(0f, 0.52f, 0.85f), new Vector3(0.66f, 0.38f, 1.4f));
    b.Box(new Vector3(0f, 0.44f, 1.62f), new Vector3(0.42f, 0.26f, 0.5f));
    b.Box(new Vector3(0f, 0.16f, 0.7f), new Vector3(0.58f, 0.2f, 1.3f), Quaternion.Euler(-8f, 0f, 0f));
    b.Box(new Vector3(0f, 0.02f, 1.38f), new Vector3(0.34f, 0.14f, 0.48f), Quaternion.Euler(-14f, 0f, 0f));
    b.Box(new Vector3(0f, 1.12f, 0.15f), new Vector3(1.0f, 0.16f, 0.32f));
    b.Sphere(new Vector3(0f, 0.38f, -0.78f), new Vector3(0.5f, 0.42f, 0.42f));
    for (int i = 0; i < 7; i++) {
      float z = 0.38f + i * 0.18f;
      float s = 0.11f + (i % 2) * 0.03f;
      // 上牙朝下，下牙朝上
      b.Cone(new Vector3(-0.14f, 0.32f, z), new Vector3(s, 0.2f, s), Quaternion.Euler(180f, 0f, 8f));
      b.Cone(new Vector3(0.14f, 0.32f, z), new Vector3(s, 0.2f, s), Quaternion.Euler(180f, 0f, -8f));
      b.Cone(new Vector3(-0.12f, 0.16f, z), new Vector3(s * 0.75f, 0.14f, s * 0.75f));
      b.Cone(new Vector3(0.12f, 0.16f, z), new Vector3(s * 0.75f, 0.14f, s * 0.75f));
    }
  }

  // 三角龙头骨：喙、鼻角、一对眉角、带缘饰的颈盾
  static void BuildTriceratops(B b) {
    b.Box(new Vector3(0f, 0.48f, 0.25f), new Vector3(0.95f, 0.72f, 1.2f));
    b.Cone(new Vector3(0f, 0.32f, 1.05f), new Vector3(0.32f, 0.42f, 0.32f), Quaternion.Euler(90f, 0f, 0f));
    b.Cone(new Vector3(0f, 0.72f, 0.72f), new Vector3(0.16f, 0.48f, 0.16f), Quaternion.Euler(55f, 0f, 0f));
    b.Cone(new Vector3(-0.28f, 0.95f, 0.28f), new Vector3(0.15f, 0.72f, 0.15f), Quaternion.Euler(32f, -12f, 18f));
    b.Cone(new Vector3(0.28f, 0.95f, 0.28f), new Vector3(0.15f, 0.72f, 0.15f), Quaternion.Euler(32f, 12f, -18f));
    // 颈盾薄盘：圆柱局部 Y 轴转到 Z，成为前后方向很薄的椭圆盾
    b.Cylinder(new Vector3(0f, 0.62f, -0.62f), new Vector3(1.85f, 0.1f, 1.55f), Quaternion.Euler(90f, 0f, 0f));
    for (int i = 0; i < 7; i++) {
      float a = (-75f + i * 25f) * Mathf.Deg2Rad;
      float x = Mathf.Sin(a) * 0.88f;
      float y = 0.62f + Mathf.Cos(a) * 0.72f;
      b.Sphere(new Vector3(x, y, -0.62f), Vector3.one * 0.16f);
    }
  }

  // 剑龙背板：直立菱形骨板，中间一条脊，底部有榫头。正面法线朝 +Z（镜头一侧）。
  static void BuildStegoPlate(B b) {
    float w = 0.78f, top = 1.28f, bot = 0.58f, th = 0.07f;
    Vector3 ft = new Vector3(0f, top, th), fr = new Vector3(w, 0.05f, th), fb = new Vector3(0f, -bot, th), fl = new Vector3(-w, 0.05f, th);
    Vector3 bt = new Vector3(0f, top, -th), br = new Vector3(w, 0.05f, -th), bb = new Vector3(0f, -bot, -th), bl = new Vector3(-w, 0.05f, -th);
    b.Quad(ft, fl, fb, fr);             // +Z 正面
    b.Quad(bt, br, bb, bl);             // -Z 背面
    b.Quad(ft, fr, br, bt);             // 右上棱
    b.Quad(fr, fb, bb, br);             // 右下棱
    b.Quad(fb, fl, bl, bb);             // 左下棱
    b.Quad(fl, ft, bt, bl);             // 左上棱
    b.Box(new Vector3(0f, 0.15f, th + 0.04f), new Vector3(0.08f, 1.15f, 0.04f));
    b.Box(new Vector3(0f, -bot - 0.16f, 0f), new Vector3(0.2f, 0.32f, 0.14f));
  }

  // 菊石：对数螺旋壳，外圈有肋
  static void BuildAmmonite(B b) {
    int steps = 60;
    var centers = new List<Vector3>(steps + 1);
    var radii = new List<float>(steps + 1);
    for (int i = 0; i <= steps; i++) {
      float t = i / (float)steps;
      float ang = t * Mathf.PI * 2f * 3.05f;
      float R = 0.055f * Mathf.Exp(0.155f * ang);
      float rib = 1f + 0.14f * Mathf.Sin(t * 52f);
      centers.Add(new Vector3(Mathf.Cos(ang) * R, 0f, Mathf.Sin(ang) * R));
      radii.Add((0.03f + t * 0.085f) * rib);
    }
    b.Tube(centers, radii, 8);
    // 壳口略张开
    Vector3 lip = centers[steps];
    b.Sphere(lip, Vector3.one * radii[steps] * 2.1f);
  }

  // 三叶虫：头甲、中轴、体节和尾盾
  static void BuildTrilobite(B b) {
    b.Sphere(new Vector3(0f, 0f, 0f), new Vector3(1.55f, 0.28f, 1.05f));
    b.Sphere(new Vector3(0f, 0.08f, -0.02f), new Vector3(0.48f, 0.22f, 0.9f));
    b.Sphere(new Vector3(0f, 0.02f, 0.52f), new Vector3(1.2f, 0.26f, 0.55f));
    b.Sphere(new Vector3(-0.32f, 0.12f, 0.58f), Vector3.one * 0.14f);
    b.Sphere(new Vector3(0.32f, 0.12f, 0.58f), Vector3.one * 0.14f);
    for (int i = 0; i < 6; i++) {
      float z = 0.22f - i * 0.16f;
      b.Box(new Vector3(0f, 0.1f, z), new Vector3(1.4f - i * 0.06f, 0.05f, 0.045f));
    }
    b.Sphere(new Vector3(0f, 0f, -0.72f), new Vector3(0.72f, 0.16f, 0.38f));
    b.Cone(new Vector3(-0.62f, 0.02f, 0.15f), new Vector3(0.1f, 0.42f, 0.1f), Quaternion.Euler(100f, 0f, 70f));
    b.Cone(new Vector3(0.62f, 0.02f, 0.15f), new Vector3(0.1f, 0.42f, 0.1f), Quaternion.Euler(100f, 0f, -70f));
  }

  // 猛犸象牙：根部粗、尖端细，向上再向前弯
  static void BuildTusk(B b) {
    int steps = 22;
    var centers = new List<Vector3>(steps + 1);
    var radii = new List<float>(steps + 1);
    for (int i = 0; i <= steps; i++) {
      float t = i / (float)steps;
      Vector3 p = new Vector3(
        Mathf.Sin(t * 0.9f) * t * 0.48f,
        0.02f + t * 1.65f,
        (1f - Mathf.Cos(t * 1.15f)) * 0.9f);
      centers.Add(p);
      radii.Add(Mathf.Lerp(0.16f, 0.018f, t * t));
    }
    b.Tube(centers, radii, 8);
    b.Sphere(centers[0], Vector3.one * radii[0] * 2.2f);
    b.Sphere(centers[steps], Vector3.one * Mathf.Max(0.04f, radii[steps] * 2f));
  }

  // ---------- 贴图：只给网格真正覆盖到的 UV 上色，其余保持黑色，清洗进度才算得准 ----------

  static Texture2D BakeAlbedo(Mesh mesh, string id) {
    const int S = 512;
    var mask = new bool[S * S];
    var uv = mesh.uv;
    var tris = mesh.triangles;
    for (int i = 0; i < tris.Length; i += 3) {
      FillTri(mask, S,
        uv[tris[i]] * (S - 1),
        uv[tris[i + 1]] * (S - 1),
        uv[tris[i + 2]] * (S - 1));
    }
    // 扩一圈，避免骨骼边缘吃到黑边
    for (int pass = 0; pass < 2; pass++) {
      var grown = (bool[])mask.Clone();
      for (int y = 1; y < S - 1; y++) for (int x = 1; x < S - 1; x++) {
        int k = y * S + x;
        if (mask[k] || mask[k - 1] || mask[k + 1] || mask[k - S] || mask[k + S]) grown[k] = true;
      }
      mask = grown;
    }
    BoneTint(id, out Color bone, out Color pore, out Color band);
    var px = new Color32[S * S];
    for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) {
      int k = y * S + x;
      if (!mask[k]) { px[k] = new Color32(0, 0, 0, 255); continue; }
      float u = x / (float)S, v = y / (float)S;
      float n = Mathf.PerlinNoise(u * 6f + 2f, v * 6f);
      float poreN = Mathf.PerlinNoise(u * 28f, v * 28f + 4f);
      Color c = Color.Lerp(bone, pore, n * 0.55f + poreN * 0.2f);
      float crack = Mathf.PerlinNoise(u * 3.5f, v * 14f);
      if (crack > 0.72f && crack < 0.78f) c *= 0.62f;
      if (id == "ammonite") {
        float stripe = Mathf.Sin((u * 10f + v * 22f) * Mathf.PI);
        if (stripe > 0.35f) c = Color.Lerp(c, band, 0.55f);
      } else if (id == "trilobite") {
        float seg = Mathf.Abs(Mathf.Sin(v * 18f));
        if (seg < 0.15f) c *= 0.75f;
      } else if (id == "mammoth_tusk") {
        c = Color.Lerp(c, band, Mathf.PerlinNoise(u * 2f, v * 8f) * 0.35f);
      }
      c.a = 1f;
      px[k] = c;
    }
    var tex = new Texture2D(S, S, TextureFormat.RGBA32, true);
    tex.SetPixels32(px);
    tex.Apply(true);
    tex.wrapMode = TextureWrapMode.Clamp;
    tex.filterMode = FilterMode.Bilinear;
    return tex;
  }

  // 洗净后露出的骨色 / 化石色。沉积物颜色在 CleanGame 里另算。
  static void BoneTint(string id, out Color bone, out Color pore, out Color band) {
    band = new Color(.55f, .38f, .22f);
    switch (id) {
      case "triceratops_skull":
        bone = new Color(.94f, .86f, .68f); pore = new Color(.72f, .56f, .36f); break;
      case "stego_plate":
        bone = new Color(.9f, .82f, .64f); pore = new Color(.62f, .5f, .34f); break;
      case "ammonite":
        bone = new Color(.86f, .76f, .54f); pore = new Color(.62f, .48f, .3f); band = new Color(.42f, .28f, .16f); break;
      case "trilobite":
        bone = new Color(.7f, .6f, .44f); pore = new Color(.48f, .4f, .3f); break;
      case "mammoth_tusk":
        bone = new Color(.96f, .9f, .7f); pore = new Color(.78f, .66f, .4f); band = new Color(.86f, .72f, .42f); break;
      default:
        bone = new Color(.93f, .88f, .74f); pore = new Color(.7f, .58f, .4f); break;
    }
  }

  static void FillTri(bool[] m, int S, Vector2 a, Vector2 b, Vector2 c) {
    float minx = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
    float maxx = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
    float miny = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
    float maxy = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
    int x0 = Mathf.Clamp(Mathf.FloorToInt(minx), 0, S - 1);
    int x1 = Mathf.Clamp(Mathf.CeilToInt(maxx), 0, S - 1);
    int y0 = Mathf.Clamp(Mathf.FloorToInt(miny), 0, S - 1);
    int y1 = Mathf.Clamp(Mathf.CeilToInt(maxy), 0, S - 1);
    float area = Edge(a, b, c);
    if (Mathf.Abs(area) < 0.05f) return;
    for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) {
      var p = new Vector2(x + 0.5f, y + 0.5f);
      float w0 = Edge(b, c, p) / area;
      float w1 = Edge(c, a, p) / area;
      float w2 = Edge(a, b, p) / area;
      if (w0 >= -0.02f && w1 >= -0.02f && w2 >= -0.02f) m[y * S + x] = true;
    }
  }

  static float Edge(Vector2 a, Vector2 b, Vector2 p) {
    return (p.x - a.x) * (b.y - a.y) - (p.y - a.y) * (b.x - a.x);
  }

  // 简单网格累加器。单位体素建在原点，再平移/旋转/缩放进去。
  class B {
    public readonly List<Vector3> v = new List<Vector3>();
    public readonly List<int> t = new List<int>();
    static List<Vector3> sphereV, cylV, coneV;
    static List<int> sphereT, cylT, coneT;

    public void Box(Vector3 pos, Vector3 scale, Quaternion rot) { AddBox(pos, scale, rot); }
    public void Box(Vector3 pos, Vector3 scale) { AddBox(pos, scale, Quaternion.identity); }
    public void Sphere(Vector3 pos, Vector3 scale) { EnsureSphere(); Add(sphereV, sphereT, pos, scale, Quaternion.identity); }
    public void Cylinder(Vector3 pos, Vector3 scale, Quaternion rot) { EnsureCyl(); Add(cylV, cylT, pos, scale, rot); }
    public void Cone(Vector3 pos, Vector3 scale, Quaternion rot) { EnsureCone(); Add(coneV, coneT, pos, scale, rot); }
    public void Cone(Vector3 pos, Vector3 scale) { Cone(pos, scale, Quaternion.identity); }

    // a-b-c-d 沿边绕一圈。三角形 (a,b,c) 的法线 = Cross(b-a, c-a)，Unity 中 Cross(right, up) = forward。
    public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
      int o = v.Count;
      v.Add(a); v.Add(b); v.Add(c); v.Add(d);
      if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-12f) return;
      t.Add(o); t.Add(o + 1); t.Add(o + 2);
      t.Add(o); t.Add(o + 2); t.Add(o + 3);
    }

    // 沿中心线扫一圈，半径可逐圈变化（象牙、菊石）
    public void Tube(List<Vector3> centers, List<float> radii, int seg) {
      int n = centers.Count;
      if (n < 2) return;
      var rings = new int[n][];
      for (int i = 0; i < n; i++) {
        Vector3 tan = i == 0 ? centers[1] - centers[0]
          : i == n - 1 ? centers[i] - centers[i - 1]
          : centers[i + 1] - centers[i - 1];
        if (tan.sqrMagnitude < 1e-8f) tan = Vector3.up;
        tan.Normalize();
        Vector3 up = Mathf.Abs(Vector3.Dot(tan, Vector3.up)) > 0.92f ? Vector3.right : Vector3.up;
        Vector3 side = Vector3.Cross(tan, up).normalized;
        Vector3 bin = Vector3.Cross(side, tan).normalized;
        rings[i] = new int[seg];
        for (int k = 0; k < seg; k++) {
          float a = k / (float)seg * Mathf.PI * 2f;
          rings[i][k] = v.Count;
          v.Add(centers[i] + (Mathf.Cos(a) * side + Mathf.Sin(a) * bin) * radii[i]);
        }
      }
      for (int i = 0; i < n - 1; i++) {
        for (int k = 0; k < seg; k++) {
          int k2 = (k + 1) % seg;
          int a = rings[i][k], c = rings[i][k2], d = rings[i + 1][k], e = rings[i + 1][k2];
          Vector3 mid = (v[a] + v[c] + v[d] + v[e]) * 0.25f;
          Vector3 outw = mid - centers[i];
          Vector3 nrm = Vector3.Cross(v[c] - v[a], v[d] - v[a]);
          if (Vector3.Dot(nrm, outw) >= 0f) {
            t.Add(a); t.Add(c); t.Add(d);
            t.Add(c); t.Add(e); t.Add(d);
          } else {
            t.Add(a); t.Add(d); t.Add(c);
            t.Add(c); t.Add(d); t.Add(e);
          }
        }
      }
    }

    public Mesh ToMesh(string name) {
      var mesh = new Mesh();
      mesh.name = name;
      mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      mesh.SetVertices(v);
      mesh.SetTriangles(t, 0);
      mesh.RecalculateNormals();
      mesh.RecalculateBounds();
      ProjectUv(mesh);
      return mesh;
    }

    // 投到跨度最大的两个轴上，刷子在可见表面是连续的一块
    static void ProjectUv(Mesh mesh) {
      var verts = mesh.vertices;
      var bb = mesh.bounds;
      Vector3 sz = bb.size;
      float[] s = { Mathf.Max(sz.x, 0.0001f), Mathf.Max(sz.y, 0.0001f), Mathf.Max(sz.z, 0.0001f) };
      int max = 0, mid = 1;
      if (s[1] > s[0]) { max = 1; mid = 0; }
      if (s[2] > s[max]) { mid = max; max = 2; }
      else if (s[2] > s[mid]) mid = 2;
      var uv = new Vector2[verts.Length];
      for (int i = 0; i < verts.Length; i++) {
        float pu = (Axis(verts[i], max) - Axis(bb.min, max)) / s[max];
        float pv = (Axis(verts[i], mid) - Axis(bb.min, mid)) / s[mid];
        uv[i] = new Vector2(Mathf.Lerp(0.03f, 0.97f, pu), Mathf.Lerp(0.03f, 0.97f, pv));
      }
      mesh.uv = uv;
    }

    static float Axis(Vector3 p, int a) { return a == 0 ? p.x : a == 1 ? p.y : p.z; }

    void Add(List<Vector3> srcV, List<int> srcT, Vector3 pos, Vector3 scale, Quaternion rot) {
      var m = Matrix4x4.TRS(pos, rot, scale);
      int o = v.Count;
      for (int i = 0; i < srcV.Count; i++) v.Add(m.MultiplyPoint3x4(srcV[i]));
      for (int i = 0; i < srcT.Count; i++) t.Add(srcT[i] + o);
    }

    void AddBox(Vector3 pos, Vector3 scale, Quaternion rot) {
      var srcV = new List<Vector3>();
      var srcT = new List<int>();
      // 单位立方体 -0.5..0.5，每面独立顶点，法线才硬
      QuadLocal(srcV, srcT, new Vector3(-.5f,-.5f,.5f), new Vector3(.5f,-.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(-.5f,.5f,.5f));
      QuadLocal(srcV, srcT, new Vector3(.5f,-.5f,-.5f), new Vector3(-.5f,-.5f,-.5f), new Vector3(-.5f,.5f,-.5f), new Vector3(.5f,.5f,-.5f));
      QuadLocal(srcV, srcT, new Vector3(-.5f,.5f,.5f), new Vector3(.5f,.5f,.5f), new Vector3(.5f,.5f,-.5f), new Vector3(-.5f,.5f,-.5f));
      QuadLocal(srcV, srcT, new Vector3(-.5f,-.5f,-.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,-.5f,.5f), new Vector3(-.5f,-.5f,.5f));
      QuadLocal(srcV, srcT, new Vector3(.5f,-.5f,.5f), new Vector3(.5f,-.5f,-.5f), new Vector3(.5f,.5f,-.5f), new Vector3(.5f,.5f,.5f));
      QuadLocal(srcV, srcT, new Vector3(-.5f,-.5f,-.5f), new Vector3(-.5f,-.5f,.5f), new Vector3(-.5f,.5f,.5f), new Vector3(-.5f,.5f,-.5f));
      Add(srcV, srcT, pos, scale, rot);
    }

    static void QuadLocal(List<Vector3> vs, List<int> ts, Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
      int o = vs.Count;
      vs.Add(a); vs.Add(b); vs.Add(c); vs.Add(d);
      // Cross(b-a, c-a) 指向体外
      ts.Add(o); ts.Add(o + 1); ts.Add(o + 2);
      ts.Add(o); ts.Add(o + 2); ts.Add(o + 3);
    }

    static void EnsureSphere() {
      if (sphereV != null) return;
      sphereV = new List<Vector3>(); sphereT = new List<int>();
      const int lon = 16, lat = 10;
      int top = 0; sphereV.Add(new Vector3(0f, 0.5f, 0f));
      for (int y = 1; y < lat; y++) {
        float phi = y / (float)lat * Mathf.PI;
        float r = Mathf.Sin(phi) * 0.5f;
        float yy = Mathf.Cos(phi) * 0.5f;
        for (int x = 0; x < lon; x++) {
          float th = x / (float)lon * Mathf.PI * 2f;
          sphereV.Add(new Vector3(Mathf.Cos(th) * r, yy, Mathf.Sin(th) * r));
        }
      }
      int bot = sphereV.Count; sphereV.Add(new Vector3(0f, -0.5f, 0f));
      for (int x = 0; x < lon; x++) {
        int a = 1 + x;
        int b = 1 + (x + 1) % lon;
        AddOutward(sphereV, sphereT, top, b, a);
        int y = lat - 2;
        int c = 1 + y * lon + x;
        int d = 1 + y * lon + (x + 1) % lon;
        AddOutward(sphereV, sphereT, bot, c, d);
      }
      for (int y = 1; y < lat - 1; y++) {
        for (int x = 0; x < lon; x++) {
          int a = 1 + (y - 1) * lon + x;
          int b = 1 + (y - 1) * lon + (x + 1) % lon;
          int c = 1 + y * lon + x;
          int d = 1 + y * lon + (x + 1) % lon;
          AddOutward(sphereV, sphereT, a, b, c);
          AddOutward(sphereV, sphereT, b, d, c);
        }
      }
    }

    // 圆柱：半径 0.5，高度 1，轴为 Y。缩放后再旋转。
    static void EnsureCyl() {
      if (cylV != null) return;
      cylV = new List<Vector3>(); cylT = new List<int>();
      const int seg = 16;
      int topC = cylV.Count; cylV.Add(new Vector3(0f, 0.5f, 0f));
      int botC = cylV.Count; cylV.Add(new Vector3(0f, -0.5f, 0f));
      int side = cylV.Count;
      for (int i = 0; i < seg; i++) {
        float th = i / (float)seg * Mathf.PI * 2f;
        float x = Mathf.Cos(th) * 0.5f, z = Mathf.Sin(th) * 0.5f;
        cylV.Add(new Vector3(x, 0.5f, z));
        cylV.Add(new Vector3(x, -0.5f, z));
      }
      for (int i = 0; i < seg; i++) {
        int i2 = (i + 1) % seg;
        int t0 = side + i * 2, t1 = side + i2 * 2;
        int b0 = t0 + 1, b1 = t1 + 1;
        AddOutward(cylV, cylT, t0, t1, b0);
        AddOutward(cylV, cylT, t1, b1, b0);
        AddOutward(cylV, cylT, topC, t1, t0);
        AddOutward(cylV, cylT, botC, b0, b1);
      }
    }

    // 圆锥：底在 y=-0.5，尖在 y=0.5
    static void EnsureCone() {
      if (coneV != null) return;
      coneV = new List<Vector3>(); coneT = new List<int>();
      const int seg = 8;
      int tip = coneV.Count; coneV.Add(new Vector3(0f, 0.5f, 0f));
      int botC = coneV.Count; coneV.Add(new Vector3(0f, -0.5f, 0f));
      int ring = coneV.Count;
      for (int i = 0; i < seg; i++) {
        float th = i / (float)seg * Mathf.PI * 2f;
        coneV.Add(new Vector3(Mathf.Cos(th) * 0.5f, -0.5f, Mathf.Sin(th) * 0.5f));
      }
      for (int i = 0; i < seg; i++) {
        int i2 = (i + 1) % seg;
        AddOutward(coneV, coneT, tip, ring + i2, ring + i);
        AddOutward(coneV, coneT, botC, ring + i, ring + i2);
      }
    }

    // 三角形若法线朝内就交换顺序。球/柱/锥都以原点为内腔。
    static void AddOutward(List<Vector3> vs, List<int> ts, int i0, int i1, int i2) {
      Vector3 n = Vector3.Cross(vs[i1] - vs[i0], vs[i2] - vs[i0]);
      if (n.sqrMagnitude < 1e-12f) return;
      Vector3 mid = (vs[i0] + vs[i1] + vs[i2]) / 3f;
      if (Vector3.Dot(n, mid) < 0f) { int s = i1; i1 = i2; i2 = s; }
      ts.Add(i0); ts.Add(i1); ts.Add(i2);
    }
  }
}
