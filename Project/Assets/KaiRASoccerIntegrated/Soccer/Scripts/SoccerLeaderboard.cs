using System;
using System.Collections.Generic;
using UnityEngine;

public static class SoccerLeaderboard
{
    public const int Capacity = 50;
    public static int SelectedDifficulty { get; set; }
    // Display times use 20 minutes per 500,000 steps, rounded to the nearest minute.
    public static readonly string[] DifficultyNames =
    {
        "レベル1（学習時間：20分）",
        "レベル2（学習時間：80分）",
        "レベル3（学習時間：160分）",
        "レベル4（学習時間：320分）",
        "レベル5（学習時間：754分）"
    };
    public static readonly string[] DifficultyTabNames = { "レベル1", "レベル2", "レベル3", "レベル4", "レベル5" };

    [Serializable]
    public class Entry
    {
        public string name;
        public int playerScore;
        public int opponentScore;
        public int Difference => playerScore - opponentScore;
    }

    [Serializable]
    class SavedRanking
    {
        public List<Entry> entries = new List<Entry>();
    }

    // Fixed-model difficulties use separate rankings from the legacy three categories.
    static string Key(int difficulty) => "SoccerLeaderboard.v2." + difficulty;

    public static List<Entry> Load(int difficulty)
    {
        try
        {
            var saved = JsonUtility.FromJson<SavedRanking>(PlayerPrefs.GetString(Key(difficulty), "{}"));
            var entries = saved?.entries ?? new List<Entry>();
            entries.RemoveAll(entry => entry == null || string.IsNullOrWhiteSpace(entry.name));
            // Stable insertion below preserves registration order for equal differences.
            for (var i = 1; i < entries.Count; i++)
            {
                var entry = entries[i];
                var j = i;
                while (j > 0 && entries[j - 1].Difference < entry.Difference)
                {
                    entries[j] = entries[j - 1];
                    j--;
                }
                entries[j] = entry;
            }
            if (entries.Count > Capacity)
                entries.RemoveRange(Capacity, entries.Count - Capacity);
            return entries;
        }
        catch (ArgumentException)
        {
            Debug.LogWarning("サッカーのランキングデータを読み込めませんでした。");
            return new List<Entry>();
        }
    }

    public static int Register(int difficulty, string playerName, int playerScore, int opponentScore)
    {
        var entries = Load(difficulty);
        var entry = new Entry { name = playerName.Trim(), playerScore = playerScore, opponentScore = opponentScore };
        var index = entries.FindIndex(existing => existing.Difference < entry.Difference);
        if (index < 0) index = entries.Count;
        entries.Insert(index, entry);
        if (entries.Count > Capacity) entries.RemoveAt(Capacity);
        PlayerPrefs.SetString(Key(difficulty), JsonUtility.ToJson(new SavedRanking { entries = entries }));
        PlayerPrefs.SetString("SoccerLeaderboard.PlayerName", entry.name);
        PlayerPrefs.Save();
        return index < Capacity ? index + 1 : 0;
    }

    static GUISkin s_RankingSkin;
    static GUISkin s_PreviousSkin;
    static int s_FontSize;
    static Texture2D s_PanelTexture;
    static Texture2D s_RowTexture;
    static Texture2D s_AlternateTexture;
    static GUIStyle s_CellStyle;
    static GUIStyle s_HeaderStyle;
    static GUIStyle s_NoteStyle;

    static Texture2D Solid(Color color)
    {
        var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    static void SetState(GUIStyleState state, Texture2D background, Color text)
    {
        state.background = background;
        state.textColor = text;
    }

    public static float UiScale => Mathf.Clamp(Screen.height / 720f, 0.6f, 3f);

    // Render glyphs at their final pixel size instead of enlarging a small text bitmap.
    public static void BeginPanel()
    {
        s_PreviousSkin = GUI.skin;
        var fontSize = Mathf.Max(14, Mathf.RoundToInt(18f * UiScale));
        if (s_RankingSkin == null)
        {
            s_RankingSkin = UnityEngine.Object.Instantiate(GUI.skin);
            s_RankingSkin.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            s_PanelTexture = Solid(new Color(0.055f, 0.08f, 0.12f, 1f));
            s_RowTexture = Solid(new Color(0.085f, 0.12f, 0.17f, 1f));
            s_AlternateTexture = Solid(new Color(0.105f, 0.145f, 0.20f, 1f));
            var buttonTexture = Solid(new Color(0.14f, 0.20f, 0.28f, 1f));
            var hoverTexture = Solid(new Color(0.20f, 0.30f, 0.41f, 1f));
            var selectedTexture = Solid(new Color(0.08f, 0.48f, 0.58f, 1f));
            var white = new Color(0.92f, 0.96f, 1f);
            SetState(s_RankingSkin.box.normal, s_PanelTexture, white);
            SetState(s_RankingSkin.button.normal, buttonTexture, white);
            SetState(s_RankingSkin.button.hover, hoverTexture, white);
            SetState(s_RankingSkin.button.active, selectedTexture, white);
            SetState(s_RankingSkin.button.onNormal, selectedTexture, Color.white);
            SetState(s_RankingSkin.button.onHover, selectedTexture, Color.white);
            SetState(s_RankingSkin.button.onActive, selectedTexture, Color.white);
            SetState(s_RankingSkin.textField.normal, s_RowTexture, white);
            SetState(s_RankingSkin.textField.focused, buttonTexture, Color.white);
            s_RankingSkin.label.normal.textColor = white;
        }
        if (s_FontSize != fontSize)
        {
            s_FontSize = fontSize;
            foreach (var style in new[] { s_RankingSkin.label, s_RankingSkin.button,
                s_RankingSkin.textField, s_RankingSkin.textArea, s_RankingSkin.box })
            {
                style.fontSize = fontSize;
                style.richText = false;
            }
            s_RankingSkin.label.wordWrap = true;
            var padding = Mathf.RoundToInt(24f * UiScale);
            s_RankingSkin.box.padding = new RectOffset(padding, padding, padding, padding);
            s_RankingSkin.button.padding = new RectOffset(12, 12, Mathf.RoundToInt(10f * UiScale), Mathf.RoundToInt(10f * UiScale));
            s_RankingSkin.button.margin = new RectOffset(3, 3, 6, 6);
            s_RankingSkin.textField.padding = new RectOffset(10, 10, 4, 4);
            s_CellStyle = new GUIStyle(s_RankingSkin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(Mathf.RoundToInt(12f * UiScale), 4, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            s_HeaderStyle = new GUIStyle(s_CellStyle) { fontSize = Mathf.Max(12, fontSize - 2), fontStyle = FontStyle.Bold };
            s_HeaderStyle.normal.textColor = new Color(0.50f, 0.76f, 0.82f);
            s_NoteStyle = new GUIStyle(s_RankingSkin.label) { fontSize = Mathf.Max(11, fontSize - 3), wordWrap = true };
            s_NoteStyle.normal.textColor = new Color(0.60f, 0.68f, 0.77f);
        }
        GUI.skin = s_RankingSkin;
        var margin = Mathf.Round(20f * UiScale);
        var width = Mathf.Min(Screen.width - margin * 2f, 900f * UiScale);
        GUILayout.BeginArea(new Rect(Mathf.Round((Screen.width - width) / 2f), margin,
            width, Screen.height - margin * 2f), GUI.skin.box);
    }

    public static void EndPanel()
    {
        GUILayout.EndArea();
        GUI.skin = s_PreviousSkin;
    }

    static void DrawCells(Rect row, string rank, string name, string difference, string score, GUIStyle style, bool podium = false)
    {
        var previous = style.normal.textColor;
        if (podium) style.normal.textColor = new Color(1f, 0.80f, 0.40f);
        GUI.Label(new Rect(row.x, row.y, row.width * 0.13f, row.height), rank, style);
        style.normal.textColor = previous;
        GUI.Label(new Rect(row.x + row.width * 0.13f, row.y, row.width * 0.45f, row.height), name, style);
        var alignment = style.alignment;
        style.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(row.x + row.width * 0.58f, row.y, row.width * 0.20f, row.height), difference, style);
        GUI.Label(new Rect(row.x + row.width * 0.78f, row.y, row.width * 0.22f, row.height), score, style);
        style.alignment = alignment;
    }

    public static void Draw(List<Entry> entries, ref Vector2 scroll)
    {
        GUILayout.Space(14f * UiScale);
        var header = GUILayoutUtility.GetRect(0f, 38f * UiScale, GUILayout.ExpandWidth(true));
        // Reserve the scrollbar width so headings line up with the scrollable rows.
        header.width -= GUI.skin.verticalScrollbar.fixedWidth + GUI.skin.verticalScrollbar.margin.left;
        DrawCells(header, "順位", "プレイヤー", "得失点差", "得点 / 失点", s_HeaderStyle);
        scroll = GUILayout.BeginScrollView(scroll, false, true, GUILayout.ExpandHeight(true));
        for (var i = 0; i < Capacity; i++)
        {
            var row = GUILayoutUtility.GetRect(0f, Mathf.Ceil(44f * UiScale), GUILayout.ExpandWidth(true));
            GUI.DrawTexture(row, i % 2 == 0 ? s_RowTexture : s_AlternateTexture);
            var entry = i < entries.Count ? entries[i] : null;
            DrawCells(row, (i + 1).ToString("00"), entry != null ? entry.name : "—",
                entry != null ? entry.Difference.ToString("+0;-0;0") : "—",
                entry != null ? $"{entry.playerScore} / {entry.opponentScore}" : "—", s_CellStyle,
                entry != null && i < 3);
        }
        GUILayout.EndScrollView();
        GUILayout.Space(10f * UiScale);
        GUILayout.Label("TOP 50  ·  同点は登録順  ·  スクロールで全順位を表示", s_NoteStyle);
    }
}
