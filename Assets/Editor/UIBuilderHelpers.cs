using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// Hàm dựng UI dùng chung cho mọi Editor builder (PanelPrefabBuilder, SceneBuilder...).
/// Tách ra file riêng để không lặp code giữa các tool.
public static class UIBuilderHelpers
{
    public static readonly Color BgColor = new Color(0.05f, 0.035f, 0.07f, 1f);
    public static readonly Color ButtonColor = new Color(0.22f, 0.16f, 0.30f, 0.95f);
    public static readonly Color GoldColor = new Color(0.85f, 0.72f, 0.42f, 1f);

    public static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    public static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = CreateUIObject(name, parent);
        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = content;
        txt.fontSize = fontSize;
        txt.alignment = align;
        txt.color = Color.white;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        SetRect(go.GetComponent<RectTransform>(), anchorMin, anchorMax, anchoredPos, sizeDelta);
        return txt;
    }

    public static (Image fill, Text label, Image portrait, Text tagText) CreateHealthBarSlot(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var container = CreateUIObject(name, parent);
        SetRect(container.GetComponent<RectTransform>(), anchorMin, anchorMax, anchoredPos, sizeDelta);
        var bg = container.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.1f, 0.92f);

        var portraitGo = CreateUIObject("Portrait", container.transform);
        var portraitRt = portraitGo.GetComponent<RectTransform>();
        portraitRt.anchorMin = new Vector2(0f, 0.5f);
        portraitRt.anchorMax = new Vector2(0f, 0.5f);
        portraitRt.anchoredPosition = new Vector2(-28, 0);
        portraitRt.sizeDelta = new Vector2(50, 50);
        var portraitBg = portraitGo.AddComponent<Image>();
        portraitBg.color = new Color(0.55f, 0.48f, 0.3f, 1f);
        var portraitInnerGo = CreateUIObject("Icon", portraitGo.transform);
        var portraitInnerRt = portraitInnerGo.GetComponent<RectTransform>();
        portraitInnerRt.anchorMin = Vector2.zero;
        portraitInnerRt.anchorMax = Vector2.one;
        portraitInnerRt.offsetMin = new Vector2(3, 3);
        portraitInnerRt.offsetMax = new Vector2(-3, -3);
        var portraitImg = portraitInnerGo.AddComponent<Image>();
        portraitImg.preserveAspect = true;
        portraitImg.color = Color.white;
        portraitImg.enabled = false;

        var fillGo = CreateUIObject("Fill", container.transform);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(26, 3);
        fillRt.offsetMax = new Vector2(-3, -3);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.35f, 0.75f, 0.35f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;

        var labelGo = CreateUIObject("Label", container.transform);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(23, 0);
        labelRt.offsetMax = Vector2.zero;
        var labelTxt = labelGo.AddComponent<Text>();
        labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.color = Color.white;
        labelTxt.fontSize = 14;
        labelTxt.fontStyle = FontStyle.Bold;
        var labelOutline = labelGo.AddComponent<Outline>();
        labelOutline.effectColor = new Color(0, 0, 0, 0.85f);
        labelOutline.effectDistance = new Vector2(1f, -1f);

        var tagGo = CreateUIObject("TagText", container.transform);
        var tagRt = tagGo.GetComponent<RectTransform>();
        tagRt.anchorMin = new Vector2(0.5f, 1f);
        tagRt.anchorMax = new Vector2(0.5f, 1f);
        tagRt.anchoredPosition = new Vector2(0, 13);
        tagRt.sizeDelta = new Vector2(sizeDelta.x, 18);
        var tagTxt = tagGo.AddComponent<Text>();
        tagTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tagTxt.alignment = TextAnchor.MiddleCenter;
        tagTxt.fontSize = 13;
        tagTxt.fontStyle = FontStyle.Bold;
        tagTxt.color = Color.white;
        var tagOutline = tagGo.AddComponent<Outline>();
        tagOutline.effectColor = new Color(0, 0, 0, 0.85f);
        tagOutline.effectDistance = new Vector2(1f, -1f);

        return (fillImg, labelTxt, portraitImg, tagTxt);
    }

    public static (Button btn, Image diamond) CreateGateDiamond(Transform parent, Vector2 anchoredPos)
    {
        var go = CreateUIObject("Gate", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(120, 120);
        rt.localRotation = Quaternion.Euler(0, 0, 45f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.08f, 0.14f, 0.35f);
        var btn = go.AddComponent<Button>();

        return (btn, img);
    }

    public static Button CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = CreateUIObject(name, parent);
        var img = go.AddComponent<Image>();
        img.color = ButtonColor;
        go.AddComponent<Button>();
        SetRect(go.GetComponent<RectTransform>(), anchorMin, anchorMax, anchoredPos, sizeDelta);

        var txtGo = CreateUIObject("Label", go.transform);
        var txt = txtGo.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontSize = 16;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 10;
        txt.resizeTextMaxSize = 20;
        var btnOutline = txtGo.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0, 0, 0, 0.8f);
        btnOutline.effectDistance = new Vector2(1f, -1f);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(12, 8);
        txtRt.offsetMax = new Vector2(-12, -8);

        return go.GetComponent<Button>();
    }

    public static Tile GetOrCreateTileAsset(Sprite sprite, string folder)
    {
        Directory.CreateDirectory(folder);
        string path = $"{folder}/Tile_{sprite.name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null) return existing;
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    public static void BuildDungeonTilemap()
    {
        const string framesFolder = "Assets/Art/Environment/DungeonTileset/frames";
        const string tileFolder = "Assets/Art/Environment/DungeonTileset/GeneratedTiles";

        Tile LoadTile(string fileName)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{framesFolder}/{fileName}.png");
            if (sprite == null)
            {
                Debug.LogWarning($"[UIBuilderHelpers] Không tìm thấy tile: {fileName}.png");
                return null;
            }
            return GetOrCreateTileAsset(sprite, tileFolder);
        }

        var floorTiles = System.Linq.Enumerable.Range(1, 8)
            .Select(i => LoadTile($"floor_{i}"))
            .Where(t => t != null)
            .ToArray();

        var wallTopLeft = LoadTile("wall_top_left");
        var wallTopMid = LoadTile("wall_top_mid");
        var wallTopRight = LoadTile("wall_top_right");
        var wallLeft = LoadTile("wall_left");
        var wallMid = LoadTile("wall_mid");
        var wallRight = LoadTile("wall_right");

        if (floorTiles.Length == 0)
        {
            Debug.LogWarning("[UIBuilderHelpers] Không tìm thấy tile sàn — bỏ qua dựng Tilemap nền.");
            return;
        }

        var gridGo = new GameObject("DungeonGrid");
        gridGo.transform.position = new Vector3(0, 0, 5f);
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 1f);

        var floorTmGo = new GameObject("FloorTilemap");
        floorTmGo.transform.SetParent(gridGo.transform, false);
        var floorTm = floorTmGo.AddComponent<Tilemap>();
        var floorRend = floorTmGo.AddComponent<TilemapRenderer>();
        floorRend.sortingOrder = -100;

        const int left = -12, right = 12;
        const int floorTop = 1, floorBottom = -6;

        var rng = new System.Random(42);
        for (int x = left; x <= right; x++)
            for (int y = floorBottom; y <= floorTop; y++)
                floorTm.SetTile(new Vector3Int(x, y, 0), floorTiles[rng.Next(floorTiles.Length)]);

        if (wallTopMid != null && wallMid != null)
        {
            var wallTmGo = new GameObject("WallTilemap");
            wallTmGo.transform.SetParent(gridGo.transform, false);
            var wallTm = wallTmGo.AddComponent<Tilemap>();
            var wallRend = wallTmGo.AddComponent<TilemapRenderer>();
            wallRend.sortingOrder = -90;

            int wallFaceY = floorTop + 1;
            int wallTopY = wallFaceY + 1;

            for (int x = left; x <= right; x++)
            {
                var face = (x == left) ? wallLeft : (x == right) ? wallRight : wallMid;
                var top = (x == left) ? wallTopLeft : (x == right) ? wallTopRight : wallTopMid;
                if (face != null) wallTm.SetTile(new Vector3Int(x, wallFaceY, 0), face);
                if (top != null) wallTm.SetTile(new Vector3Int(x, wallTopY, 0), top);
            }
        }

        AssetDatabase.SaveAssets();
    }

    public static int ParseTrailingIndex(string spriteName)
    {
        var m = Regex.Match(spriteName, @"_(\d+)$");
        return m.Success ? int.Parse(m.Groups[1].Value) : -1;
    }
}
