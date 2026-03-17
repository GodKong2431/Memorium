using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class TmpFontAssetGenerator
{
    private const string SourceFontFolder = "Assets/02. Prefabs/Font";
    private const string OutputFolder = "Assets/02. Prefabs/Font/TMP_FontAssets";
    private const string SessionKey = "TmpFontAssetGenerator_Initialized";
    private const string KoreanValidationSample = "가나다라마바사아자차카타파하힣한글테스트0123456789ABCabc";

    // Unity 에디터가 스크립트를 다시 읽은 뒤 한 번만 자동 실행한다.
    // [InitializeOnLoadMethod]
    // private static void GenerateOnLoad()
    // {
    //     if (SessionState.GetBool(SessionKey, false))
    //         return;

    //     SessionState.SetBool(SessionKey, true);
    //     EditorApplication.delayCall += GenerateOrUpdateFontAssets;
    // }

    // 메뉴에서 수동으로 TMP 폰트 자산 생성과 검증을 실행한다.
    [MenuItem("Tools/Fonts/Generate TMP Font Assets")]
    private static void GenerateFromMenu()
    {
        GenerateOrUpdateFontAssets();
    }

    // 폰트 폴더를 순회하며 TMP 폰트 자산을 생성하거나 한글 사용 가능 상태로 갱신한다.
    private static void GenerateOrUpdateFontAssets()
    {
        if (!AssetDatabase.IsValidFolder(SourceFontFolder))
            return;

        EnsureFolder(OutputFolder);

        string[] fontGuids = AssetDatabase.FindAssets("t:Font", new[] { SourceFontFolder });
        int createdCount = 0;
        int updatedCount = 0;

        foreach (string fontGuid in fontGuids)
        {
            string fontPath = AssetDatabase.GUIDToAssetPath(fontGuid);
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);

            if (sourceFont == null)
                continue;

            string outputPath = GetOutputPath(sourceFont.name);
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);

            if (fontAsset == null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                fontAsset.name = sourceFont.name + " TMP";
                AssetDatabase.CreateAsset(fontAsset, outputPath);
                var atlasTex = (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
        ? fontAsset.atlasTextures[0]
        : null;

                var mat = fontAsset.material;

                if (atlasTex != null)
                {
                    atlasTex.name = sourceFont.name + "_Atlas";
                    AssetDatabase.AddObjectToAsset(atlasTex, fontAsset);
                    EditorUtility.SetDirty(atlasTex);
                }

                if (mat != null)
                {
                    mat.name = sourceFont.name + "_Material";
                    AssetDatabase.AddObjectToAsset(mat, fontAsset);
                    EditorUtility.SetDirty(mat);
                }

                if (mat != null && atlasTex != null)
                {
                    mat.mainTexture = atlasTex;
                    EditorUtility.SetDirty(mat);
                }

                EditorUtility.SetDirty(fontAsset);
                createdCount++;
            }

            if (ConfigureForKorean(fontAsset, sourceFont.name))
                updatedCount++;
        }

        if (createdCount > 0 || updatedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TmpFontAssetGenerator] TMP 폰트 자산 생성 {createdCount}개, 갱신 {updatedCount}개를 완료했습니다.");
        }
    }

    // 한글 런타임 사용이 가능하도록 동적 설정과 샘플 글리프 추가를 수행한다.
    private static bool ConfigureForKorean(TMP_FontAsset fontAsset, string sourceFontName)
    {
        bool changed = false;

        if (fontAsset == null)
            return false;

        if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic)
        {
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            changed = true;
        }

        if (!fontAsset.TryAddCharacters(KoreanValidationSample, out string missingCharacters))
        {
            Debug.LogWarning($"[TmpFontAssetGenerator] {sourceFontName} TMP에 없는 한글/샘플 문자: {missingCharacters}");
        }
        else
        {
            changed = true;
        }

        if (changed)
            EditorUtility.SetDirty(fontAsset);

        return changed;
    }

    // 출력 폴더가 없으면 상위부터 순서대로 만든다.
    private static void EnsureFolder(string assetFolderPath)
    {
        string[] segments = assetFolderPath.Split('/');
        string currentPath = segments[0];

        for (int i = 1; i < segments.Length; i++)
        {
            string nextPath = currentPath + "/" + segments[i];

            if (!AssetDatabase.IsValidFolder(nextPath))
                AssetDatabase.CreateFolder(currentPath, segments[i]);

            currentPath = nextPath;
        }
    }

    // 원본 폰트 이름 기준으로 TMP 폰트 자산 경로를 만든다.
    private static string GetOutputPath(string fontName)
    {
        string safeFileName = string.Join("_", fontName.Split(Path.GetInvalidFileNameChars()));
        return $"{OutputFolder}/{safeFileName} TMP.asset";
    }
}
