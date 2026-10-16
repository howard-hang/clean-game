using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// 洗洗更解压 v2 — layered dirt (mud + grime), wet sheen, rich particles, tools, custom IMGUI.
public class CleanGame : MonoBehaviour {
  class Lvl { public string name, mesh; public float tilt, yaw; }
  Lvl[] levels = {
    new Lvl{name="油腻的青花盘",mesh="plate",tilt=55,yaw=0},
    new Lvl{name="泥巴木箱",mesh="crate",tilt=0,yaw=35},
    new Lvl{name="泥坑里的足球",mesh="ball",tilt=0,yaw=0},
    new Lvl{name="古董花瓶",mesh="vase",tilt=0,yaw=20},
    new Lvl{name="小黄鸭",mesh="duck",tilt=0,yaw=-60},
  };
  const int N = 512;
  enum St { Title, Play, Win }
  St state = St.Title;
  int cur; GameObject obj; Texture2D tex, glossTex; Color32[] basePx, outPx, glossPx; float[] mud, grime, wet; bool[] used; Color32[] mudCol;
  float totalDirt, progress, shownProgress, levelStart, winT, wetTimer, shake; int stars;
  bool sponge; Camera cam; float camYaw=180, camPitch=18, camDist=5.2f, titleSpin;
  float objYaw, objPitch; Quaternion objBaseRot; Vector3 objCenter;
  ParticleSystem water, mudPs, foam, confetti, glint, mist; LineRenderer jet;
  AudioSource hiss, sfx; GameObject puddle, nozzle, spongeObj; Material puddleMat;
  Vector2 pointer; bool pointerDown, autoMode, rotating; Vector3 toolVel, toolPos; Vector2 lastRotMouse; List<Vector3> cleanSpots = new List<Vector3>();
  Font font; Dictionary<string,Texture2D> T = new Dictionary<string,Texture2D>();
  AudioClip popClip, dingClip, chord;

  // ---------- setup ----------
  void Start() {
    Application.targetFrameRate = 60; QualitySettings.shadowDistance = 25; QualitySettings.antiAliasing = 4;
    font = Resources.Load<Font>("NotoSansSC");
    cam = Camera.main; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.72f,.86f,.94f); cam.fieldOfView = 40; cam.allowMSAA = true;
    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
    RenderSettings.ambientSkyColor = new Color(1f,.97f,.92f); RenderSettings.ambientEquatorColor = new Color(.78f,.82f,.86f); RenderSettings.ambientGroundColor = new Color(.45f,.4f,.38f);
    RenderSettings.fog = true; RenderSettings.fogColor = new Color(.78f,.88f,.94f); RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 10; RenderSettings.fogEndDistance = 28;
    QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.High; QualitySettings.shadowDistance = 28;
    foreach (var l0 in FindObjectsOfType<Light>()) Destroy(l0.gameObject);
    var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1f,.93f,.82f); sun.intensity = 1.35f; sun.shadows = LightShadows.Soft; sun.shadowStrength=.72f; sun.shadowBias=.04f; sun.transform.rotation = Quaternion.Euler(42,-48,0);
    var fill = new GameObject("Fill").AddComponent<Light>(); fill.type = LightType.Point; fill.color = new Color(.55f,.72f,.95f); fill.intensity = 1.6f; fill.range = 14; fill.transform.position = new Vector3(-3.2f,3.4f,2.8f);
    var rim = new GameObject("Rim").AddComponent<Light>(); rim.type = LightType.Point; rim.color = new Color(1f,.78f,.92f); rim.intensity = 1.8f; rim.range = 10; rim.transform.position = new Vector3(2.8f,2.8f,-2.2f);
    var windowLight = new GameObject("WindowLight").AddComponent<Light>(); windowLight.type = LightType.Spot; windowLight.color = new Color(1f,.96f,.88f); windowLight.intensity = 4.5f; windowLight.range = 12; windowLight.spotAngle = 55; windowLight.innerSpotAngle = 25; windowLight.shadows = LightShadows.Soft; windowLight.transform.position = new Vector3(-2.6f,3.7f,-4.0f); windowLight.transform.rotation = Quaternion.Euler(25,0,0);
    BuildRoom();
    water = PS(new Color(.8f,.93f,1f,.75f), .025f, .07f, 1f, .5f, true);
    mist = PS(new Color(1,1,1,.25f), .25f, .5f, -.05f, .8f, false);
    mudPs = PS(new Color(.38f,.25f,.13f), .04f, .11f, 1.4f, 1.6f, true);
    foam = PS(new Color(1,1,1,.92f), .08f, .2f, -.01f, 3.2f, false); var fs = foam.sizeOverLifetime; fs.enabled = true;
    fs.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0,.2f), new Keyframe(.15f,1), new Keyframe(.9f,1.05f), new Keyframe(1,0)));
    var fr = foam.GetComponent<ParticleSystemRenderer>(); fr.material.mainTexture = Bubble();
    confetti = PS(Color.white, .08f, .14f, .6f, 3.5f, true); var cr = confetti.GetComponent<ParticleSystemRenderer>(); cr.material.mainTexture = Texture2D.whiteTexture;
    var rot = confetti.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-6, 6);
    glint = PS(new Color(1,1,.9f), .12f, .3f, 0, .6f, false); glint.GetComponent<ParticleSystemRenderer>().material.mainTexture = Star4();
    var gs = glint.sizeOverLifetime; gs.enabled = true; gs.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0,0), new Keyframe(.4f,1), new Keyframe(1,0)));
    var gr = glint.rotationOverLifetime; gr.enabled = true; gr.z = 2;
    jet = new GameObject("jet").AddComponent<LineRenderer>(); jet.positionCount = 8; jet.useWorldSpace = true; jet.numCapVertices = 4; jet.numCornerVertices = 4;
    jet.widthCurve = new AnimationCurve(new Keyframe(0,.07f), new Keyframe(.5f,.12f), new Keyframe(1,.03f));
    jet.material = new Material(Shader.Find("Sprites/Default")); jet.material.color = new Color(.55f,.85f,1f,1f);
    jet.startColor = new Color(.6f,.9f,1f,1f); jet.endColor = new Color(1f,1f,1f,.55f); jet.enabled = false;
    jet.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; jet.receiveShadows = false;
    BuildTools();
    hiss = gameObject.AddComponent<AudioSource>(); hiss.clip = Noise(); hiss.loop = true; hiss.volume = 0; hiss.Play();
    sfx = gameObject.AddComponent<AudioSource>(); popClip = Pop(); dingClip = Tone(880,.25f); chord = Chord();
    Load(0); state = St.Title;
  }
  Material M(Color c, float gloss, Texture t = null, float metal = 0) { var m = new Material(Shader.Find("Standard")); m.color = c; m.SetFloat("_Glossiness", gloss); m.SetFloat("_Metallic", metal); if (t) m.mainTexture = t; return m; }
  GameObject Prim(PrimitiveType p, Vector3 pos, Vector3 scale, Material m, Transform parent = null, bool col = false) {
    var g = GameObject.CreatePrimitive(p); if (!col) Destroy(g.GetComponent<Collider>()); g.transform.SetParent(parent, false); g.transform.localPosition = pos; g.transform.localScale = scale; g.GetComponent<Renderer>().material = m; return g; }
  Texture2D Tiles(Color a, Color grout, int n, bool wood) {
    int S=512; var t = new Texture2D(S,S,TextureFormat.RGBA32,true); var px = new Color[S*S]; int cell = S/n;
    for (int y=0;y<S;y++) for (int x=0;x<S;x++) {
      Color c; float fx=x/(float)S, fy=y/(float)S;
      if (wood) {
        int row = y/cell; int off = (row%2)*(cell/2); bool g = (y%cell)<2 || ((x+off)%cell)<2;
        float grain = Mathf.PerlinNoise(x*.008f+row*3.1f, y*.35f);
        float pore = Mathf.PerlinNoise(x*.2f, y*.05f+row);
        c = Color.Lerp(a, a*new Color(.72f,.55f,.35f), grain*.55f);
        c *= .88f + .12f*Mathf.Sin(row*1.3f) + pore*.06f;
        if (g) c = Color.Lerp(c, grout, .85f);
      } else {
        int cx=x%cell, cy=y%cell; bool g = cx<4 || cy<4 || cx>cell-5 || cy>cell-5;
        float v = Mathf.PerlinNoise(x*.03f+7,y*.03f)*.08f + Mathf.PerlinNoise(x*.12f,y*.12f)*.04f;
        float bevel = 0; if (!g) { float e=Mathf.Min(Mathf.Min(cx,cell-1-cx),Mathf.Min(cy,cell-1-cy)); bevel = Mathf.Clamp01(e/10f); }
        c = g ? grout : a * (0.93f + v + bevel*.07f);
        if (!g && cx>cell-18 && cy>cell-18) c = Color.Lerp(c, Color.white, .12f); // soft specular corner
      }
      c.a = 1; px[y*S+x] = c;
    }
    t.SetPixels(px); t.Apply(true); t.wrapMode = TextureWrapMode.Repeat; t.anisoLevel = 8; t.filterMode = FilterMode.Trilinear; return t;
  }
  Texture2D Marble(Color baseC, Color vein) {
    int S=512; var t=new Texture2D(S,S,TextureFormat.RGBA32,true); var px=new Color[S*S];
    for(int y=0;y<S;y++) for(int x=0;x<S;x++){
      float u=x/(float)S,v=y/(float)S;
      float n1=Mathf.PerlinNoise(u*3+2,v*3), n2=Mathf.PerlinNoise(u*12,v*12+4);
      float veins=Mathf.Abs(Mathf.Sin((u*2.2f+v*1.1f+n1*.8f)*Mathf.PI*2));
      veins=Mathf.Pow(1f-Mathf.Clamp01(veins*1.6f+n2*.25f), 8f);
      Color c=Color.Lerp(baseC, vein, veins*.85f); c*=.92f+n1*.1f; c.a=1; px[y*S+x]=c;
    }
    t.SetPixels(px); t.Apply(true); t.wrapMode=TextureWrapMode.Repeat; t.anisoLevel=8; return t;
  }
  Texture2D Fabric(Color a, Color b) {
    int S=256; var t=new Texture2D(S,S); var px=new Color[S*S];
    for(int y=0;y<S;y++) for(int x=0;x<S;x++){
      float weave=((x/3)+(y/3))%2==0?0f:1f; float n=Mathf.PerlinNoise(x*.08f,y*.08f);
      Color c=Color.Lerp(a,b,.35f*weave+.3f*n); c.a=1; px[y*S+x]=c;
    }
    t.SetPixels(px); t.Apply(); t.wrapMode=TextureWrapMode.Repeat; return t;
  }
  Texture2D OutsideSky() {
    int S=256; var t=new Texture2D(S,S); var px=new Color[S*S];
    for(int y=0;y<S;y++) for(int x=0;x<S;x++){
      float v=y/(float)S,u=x/(float)S;
      Color sky=Color.Lerp(new Color(.55f,.78f,.95f), new Color(.95f,.97f,1f), v);
      float cloud=Mathf.PerlinNoise(u*4+1,v*2.5f); if(cloud>.55f) sky=Color.Lerp(sky,Color.white,(cloud-.55f)*2f);
      // soft tree silhouette at bottom
      float hill=Mathf.PerlinNoise(u*3,2)*.18f+.08f; if(v<hill) sky=Color.Lerp(new Color(.25f,.45f,.28f), sky, v/hill);
      sky.a=1; px[y*S+x]=sky;
    }
    t.SetPixels(px); t.Apply(); return t;
  }
  Material Emit(Color c, float strength){ var m=M(c,.2f); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c*strength); return m; }
  void BuildRoom() {
    var room = new GameObject("Bathroom").transform;
    // glossy floor wood
    var floorT = Tiles(new Color(.78f,.58f,.38f), new Color(.42f,.28f,.16f), 6, true);
    var floorM = M(Color.white, .55f, floorT); floorM.mainTextureScale = new Vector2(3.5f,3.5f);
    Prim(PrimitiveType.Plane, Vector3.zero, Vector3.one*3.2f, floorM, room, true);
    // glossy subway tiles
    var wallT = Tiles(new Color(.78f,.90f,.92f), new Color(.94f,.96f,.97f), 7, false);
    var wm = M(Color.white, .82f, wallT); wm.mainTextureScale = new Vector2(5.5f, 2.8f);
    Prim(PrimitiveType.Cube, new Vector3(0,3.1f,-4.55f), new Vector3(16.5f,6.4f,.22f), wm, room);
    var wmS = M(Color.white, .82f, wallT); wmS.mainTextureScale = new Vector2(3.8f, 2.8f);
    Prim(PrimitiveType.Cube, new Vector3(-6.2f,3.1f,0), new Vector3(.22f,6.4f,10.5f), wmS, room);
    Prim(PrimitiveType.Cube, new Vector3(6.2f,3.1f,0), new Vector3(.22f,6.4f,10.5f), wmS, room);
    // marble wainscot / skirting
    var marbleT = Marble(new Color(.93f,.93f,.91f), new Color(.55f,.55f,.58f));
    var marble = M(Color.white, .88f, marbleT); marble.mainTextureScale = new Vector2(4,1);
    Prim(PrimitiveType.Cube, new Vector3(0,.55f,-4.42f), new Vector3(16.3f,1.1f,.18f), marble, room);
    Prim(PrimitiveType.Cube, new Vector3(-6.05f,.55f,0), new Vector3(.18f,1.1f,10.2f), marble, room);
    Prim(PrimitiveType.Cube, new Vector3(6.05f,.55f,0), new Vector3(.18f,1.1f,10.2f), marble, room);
    var trim = M(new Color(.98f,.97f,.94f), .75f);
    Prim(PrimitiveType.Cube, new Vector3(0,1.12f,-4.32f), new Vector3(16.2f,.06f,.12f), trim, room);
    Prim(PrimitiveType.Cube, new Vector3(0,.04f,-4.32f), new Vector3(16.2f,.08f,.14f), trim, room);
    // ceiling
    Prim(PrimitiveType.Cube, new Vector3(0,6.2f,0), new Vector3(16.5f,.15f,10.5f), M(new Color(.97f,.96f,.94f), .35f), room);
    // soft ceiling cove light strip
    Prim(PrimitiveType.Cube, new Vector3(0,6.05f,-3.8f), new Vector3(10f,.04f,.15f), Emit(new Color(1f,.95f,.85f), 1.4f), room);
    Prim(PrimitiveType.Cube, new Vector3(0,6.05f,2.5f), new Vector3(8f,.04f,.12f), Emit(new Color(1f,.95f,.85f), 1.0f), room);

    // window with outside sky + soft shafts (emissive quads)
    var chrome = M(new Color(.82f,.84f,.88f), .95f, null, 1f);
    Prim(PrimitiveType.Cube, new Vector3(-2.55f,3.55f,-4.48f), new Vector3(2.7f,2.1f,.08f), chrome, room); // frame
    var skyM = M(Color.white, .15f, OutsideSky()); skyM.EnableKeyword("_EMISSION"); skyM.SetColor("_EmissionColor", new Color(.85f,.92f,1f)*0.55f);
    Prim(PrimitiveType.Quad, new Vector3(-2.55f,3.55f,-4.44f), new Vector3(2.4f,1.8f,1), skyM, room);
    // muntins
    Prim(PrimitiveType.Cube, new Vector3(-2.55f,3.55f,-4.42f), new Vector3(.05f,1.8f,.04f), trim, room);
    Prim(PrimitiveType.Cube, new Vector3(-2.55f,3.55f,-4.42f), new Vector3(2.4f,.05f,.04f), trim, room);
    // light shafts (transparent emissive planes)
    var shaft = M(new Color(1f,.96f,.85f,.12f), 0f); shaft.SetFloat("_Mode",3); shaft.SetInt("_SrcBlend",5); shaft.SetInt("_DstBlend",10); shaft.SetInt("_ZWrite",0); shaft.EnableKeyword("_ALPHABLEND_ON"); shaft.renderQueue=3100; shaft.EnableKeyword("_EMISSION"); shaft.SetColor("_EmissionColor", new Color(1f,.95f,.8f)*.35f);
    var sh1 = Prim(PrimitiveType.Quad, new Vector3(-2.2f,2.4f,-2.8f), new Vector3(1.6f,3.2f,1), shaft, room); sh1.transform.rotation = Quaternion.Euler(18, 25, -8);
    var sh2 = Prim(PrimitiveType.Quad, new Vector3(-1.4f,2.1f,-2.2f), new Vector3(1.1f,2.6f,1), shaft, room); sh2.transform.rotation = Quaternion.Euler(22, 35, -5);

    // bathtub — ceramic shell + water + foam clusters
    var tubM = M(new Color(.97f,.97f,.98f), .92f);
    var tub = Prim(PrimitiveType.Capsule, new Vector3(3.5f,.62f,-3.35f), new Vector3(1.15f,1.75f,1.15f), tubM, room); tub.transform.rotation = Quaternion.Euler(0,0,90);
    Prim(PrimitiveType.Cube, new Vector3(3.5f,.35f,-3.35f), new Vector3(2.6f,.5f,1.35f), tubM, room); // apron
    var waterM = M(new Color(.55f,.75f,.88f,.55f), .95f); waterM.SetFloat("_Mode",3); waterM.SetInt("_SrcBlend",5); waterM.SetInt("_DstBlend",10); waterM.SetInt("_ZWrite",0); waterM.EnableKeyword("_ALPHABLEND_ON"); waterM.renderQueue=3000;
    Prim(PrimitiveType.Cube, new Vector3(3.5f,.95f,-3.35f), new Vector3(2.3f,.02f,1.0f), waterM, room);
    var foamM = M(new Color(1f,1f,1f), .08f);
    for (int i=0;i<28;i++) {
      float u=i/28f; float x=2.45f+u*2.15f; float z=-3.35f+Mathf.Sin(i*1.7f)*.38f; float y=.98f+Mathf.PerlinNoise(i*.4f,1)*.12f;
      Prim(PrimitiveType.Sphere, new Vector3(x,y,z), Vector3.one*(.18f+.14f*Mathf.PerlinNoise(i,2)), foamM, room);
    }
    // chrome faucet + handles
    Prim(PrimitiveType.Cylinder, new Vector3(4.85f,1.55f,-3.35f), new Vector3(.07f,.42f,.07f), chrome, room);
    Prim(PrimitiveType.Cylinder, new Vector3(4.55f,1.95f,-3.35f), new Vector3(.05f,.22f,.05f), chrome, room).transform.rotation = Quaternion.Euler(0,0,90);
    Prim(PrimitiveType.Sphere, new Vector3(4.32f,1.95f,-3.35f), Vector3.one*.09f, chrome, room);
    Prim(PrimitiveType.Cylinder, new Vector3(4.85f,1.25f,-3.55f), new Vector3(.05f,.08f,.05f), chrome, room);
    Prim(PrimitiveType.Cylinder, new Vector3(4.85f,1.25f,-3.15f), new Vector3(.05f,.08f,.05f), chrome, room);

    // vanity cabinet + marble top + sink bowl + mirror
    var woodCab = M(new Color(.55f,.38f,.24f), .45f, Tiles(new Color(.62f,.42f,.26f), new Color(.35f,.22f,.12f), 4, true));
    Prim(PrimitiveType.Cube, new Vector3(-4.35f,.85f,-3.55f), new Vector3(1.9f,1.7f,1.0f), woodCab, room);
    var topM = M(Color.white, .9f, marbleT); Prim(PrimitiveType.Cube, new Vector3(-4.35f,1.75f,-3.55f), new Vector3(2.05f,.08f,1.15f), topM, room);
    Prim(PrimitiveType.Sphere, new Vector3(-4.35f,1.55f,-3.55f), new Vector3(.7f,.22f,.55f), M(new Color(.92f,.94f,.96f), .95f), room); // basin
    // mirror with frame
    Prim(PrimitiveType.Cube, new Vector3(-4.35f,3.15f,-4.35f), new Vector3(1.5f,1.7f,.06f), chrome, room);
    var mir = M(new Color(.75f,.82f,.88f), 1f, null, .85f); mir.EnableKeyword("_EMISSION"); mir.SetColor("_EmissionColor", new Color(.4f,.5f,.6f)*.25f);
    Prim(PrimitiveType.Cube, new Vector3(-4.35f,3.15f,-4.32f), new Vector3(1.3f,1.5f,.03f), mir, room);
    // toiletries
    Prim(PrimitiveType.Cylinder, new Vector3(-3.7f,2.05f,-3.35f), new Vector3(.12f,.28f,.12f), M(new Color(.95f,.55f,.55f), .6f), room);
    Prim(PrimitiveType.Cylinder, new Vector3(-3.55f,2.0f,-3.55f), new Vector3(.1f,.22f,.1f), M(new Color(.55f,.75f,.95f), .6f), room);
    Prim(PrimitiveType.Cylinder, new Vector3(-5.0f,2.02f,-3.4f), new Vector3(.14f,.2f,.14f), M(new Color(.98f,.92f,.7f), .5f), room);
    Prim(PrimitiveType.Sphere, new Vector3(-5.0f,2.28f,-3.4f), Vector3.one*.1f, M(new Color(.98f,.92f,.7f), .5f), room);

    // plant with elongated leaves
    Prim(PrimitiveType.Cylinder, new Vector3(-4.9f,2.0f,-3.7f), new Vector3(.28f,.12f,.28f), M(new Color(.75f,.4f,.28f), .35f), room);
    var leaf = M(new Color(.32f,.62f,.38f), .35f);
    for (int i=0;i<10;i++) {
      float a=i/10f*Mathf.PI*2; var lf=Prim(PrimitiveType.Sphere, new Vector3(-4.9f+.08f*Mathf.Cos(a), 2.35f+(i%3)*.12f, -3.7f+.08f*Mathf.Sin(a)), new Vector3(.12f,.42f,.08f), leaf, room);
      lf.transform.rotation = Quaternion.Euler(25*Mathf.Cos(a), a*Mathf.Rad2Deg, 15);
    }

    // hanging towels with soft folds (stacked thin cubes + slight bend via offset)
    var towelA = M(Color.white, .15f, Fabric(new Color(1f,.62f,.58f), new Color(.9f,.45f,.42f))); towelA.mainTextureScale=new Vector2(2,2);
    var towelB = M(Color.white, .15f, Fabric(new Color(.55f,.75f,.95f), new Color(.35f,.55f,.85f))); towelB.mainTextureScale=new Vector2(2,2);
    Prim(PrimitiveType.Cylinder, new Vector3(1.35f,3.45f,-4.35f), new Vector3(.04f,.55f,.04f), chrome, room).transform.rotation = Quaternion.Euler(0,0,90);
    for (int i=0;i<5;i++) Prim(PrimitiveType.Cube, new Vector3(1.15f+i*.02f, 2.95f-i*.02f, -4.28f), new Vector3(.85f-i*.02f, .9f, .04f), towelA, room);
    for (int i=0;i<4;i++) Prim(PrimitiveType.Cube, new Vector3(2.15f+i*.02f, 2.9f-i*.015f, -4.28f), new Vector3(.7f, .85f, .035f), towelB, room);

    // corner shelf + candles
    Prim(PrimitiveType.Cube, new Vector3(5.4f,2.2f,-3.8f), new Vector3(.9f,.06f,.7f), woodCab, room);
    Prim(PrimitiveType.Cylinder, new Vector3(5.25f,2.45f,-3.7f), new Vector3(.1f,.22f,.1f), M(new Color(.95f,.9f,.75f), .4f), room);
    Prim(PrimitiveType.Sphere, new Vector3(5.25f,2.72f,-3.7f), Vector3.one*.06f, Emit(new Color(1f,.7f,.3f), 2.2f), room);
    Prim(PrimitiveType.Cylinder, new Vector3(5.55f,2.4f,-3.9f), new Vector3(.08f,.16f,.08f), M(new Color(.95f,.9f,.75f), .4f), room);
    Prim(PrimitiveType.Sphere, new Vector3(5.55f,2.6f,-3.9f), Vector3.one*.05f, Emit(new Color(1f,.65f,.25f), 1.8f), room);

    // bath mat (softer pattern)
    var matT = Fabric(new Color(.45f,.72f,.78f), new Color(.7f,.88f,.9f));
    var matM = M(Color.white, .08f, matT); matM.mainTextureScale = new Vector2(3,3);
    Prim(PrimitiveType.Cylinder, new Vector3(0,.015f,0), new Vector3(4.4f,.012f,4.4f), matM, room, true);
    // marble pedestal
    var pedM = M(Color.white, .88f, marbleT); pedM.mainTextureScale = new Vector2(1.2f,1.2f);
    Prim(PrimitiveType.Cylinder, new Vector3(0,.14f,0), new Vector3(2.85f,.14f,2.85f), pedM, room, true);
    Prim(PrimitiveType.Cylinder, new Vector3(0,.30f,0), new Vector3(2.55f,.02f,2.55f), M(new Color(.35f,.78f,.75f), .75f), room);
    // gold accent ring
    Prim(PrimitiveType.Cylinder, new Vector3(0,.31f,0), new Vector3(2.62f,.015f,2.62f), M(new Color(.85f,.7f,.35f), .9f, null, .6f), room);

    puddleMat = M(new Color(.55f,.78f,.92f,.0f), .98f); puddleMat.SetFloat("_Mode",3); puddleMat.SetInt("_SrcBlend",5); puddleMat.SetInt("_DstBlend",10); puddleMat.SetInt("_ZWrite",0); puddleMat.EnableKeyword("_ALPHABLEND_ON"); puddleMat.renderQueue=3000;
    puddle = Prim(PrimitiveType.Cylinder, new Vector3(0,.325f,0), new Vector3(0,.002f,0), puddleMat, room);
  }
  void BuildTools() {
    nozzle = new GameObject("PressureWasher"); var y = M(new Color(1f,.78f,.1f),.6f); var blk = M(new Color(.15f,.15f,.18f),.4f); var metal = M(new Color(.8f,.82f,.85f),.9f,null,1);
    Prim(PrimitiveType.Cylinder, new Vector3(0,0,-.25f), new Vector3(.14f,.25f,.14f), y, nozzle.transform).transform.localRotation = Quaternion.Euler(90,0,0);
    Prim(PrimitiveType.Cylinder, new Vector3(0,0,.12f), new Vector3(.05f,.18f,.05f), metal, nozzle.transform).transform.localRotation = Quaternion.Euler(90,0,0);
    Prim(PrimitiveType.Cylinder, new Vector3(0,0,.31f), new Vector3(.08f,.03f,.08f), blk, nozzle.transform).transform.localRotation = Quaternion.Euler(90,0,0);
    var grip = Prim(PrimitiveType.Cube, new Vector3(0,-.16f,-.32f), new Vector3(.08f,.26f,.1f), blk, nozzle.transform); grip.transform.localRotation = Quaternion.Euler(-15,0,0);
    Prim(PrimitiveType.Cube, new Vector3(0,-.08f,-.2f), new Vector3(.03f,.08f,.03f), M(Color.red,.5f), nozzle.transform);
    spongeObj = new GameObject("Sponge"); Prim(PrimitiveType.Cube, Vector3.zero, new Vector3(.42f,.14f,.3f), M(new Color(1f,.85f,.3f),.05f), spongeObj.transform);
    Prim(PrimitiveType.Cube, new Vector3(0,.09f,0), new Vector3(.42f,.05f,.3f), M(new Color(.3f,.72f,.35f),.05f), spongeObj.transform);
    foreach (var go in new[]{nozzle, spongeObj}) foreach (var c in go.GetComponentsInChildren<Collider>()) Destroy(c);
    spongeObj.SetActive(false); nozzle.SetActive(false);
  }
  ParticleSystem PS(Color c, float s0, float s1, float grav, float life, bool collide) {
    var g = new GameObject("ps"); var ps = g.AddComponent<ParticleSystem>(); ps.Stop();
    var m = ps.main; m.startColor = c; m.startSize = new ParticleSystem.MinMaxCurve(s0, s1); m.gravityModifier = grav; m.startLifetime = new ParticleSystem.MinMaxCurve(life*.7f, life); m.startSpeed = 0; m.maxParticles = 4000; m.simulationSpace = ParticleSystemSimulationSpace.World;
    var e = ps.emission; e.rateOverTime = 0; var s = ps.shape; s.enabled = false;
    if (collide) { var col = ps.collision; col.enabled = true; col.type = ParticleSystemCollisionType.World; col.bounce = .15f; col.dampen = .7f; col.lifetimeLoss = .2f; col.quality = ParticleSystemCollisionQuality.Medium; }
    var r = g.GetComponent<ParticleSystemRenderer>(); r.material = new Material(Shader.Find("Sprites/Default")); r.material.mainTexture = Dot();
    ps.Play(); return ps; }
  // ---------- procedural textures & sounds ----------
  Texture2D Dot() { return Cached("dot", 32, (x,y,u,v) => { float d=Mathf.Clamp01(1-Vector2.Distance(new Vector2(u,v),new Vector2(.5f,.5f))*2); return new Color(1,1,1,Mathf.SmoothStep(0,1,d*1.5f)); }); }
  Texture2D Bubble() { return Cached("bub", 64, (x,y,u,v) => { float d=Vector2.Distance(new Vector2(u,v),new Vector2(.5f,.5f))*2; float a = d>1?0: Mathf.Pow(d,4)*.9f+.15f; float hl = Mathf.Clamp01(1-Vector2.Distance(new Vector2(u,v),new Vector2(.35f,.68f))*7);
    Color c = Color.Lerp(new Color(.85f,.95f,1), new Color(1,.85f,1), u); c.a = Mathf.Clamp01(a+hl); if(d>.97f)c.a*=.3f; return Color.Lerp(c, Color.white, hl); }); }
  Texture2D Star4() { return Cached("star", 64, (x,y,u,v) => { float dx=Mathf.Abs(u-.5f)*2, dy=Mathf.Abs(v-.5f)*2; float a = Mathf.Clamp01(1-dx*dy*30-Mathf.Sqrt(dx*dx+dy*dy)*.9f); return new Color(1,1,1,a*a); }); }
  Texture2D Cached(string k, int s, System.Func<int,int,float,float,Color> f) { if (T.TryGetValue(k, out var t)) return t; t = new Texture2D(s,s,TextureFormat.RGBA32,false); t.wrapMode = TextureWrapMode.Clamp;
    for (int y=0;y<s;y++) for (int x=0;x<s;x++) t.SetPixel(x,y,f(x,y,(x+.5f)/s,(y+.5f)/s)); t.Apply(); T[k]=t; return t; }
  Texture2D Round(string k, Color top, Color bot, int rad = 24, Color? border = null) { return Cached(k, 64, (x,y,u,v) => {
      float cx = Mathf.Clamp(x+.5f, rad, 64-rad), cy = Mathf.Clamp(y+.5f, rad, 64-rad); float d = Vector2.Distance(new Vector2(x+.5f,y+.5f), new Vector2(cx,cy));
      Color c = Color.Lerp(bot, top, v); if (border.HasValue && d > rad-3) c = border.Value; c.a *= Mathf.Clamp01(rad - d + .5f); return c; }); }
  AudioClip Noise() { int sr=44100; var d=new float[sr*2]; float lp=0, lp2=0; for(int i=0;i<d.Length;i++){ float w=Random.value*2-1; lp = lp*.55f + w*.45f; lp2 = lp2*.97f + w*.03f; d[i]=lp*.7f + lp2*1.5f*(1+.3f*Mathf.Sin(i*.0007f)); } var a=AudioClip.Create("n",d.Length,1,sr,false); a.SetData(d,0); return a; }
  AudioClip Tone(float f, float t) { int sr=44100,n=(int)(sr*t); var d=new float[n]; for(int i=0;i<n;i++){ float e=Mathf.Exp(-i/(float)n*6); d[i]=(Mathf.Sin(2*Mathf.PI*f*i/sr)+.3f*Mathf.Sin(4*Mathf.PI*f*i/sr))*e*.35f;} var a=AudioClip.Create("t",n,1,sr,false); a.SetData(d,0); return a; }
  AudioClip Pop() { int sr=44100,n=sr/20; var d=new float[n]; for(int i=0;i<n;i++){ float t=i/(float)sr; d[i]=Mathf.Sin(2*Mathf.PI*(900-6000*t)*t)*Mathf.Exp(-t*90)*.5f; } var a=AudioClip.Create("p",n,1,sr,false); a.SetData(d,0); return a; }
  AudioClip Chord() { int sr=44100,n=(int)(sr*1.6f); var d=new float[n]; float[] f={523.25f,659.25f,783.99f,1046.5f,1318.5f};
    for(int i=0;i<n;i++){ float t=i/(float)sr, s=0; for(int k=0;k<f.Length;k++){ float st=k*.09f; if(t>st) s+=Mathf.Sin(2*Mathf.PI*f[k]*(t-st))*Mathf.Exp(-(t-st)*2.5f)*.16f; } d[i]=s; } var a=AudioClip.Create("c",n,1,sr,false); a.SetData(d,0); return a; }

  // ---------- level ----------
  void Load(int i) {
    cur = (i + levels.Length) % levels.Length; var L = levels[cur]; if (obj) Destroy(obj); cleanSpots.Clear();
    var src = Resources.Load<GameObject>(L.mesh); var mf0 = src.GetComponentInChildren<MeshFilter>(); var mesh = mf0.sharedMesh;
    obj = new GameObject(L.name); var mf = obj.AddComponent<MeshFilter>(); mf.sharedMesh = mesh; var mr = obj.AddComponent<MeshRenderer>();
    obj.transform.rotation = mf0.transform.rotation * Quaternion.identity; obj.transform.localScale = mf0.transform.lossyScale;
    obj.transform.rotation = Quaternion.Euler(L.tilt, L.yaw, 0) * obj.transform.rotation;
    var b = mr.bounds; float s = 2.3f / Mathf.Max(b.size.x, b.size.y, b.size.z); obj.transform.localScale *= s; b = mr.bounds;
    obj.transform.position += new Vector3(-b.center.x, .34f - b.min.y, -b.center.z);
    var mc = obj.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; mc.convex = false;
    var baseT = Resources.Load<Texture2D>(L.mesh + "_base"); var bp = baseT.GetPixels32(); int bw = baseT.width;
    tex = new Texture2D(N, N, TextureFormat.RGBA32, true); glossTex = new Texture2D(N, N, TextureFormat.RGBA32, true);
    basePx = new Color32[N*N]; outPx = new Color32[N*N]; glossPx = new Color32[N*N]; mud = new float[N*N]; grime = new float[N*N]; wet = new float[N*N]; used = new bool[N*N]; mudCol = new Color32[N*N];
    float ox = Random.value*50, oy = Random.value*50; totalDirt = 0;
    for (int y=0;y<N;y++) for (int x=0;x<N;x++) { int k=y*N+x; var c = bp[(y*bw/N)*bw + x*bw/N]; basePx[k]=c; used[k] = c.r+c.g+c.b > 8;
      float u=x/(float)N, v=y/(float)N; float n1 = Mathf.PerlinNoise(u*5+ox, v*5+oy), n2 = Mathf.PerlinNoise(u*22+oy, v*22+ox), n3 = Mathf.PerlinNoise(u*60, v*60+ox);
      float thick = Mathf.Clamp01((n1*1.2f + n2*.5f - .45f) * 2.2f); mud[k] = thick > .05f ? Mathf.Clamp01(thick*1.4f) : 0; grime[k] = 1;
      mudCol[k] = Color.Lerp(new Color(.30f,.20f,.11f), new Color(.52f,.40f,.24f), n2*.9f + n3*.3f) * (n3 > .78f ? .75f : 1f);
      if (used[k]) totalDirt += mud[k]*.5f + 1; }
    progress = shownProgress = 0; wetTimer = 0; Compose(); levelStart = Time.time;
    var m = M(Color.white, 1, tex); m.EnableKeyword("_METALLICGLOSSMAP"); m.SetTexture("_MetallicGlossMap", glossTex); m.SetFloat("_GlossMapScale", 1);
    var mats = new Material[mesh.subMeshCount]; for (int q=0;q<mats.Length;q++) mats[q]=m; mr.sharedMaterials = mats; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    puddle.transform.localScale = new Vector3(0,.002f,0);
    objBaseRot = obj.transform.rotation; objYaw = 0; objPitch = 0;
    objCenter = obj.GetComponent<MeshRenderer>().bounds.center;
  }
  static readonly Color32 grimeC = new Color32(96,104,70,255);
  void Compose() {
    for (int k=0;k<N*N;k++) { Color32 c = basePx[k]; float g = grime[k], md = mud[k], w = wet[k];
      if (g > 0) c = Color32.Lerp(c, grimeC, g*.72f);
      if (md > 0) { float a = Mathf.Clamp01(md*2.5f); c = Color32.Lerp(c, mudCol[k], a); }
      if (w > 0) { float dk = 1 - w*.22f; c.r=(byte)(c.r*dk); c.g=(byte)(c.g*dk); c.b=(byte)(c.b*dk); }
      outPx[k] = c; float sm = Mathf.Lerp(.82f, .08f, Mathf.Clamp01(md*3)) * (1 - g*.55f); sm = Mathf.Max(sm, w*.97f);
      glossPx[k] = new Color32(0,0,0,(byte)(sm*255)); }
    tex.SetPixels32(outPx); tex.Apply(true); glossTex.SetPixels32(glossPx); glossTex.Apply(true);
  }
  float Brush(Vector2 uv, float r, float power) {
    int cx=(int)(uv.x*N), cy=(int)(uv.y*N), R=Mathf.Max(2,(int)(r*N)); float removed=0, mudRemoved=0;
    for (int y=Mathf.Max(0,cy-R); y<Mathf.Min(N,cy+R); y++) for (int x=Mathf.Max(0,cx-R); x<Mathf.Min(N,cx+R); x++) {
      float d = ((x-cx)*(x-cx)+(y-cy)*(y-cy))/(float)(R*R); if (d>1) continue; int k=y*N+x; if (!used[k]) continue;
      float f = power * (1-d) * (1-d) * Time.deltaTime * 60f; float n = .7f + .6f*((((uint)k*2654435761u) >> 24)/255f);
      wet[k] = Mathf.Max(wet[k], Mathf.Clamp01((1-d)*1.4f));
      if (mud[k] > 0) { float o = mud[k]; mud[k] = Mathf.Max(0, o - f*n*.35f); mudRemoved += o-mud[k]; removed += (o-mud[k])*.5f; }
      else if (grime[k] > 0) { float o = grime[k]; grime[k] = Mathf.Max(0, o - f*n*.25f); removed += o-grime[k]; } }
    progress += removed / totalDirt; wetTimer = 4; return mudRemoved; }

  // ---------- loop ----------
  float lastPop, dingStep;
  RaycastHit HitObject(Ray ray) {
    var hits = Physics.RaycastAll(ray, 80f, ~0, QueryTriggerInteraction.Ignore);
    System.Array.Sort(hits, (a,b)=>a.distance.CompareTo(b.distance));
    foreach (var h in hits) if (obj != null && h.collider != null && h.collider.gameObject == obj) return h;
    return default;
  }
  bool HasHit(RaycastHit h) => h.collider != null;
  void ApplyObjectRotation() {
    if (!obj) return;
    obj.transform.rotation = Quaternion.AngleAxis(objYaw, Vector3.up) * Quaternion.AngleAxis(objPitch, Vector3.right) * objBaseRot;
    var b = obj.GetComponent<MeshRenderer>().bounds;
    obj.transform.position += objCenter - b.center;
  }
  void Update() {
    if (!autoMode) {
      // Mouse + touch: first finger cleans; two fingers / right-drag rotates object
      bool touchClean = false, touchRotate = false;
      if (Input.touchCount >= 2) {
        var t0 = Input.GetTouch(0); var t1 = Input.GetTouch(1);
        pointer = (t0.position + t1.position) * .5f;
        Vector2 d = t0.deltaPosition + t1.deltaPosition;
        // pinch zoom
        float curDist = Vector2.Distance(t0.position, t1.position);
        float prevDist = Vector2.Distance(t0.position - t0.deltaPosition, t1.position - t1.deltaPosition);
        if (prevDist > 8f) camDist = Mathf.Clamp(camDist - (curDist - prevDist) * .01f, 3.2f, 8.5f);
        objYaw -= d.x * .35f; objPitch = Mathf.Clamp(objPitch + d.y * .28f, -70f, 70f);
        ApplyObjectRotation(); touchRotate = true; rotating = true;
      } else if (Input.touchCount == 1) {
        var t0 = Input.GetTouch(0); pointer = t0.position;
        bool overUI = pointer.y < 110 || pointer.y > Screen.height - 110;
        if (!overUI && GUIUtility.hotControl == 0 && (t0.phase == TouchPhase.Began || t0.phase == TouchPhase.Moved || t0.phase == TouchPhase.Stationary))
          touchClean = true;
        rotating = false;
      } else {
        pointer = Input.mousePosition;
        bool overUI = pointer.y < 110 || pointer.y > Screen.height - 110;
        if (Input.GetMouseButtonDown(1)) { rotating = true; lastRotMouse = pointer; }
        if (Input.GetMouseButtonUp(1)) rotating = false;
        if (rotating && Input.GetMouseButton(1)) {
          Vector2 d = (Vector2)pointer - lastRotMouse; lastRotMouse = pointer;
          objYaw -= d.x * .45f; objPitch = Mathf.Clamp(objPitch + d.y * .35f, -70f, 70f);
          ApplyObjectRotation(); touchRotate = true;
        }
        touchClean = Input.GetMouseButton(0) && !rotating && !overUI && GUIUtility.hotControl == 0;
      }
      pointerDown = touchClean && !touchRotate;
    }
    if (Input.GetKeyDown(KeyCode.Alpha1)) sponge=false; if (Input.GetKeyDown(KeyCode.Alpha2)) sponge=true;

    // Camera stays FIXED during play; title screen gently orbits for polish only
    if (state == St.Title) {
      titleSpin += Time.deltaTime; camYaw = 180 + Mathf.Sin(titleSpin*.4f)*18;
      if (obj) { objYaw = titleSpin*25f; ApplyObjectRotation(); }
    }
    var tgt = new Vector3(0, 1.45f, 0);
    shake = Mathf.MoveTowards(shake, 0, Time.deltaTime*.2f);
    Vector3 jit = new Vector3(Mathf.PerlinNoise(Time.time*25,0)-.5f, Mathf.PerlinNoise(0,Time.time*25)-.5f, 0) * shake;
    cam.transform.position = tgt + Quaternion.Euler(camPitch, camYaw, 0) * new Vector3(0,0,-camDist);
    cam.transform.LookAt(tgt); cam.transform.position += cam.transform.rotation * jit;
    // scroll still zooms camera in/out (viewpoint distance only, no orbit)
    if (!autoMode) camDist = Mathf.Clamp(camDist - Input.mouseScrollDelta.y*.4f, 3.2f, 8.5f);

    bool playing = state == St.Play; nozzle.SetActive(playing && !sponge); spongeObj.SetActive(playing && sponge);
    bool cleaning = false; jet.enabled = false;
    if (playing) {
      var ray = cam.ScreenPointToRay(pointer);
      var hit = HitObject(ray); bool hitObj = HasHit(hit);
      Vector3 aim = hitObj ? hit.point : ray.GetPoint(4.2f);
      Vector3 n = hitObj ? hit.normal.normalized : -ray.direction.normalized;
      // Tools sit ON the surface (along normal), never inside
      Vector3 want; Quaternion wantR;
      if (!sponge) {
        // gun hangs off to the side of the hit, tip points at surface
        want = aim + n*.18f - ray.direction*.95f + cam.transform.right*.32f - cam.transform.up*.12f;
        wantR = Quaternion.LookRotation((aim - want).normalized, Vector3.up);
      } else {
        // half sponge thickness (~0.07) + margin so block rests on surface
        want = aim + n * .16f;
        Vector3 up = Vector3.ProjectOnPlane(cam.transform.up, n); if (up.sqrMagnitude < .001f) up = Vector3.ProjectOnPlane(Vector3.up, n);
        wantR = Quaternion.LookRotation(up.normalized, n);
      }
      float bob = pointerDown && hitObj ? Mathf.Sin(Time.time*(sponge?18:36))*(sponge?.015f:.006f) : Mathf.Sin(Time.time*3)*.015f;
      var t = sponge ? spongeObj.transform : nozzle.transform; var prev = t.position;
      t.position = Vector3.SmoothDamp(t.position, want + n*bob, ref toolVel, .04f);
      t.rotation = Quaternion.Slerp(t.rotation, wantR, .35f);
      if (sponge) { var v = (t.position-prev)/Mathf.Max(Time.deltaTime,.001f); t.Rotate(Vector3.forward, Mathf.Clamp(Vector3.Dot(v, cam.transform.right)*-5, -18, 18), Space.Self); }

      if (pointerDown) {
        // Washer: also try a ray from nozzle tip toward the aim so cleaning works even if camera ray grazes
        if (!hitObj && !sponge) {
          var tip0 = nozzle.transform.TransformPoint(0,0,.33f);
          hit = HitObject(new Ray(tip0, (aim - tip0).normalized));
          hitObj = HasHit(hit);
          if (hitObj) { aim = hit.point; n = hit.normal.normalized; }
        }
        if (hitObj) {
          cleaning = true;
          Vector2 uv = hit.textureCoord;
          if (uv.x <= 0 && uv.y <= 0) {
            // fallback UV from barycentric if MeshCollider gave zero
            uv = hit.textureCoord2;
          }
          // larger brush + stronger power for washer
          float mr = Brush(uv, sponge ? .085f : .14f, sponge ? 1.4f : 3.2f);
          var ep = new ParticleSystem.EmitParams();
          Vector3 surface = hit.point + n * .04f;
          if (!sponge) {
            var tip = nozzle.transform.TransformPoint(0,0,.33f);
            // visible multi-segment water beam
            jet.enabled = true;
            for (int i=0;i<jet.positionCount;i++) {
              float u = i/(float)(jet.positionCount-1);
              Vector3 p = Vector3.Lerp(tip, surface, u);
              // slight droop for realism
              p += Vector3.down * (u*u*.08f);
              jet.SetPosition(i, p);
            }
            shake = Mathf.Max(shake, .04f);
            Vector3 dir = (surface - tip).normalized;
            for (int i=0;i<10;i++){ ep.position = Vector3.Lerp(tip, surface, Random.value); ep.velocity = dir*(8+Random.value*4) + Random.insideUnitSphere*.6f; ep.startLifetime=.1f; ep.startSize=Random.Range(.02f,.05f); water.Emit(ep,1); }
            for (int i=0;i<10;i++){ ep.position = surface; ep.velocity = Vector3.Reflect(dir, n)*3f + Random.insideUnitSphere*2.5f + Vector3.up*1.2f; ep.startLifetime=.5f; water.Emit(ep,1); }
            if (Random.value < .7f) { ep.position = surface; ep.velocity = n*.5f + Random.insideUnitSphere*.4f; mist.Emit(ep,1); }
          } else if (Random.value < .65f) {
            ep.position = surface + Random.insideUnitSphere*.05f; ep.velocity = n*.06f; ep.startSize = Random.Range(.05f,.16f); foam.Emit(ep,1);
          }
          int chunks = Mathf.Min(8, (int)(mr*.05f) + (Random.value < mr*.012f ? 1 : 0));
          for (int i=0;i<chunks;i++){ ep.position = surface; ep.velocity = (sponge ? Vector3.down*.3f + n*.5f : n*2.5f + Random.insideUnitSphere*1.8f); ep.startSize = Random.Range(.03f,.1f); mudPs.Emit(ep,1); }
          if (mr < 1 && Random.value < .14f) { ep.position = surface + Random.insideUnitSphere*.12f; ep.velocity = Vector3.zero; glint.Emit(ep,1); if (cleanSpots.Count < 200) cleanSpots.Add(surface); }
          float ps = Mathf.Min(1.9f, puddle.transform.localScale.x + Time.deltaTime*.14f); puddle.transform.localScale = new Vector3(ps,.002f,ps); puddleMat.color = new Color(.6f,.8f,.95f, Mathf.Min(.55f, ps*.35f));
          if (progress*10 >= dingStep+1) { dingStep = Mathf.Floor(progress*10); sfx.pitch = 1 + dingStep*.06f; sfx.PlayOneShot(dingClip,.5f); sfx.pitch = 1; }
          if (progress >= .95f) StartCoroutine(WinSeq());
        }
      }
      if (foam.particleCount > 0 && Time.time - lastPop > .12f && Random.value < .3f) { lastPop = Time.time; sfx.PlayOneShot(popClip, .25f); }
      if (cleanSpots.Count > 0 && Random.value < .06f) { var ep2 = new ParticleSystem.EmitParams(); ep2.position = cleanSpots[Random.Range(0,cleanSpots.Count)]; ep2.velocity=Vector3.zero; glint.Emit(ep2,1); }
    }
    if (wetTimer > 0 && obj) { wetTimer -= Time.deltaTime; float dec = Time.deltaTime*.3f; for (int k=0;k<wet.Length;k++) if (wet[k]>0) wet[k]=Mathf.Max(0,wet[k]-dec); Compose(); }
    hiss.volume = Mathf.MoveTowards(hiss.volume, cleaning ? (sponge?.18f:.4f) : 0, Time.deltaTime*3); hiss.pitch = sponge ? .55f : 1.15f + Mathf.Sin(Time.time*7)*.03f;
    shownProgress = Mathf.Lerp(shownProgress, progress, Time.deltaTime*6);
    if (state == St.Win && obj) { objYaw += 60*Time.deltaTime; ApplyObjectRotation(); }
  }
  float shineT = -1, winScale;
  IEnumerator WinSeq() {
    state = St.Win; progress = 1; hiss.volume = 0; jet.enabled = false;
    float time = Time.time - levelStart; stars = time < 50 ? 3 : time < 100 ? 2 : 1; int best = PlayerPrefs.GetInt("stars"+cur,0); if (stars > best) PlayerPrefs.SetInt("stars"+cur, stars);
    for (int k=0;k<mud.Length;k++){ mud[k]=0; grime[k]=0; wet[k]=Mathf.Max(wet[k],.6f);} wetTimer = 3; Compose();
    sfx.PlayOneShot(chord, .9f); shineT = 0; var b = obj.GetComponent<MeshRenderer>().bounds; var ep = new ParticleSystem.EmitParams();
    for (int i=0;i<40;i++){ ep.position = b.center + Vector3.Scale(Random.insideUnitSphere, b.extents); glint.Emit(ep,1); }
    for (int i=0;i<260;i++){ ep.position = new Vector3(Random.Range(-.5f,.5f), 2f, Random.Range(-.5f,.5f)); ep.velocity = new Vector3(Random.Range(-4f,4f), Random.Range(4f,8f), Random.Range(-3f,4f)); ep.startColor = Color.HSVToRGB(Random.value,.65f,1); ep.startSize = Random.Range(.06f,.13f); confetti.Emit(ep,1); }
    Vector3 s0 = obj.transform.localScale; for (float t=0;t<.5f;t+=Time.deltaTime){ float k = 1 + Mathf.Sin(t/.5f*Mathf.PI)*.12f; obj.transform.localScale = s0*k; yield return null; } obj.transform.localScale = s0;
    winT = Time.time;
  }

  // ---------- UI ----------
  GUIStyle St_(int size, Color c, FontStyle fs = FontStyle.Bold, TextAnchor a = TextAnchor.MiddleCenter) { var s = new GUIStyle(); s.font = font; s.fontSize = size; s.fontStyle = fs; s.alignment = a; s.normal.textColor = c; s.richText = true; return s; }
  void Shadowed(Rect r, string t, GUIStyle s, Color sh, float off = 3) { var c = s.normal.textColor; s.normal.textColor = sh; GUI.Label(new Rect(r.x, r.y+off, r.width, r.height), t, s); s.normal.textColor = c; GUI.Label(r, t, s); }
  void Panel(Rect r, Texture2D t) { var s = new GUIStyle(); s.normal.background = t; s.border = new RectOffset(26,26,26,26); GUI.Box(r, GUIContent.none, s); }
  bool Btn(Rect r, string text, Texture2D bg, Texture2D bgHover, int size = 24, Texture2D icon = null) {
    bool hov = r.Contains(Event.current.mousePosition); var s = new GUIStyle(); s.normal.background = hov ? bgHover : bg; s.border = new RectOffset(26,26,26,26);
    var shadow = Round("btnsh", new Color(0,0,0,.18f), new Color(0,0,0,.18f)); Panel(new Rect(r.x, r.y+5, r.width, r.height), shadow);
    bool click = GUI.Button(hov ? new Rect(r.x, r.y-2, r.width, r.height) : r, GUIContent.none, s);
    var rr = hov ? new Rect(r.x, r.y-2, r.width, r.height) : r;
    if (icon) { GUI.DrawTexture(new Rect(rr.x+16, rr.y+(rr.height-36)/2, 36, 36), icon); rr.x += 30; rr.width -= 30; }
    GUI.Label(rr, text, St_(size, Color.white)); return click; }
  Texture2D DropIcon() { return Cached("drop", 64, (x,y,u,v) => { float cx=.5f, cy=.38f; float d = Vector2.Distance(new Vector2(u,v), new Vector2(cx,cy)); bool tip = v>cy && Mathf.Abs(u-cx) < (1-(v-cy)/.55f)*.28f; float a = (d<.28f||tip)?1:0;
    Color c = Color.Lerp(new Color(.3f,.7f,1f), new Color(.75f,.95f,1f), v); if (Vector2.Distance(new Vector2(u,v), new Vector2(.42f,.32f))<.07f) c=Color.white; c.a=a; return c; }); }
  Texture2D SpongeIcon() { return Cached("spg", 64, (x,y,u,v) => { bool body = u>.12f&&u<.88f&&v>.25f&&v<.62f; bool top = u>.12f&&u<.88f&&v>=.62f&&v<.75f; Color c = top?new Color(.3f,.75f,.35f):new Color(1f,.85f,.3f); if (body && Mathf.PerlinNoise(u*20,v*20)>.7f) c*=.85f; c.a=(body||top)?1:0; return c; }); }
  Texture2D StarIcon(bool on) { return Cached(on?"st1":"st0", 96, (x,y,u,v) => { float px=u-.5f, py=.5f-v; float ang = Mathf.Atan2(py,px), r = Mathf.Sqrt(px*px+py*py); float k = Mathf.Cos(Mathf.PI/5)/Mathf.Cos(Mathf.Repeat(ang+Mathf.PI/2, 2*Mathf.PI/5) - Mathf.PI/5);
    float lim = .48f * Mathf.Lerp(.45f, 1, Mathf.Pow(Mathf.Abs(Mathf.Cos((ang+Mathf.PI/2)*2.5f)),3)); float a = Mathf.Clamp01((lim - r)*80);
    Color c = on ? Color.Lerp(new Color(1f,.65f,.1f), new Color(1f,.92f,.35f), v) : new Color(.8f,.82f,.86f); if (r > lim - .04f) c = on ? new Color(.85f,.45f,.05f) : new Color(.65f,.67f,.72f); c.a = a; return c; }); }
  void OnGUI() {
    float w = Screen.width, h = Screen.height; float ui = Mathf.Clamp(h/800f, .7f, 1.6f); GUI.matrix = Matrix4x4.Scale(new Vector3(ui,ui,1)); w/=ui; h/=ui;
    var teal = Round("teal", new Color(.35f,.85f,.8f), new Color(.15f,.65f,.65f)); var tealH = Round("tealH", new Color(.45f,.92f,.86f), new Color(.2f,.72f,.7f));
    var orange = Round("or", new Color(1f,.72f,.35f), new Color(.98f,.5f,.25f)); var orangeH = Round("orH", new Color(1f,.8f,.45f), new Color(1f,.6f,.3f));
    var gray = Round("gr", new Color(.75f,.8f,.85f), new Color(.6f,.66f,.72f)); var white = Round("wh", new Color(1,1,1,.97f), new Color(.95f,.98f,1f,.97f), 28, new Color(.75f,.9f,.95f));
    var dark = Color.Lerp(new Color(.1f,.25f,.35f), Color.black, .1f);
    if (state == St.Title) {
      GUI.DrawTexture(new Rect(0,0,w,h), Round("veil", new Color(.85f,.95f,1f,.35f), new Color(.85f,.95f,1f,.75f), 1));
      float bob = Mathf.Sin(Time.time*2)*6;
      Shadowed(new Rect(0, 70+bob, w, 100), "洗洗更解压", St_(84, Color.white), new Color(.15f,.6f,.62f), 6);
      GUI.Label(new Rect(0, 175, w, 40), "冲掉泥巴 · 搓出泡泡 · 洗到闪闪发亮", St_(24, dark, FontStyle.Normal));
      float cw = 168, gap = 18, tot = levels.Length*cw + (levels.Length-1)*gap, x0 = (w-tot)/2, y0 = h*.5f;
      for (int i=0;i<levels.Length;i++) { var r = new Rect(x0 + i*(cw+gap), y0, cw, 190); bool hov = r.Contains(Event.current.mousePosition); if (hov) r.y -= 6;
        Panel(new Rect(r.x, r.y+6, r.width, r.height), Round("btnsh", new Color(0,0,0,.18f), new Color(0,0,0,.18f))); Panel(r, white);
        GUI.Label(new Rect(r.x, r.y+16, cw, 50), (i+1).ToString(), St_(46, new Color(.2f,.65f,.65f))); GUI.Label(new Rect(r.x+6, r.y+74, cw-12, 50), levels[i].name, St_(20, dark));
        int st = PlayerPrefs.GetInt("stars"+i,0); for (int s=0;s<3;s++) GUI.DrawTexture(new Rect(r.x+cw/2-54+s*36, r.y+132, 32, 32), StarIcon(s<st));
        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { StartLevel(i); } }
      if (Btn(new Rect(w/2-130, y0+230, 260, 70), "开始清洗", orange, orangeH, 30)) StartLevel(0);
      GUI.Label(new Rect(0, h-40, w, 30), "单指拖动清洁 · 双指旋转/缩放 · 点按钮切换工具", St_(16, dark, FontStyle.Normal));
      return;
    }
    // top bar
    var top = new Rect(w/2-260, 14, 520, 92); Panel(top, white);
    if (Btn(new Rect(top.x+12, top.y+18, 56, 56), "≡", gray, gray, 28)) { state = St.Title; hiss.volume = 0; }
    GUI.Label(new Rect(top.x+70, top.y+8, top.width-140, 34), $"第{cur+1}关 · {levels[cur].name}", St_(24, dark));
    var bar = new Rect(top.x+86, top.y+50, top.width-190, 26); GUI.DrawTexture(bar, Round("barbg", new Color(.85f,.9f,.93f), new Color(.92f,.95f,.97f), 13));
    float pw = Mathf.Max(26, bar.width*shownProgress); var fillR = new Rect(bar.x, bar.y, pw, bar.height);
    GUI.DrawTexture(fillR, Round("barfill", new Color(.45f,.95f,.75f), new Color(.2f,.7f,.9f), 13));
    float sx = Mathf.Repeat(Time.time*.7f, 1.4f)-.2f; GUI.BeginGroup(fillR); GUI.DrawTexture(new Rect(sx*fillR.width - 30, 0, 30, bar.height), Round("shine", new Color(1,1,1,.55f), new Color(1,1,1,.25f), 12)); GUI.EndGroup();
    GUI.DrawTexture(new Rect(bar.x+2, bar.y+3, pw-4, 7), Round("barhl", new Color(1,1,1,.45f), new Color(1,1,1,.2f), 3));
    GUI.Label(new Rect(bar.xMax+6, bar.y-4, 80, 34), $"{Mathf.RoundToInt(shownProgress*100)}%", St_(24, new Color(.15f,.6f,.62f)));
    for (int i=1;i<=3;i++){ float th = i==1?.5f:i==2?.8f:.95f; GUI.DrawTexture(new Rect(bar.x + bar.width*th - 11, bar.y+2, 22, 22), StarIcon(shownProgress>=th)); }
    // tool selector
    if (state == St.Play) {
      var a = new Rect(w/2-230, h-96, 220, 74); var b2 = new Rect(w/2+10, h-96, 220, 74);
      if (Btn(a, "高压水枪", !sponge?teal:gray, tealH, 24, DropIcon())) sponge = false;
      if (Btn(b2, "泡泡海绵", sponge?orange:gray, orangeH, 24, SpongeIcon())) sponge = true;
      GUI.Label(new Rect(0, h-122, w, 22), "单指拖动清洁 · 双指旋转/缩放", St_(15, dark, FontStyle.Normal));
    }
    if (state == St.Win) {
      if (shineT >= 0) { shineT += Time.deltaTime; float p = shineT/.9f; if (p < 1.2f) { var mtx = GUI.matrix; GUIUtility.RotateAroundPivot(25, new Vector2(w/2, h/2)); GUI.DrawTexture(new Rect(-w*.3f + p*w*1.6f - 120, -h, 160, h*3), Round("sweep", new Color(1,1,1,.0f), new Color(1,1,1,.55f), 2)); GUI.matrix = mtx; } }
      float t = Time.time - winT; if (winT <= 0 || t < 0) return;
      float pop = Mathf.Min(1, t/.35f); float sc = pop < 1 ? Mathf.Lerp(.5f, 1.08f, pop) : Mathf.Lerp(1.08f, 1, Mathf.Min(1,(t-.35f)/.15f));
      var pr = new Rect(w-460, h-340, 420, 300); var mtx2 = GUI.matrix; GUIUtility.ScaleAroundPivot(Vector2.one*sc, pr.center);
      Panel(new Rect(pr.x, pr.y+8, pr.width, pr.height), Round("btnsh", new Color(0,0,0,.18f), new Color(0,0,0,.18f))); Panel(pr, white);
      var rib = new Rect(pr.x+40, pr.y-38, pr.width-80, 76); Panel(rib, Round("rib", new Color(1f,.55f,.45f), new Color(.93f,.35f,.4f)));
      Shadowed(rib, "闪闪发亮！", St_(40, Color.white), new Color(.6f,.15f,.2f), 3);
      for (int s=0;s<3;s++){ float st = Mathf.Clamp01((t - .3f - s*.25f)/.25f); float ss = st<1 ? Mathf.Lerp(0,1.3f,st) : 1; float sz = (s==1?90:72)*ss; float cx = pr.center.x + (s-1)*100, cy = pr.y + 95 + (s==1?-8:0);
        GUI.DrawTexture(new Rect(cx-sz/2, cy-sz/2, sz, sz), StarIcon(s < stars)); }
      float secs = (winT - levelStart); GUI.Label(new Rect(pr.x, pr.y+145, pr.width, 34), $"用时 {secs:0} 秒   干净度 100%", St_(22, dark, FontStyle.Normal));
      if (t > .8f) { if (Btn(new Rect(pr.x+30, pr.y+205, 160, 64), "选关", gray, gray, 24)) state = St.Title;
        if (Btn(new Rect(pr.x+210, pr.y+205, 180, 64), cur+1<levels.Length?"下一关 ▶":"再玩一轮", orange, orangeH, 24)) StartLevel(cur+1); }
      GUI.matrix = mtx2;
    }
  }
  void StartLevel(int i) { Load(i); state = St.Play; dingStep = 0; winT = 0; shineT = -1; camYaw = 180; camPitch = 18; objYaw = 0; objPitch = 0; ApplyObjectRotation(); rotating = false; }

  // ---------- automated screenshots ----------

}
