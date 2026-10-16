using UnityEditor;
public class ImportSettings : AssetPostprocessor {
  void OnPreprocessTexture() { if (!assetPath.Contains("Resources")) return; var t=(TextureImporter)assetImporter; t.isReadable=true; t.mipmapEnabled=true; t.textureCompression=TextureImporterCompression.Uncompressed; t.maxTextureSize=1024; }
  void OnPreprocessModel() { var m=(ModelImporter)assetImporter; m.isReadable=true; m.materialImportMode=ModelImporterMaterialImportMode.None; m.indexFormat=ModelImporterIndexFormat.UInt32; }
}
