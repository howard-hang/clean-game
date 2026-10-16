# 洗洗更解压 (Clean Game)

Unity casual cleaning game. Controls: 清洗/旋转 modes, water gun / sponge.

## v1.3.0 — Bathroom model upgrade
- AI concept four-view sheets under `refs/bath/<id>/fourview.jpg` (Flux via Pollinations)
- Blender orthographic four-views `refs/bath/<id>/blender_fourview.jpg`
- Rebuilt bathroom FBX meshes (plate/duck/soap/shampoo/cup/loofah) via Blender 5.2 CLI (Tripo unavailable)
- Windows + Android builds; keep 清洗/旋转 interaction

## Download
See [Releases](https://github.com/howard-hang/clean-game/releases)

# 娲楁礂鏇磋В鍘?(Clean Game) v1.1

瑙ｅ帇娓呮磥灏忔父鎴忥細鍐叉帀娉ュ反銆佹悡鍑烘场娉°€佹礂鍒伴棯闂彂浜€?

## 涓婚涓庡叧鍗★紙鍏?4 涓婚 路 24 鍏筹級

| 涓婚 | 鐗╁搧 |
|------|------|
| 娴村 | 闈掕姳鐩樸€佸皬榛勯腑銆佽偉鐨傘€佹礂鍙戠摱銆佺墮鍒锋澂銆佷笣鐡滅粶 |
| 鍘ㄦ埧 | 骞冲簳閿呫€佽尪澹躲€侀┈鍏嬫澂銆佸ぇ纰椼€佺牕鏉裤€佹按澹?|
| 杞﹀簱 | 宸ュ叿绠便€佽疆鑳庛€佹补妗躲€佸ご鐩斻€佹満娌瑰６銆佹壋鎵?|
| 鑺卞洯 | 鑺辩泦銆佹磼姘村６銆佸皬鐭汉銆侀笩灞嬨€侀潚铔欍€佹墜鎺ㄨ溅 |

## 鎿嶄綔锛坴1.2 浜掍笉鍐茬獊锛?

搴曢儴鍏堥€夋ā寮忥紝鍐嶅崟鎸囨嫋鍔ㄢ€斺€?*涓€绉嶆墜鍔垮彧鍋氫竴浠朵簨**锛?

- **娓呮礂妯″紡**锛氬崟鎸囧湪鐗╀綋涓婃嫋鍔ㄦ竻娲侊紱鍙€夈€岄珮鍘嬫按鏋?/ 娉℃场娴风坏銆?
- **鏃嬭浆妯″紡**锛氬崟鎸囨嫋鍔ㄥ彧杞墿浣擄紝**涓嶄細娓呮磥**
- **鍙屾寚寮€鍚?/ 婊氳疆**锛氱缉鏀撅紙鐩告満浣嶇疆鍥哄畾锛屽彧鏀硅繙杩戯級
- **鍙抽敭鎷栧姩锛堢數鑴戯級**锛氶殢鏃舵棆杞紝浠庝笉娓呮磥
- 蹇嵎閿細\Q\ 娓呮礂 / \E\ 鏃嬭浆 / \ \ 鍒囨崲宸ュ叿

## 宸ョ▼

- Windows锛歎nity **6000.3.26f1**锛屾墦寮€鏈粨搴擄紝鑿滃崟鎴?`BuildScript.Win`
- Android锛歎nity **2022.3.58f1c1**锛圲nity 6 Android 妯″潡褰撳墠 CDN 404锛夛紝鐢?`clean-game-android` 鍚屾簮宸ョ▼ `BuildScript.Android`
- 妯″瀷锛歚make_assets.py` + Blender 5.x 鎵瑰鐞嗙儤鐒?

```bash
blender -b -P make_assets.py -- Assets/Resources
```

## 涓嬭浇

- 婧愮爜锛歔GitHub](https://github.com/howard-hang/clean-game)
- Android APK锛氳 [Releases](https://github.com/howard-hang/clean-game/releases)

## 璇存槑

璋冭瘯绛惧悕 APK锛屽彲渚ц浇锛涙ā鍨嬩负绋嬪簭鍖?Blender 椋庢牸鍖栬祫浜э紝閫傚悎绉诲姩绔潰鏁般€?

