using UnityEngine;
using System.Collections;
using System.Collections.Generic;
// 洗洗更解压 v5 — multi-theme, richer models, theme/level select UI
public class CleanGame : MonoBehaviour {
  class Lvl { public string name, mesh; public float tilt, yaw; public Lvl(string n,string m,float t=0,float y=0){name=n;mesh=m;tilt=t;yaw=y;} }
  class Theme {
    public string id, name, blurb; public Color accent, sky, fog;
    public Lvl[] levels;
    public Theme(string id,string name,string blurb,Color accent,Color sky,Color fog,Lvl[] levels){
      this.id=id; this.name=name; this.blurb=blurb; this.accent=accent; this.sky=sky; this.fog=fog; this.levels=levels;
    }
  }
  Theme[] themes;
  const int N = 512;
  enum St { ThemeSelect, LevelSelect, Play, Win }
  enum Mode { Clean, Rotate } // single-finger drag only does active mode — never both
  St state = St.ThemeSelect;
  Mode mode = Mode.Clean;
  int themeIdx, cur; GameObject obj; Texture2D tex, glossTex; Color32[] basePx, outPx, glossPx; float[] mud, grime, wet; bool[] used; Color32[] mudCol;
  float totalDirt, progress, shownProgress, levelStart, winT, wetTimer, shake; int stars;
  bool sponge; Camera cam; float camYaw=180, camPitch=18, camDist=5.2f, titleSpin;
  float objYaw, objPitch; Quaternion objBaseRot; Vector3 objCenter;
  ParticleSystem water, mudPs, foam, confetti, glint, mist; LineRenderer jet;
  AudioSource hiss, sfx; GameObject puddle, nozzle, spongeObj, roomRoot; Material puddleMat;
  Light sun, fill, rim, windowLight;
  Vector2 pointer; bool pointerDown, autoMode, rotating; Vector3 toolVel; Vector2 lastRotMouse; List<Vector3> cleanSpots = new List<Vector3>();
  Font font; Dictionary<string,Texture2D> T = new Dictionary<string,Texture2D>();
  AudioClip popClip, dingClip, chord;
  bool autoShot; string shotDir;

  void Awake() {
    themes = new[] {
      new Theme("bath","浴室","浴缸边的解压清洁",
        new Color(.35f,.78f,.82f), new Color(.72f,.86f,.94f), new Color(.78f,.88f,.94f), new[]{
          new Lvl("油腻的青花盘","plate",55,0),
          new Lvl("小黄鸭","duck",0,-60),
          new Lvl("香香肥皂","soap",0,25),
          new Lvl("洗发水瓶","shampoo",0,15),
          new Lvl("牙刷杯","cup",0,10),
          new Lvl("洗澡丝瓜络","loofah",0,30),
        }),
      new Theme("kitchen","厨房","油烟与餐具的闪亮大作战",
        new Color(.95f,.55f,.3f), new Color(.95f,.88f,.78f), new Color(.92f,.85f,.75f), new[]{
          new Lvl("平底锅","pan",20,30),
          new Lvl("红釉茶壶","teapot",0,20),
          new Lvl("条纹马克杯","mug",0,15),
          new Lvl("青瓷大碗","bowl",0,0),
          new Lvl("砧板","cuttingboard",70,0),
          new Lvl("不锈钢水壶","kettle",0,25),
        }),
      new Theme("garage","车库","机油泥土统统冲掉",
        new Color(.9f,.7f,.25f), new Color(.55f,.58f,.62f), new Color(.5f,.52f,.55f), new[]{
          new Lvl("红色工具箱","toolbox",0,20),
          new Lvl("脏轮胎","tire",90,0),
          new Lvl("绿色油桶","jerrycan",0,15),
          new Lvl("安全头盔","helmet",0,-20),
          new Lvl("机油壶","oilcan",0,10),
          new Lvl("活动扳手","wrench",0,45),
        }),
      new Theme("garden","花园","园艺好物洗得亮晶晶",
        new Color(.45f,.75f,.4f), new Color(.75f,.88f,.7f), new Color(.7f,.82f,.65f), new[]{
          new Lvl("鲜花花盆","flowerpot",0,20),
          new Lvl("洒水壶","wateringcan",0,25),
          new Lvl("花园小矮人","gnome",0,15),
          new Lvl("小鸟屋","birdhouse",0,20),
          new Lvl("花园青蛙","frog",0,-30),
          new Lvl("手推车","wheelbarrow",0,35),
        }),
    };
  }

  void Start() {
    Application.targetFrameRate = 60; QualitySettings.shadowDistance = 25; QualitySettings.antiAliasing = 4;
    font = Resources.Load<Font>("NotoSansSC");
    cam = Camera.main; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = themes[0].sky; cam.fieldOfView = 40; cam.allowMSAA = true;
    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
    RenderSettings.ambientSkyColor = new Color(1f,.97f,.92f); RenderSettings.ambientEquatorColor = new Color(.78f,.82f,.86f); RenderSettings.ambientGroundColor = new Color(.45f,.4f,.38f);
    RenderSettings.fog = true; RenderSettings.fogColor = themes[0].fog; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 10; RenderSettings.fogEndDistance = 28;
    QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.High; QualitySettings.shadowDistance = 28;
    foreach (var l0 in FindObjectsOfType<Light>()) Destroy(l0.gameObject);
    sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1f,.93f,.82f); sun.intensity = 1.35f; sun.shadows = LightShadows.Soft; sun.shadowStrength=.72f; sun.shadowBias=.04f; sun.transform.rotation = Quaternion.Euler(42,-48,0);
    fill = new GameObject("Fill").AddComponent<Light>(); fill.type = LightType.Point; fill.color = new Color(.55f,.72f,.95f); fill.intensity = 1.6f; fill.range = 14; fill.transform.position = new Vector3(-3.2f,3.4f,2.8f);
    rim = new GameObject("Rim").AddComponent<Light>(); rim.type = LightType.Point; rim.color = new Color(1f,.78f,.92f); rim.intensity = 1.8f; rim.range = 10; rim.transform.position = new Vector3(2.8f,2.8f,-2.2f);
    windowLight = new GameObject("WindowLight").AddComponent<Light>(); windowLight.type = LightType.Spot; windowLight.color = new Color(1f,.96f,.88f); windowLight.intensity = 4.5f; windowLight.range = 12; windowLight.spotAngle = 55; windowLight.innerSpotAngle = 25; windowLight.shadows = LightShadows.Soft; windowLight.transform.position = new Vector3(-2.6f,3.7f,-4.0f); windowLight.transform.rotation = Quaternion.Euler(25,0,0);
    BuildRoom(0);
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
    themeIdx = 0; Load(0); state = St.ThemeSelect;
#if UNITY_STANDALONE || UNITY_EDITOR
    foreach (var a in System.Environment.GetCommandLineArgs()) {
      if (a == "-autoscreenshot") autoShot = true;
      if (a.StartsWith("-shotdir=")) shotDir = a.Substring(9);
    }
    if (autoShot) {
      if (string.IsNullOrEmpty(shotDir)) shotDir = System.IO.Path.Combine(Application.dataPath, "..", "screenshots");
      System.IO.Directory.CreateDirectory(shotDir);
      StartCoroutine(AutoScreenshotsV6());
    }
#endif
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
        if (!g && cx>cell-18 && cy>cell-18) c = Color.Lerp(c, Color.white, .12f);
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
  Texture2D OutsideSky(Color top, Color bot) {
    int S=256; var t=new Texture2D(S,S); var px=new Color[S*S];
    for(int y=0;y<S;y++) for(int x=0;x<S;x++){
      float v=y/(float)S,u=x/(float)S;
      Color sky=Color.Lerp(bot, top, v);
      float cloud=Mathf.PerlinNoise(u*4+1,v*2.5f); if(cloud>.55f) sky=Color.Lerp(sky,Color.white,(cloud-.55f)*2f);
      float hill=Mathf.PerlinNoise(u*3,2)*.18f+.08f; if(v<hill) sky=Color.Lerp(new Color(.25f,.45f,.28f), sky, v/hill);
      sky.a=1; px[y*S+x]=sky;
    }
    t.SetPixels(px); t.Apply(); return t;
  }
  Texture2D BrickTex(Color a, Color b) {
    int S=512; var t=new Texture2D(S,S,TextureFormat.RGBA32,true); var px=new Color[S*S]; int bw=S/6, bh=S/12;
    for(int y=0;y<S;y++) for(int x=0;x<S;x++){
      int row=y/bh; int off=(row%2)*(bw/2); int cx=(x+off)%bw, cy=y%bh;
      bool g=cx<3||cy<3; float n=Mathf.PerlinNoise(x*.05f,y*.05f);
      Color c=g?new Color(.35f,.32f,.3f):Color.Lerp(a,b,n); c.a=1; px[y*S+x]=c;
    }
    t.SetPixels(px); t.Apply(true); t.wrapMode=TextureWrapMode.Repeat; return t;
  }
  Texture2D Concrete(Color a) {
    int S=512; var t=new Texture2D(S,S); var px=new Color[S*S];
    for(int y=0;y<S;y++) for(int x=0;x<S;x++){
      float n=Mathf.PerlinNoise(x*.04f,y*.04f)*.15f+Mathf.PerlinNoise(x*.2f,y*.2f)*.08f;
      Color c=a*(.9f+n); c.a=1; px[y*S+x]=c;
    }
    t.SetPixels(px); t.Apply(true); t.wrapMode=TextureWrapMode.Repeat; return t;
  }
  Texture2D GrassTex() {
    int S=512; var t=new Texture2D(S,S); var px=new Color[S*S];
    for(int y=0;y<S;y++) for(int x=0;x<S;x++){
      float n=Mathf.PerlinNoise(x*.08f,y*.08f), n2=Mathf.PerlinNoise(x*.3f+3,y*.3f);
      Color c=Color.Lerp(new Color(.28f,.55f,.22f), new Color(.45f,.72f,.3f), n);
      if(n2>.7f) c=Color.Lerp(c,new Color(.7f,.8f,.35f),(n2-.7f)*2);
      c.a=1; px[y*S+x]=c;
    }
    t.SetPixels(px); t.Apply(true); t.wrapMode=TextureWrapMode.Repeat; return t;
  }
  Material Emit(Color c, float strength){ var m=M(c,.2f); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c*strength); return m; }

  void ClearRoom() { if (roomRoot) Destroy(roomRoot); }
  void ApplyThemeLights(int ti) {
    var th = themes[ti];
    cam.backgroundColor = th.sky; RenderSettings.fogColor = th.fog;
    if (ti==0) { // bath — cool daylight
      sun.color = new Color(1f,.93f,.82f); sun.intensity = 1.35f; sun.transform.rotation = Quaternion.Euler(42,-48,0);
      fill.color = new Color(.55f,.72f,.95f); fill.intensity = 1.6f; rim.color = new Color(1f,.78f,.92f); rim.intensity = 1.8f;
      windowLight.intensity = 4.5f; windowLight.color = new Color(1f,.96f,.88f);
      RenderSettings.ambientSkyColor = new Color(1f,.97f,.92f);
    } else if (ti==1) { // kitchen — warm
      sun.color = new Color(1f,.9f,.75f); sun.intensity = 1.45f; sun.transform.rotation = Quaternion.Euler(50,-30,0);
      fill.color = new Color(1f,.75f,.55f); fill.intensity = 1.8f; rim.color = new Color(.95f,.85f,.6f); rim.intensity = 1.5f;
      windowLight.intensity = 3.5f; windowLight.color = new Color(1f,.92f,.8f);
      RenderSettings.ambientSkyColor = new Color(1f,.95f,.88f);
    } else if (ti==2) { // garage — cooler industrial
      sun.color = new Color(.9f,.92f,.95f); sun.intensity = 1.15f; sun.transform.rotation = Quaternion.Euler(55,-70,0);
      fill.color = new Color(.7f,.75f,.4f); fill.intensity = 2.0f; rim.color = new Color(1f,.55f,.2f); rim.intensity = 1.4f;
      windowLight.intensity = 2.2f; windowLight.color = new Color(.85f,.9f,1f);
      RenderSettings.ambientSkyColor = new Color(.75f,.78f,.82f);
    } else { // garden — sunny green
      sun.color = new Color(1f,.97f,.85f); sun.intensity = 1.55f; sun.transform.rotation = Quaternion.Euler(38,-20,0);
      fill.color = new Color(.55f,.85f,.55f); fill.intensity = 1.5f; rim.color = new Color(1f,.85f,.55f); rim.intensity = 1.6f;
      windowLight.intensity = 5f; windowLight.color = new Color(1f,.98f,.9f);
      RenderSettings.ambientSkyColor = new Color(.95f,1f,.92f);
    }
  }

  void BuildRoom(int ti) {
    ClearRoom(); ApplyThemeLights(ti);
    roomRoot = new GameObject("Room_"+themes[ti].id);
    var room = roomRoot.transform;
    if (ti==0) BuildBathroom(room);
    else if (ti==1) BuildKitchen(room);
    else if (ti==2) BuildGarage(room);
    else BuildGarden(room);
    puddleMat = M(new Color(.55f,.78f,.92f,.0f), .98f); puddleMat.SetFloat("_Mode",3); puddleMat.SetInt("_SrcBlend",5); puddleMat.SetInt("_DstBlend",10); puddleMat.SetInt("_ZWrite",0); puddleMat.EnableKeyword("_ALPHABLEND_ON"); puddleMat.renderQueue=3000;
    puddle = Prim(PrimitiveType.Cylinder, new Vector3(0,.325f,0), new Vector3(0,.002f,0), puddleMat, room);
  }

  void BuildBathroom(Transform room) {
    var floorT = Tiles(new Color(.78f,.58f,.38f), new Color(.42f,.28f,.16f), 6, true);
    var floorM = M(Color.white, .55f, floorT); floorM.mainTextureScale = new Vector2(3.5f,3.5f);
    Prim(PrimitiveType.Plane, Vector3.zero, Vector3.one*3.2f, floorM, room, true);
    var wallT = Tiles(new Color(.78f,.90f,.92f), new Color(.94f,.96f,.97f), 7, false);
    var wm = M(Color.white, .82f, wallT); wm.mainTextureScale = new Vector2(5.5f, 2.8f);
    Prim(PrimitiveType.Cube, new Vector3(0,3.1f,-4.55f), new Vector3(16.5f,6.4f,.22f), wm, room);
    var wmS = M(Color.white, .82f, wallT); wmS.mainTextureScale = new Vector2(3.8f, 2.8f);
    Prim(PrimitiveType.Cube, new Vector3(-6.2f,3.1f,0), new Vector3(.22f,6.4f,10.5f), wmS, room);
    Prim(PrimitiveType.Cube, new Vector3(6.2f,3.1f,0), new Vector3(.22f,6.4f,10.5f), wmS, room);
    var marbleT = Marble(new Color(.93f,.93f,.91f), new Color(.55f,.55f,.58f));
    var marble = M(Color.white, .88f, marbleT); marble.mainTextureScale = new Vector2(4,1);
    Prim(PrimitiveType.Cube, new Vector3(0,.55f,-4.42f), new Vector3(16.3f,1.1f,.18f), marble, room);
    Prim(PrimitiveType.Cube, new Vector3(-6.05f,.55f,0), new Vector3(.18f,1.1f,10.2f), marble, room);
    Prim(PrimitiveType.Cube, new Vector3(6.05f,.55f,0), new Vector3(.18f,1.1f,10.2f), marble, room);
    var trim = M(new Color(.98f,.97f,.94f), .75f);
    Prim(PrimitiveType.Cube, new Vector3(0,1.12f,-4.32f), new Vector3(16.2f,.06f,.12f), trim, room);
    Prim(PrimitiveType.Cube, new Vector3(0,6.2f,0), new Vector3(16.5f,.15f,10.5f), M(new Color(.97f,.96f,.94f), .35f), room);
    Prim(PrimitiveType.Cube, new Vector3(0,6.05f,-3.8f), new Vector3(10f,.04f,.15f), Emit(new Color(1f,.95f,.85f), 1.4f), room);
    var chrome = M(new Color(.82f,.84f,.88f), .95f, null, 1f);
    Prim(PrimitiveType.Cube, new Vector3(-2.55f,3.55f,-4.48f), new Vector3(2.7f,2.1f,.08f), chrome, room);
    var skyM = M(Color.white, .15f, OutsideSky(new Color(.95f,.97f,1f), new Color(.55f,.78f,.95f))); skyM.EnableKeyword("_EMISSION"); skyM.SetColor("_EmissionColor", new Color(.85f,.92f,1f)*0.55f);
    Prim(PrimitiveType.Quad, new Vector3(-2.55f,3.55f,-4.44f), new Vector3(2.4f,1.8f,1), skyM, room);
    var tubM = M(new Color(.97f,.97f,.98f), .92f);
    var tub = Prim(PrimitiveType.Capsule, new Vector3(3.5f,.62f,-3.35f), new Vector3(1.15f,1.75f,1.15f), tubM, room); tub.transform.rotation = Quaternion.Euler(0,0,90);
    Prim(PrimitiveType.Cube, new Vector3(3.5f,.35f,-3.35f), new Vector3(2.6f,.5f,1.35f), tubM, room);
    var waterM = M(new Color(.55f,.75f,.88f,.55f), .95f); waterM.SetFloat("_Mode",3); waterM.SetInt("_SrcBlend",5); waterM.SetInt("_DstBlend",10); waterM.SetInt("_ZWrite",0); waterM.EnableKeyword("_ALPHABLEND_ON"); waterM.renderQueue=3000;
    Prim(PrimitiveType.Cube, new Vector3(3.5f,.95f,-3.35f), new Vector3(2.3f,.02f,1.0f), waterM, room);
    var woodCab = M(new Color(.55f,.38f,.24f), .45f, Tiles(new Color(.62f,.42f,.26f), new Color(.35f,.22f,.12f), 4, true));
    Prim(PrimitiveType.Cube, new Vector3(-4.35f,.85f,-3.55f), new Vector3(1.9f,1.7f,1.0f), woodCab, room);
    Prim(PrimitiveType.Cube, new Vector3(-4.35f,1.75f,-3.55f), new Vector3(2.05f,.08f,1.15f), M(Color.white, .9f, marbleT), room);
    Prim(PrimitiveType.Sphere, new Vector3(-4.35f,1.55f,-3.55f), new Vector3(.7f,.22f,.55f), M(new Color(.92f,.94f,.96f), .95f), room);
    Prim(PrimitiveType.Cube, new Vector3(-4.35f,3.15f,-4.35f), new Vector3(1.5f,1.7f,.06f), chrome, room);
    var mir = M(new Color(.75f,.82f,.88f), 1f, null, .85f); mir.EnableKeyword("_EMISSION"); mir.SetColor("_EmissionColor", new Color(.4f,.5f,.6f)*.25f);
    Prim(PrimitiveType.Cube, new Vector3(-4.35f,3.15f,-4.32f), new Vector3(1.3f,1.5f,.03f), mir, room);
    var pedM = M(Color.white, .88f, marbleT); pedM.mainTextureScale = new Vector2(1.2f,1.2f);
    Prim(PrimitiveType.Cylinder, new Vector3(0,.14f,0), new Vector3(2.85f,.14f,2.85f), pedM, room, true);
    Prim(PrimitiveType.Cylinder, new Vector3(0,.30f,0), new Vector3(2.55f,.02f,2.55f), M(new Color(.35f,.78f,.75f), .75f), room);
    Prim(PrimitiveType.Cylinder, new Vector3(0,.31f,0), new Vector3(2.62f,.015f,2.62f), M(new Color(.85f,.7f,.35f), .9f, null, .6f), room);
  }

  void BuildKitchen(Transform room) {
    var floorT = Tiles(new Color(.92f,.88f,.8f), new Color(.7f,.65f,.55f), 8, false);
    var floorM = M(Color.white, .7f, floorT); floorM.mainTextureScale = new Vector2(4,4);
    Prim(PrimitiveType.Plane, Vector3.zero, Vector3.one*3.2f, floorM, room, true);
    var wallT = Tiles(new Color(.98f,.95f,.88f), new Color(.9f,.85f,.75f), 6, false);
    var wm = M(Color.white, .55f, wallT); wm.mainTextureScale = new Vector2(5,2.5f);
    Prim(PrimitiveType.Cube, new Vector3(0,3.1f,-4.55f), new Vector3(16.5f,6.4f,.22f), wm, room);
    Prim(PrimitiveType.Cube, new Vector3(-6.2f,3.1f,0), new Vector3(.22f,6.4f,10.5f), wm, room);
    Prim(PrimitiveType.Cube, new Vector3(6.2f,3.1f,0), new Vector3(.22f,6.4f,10.5f), wm, room);
    Prim(PrimitiveType.Cube, new Vector3(0,6.2f,0), new Vector3(16.5f,.15f,10.5f), M(new Color(.98f,.96f,.92f), .3f), room);
    // cabinets
    var cab = M(new Color(.35f,.55f,.48f), .55f);
    Prim(PrimitiveType.Cube, new Vector3(0,.7f,-3.8f), new Vector3(8f,1.4f,1.2f), cab, room);
    var top = Marble(new Color(.95f,.93f,.9f), new Color(.6f,.55f,.5f));
    Prim(PrimitiveType.Cube, new Vector3(0,1.45f,-3.8f), new Vector3(8.2f,.08f,1.35f), M(Color.white,.9f,top), room);
    // upper cabinets
    Prim(PrimitiveType.Cube, new Vector3(-2.5f,3.6f,-4.1f), new Vector3(3.2f,1.6f,.7f), cab, room);
    Prim(PrimitiveType.Cube, new Vector3(2.8f,3.6f,-4.1f), new Vector3(2.8f,1.6f,.7f), cab, room);
    // stove
    var metal = M(new Color(.75f,.76f,.78f), .85f, null, .9f);
    Prim(PrimitiveType.Cube, new Vector3(3.2f,.85f,-3.5f), new Vector3(1.6f,1.5f,1.4f), metal, room);
    Prim(PrimitiveType.Cylinder, new Vector3(2.9f,1.65f,-3.3f), new Vector3(.35f,.02f,.35f), M(new Color(.15f,.15f,.15f),.3f), room);
    Prim(PrimitiveType.Cylinder, new Vector3(3.5f,1.65f,-3.3f), new Vector3(.35f,.02f,.35f), M(new Color(.15f,.15f,.15f),.3f), room);
    // fridge
    Prim(PrimitiveType.Cube, new Vector3(-4.8f,1.8f,-3.5f), new Vector3(1.5f,3.5f,1.3f), M(new Color(.92f,.93f,.95f),.7f), room);
    Prim(PrimitiveType.Cube, new Vector3(-4.05f,2.2f,-3.5f), new Vector3(.05f,1.2f,.08f), metal, room);
    // window
    var chrome = M(new Color(.82f,.84f,.88f), .95f, null, 1f);
    Prim(PrimitiveType.Cube, new Vector3(0,3.4f,-4.48f), new Vector3(2.4f,1.8f,.08f), chrome, room);
    var skyM = M(Color.white, .15f, OutsideSky(new Color(1f,.95f,.85f), new Color(.95f,.75f,.55f)));
    Prim(PrimitiveType.Quad, new Vector3(0,3.4f,-4.44f), new Vector3(2.1f,1.5f,1), skyM, room);
    // pendant lights
    for (float x=-1.5f; x<=1.5f; x+=1.5f) {
      Prim(PrimitiveType.Cylinder, new Vector3(x,5.5f,-2.5f), new Vector3(.03f,.5f,.03f), metal, room);
      Prim(PrimitiveType.Sphere, new Vector3(x,4.9f,-2.5f), Vector3.one*.25f, Emit(new Color(1f,.9f,.7f), 1.8f), room);
    }
    // island pedestal
    Prim(PrimitiveType.Cylinder, new Vector3(0,.14f,0), new Vector3(2.85f,.14f,2.85f), M(new Color(.55f,.38f,.25f),.5f), room, true);
    Prim(PrimitiveType.Cylinder, new Vector3(0,.30f,0), new Vector3(2.55f,.02f,2.55f), M(new Color(.9f,.55f,.3f),.7f), room);
  }

  void BuildGarage(Transform room) {
    var floorM = M(Color.white, .35f, Concrete(new Color(.55f,.55f,.52f))); floorM.mainTextureScale = new Vector2(3,3);
    Prim(PrimitiveType.Plane, Vector3.zero, Vector3.one*3.2f, floorM, room, true);
    var brick = M(Color.white, .25f, BrickTex(new Color(.7f,.4f,.28f), new Color(.55f,.3f,.2f))); brick.mainTextureScale = new Vector2(4,2.5f);
    Prim(PrimitiveType.Cube, new Vector3(0,3.1f,-4.55f), new Vector3(16.5f,6.4f,.22f), brick, room);
    Prim(PrimitiveType.Cube, new Vector3(-6.2f,3.1f,0), new Vector3(.22f,6.4f,10.5f), brick, room);
    Prim(PrimitiveType.Cube, new Vector3(6.2f,3.1f,0), new Vector3(.22f,6.4f,10.5f), brick, room);
    Prim(PrimitiveType.Cube, new Vector3(0,6.2f,0), new Vector3(16.5f,.15f,10.5f), M(new Color(.45f,.45f,.42f), .2f), room);
    // metal shelves
    var metal = M(new Color(.6f,.62f,.65f), .7f, null, .8f);
    for (int i=0;i<3;i++) Prim(PrimitiveType.Cube, new Vector3(-4.5f,1.2f+i*.7f,-3.8f), new Vector3(2.2f,.05f,.8f), metal, room);
    Prim(PrimitiveType.Cube, new Vector3(-5.4f,2.2f,-3.8f), new Vector3(.08f,2.5f,.08f), metal, room);
    Prim(PrimitiveType.Cube, new Vector3(-3.6f,2.2f,-3.8f), new Vector3(.08f,2.5f,.08f), metal, room);
    // workbench
    var wood = M(new Color(.55f,.38f,.22f), .4f, Tiles(new Color(.6f,.4f,.25f), new Color(.35f,.22f,.12f), 4, true));
    Prim(PrimitiveType.Cube, new Vector3(3.5f,.9f,-3.5f), new Vector3(2.5f,.12f,1.2f), wood, room);
    Prim(PrimitiveType.Cube, new Vector3(2.6f,.45f,-3.5f), new Vector3(.15f,.9f,1.1f), wood, room);
    Prim(PrimitiveType.Cube, new Vector3(4.4f,.45f,-3.5f), new Vector3(.15f,.9f,1.1f), wood, room);
    // garage door stripes
    for (int i=0;i<6;i++) Prim(PrimitiveType.Cube, new Vector3(0,1+i*.7f,-4.4f), new Vector3(5f,.08f,.05f), metal, room);
    // hanging lamp
    Prim(PrimitiveType.Cylinder, new Vector3(0,5.8f,0), new Vector3(.05f,.4f,.05f), metal, room);
    Prim(PrimitiveType.Sphere, new Vector3(0,5.2f,0), Vector3.one*.35f, Emit(new Color(1f,.85f,.55f), 2.5f), room);
    // oil drum decor
    Prim(PrimitiveType.Cylinder, new Vector3(5.2f,.8f,-2.5f), new Vector3(.9f,1.6f,.9f), M(new Color(.7f,.25f,.15f),.4f), room);
    Prim(PrimitiveType.Cylinder, new Vector3(0,.14f,0), new Vector3(2.85f,.14f,2.85f), M(new Color(.4f,.4f,.38f),.3f), room, true);
    Prim(PrimitiveType.Cylinder, new Vector3(0,.30f,0), new Vector3(2.55f,.02f,2.55f), M(new Color(.9f,.7f,.2f),.6f), room);
    // caution stripes
    Prim(PrimitiveType.Cube, new Vector3(0,.32f,1.4f), new Vector3(2.2f,.02f,.15f), M(new Color(.95f,.8f,.15f),.4f), room);
  }

  void BuildGarden(Transform room) {
    var grass = M(Color.white, .15f, GrassTex()); grass.mainTextureScale = new Vector2(4,4);
    Prim(PrimitiveType.Plane, Vector3.zero, Vector3.one*3.5f, grass, room, true);
    // fence back
    var wood = M(new Color(.7f,.5f,.3f), .35f, Tiles(new Color(.75f,.55f,.32f), new Color(.45f,.3f,.15f), 5, true));
    for (float x=-5; x<=5; x+=1.1f) Prim(PrimitiveType.Cube, new Vector3(x,1.2f,-4.3f), new Vector3(.15f,2.4f,.08f), wood, room);
    Prim(PrimitiveType.Cube, new Vector3(0,1.8f,-4.3f), new Vector3(11f,.12f,.1f), wood, room);
    Prim(PrimitiveType.Cube, new Vector3(0,.6f,-4.3f), new Vector3(11f,.12f,.1f), wood, room);
    // side hedges
    var leaf = M(new Color(.25f,.55f,.28f), .25f);
    for (int i=0;i<8;i++) {
      Prim(PrimitiveType.Sphere, new Vector3(-5.5f,.8f,-3+i*.9f), new Vector3(1.2f,1.6f,1.2f), leaf, room);
      Prim(PrimitiveType.Sphere, new Vector3(5.5f,.8f,-3+i*.9f), new Vector3(1.2f,1.6f,1.2f), leaf, room);
    }
    // sky dome feel via distant quads
    var skyM = M(Color.white, .1f, OutsideSky(new Color(.7f,.88f,1f), new Color(.95f,.9f,.75f)));
    Prim(PrimitiveType.Quad, new Vector3(0,4f,-5.5f), new Vector3(16,6,1), skyM, room);
    // stone path ring
    var stone = M(Color.white, .4f, Concrete(new Color(.65f,.62f,.55f)));
    Prim(PrimitiveType.Cylinder, new Vector3(0,.02f,0), new Vector3(3.2f,.03f,3.2f), stone, room, true);
    Prim(PrimitiveType.Cylinder, new Vector3(0,.14f,0), new Vector3(2.85f,.14f,2.85f), M(new Color(.55f,.48f,.38f),.4f), room, true);
    Prim(PrimitiveType.Cylinder, new Vector3(0,.30f,0), new Vector3(2.55f,.02f,2.55f), M(new Color(.45f,.75f,.4f),.5f), room);
    // flower beds
    for (int i=0;i<5;i++) {
      float a=i/5f*Mathf.PI*2;
      Prim(PrimitiveType.Sphere, new Vector3(Mathf.Cos(a)*3.5f,.3f,Mathf.Sin(a)*3.5f-1f), Vector3.one*.35f, M(Color.HSVToRGB(i/5f,.7f,.9f),.3f), room);
    }
    // sun flowers tall
    for (int i=0;i<3;i++) {
      Prim(PrimitiveType.Cylinder, new Vector3(-3.5f+i*.4f,1.2f,-3.5f), new Vector3(.05f,1.2f,.05f), leaf, room);
      Prim(PrimitiveType.Cylinder, new Vector3(-3.5f+i*.4f,2.4f,-3.5f), new Vector3(.35f,.05f,.35f), M(new Color(.95f,.75f,.15f),.4f), room);
      Prim(PrimitiveType.Sphere, new Vector3(-3.5f+i*.4f,2.4f,-3.5f), Vector3.one*.15f, M(new Color(.35f,.22f,.1f),.5f), room);
    }
    // bench
    Prim(PrimitiveType.Cube, new Vector3(3.8f,.55f,-3.2f), new Vector3(1.8f,.1f,.6f), wood, room);
    Prim(PrimitiveType.Cube, new Vector3(3.8f,.95f,-3.45f), new Vector3(1.8f,.5f,.08f), wood, room);
    Prim(PrimitiveType.Cube, new Vector3(3.1f,.3f,-3.2f), new Vector3(.1f,.5f,.55f), wood, room);
    Prim(PrimitiveType.Cube, new Vector3(4.5f,.3f,-3.2f), new Vector3(.1f,.5f,.55f), wood, room);
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

  Lvl[] CurLevels => themes[themeIdx].levels;
  string StarKey(int ti, int li) => $"stars_{ti}_{li}";

  void Load(int i) {
    var levels = CurLevels;
    cur = (i + levels.Length) % levels.Length; var L = levels[cur]; if (obj) Destroy(obj); cleanSpots.Clear();
    var src = Resources.Load<GameObject>(L.mesh);
    if (src == null) { Debug.LogError("Missing mesh resource: " + L.mesh); return; }
    var mf0 = src.GetComponentInChildren<MeshFilter>(); var mesh = mf0.sharedMesh;
    obj = new GameObject(L.name); var mf = obj.AddComponent<MeshFilter>(); mf.sharedMesh = mesh; var mr = obj.AddComponent<MeshRenderer>();
    obj.transform.rotation = mf0.transform.rotation * Quaternion.identity; obj.transform.localScale = mf0.transform.lossyScale;
    obj.transform.rotation = Quaternion.Euler(L.tilt, L.yaw, 0) * obj.transform.rotation;
    var b = mr.bounds; float s = 2.3f / Mathf.Max(b.size.x, b.size.y, b.size.z); obj.transform.localScale *= s; b = mr.bounds;
    obj.transform.position += new Vector3(-b.center.x, .34f - b.min.y, -b.center.z);
    var mc = obj.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; mc.convex = false;
    var baseT = Resources.Load<Texture2D>(L.mesh + "_base");
    if (baseT == null) { Debug.LogError("Missing texture: " + L.mesh + "_base"); return; }
    var bp = baseT.GetPixels32(); int bw = baseT.width;
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
    if (puddle) puddle.transform.localScale = new Vector3(0,.002f,0);
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
  bool IsOverPlayUI(Vector2 screenPos) {
    // Bottom mode+tool bar (~180px) and top progress bar (~110px)
    return screenPos.y < 190 || screenPos.y > Screen.height - 110;
  }
  void Update() {
    if (!autoMode && state == St.Play) {
      bool wantClean = false, wantRotate = false;
      // Two-finger: pinch ZOOM only (never clean, never rotate) — keeps gestures unambiguous
      if (Input.touchCount >= 2) {
        var t0 = Input.GetTouch(0); var t1 = Input.GetTouch(1);
        pointer = (t0.position + t1.position) * .5f;
        float curDist = Vector2.Distance(t0.position, t1.position);
        float prevDist = Vector2.Distance(t0.position - t0.deltaPosition, t1.position - t1.deltaPosition);
        if (prevDist > 8f) camDist = Mathf.Clamp(camDist - (curDist - prevDist) * .01f, 3.2f, 8.5f);
        rotating = false; pointerDown = false;
      } else if (Input.touchCount == 1) {
        var t0 = Input.GetTouch(0); pointer = t0.position;
        bool overUI = IsOverPlayUI(pointer);
        bool dragging = !overUI && GUIUtility.hotControl == 0 &&
          (t0.phase == TouchPhase.Began || t0.phase == TouchPhase.Moved || t0.phase == TouchPhase.Stationary);
        if (dragging) {
          if (mode == Mode.Clean) wantClean = true;
          else {
            wantRotate = true;
            Vector2 d = t0.deltaPosition;
            objYaw -= d.x * .35f;
            objPitch = Mathf.Clamp(objPitch + d.y * .28f, -70f, 70f);
            ApplyObjectRotation();
          }
        }
        rotating = wantRotate; pointerDown = wantClean;
      } else {
        // Mouse / PC
        pointer = Input.mousePosition;
        bool overUI = IsOverPlayUI(pointer);
        // Right-drag ALWAYS rotates (never cleans) — escape hatch
        if (Input.GetMouseButtonDown(1)) { rotating = true; lastRotMouse = pointer; }
        if (Input.GetMouseButtonUp(1)) rotating = false;
        if (rotating && Input.GetMouseButton(1)) {
          Vector2 d = (Vector2)pointer - lastRotMouse; lastRotMouse = pointer;
          objYaw -= d.x * .45f; objPitch = Mathf.Clamp(objPitch + d.y * .35f, -70f, 70f);
          ApplyObjectRotation(); wantRotate = true;
        }
        // Left-drag follows active mode only
        if (Input.GetMouseButton(0) && !rotating && !overUI && GUIUtility.hotControl == 0) {
          if (mode == Mode.Clean) wantClean = true;
          else {
            wantRotate = true;
            if (Input.GetMouseButtonDown(0)) lastRotMouse = pointer;
            Vector2 d = (Vector2)pointer - lastRotMouse; lastRotMouse = pointer;
            objYaw -= d.x * .45f; objPitch = Mathf.Clamp(objPitch + d.y * .35f, -70f, 70f);
            ApplyObjectRotation();
          }
        }
        rotating = wantRotate || (rotating && Input.GetMouseButton(1));
        pointerDown = wantClean && !wantRotate;
      }
    } else if (!autoMode) {
      pointer = Input.mousePosition;
      if (Input.touchCount >= 1) pointer = Input.GetTouch(0).position;
      pointerDown = false; rotating = false;
    }
    // Hotkeys: 1/2 tools, Q clean mode, E rotate mode
    if (Input.GetKeyDown(KeyCode.Alpha1)) { sponge=false; mode=Mode.Clean; }
    if (Input.GetKeyDown(KeyCode.Alpha2)) { sponge=true; mode=Mode.Clean; }
    if (Input.GetKeyDown(KeyCode.Q)) mode = Mode.Clean;
    if (Input.GetKeyDown(KeyCode.E)) mode = Mode.Rotate;

    if (state == St.ThemeSelect || state == St.LevelSelect) {
      titleSpin += Time.deltaTime; camYaw = 180 + Mathf.Sin(titleSpin*.4f)*18;
      if (obj) { objYaw = titleSpin*25f; ApplyObjectRotation(); }
    }

    var tgt = new Vector3(0, 1.45f, 0);
    shake = Mathf.MoveTowards(shake, 0, Time.deltaTime*.2f);
    Vector3 jit = new Vector3(Mathf.PerlinNoise(Time.time*25,0)-.5f, Mathf.PerlinNoise(0,Time.time*25)-.5f, 0) * shake;
    cam.transform.position = tgt + Quaternion.Euler(camPitch, camYaw, 0) * new Vector3(0,0,-camDist);
    cam.transform.LookAt(tgt); cam.transform.position += cam.transform.rotation * jit;
    if (!autoMode) camDist = Mathf.Clamp(camDist - Input.mouseScrollDelta.y*.4f, 3.2f, 8.5f);

    bool playing = state == St.Play; bool showTool = playing && mode == Mode.Clean; nozzle.SetActive(showTool && !sponge); spongeObj.SetActive(showTool && sponge);
    bool cleaning = false; jet.enabled = false;
    if (playing) {
      var ray = cam.ScreenPointToRay(pointer);
      var hit = HitObject(ray); bool hitObj = HasHit(hit);
      Vector3 aim = hitObj ? hit.point : ray.GetPoint(4.2f);
      Vector3 n = hitObj ? hit.normal.normalized : -ray.direction.normalized;
      Vector3 want; Quaternion wantR;
      if (!sponge) {
        want = aim + n*.18f - ray.direction*.95f + cam.transform.right*.32f - cam.transform.up*.12f;
        wantR = Quaternion.LookRotation((aim - want).normalized, Vector3.up);
      } else {
        want = aim + n * .16f;
        Vector3 up = Vector3.ProjectOnPlane(cam.transform.up, n); if (up.sqrMagnitude < .001f) up = Vector3.ProjectOnPlane(Vector3.up, n);
        wantR = Quaternion.LookRotation(up.normalized, n);
      }
      float bob = pointerDown && hitObj ? Mathf.Sin(Time.time*(sponge?18:36))*(sponge?.015f:.006f) : Mathf.Sin(Time.time*3)*.015f;
      var t = sponge ? spongeObj.transform : nozzle.transform; var prev = t.position;
      t.position = Vector3.SmoothDamp(t.position, want + n*bob, ref toolVel, .04f);
      t.rotation = Quaternion.Slerp(t.rotation, wantR, .35f);
      if (sponge) { var v = (t.position-prev)/Mathf.Max(Time.deltaTime,.001f); t.Rotate(Vector3.forward, Mathf.Clamp(Vector3.Dot(v, cam.transform.right)*-5, -18, 18), Space.Self); }

      if (pointerDown && mode == Mode.Clean) {
        if (!hitObj && !sponge) {
          var tip0 = nozzle.transform.TransformPoint(0,0,.33f);
          hit = HitObject(new Ray(tip0, (aim - tip0).normalized));
          hitObj = HasHit(hit);
          if (hitObj) { aim = hit.point; n = hit.normal.normalized; }
        }
        if (hitObj) {
          cleaning = true;
          Vector2 uv = hit.textureCoord;
          if (uv.x <= 0 && uv.y <= 0) uv = hit.textureCoord2;
          float mr = Brush(uv, sponge ? .085f : .14f, sponge ? 1.4f : 3.2f);
          var ep = new ParticleSystem.EmitParams();
          Vector3 surface = hit.point + n * .04f;
          if (!sponge) {
            var tip = nozzle.transform.TransformPoint(0,0,.33f);
            jet.enabled = true;
            for (int i=0;i<jet.positionCount;i++) {
              float u = i/(float)(jet.positionCount-1);
              Vector3 p = Vector3.Lerp(tip, surface, u);
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
  float shineT = -1;
  IEnumerator WinSeq() {
    state = St.Win; progress = 1; hiss.volume = 0; jet.enabled = false;
    float time = Time.time - levelStart; stars = time < 50 ? 3 : time < 100 ? 2 : 1;
    int best = PlayerPrefs.GetInt(StarKey(themeIdx,cur),0); if (stars > best) PlayerPrefs.SetInt(StarKey(themeIdx,cur), stars);
    for (int k=0;k<mud.Length;k++){ mud[k]=0; grime[k]=0; wet[k]=Mathf.Max(wet[k],.6f);} wetTimer = 3; Compose();
    sfx.PlayOneShot(chord, .9f); shineT = 0; var b = obj.GetComponent<MeshRenderer>().bounds; var ep = new ParticleSystem.EmitParams();
    for (int i=0;i<40;i++){ ep.position = b.center + Vector3.Scale(Random.insideUnitSphere, b.extents); glint.Emit(ep,1); }
    for (int i=0;i<260;i++){ ep.position = new Vector3(Random.Range(-.5f,.5f), 2f, Random.Range(-.5f,.5f)); ep.velocity = new Vector3(Random.Range(-4f,4f), Random.Range(4f,8f), Random.Range(-3f,4f)); ep.startColor = Color.HSVToRGB(Random.value,.65f,1); ep.startSize = Random.Range(.06f,.13f); confetti.Emit(ep,1); }
    Vector3 s0 = obj.transform.localScale; for (float t=0;t<.5f;t+=Time.deltaTime){ float k = 1 + Mathf.Sin(t/.5f*Mathf.PI)*.12f; obj.transform.localScale = s0*k; yield return null; } obj.transform.localScale = s0;
    winT = Time.time;
  }

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
  Texture2D StarIcon(bool on) { return Cached(on?"st1":"st0", 96, (x,y,u,v) => { float px=u-.5f, py=.5f-v; float ang = Mathf.Atan2(py,px), r = Mathf.Sqrt(px*px+py*py);
    float lim = .48f * Mathf.Lerp(.45f, 1, Mathf.Pow(Mathf.Abs(Mathf.Cos((ang+Mathf.PI/2)*2.5f)),3)); float a = Mathf.Clamp01((lim - r)*80);
    Color c = on ? Color.Lerp(new Color(1f,.65f,.1f), new Color(1f,.92f,.35f), v) : new Color(.8f,.82f,.86f); if (r > lim - .04f) c = on ? new Color(.85f,.45f,.05f) : new Color(.65f,.67f,.72f); c.a = a; return c; }); }

  Texture2D RotateIcon() { return Cached("rot", 64, (x,y,u,v) => {
      float cx=.5f, cy=.5f; float dx=u-cx, dy=v-cy; float r=Mathf.Sqrt(dx*dx+dy*dy);
      float ang=Mathf.Atan2(dy,dx);
      // ring
      float ring = Mathf.Abs(r-.32f);
      float a = ring<.07f ? 1f : 0f;
      // arrow head at top-right of ring
      float aa = Mathf.Repeat(ang + Mathf.PI*.15f, Mathf.PI*2);
      if (r>.22f && r<.42f && aa < .55f) a = Mathf.Max(a, 1f);
      // center dot
      if (r<.08f) a = 1f;
      Color c = Color.Lerp(new Color(.85f,.75f,1f), new Color(.55f,.35f,.95f), v);
      c.a = a; return c;
    }); }

  void OnGUI() {
    float w = Screen.width, h = Screen.height; float ui = Mathf.Clamp(h/800f, .7f, 1.6f); GUI.matrix = Matrix4x4.Scale(new Vector3(ui,ui,1)); w/=ui; h/=ui;
    var teal = Round("teal", new Color(.35f,.85f,.8f), new Color(.15f,.65f,.65f)); var tealH = Round("tealH", new Color(.45f,.92f,.86f), new Color(.2f,.72f,.7f));
    var orange = Round("or", new Color(1f,.72f,.35f), new Color(.98f,.5f,.25f)); var orangeH = Round("orH", new Color(1f,.8f,.45f), new Color(1f,.6f,.3f));
    var gray = Round("gr", new Color(.75f,.8f,.85f), new Color(.6f,.66f,.72f)); var white = Round("wh", new Color(1,1,1,.97f), new Color(.95f,.98f,1f,.97f), 28, new Color(.75f,.9f,.95f));
    var dark = Color.Lerp(new Color(.1f,.25f,.35f), Color.black, .1f);

    if (state == St.ThemeSelect) {
      GUI.DrawTexture(new Rect(0,0,w,h), Round("veil", new Color(.85f,.95f,1f,.35f), new Color(.85f,.95f,1f,.75f), 1));
      float bob = Mathf.Sin(Time.time*2)*6;
      Shadowed(new Rect(0, 50+bob, w, 90), "洗洗更解压", St_(78, Color.white), new Color(.15f,.6f,.62f), 6);
      GUI.Label(new Rect(0, 145, w, 36), "选择一个主题开始闪亮大作战", St_(24, dark, FontStyle.Normal));
      float cw = 200, gap = 22, tot = themes.Length*cw + (themes.Length-1)*gap, x0 = (w-tot)/2, y0 = h*.42f;
      for (int i=0;i<themes.Length;i++) {
        var th = themes[i]; var r = new Rect(x0 + i*(cw+gap), y0, cw, 230); bool hov = r.Contains(Event.current.mousePosition); if (hov) r.y -= 6;
        Panel(new Rect(r.x, r.y+6, r.width, r.height), Round("btnsh", new Color(0,0,0,.18f), new Color(0,0,0,.18f)));
        Panel(r, white);
        var accent = Round("acc"+i, Color.Lerp(th.accent,Color.white,.35f), th.accent);
        GUI.DrawTexture(new Rect(r.x+16, r.y+16, cw-32, 70), accent);
        GUI.Label(new Rect(r.x, r.y+28, cw, 46), th.name, St_(36, Color.white));
        GUI.Label(new Rect(r.x+10, r.y+100, cw-20, 50), th.blurb, St_(16, dark, FontStyle.Normal));
        int got=0, max=th.levels.Length*3;
        for (int li=0;li<th.levels.Length;li++) got += PlayerPrefs.GetInt(StarKey(i,li),0);
        GUI.Label(new Rect(r.x, r.y+155, cw, 28), $"{th.levels.Length} 个物品 · {got}/{max}★", St_(16, new Color(.2f,.55f,.55f), FontStyle.Normal));
        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { OpenTheme(i); }
      }
      GUI.Label(new Rect(0, h-40, w, 30), "共 4 大主题 · 24 个精美模型 · 单指清洁 · 双指旋转", St_(16, dark, FontStyle.Normal));
      return;
    }

    if (state == St.LevelSelect) {
      var th = themes[themeIdx];
      GUI.DrawTexture(new Rect(0,0,w,h), Round("veil2", new Color(th.sky.r,th.sky.g,th.sky.b,.4f), new Color(th.sky.r,th.sky.g,th.sky.b,.75f), 1));
      if (Btn(new Rect(24, 24, 100, 52), "← 主题", gray, gray, 22)) { state = St.ThemeSelect; BuildRoom(0); Load(0); }
      Shadowed(new Rect(0, 30, w, 70), th.name, St_(56, Color.white), th.accent*0.6f, 4);
      GUI.Label(new Rect(0, 100, w, 30), th.blurb, St_(20, dark, FontStyle.Normal));
      var levels = th.levels;
      int cols = Mathf.Min(3, levels.Length); int rows = (levels.Length + cols - 1) / cols;
      float cw = 200, ch = 150, gap = 18;
      float totW = cols*cw + (cols-1)*gap; float totH = rows*ch + (rows-1)*gap;
      float x0 = (w-totW)/2, y0 = Mathf.Max(150, (h-totH)/2);
      for (int i=0;i<levels.Length;i++) {
        int col=i%cols, row=i/cols;
        var r = new Rect(x0 + col*(cw+gap), y0 + row*(ch+gap), cw, ch);
        bool hov = r.Contains(Event.current.mousePosition); if (hov) r.y -= 4;
        Panel(new Rect(r.x, r.y+5, r.width, r.height), Round("btnsh", new Color(0,0,0,.18f), new Color(0,0,0,.18f)));
        Panel(r, white);
        GUI.Label(new Rect(r.x, r.y+14, cw, 36), (i+1).ToString(), St_(32, th.accent));
        GUI.Label(new Rect(r.x+8, r.y+55, cw-16, 40), levels[i].name, St_(20, dark));
        int st = PlayerPrefs.GetInt(StarKey(themeIdx,i),0);
        for (int s=0;s<3;s++) GUI.DrawTexture(new Rect(r.x+cw/2-50+s*34, r.y+105, 28, 28), StarIcon(s<st));
        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) StartLevel(i);
      }
      return;
    }

    // top bar play/win
    var top = new Rect(w/2-280, 14, 560, 92); Panel(top, white);
    if (Btn(new Rect(top.x+12, top.y+18, 56, 56), "≡", gray, gray, 28)) { state = St.LevelSelect; hiss.volume = 0; }
    GUI.Label(new Rect(top.x+70, top.y+8, top.width-140, 34), $"{themes[themeIdx].name} · {CurLevels[cur].name}", St_(22, dark));
    var bar = new Rect(top.x+86, top.y+50, top.width-190, 26); GUI.DrawTexture(bar, Round("barbg", new Color(.85f,.9f,.93f), new Color(.92f,.95f,.97f), 13));
    float pw = Mathf.Max(26, bar.width*shownProgress); var fillR = new Rect(bar.x, bar.y, pw, bar.height);
    GUI.DrawTexture(fillR, Round("barfill", new Color(.45f,.95f,.75f), new Color(.2f,.7f,.9f), 13));
    float sx = Mathf.Repeat(Time.time*.7f, 1.4f)-.2f; GUI.BeginGroup(fillR); GUI.DrawTexture(new Rect(sx*fillR.width - 30, 0, 30, bar.height), Round("shine", new Color(1,1,1,.55f), new Color(1,1,1,.25f), 12)); GUI.EndGroup();
    GUI.Label(new Rect(bar.xMax+6, bar.y-4, 80, 34), $"{Mathf.RoundToInt(shownProgress*100)}%", St_(24, new Color(.15f,.6f,.62f)));
    if (state == St.Play) {
      // Mode row: 清洗 / 旋转 — mutually exclusive, clearly highlighted
      float mw = 160, mh = 64, gap = 14;
      float modeY = h - 168;
      var cleanR = new Rect(w/2 - mw - gap/2, modeY, mw, mh);
      var rotR = new Rect(w/2 + gap/2, modeY, mw, mh);
      var purple = Round("pur", new Color(.7f,.55f,.95f), new Color(.5f,.35f,.85f));
      var purpleH = Round("purH", new Color(.8f,.65f,1f), new Color(.6f,.45f,.9f));
      if (Btn(cleanR, "清洗", mode==Mode.Clean?teal:gray, tealH, 26, DropIcon())) mode = Mode.Clean;
      if (Btn(rotR, "旋转", mode==Mode.Rotate?purple:gray, purpleH, 26, RotateIcon())) mode = Mode.Rotate;
      // Tool row (only meaningful in Clean mode; still visible but dimmed in Rotate)
      var a = new Rect(w/2-230, h-90, 220, 64); var b2 = new Rect(w/2+10, h-90, 220, 64);
      if (mode == Mode.Clean) {
        if (Btn(a, "高压水枪", !sponge?teal:gray, tealH, 22, DropIcon())) sponge = false;
        if (Btn(b2, "泡泡海绵", sponge?orange:gray, orangeH, 22, SpongeIcon())) sponge = true;
      } else {
        Panel(a, gray); GUI.Label(a, "高压水枪", St_(22, new Color(1,1,1,.55f)));
        Panel(b2, gray); GUI.Label(b2, "泡泡海绵", St_(22, new Color(1,1,1,.55f)));
      }
      string tip = mode == Mode.Clean
        ? "清洗模式：单指在物体上拖动清洁 · 双指开合缩放 · 右键也可旋转"
        : "旋转模式：单指拖动只转物体 · 不会清洁 · 双指开合缩放";
      GUI.Label(new Rect(0, modeY - 28, w, 24), tip, St_(15, dark, FontStyle.Normal));
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
      if (t > .8f) {
        if (Btn(new Rect(pr.x+30, pr.y+205, 160, 64), "选关", gray, gray, 24)) state = St.LevelSelect;
        string nextLabel = cur+1<CurLevels.Length ? "下一关 ▶" : "下一主题";
        if (Btn(new Rect(pr.x+210, pr.y+205, 180, 64), nextLabel, orange, orangeH, 24)) {
          if (cur+1<CurLevels.Length) StartLevel(cur+1);
          else { int nt=(themeIdx+1)%themes.Length; OpenTheme(nt); StartLevel(0); }
        }
      }
      GUI.matrix = mtx2;
    }
  }
  void OpenTheme(int ti) {
    themeIdx = ti; BuildRoom(ti); Load(0); state = St.LevelSelect; camYaw = 180; camPitch = 18;
  }
  void StartLevel(int i) {
    Load(i); state = St.Play; mode = Mode.Clean; dingStep = 0; winT = 0; shineT = -1; camYaw = 180; camPitch = 18; objYaw = 0; objPitch = 0; ApplyObjectRotation(); rotating = false; pointerDown = false;
  }

#if UNITY_STANDALONE || UNITY_EDITOR
  IEnumerator CapShotIE(string name){
    yield return new WaitForEndOfFrame();
    try {
      var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
      tex.ReadPixels(new Rect(0,0,Screen.width,Screen.height), 0, 0); tex.Apply();
      System.IO.Directory.CreateDirectory(shotDir);
      System.IO.File.WriteAllBytes(System.IO.Path.Combine(shotDir, name), tex.EncodeToPNG());
      Object.Destroy(tex); Debug.Log("SHOT "+name);
    } catch (System.Exception e) { Debug.LogWarning("SHOT fail "+e.Message); }
  }
  void CapShot(string name){ StartCoroutine(CapShotIE(name)); }
  System.Collections.IEnumerator AutoScreenshotsV6() {
    autoMode = true; yield return new WaitForSeconds(1.0f);
    OpenTheme(0); StartLevel(0); mode = Mode.Clean; sponge = false;
    yield return new WaitForSeconds(0.9f);
    CapShot("unity-v7-shot01-mode-clean.png");
    pointer = new Vector2(Screen.width/2f, Screen.height*0.55f); pointerDown = true; mode = Mode.Clean;
    float endt = Time.time + 2.0f; while (Time.time < endt) yield return null;
    CapShot("unity-v7-shot02-cleaning.png");
    pointerDown = false;
    mode = Mode.Rotate; yield return new WaitForSeconds(0.4f);
    CapShot("unity-v7-shot03-mode-rotate.png");
    for (int i=0;i<40;i++){ objYaw -= 4f; ApplyObjectRotation(); yield return null; }
    CapShot("unity-v7-shot04-rotated-no-clean.png");
    mode = Mode.Clean; yield return new WaitForSeconds(0.3f);
    CapShot("unity-v7-shot05-back-to-clean.png");
    // bathroom model showcase
    string[] tags = {"plate","duck","soap","shampoo","cup","loofah"};
    for (int li=0; li<tags.Length; li++) {
      StartLevel(li); mode = Mode.Rotate;
      for (int i=0;i<25;i++){ objYaw -= 3f; ApplyObjectRotation(); yield return null; }
      yield return new WaitForSeconds(0.35f);
      CapShot("unity-v7-model-"+tags[li]+".png");
    }
    OpenTheme(1); StartLevel(0); mode = Mode.Rotate; yield return new WaitForSeconds(0.8f);
    CapShot("unity-v7-shot06-kitchen-rotate-ui.png");
    Debug.Log("AUTO_SHOTS_DONE"); Application.Quit();
  }
#endif

}
