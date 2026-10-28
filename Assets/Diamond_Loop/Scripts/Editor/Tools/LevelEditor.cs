#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ClawbearGames
{
    /// <summary>
    /// Editor window for creating and editing puzzle levels.
    ///
    /// Modes
    /// ─────
    ///  Image Mode    — Load source image, set normalizeStep, initialize ImageData and palette.
    ///  Grid Mode     — Set dimensions and toggle tiles on/off.
    ///  Pot Mode      — Configure per-color min/max, choose distribution method, init pots.
    ///  State Mode    — Assign per-pot PotState; Decay pots show a decay-time sub-panel.
    ///  Settings Mode — Configure power-ups, coin rewards, and star-rating thresholds.
    ///
    /// Data init order: Image → Grid → Pots → States
    /// </summary>
    public class LevelEditor : EditorWindow
    {
        #region Constants & Fields

        private const int CELL_SIZE = 64;
        private const int CELL_SPACING = 5;
        private const int LEFT_WIDTH = 220;
        private const float PANEL_MIN_W = 980f;
        private const float PANEL_MIN_H = 600f;
        private const string ASSET_FOLDER = "Assets/Diamond_Loop/Resources/Levels/";

        private List<LevelData> allLevels = new List<LevelData>();
        private LevelData currentLevel = null;

        private Vector2 leftScrollPos = Vector2.zero;
        private Vector2 rightScrollPos = Vector2.zero;

        private int gridWidth = 8;
        private int gridHeight = 8;

        private enum EditorMode { Image = 0, Grid = 1, Pot = 2, State = 3, Settings = 4 }
        private EditorMode currentMode = EditorMode.Image;

        // State Mode — currently selected tile
        private Vector2Int selectedTile = new Vector2Int(-1, -1);

        // Image Mode — preview texture rebuilt after init
        private Texture2D _previewTex;

        // Cached GUIStyles — never create styles inside a draw loop (allocates every frame)
        private GUIStyle _cellStyle;
        private GUIStyle _overlayStyle;
        private GUIStyle _badgeStyle;
        private GUIStyle _powerUpHeaderStyle;

        /// <summary>Shared grid cell button style. Lazy-initialized and cached.</summary>
        private GUIStyle CellStyle
        {
            get
            {
                if (_cellStyle != null) return _cellStyle;
                _cellStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _cellStyle.normal.textColor = Color.white;
                _cellStyle.active.textColor = Color.white;
                _cellStyle.hover.textColor = Color.white;
                return _cellStyle;
            }
        }

        #endregion

        #region Window Setup

        /// <summary>Opens the Level Editor window from the Tools menu.</summary>
        [MenuItem("Tools/Level Editor")]
        public static void ShowWindow()
        {
            var win = GetWindow<LevelEditor>("Level Editor");
            win.minSize = new Vector2(PANEL_MIN_W, PANEL_MIN_H);
        }

        /// <summary>Called when the window is first opened or re-enabled.</summary>
        private void OnEnable() => RefreshLevelList();

        /// <summary>Cleans up the preview texture to avoid leaking GPU memory when the window is closed or disabled.</summary>
        private void OnDisable()
        {
            if (_previewTex != null)
            {
                DestroyImmediate(_previewTex);
                if (_previewTex != null) { DestroyImmediate(_previewTex); _previewTex = null; }
            }
        }

        #endregion

        #region GUI Entry Point

        /// <summary>Main Unity IMGUI entry point — draws left and right panels side by side.</summary>
        private void OnGUI()
        {
            if (allLevels == null) RefreshLevelList();
            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Left Panel

        /// <summary>Draws the scrollable level list and the New Level button on the left side.</summary>
        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LEFT_WIDTH));
            GUILayout.Label("LEVELS", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            LevelData toDelete = null;
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);

            foreach (LevelData level in allLevels)
            {
                GUI.backgroundColor = level == currentLevel ? Color.cyan : Color.white;
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button($"Level {level.levelNumber}", GUILayout.Height(30)))
                    SelectLevel(level);

                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("✕", GUILayout.Width(30), GUILayout.Height(30)))
                    toDelete = level;

                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // Deferred delete to avoid modifying the collection during iteration
            if (toDelete != null) { DeleteLevel(toDelete); Repaint(); }

            EditorGUILayout.Space(8);
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("＋  New Level", GUILayout.Height(38)))
                CreateNewLevel();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Right Panel

        /// <summary>
        /// Draws the right editing area: level title, mode selector, grid info, and active mode content.
        /// </summary>
        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            if (currentLevel == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("Select or create a level to begin editing.", MessageType.Info);
                GUILayout.FlexibleSpace();
            }
            else
            {
                rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
                GUILayout.Label($"Level {currentLevel.levelNumber}", EditorStyles.largeLabel);
                EditorGUILayout.Space(6);
                DrawModeSelector();
                EditorGUILayout.Space(8);

                // Show the grid size section for all modes except Image and Settings
                if (currentMode != EditorMode.Image && currentMode != EditorMode.Settings)
                {
                    DrawGridSizeSection();
                    EditorGUILayout.Space(8);
                }

                DrawRightContent();
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Mode Selector


        /// <summary>
        /// Draws the row of mode buttons at the top of the right panel.
        /// </summary>
        private void DrawModeSelector()
        {
            EditorGUILayout.BeginHorizontal();
            DrawModeButton(EditorMode.Image, "🖼️  Image");
            DrawModeButton(EditorMode.Grid, "🔲  Grid");
            DrawModeButton(EditorMode.Pot, "🪣  Pots");
            DrawModeButton(EditorMode.State, "🔒  States");
            DrawModeButton(EditorMode.Settings, "⚙️  Settings");
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws a single mode button, highlighted if it matches the current mode.
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="label"></param>
        private void DrawModeButton(EditorMode mode, string label)
        {
            GUI.backgroundColor = currentMode == mode ? Color.cyan : Color.white;
            if (GUILayout.Button(label, GUILayout.Height(32)))
            {
                currentMode = mode;
                GUI.FocusControl(null);
            }
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Grid Size Section

        /// <summary>
        /// Shows grid dimensions. In Grid Mode the sliders are active and trigger a resize.
        /// In other modes the size is shown read-only.
        /// </summary>
        private void DrawGridSizeSection()
        {
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (currentLevel.IsGridInitialized())
            {
                gridWidth = currentLevel.GetGridWidth();
                gridHeight = currentLevel.GetGridHeight();
            }

            if (currentMode == EditorMode.Grid)
            {
                EditorGUI.BeginChangeCheck();
                gridWidth = EditorGUILayout.IntSlider("Width", gridWidth, GridData.MIN_SIZE, 20);
                gridHeight = EditorGUILayout.IntSlider("Height", gridHeight, GridData.MIN_SIZE, 20);
                if (EditorGUI.EndChangeCheck())
                {
                    ResizeGrid(gridWidth, gridHeight);
                    if (Event.current.type == EventType.MouseUp)
                        SaveAndMarkDirty();
                }

                EditorGUILayout.HelpBox(
                    "Resizing removes pots that fall outside the new bounds.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"Size: {gridWidth} × {gridHeight}  (edit in Grid Mode)");
            }

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Right Content Router

        /// <summary>Delegates drawing to the appropriate mode method.</summary>
        private void DrawRightContent()
        {
            switch (currentMode)
            {
                case EditorMode.Image: DrawImageMode(); break;
                case EditorMode.Grid: DrawGridMode(); break;
                case EditorMode.Pot: DrawPotMode(); break;
                case EditorMode.State: DrawStateMode(); break;
                case EditorMode.Settings: DrawSettingsMode(); break;
            }
        }

        #endregion

        #region Image Mode

        /// <summary>
        /// Draws the Image Mode UI: source image picker, normalizeStep slider,
        /// Init button, palette preview, and normalized image preview.
        /// </summary>
        private void DrawImageMode()
        {
            ImageData img = currentLevel.imageData;
            if (img == null) { currentLevel.imageData = new ImageData(); img = currentLevel.imageData; }

            EditorGUILayout.LabelField("🖼️  Image Data", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // Source image object field
            EditorGUI.BeginChangeCheck();
            Texture2D newTex = (Texture2D)EditorGUILayout.ObjectField(
                "Source Image", img.sourceImage, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentLevel, "Set Source Image");
                img.sourceImage = newTex;
                if (_previewTex != null) { DestroyImmediate(_previewTex); _previewTex = null; }
                SaveAndMarkDirty();
            }

            // Load Image — opens a native file picker rooted at the project's Assets folder
            if (GUILayout.Button("Load Image", GUILayout.Height(26)))
                OpenLoadImageDialog(img);

            EditorGUILayout.Space(6);

            // normalizeStep slider — must be set before Init
            EditorGUILayout.LabelField(
                "Normalize Step  (set before Init — lower value = fewer colors)",
                EditorStyles.miniLabel);
            EditorGUI.BeginChangeCheck();
            float newStep = EditorGUILayout.Slider(img.normalizeStep, 2f, 50f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentLevel, "Change Normalize Step");
                img.normalizeStep = newStep;
                SaveAndMarkDirty();
            }



            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(
                "Merge Threshold  (RGB distance 0–255; colors closer than this are merged)",
                EditorStyles.miniLabel);
            EditorGUI.BeginChangeCheck();
            float newMerge = EditorGUILayout.Slider(img.mergeThreshold, 0f, 80f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentLevel, "Change Merge Threshold");
                img.mergeThreshold = newMerge;
                SaveAndMarkDirty();
            }


            EditorGUILayout.Space(8);

            // Init button with guards
            if (img.sourceImage == null)
            {
                EditorGUILayout.HelpBox("Assign a source image above.", MessageType.Error);
            }
            else if (!img.sourceImage.isReadable)
            {
                EditorGUILayout.HelpBox(
                    "Enable Read/Write in the texture's Import Settings, then re-run Init.",
                    MessageType.Warning);
            }
            else
            {
                GUI.backgroundColor = new Color(0.4f, 0.85f, 1f);
                if (GUILayout.Button("⚡  Init Image Data", GUILayout.Height(34)))
                    TryInitImageData();
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();

            // Palette and image previews — only shown after a successful init
            if (img.IsInitialized)
            {
                EditorGUILayout.Space(10);
                DrawPalettePreview(img);
                EditorGUILayout.Space(10);
                DrawImagePreview(img);
            }
        }

        /// <summary>
        /// Opens a native OS file picker rooted at the project's Assets folder.
        /// The selected file is loaded as a Texture2D via AssetDatabase and assigned
        /// to img.sourceImage. Shows an error dialog if the path is outside Assets
        /// or the asset cannot be loaded as a Texture2D.
        /// </summary>
        private void OpenLoadImageDialog(ImageData img)
        {
            // Application.dataPath = "<project>/Assets" (no trailing slash)
            string assetsPath = Application.dataPath;

            string absPath = EditorUtility.OpenFilePanelWithFilters(
                "Select Image", assetsPath,
                new[] { "Image files", "png,jpg,jpeg,tga,psd,bmp,gif", "All files", "*" });

            if (string.IsNullOrEmpty(absPath)) return; // user cancelled

            // Convert absolute OS path → project-relative "Assets/..." path
            // Use OrdinalIgnoreCase so drive-letter casing differences on Windows don't cause a false mismatch
            if (!absPath.StartsWith(assetsPath, System.StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Invalid Path",
                    "Please select an image that lives inside the project's Assets folder.", "OK");
                return;
            }

            string relativePath = "Assets" + absPath.Substring(assetsPath.Length);

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(relativePath);
            if (tex == null)
            {
                EditorUtility.DisplayDialog("Load Failed",
                    $"Could not load a Texture2D at:\n{relativePath}\n\n" +
                    "Make sure the file is imported by Unity and its type is Texture.", "OK");
                return;
            }

            Undo.RecordObject(currentLevel, "Set Source Image");
            img.sourceImage = tex;
            if (_previewTex != null) { DestroyImmediate(_previewTex); _previewTex = null; }
            SaveAndMarkDirty();
            Repaint();
        }

        /// <summary>
        /// Reads pixels from the source image, normalizes them, builds the palette,
        /// clears existing pots, and syncs potColorConfigs.
        /// </summary>
        private void TryInitImageData()
        {
            ImageData img = currentLevel.imageData;

            if (currentLevel.pots.Count > 0)
            {
                if (!EditorUtility.DisplayDialog("Re-initialize Image Data",
                    "Re-initializing will clear all pots. Continue?", "Continue", "Cancel"))
                    return;
            }

            Undo.RecordObject(currentLevel, "Init Image Data");

            Color32[] raw = img.sourceImage.GetPixels32();
            int w = img.sourceImage.width;
            int h = img.sourceImage.height;

            img.imageWidth = w;
            img.imageHeight = h;
            img.pixelColors = new Color32[w * h];
            img.colorPalette = new List<Color32>();
            img.colorCounts = new List<int>();

            // Scan y=0 (bottom) → y=h-1 (top) explicitly.
            // Unity's GetPixels32 is already bottom-to-top, but this makes the intent
            // clear: palette[0] is always the first color encountered at the bottom row,
            // which guarantees pots[0] at runtime always matches a jumping cube.
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    Color c = new Color(raw[i].r / 255f, raw[i].g / 255f, raw[i].b / 255f, 1f);
                    Color32 normalized = img.Normalize(c);
                    img.pixelColors[i] = normalized;

                    int idx = img.GetColorIndex(normalized);
                    if (idx < 0)
                    {
                        img.colorPalette.Add(normalized);
                        img.colorCounts.Add(1);
                    }
                    else
                    {
                        img.colorCounts[idx]++;
                    }
                }
            }

            img.MergeSimilarColors();   // collapse visually similar palette entries
            currentLevel.pots.Clear();
            currentLevel.SyncPotColorConfigs();

            if (_previewTex != null) { DestroyImmediate(_previewTex); _previewTex = null; }
            SaveAndMarkDirty();
            Repaint();
        }

        /// <summary>Draws a colored swatch + hex code + pixel count for each palette color.</summary>
        private void DrawPalettePreview(ImageData img)
        {
            EditorGUILayout.LabelField("🎨  Color Palette", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            for (int i = 0; i < img.colorPalette.Count; i++)
            {
                Color32 c = img.colorPalette[i];
                int count = img.colorCounts[i];

                EditorGUILayout.BeginHorizontal();

                Rect swatchRect = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28), GUILayout.Height(28));
                EditorGUI.DrawRect(swatchRect, new Color32(c.r, c.g, c.b, 255));

                GUILayout.Label($"#{c.r:X2}{c.g:X2}{c.b:X2}", GUILayout.Width(70));
                GUILayout.Label($"{count} px", EditorStyles.miniLabel, GUILayout.Width(60));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.LabelField(
                $"Total: {img.colorPalette.Count} color(s) from {img.imageWidth}×{img.imageHeight} image",
                EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        /// <summary>Draws a small scaled preview of the normalized image pixels.</summary>
        private void DrawImagePreview(ImageData img)
        {
            if (_previewTex == null) RebuildPreviewTex(img);
            if (_previewTex == null) return;

            EditorGUILayout.LabelField("🔍  Normalized Preview", EditorStyles.boldLabel);

            const float PREVIEW_SIZE = 160f;
            Rect previewRect = GUILayoutUtility.GetRect(PREVIEW_SIZE, PREVIEW_SIZE,
                GUILayout.Width(PREVIEW_SIZE), GUILayout.Height(PREVIEW_SIZE));
            GUI.DrawTexture(previewRect, _previewTex, ScaleMode.ScaleToFit, false);
        }

        /// <summary>Builds the _previewTex Texture2D from the stored normalized pixelColors.</summary>
        private void RebuildPreviewTex(ImageData img)
        {
            if (img.pixelColors == null || img.pixelColors.Length == 0) return;

            _previewTex = new Texture2D(img.imageWidth, img.imageHeight, TextureFormat.RGB24, false)
            {
                filterMode = FilterMode.Point
            };

            Color32[] pixels = new Color32[img.pixelColors.Length];
            for (int y = 0; y < img.imageHeight; y++)
                for (int x = 0; x < img.imageWidth; x++)
                    pixels[y * img.imageWidth + x] = img.GetPixel(x, y);

            _previewTex.SetPixels32(pixels);
            _previewTex.Apply();
        }

        #endregion

        #region Grid Mode

        /// <summary>Draws the Grid Mode UI: Fill/Clear buttons, instructions, and the interactive grid.</summary>
        private void DrawGridMode()
        {
            EditorGUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Fill All", GUILayout.Height(28))) FillAllTiles();
            if (GUILayout.Button("Clear All", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("Clear All Tiles",
                    "This will disable every tile and remove all pots. Continue?", "Yes", "Cancel"))
                    ClearAll();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Left Click: Enable tile    Right Click: Disable tile\n" +
                "Pots shown as read-only overlays — edit them in Pot Mode.",
                MessageType.Info);
            EditorGUILayout.Space(6);

            if (!currentLevel.IsGridInitialized())
            {
                EditorGUILayout.HelpBox("Initialize the grid first.", MessageType.Info);
                return;
            }

            DrawGrid(DrawGridModeCell);
        }

        /// <summary>Draws one cell in Grid Mode: left-click enables, right-click disables and removes pots.</summary>
        private void DrawGridModeCell(int x, int y)
        {
            GridData grid = currentLevel.GridData;
            Vector2Int pos = new Vector2Int(x, y);
            bool enabled = grid.IsEnabled(x, y);
            List<PotData> potsHere = currentLevel.GetPotsAtTile(pos);

            Color bg;
            string label;

            if (!enabled)
            {
                bg = new Color(0.18f, 0.18f, 0.18f);  // disabled — darkest
                label = "x";
            }
            else if (potsHere.Count > 0)
            {
                bg = new Color(0.30f, 0.30f, 0.36f);  // occupied — medium dark
                label = string.Empty;
            }
            else
            {
                bg = new Color(0.55f, 0.55f, 0.55f);  // empty — lightest
                label = string.Empty;
            }

            GUI.backgroundColor = bg;
            Rect rect = GUILayoutUtility.GetRect(CELL_SIZE, CELL_SIZE,
                GUILayout.Width(CELL_SIZE), GUILayout.Height(CELL_SIZE));

            if (GUI.Button(rect, label, CellStyle))
            {
                if (Event.current.button == 0)
                {
                    Undo.RecordObject(currentLevel, "Enable Tile");
                    grid.SetEnabled(x, y, true);
                    SaveAndMarkDirty();
                }
                else if (Event.current.button == 1)
                {
                    if (enabled)
                    {
                        Undo.RecordObject(currentLevel, "Disable Tile");
                        currentLevel.pots.RemoveAll(p => p.tilePosition == pos);
                        grid.SetEnabled(x, y, false);
                        SaveAndMarkDirty();
                    }
                }
            }

            if (potsHere.Count > 0)
            {
                DrawPotIcon(rect);
                DrawPotCountBadge(rect, potsHere.Count);
            }

            GUI.backgroundColor = Color.white;
        }


        #endregion

        #region Pot Mode

        /// <summary>
        /// Draws the Pot Mode UI: per-color min/max sliders, distribution method,
        /// live pot/tile info, Init Pots button, and the interactive grid.
        /// </summary>
        private void DrawPotMode()
        {
            ImageData img = currentLevel.imageData;

            if (img == null || !img.IsInitialized)
            {
                EditorGUILayout.HelpBox(
                    "Initialize image data in Image Mode first.",
                    MessageType.Warning);
                return;
            }

            // ── Per-color config ──────────────────────────────────
            EditorGUILayout.LabelField("🎨  Per-Color Pot Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            for (int i = 0; i < currentLevel.potColorConfigs.Count; i++)
            {
                PotColorConfig cfg = currentLevel.potColorConfigs[i];
                Color32 c = cfg.color;
                int pixCount = img.GetColorCount(c);

                EditorGUILayout.BeginHorizontal();

                Rect swatchRect = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28), GUILayout.Height(28));
                EditorGUI.DrawRect(swatchRect, new Color32(c.r, c.g, c.b, 255));

                GUILayout.Label($"{pixCount}px", EditorStyles.miniLabel, GUILayout.Width(44));

                GUILayout.Label("Min:", GUILayout.Width(28));
                EditorGUI.BeginChangeCheck();
                int newMin = EditorGUILayout.IntSlider(cfg.minPots, 1, 20, GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(currentLevel, "Change Min Pots");
                    cfg.minPots = newMin;
                    cfg.maxPots = Mathf.Max(cfg.maxPots, newMin);
                    SaveAndMarkDirty();
                }

                GUILayout.Label("Max:", GUILayout.Width(30));
                EditorGUI.BeginChangeCheck();
                int newMax = EditorGUILayout.IntSlider(cfg.maxPots, 1, 20, GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(currentLevel, "Change Max Pots");
                    cfg.maxPots = Mathf.Max(newMax, cfg.minPots);
                    SaveAndMarkDirty();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndVertical();

            // ── Distribution method ───────────────────────────────
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Distribution Method", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            bool isRandom = currentLevel.potDistributionMethod == PotDistributionMethod.RandomShuffle;

            GUI.backgroundColor = isRandom ? Color.cyan : Color.white;
            if (GUILayout.Button("🎲 Random Shuffle", GUILayout.Height(30)) && !isRandom)
            {
                Undo.RecordObject(currentLevel, "Change Distribution Method");
                currentLevel.potDistributionMethod = PotDistributionMethod.RandomShuffle;
                SaveAndMarkDirty();
            }

            GUI.backgroundColor = !isRandom ? Color.cyan : Color.white;
            if (GUILayout.Button("📐 Evenly Spread", GUILayout.Height(30)) && isRandom)
            {
                Undo.RecordObject(currentLevel, "Change Distribution Method");
                currentLevel.potDistributionMethod = PotDistributionMethod.EvenlySpreadByColor;
                SaveAndMarkDirty();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();


            // ── Live info labels ──────────────────────────────────
            EditorGUILayout.Space(6);
            int totalMin = currentLevel.potColorConfigs.Sum(c => c.minPots);
            int totalMax = currentLevel.potColorConfigs.Sum(c => c.maxPots);
            int enabledTileCount = currentLevel.IsGridInitialized()
                ? currentLevel.GetEnabledTiles().Count : 0;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Total pots to place:  {totalMin} – {totalMax}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Enabled tiles:  {enabledTileCount}", EditorStyles.miniLabel);
            if (enabledTileCount > 0)
            {
                float avgMin = (float)totalMin / enabledTileCount;
                float avgMax = (float)totalMax / enabledTileCount;
                EditorGUILayout.LabelField($"Avg pots/tile:  {avgMin:F1} – {avgMax:F1}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();

            if (enabledTileCount > 0 && totalMax < enabledTileCount)
                EditorGUILayout.HelpBox(
                    "Total max pots is less than enabled tile count — many tiles will be empty.",
                    MessageType.Warning);
            else if (enabledTileCount > 0 && totalMin > enabledTileCount)
                EditorGUILayout.HelpBox(
                    "Total min pots exceeds enabled tile count — stacking will occur.",
                    MessageType.Info);

            // ── Init Pots button ──────────────────────────────────
            EditorGUILayout.Space(8);
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
            if (GUILayout.Button("⚡  Init Pots", GUILayout.Height(34)))
                TryInitPots();
            GUI.backgroundColor = Color.white;

            // ── Grid display ──────────────────────────────────────
            if (currentLevel.IsGridInitialized())
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox(
                    "Right Click on a cell to remove all pots on that tile.",
                    MessageType.Info);
                EditorGUILayout.Space(4);
                DrawGrid(DrawPotModeCell);
                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField(
                    $"Total pots placed: {currentLevel.pots.Count}",
                    EditorStyles.boldLabel);
            }
        }

        /// <summary>Draws one cell in Pot Mode: shows blended pot color and count badge. Right-click removes pots.</summary>
        private void DrawPotModeCell(int x, int y)
        {
            GridData grid = currentLevel.GridData;
            Vector2Int pos = new Vector2Int(x, y);
            bool enabled = grid.IsEnabled(x, y);
            List<PotData> potsHere = currentLevel.GetPotsAtTile(pos);

            Color bg;
            if (!enabled)
                bg = new Color(0.18f, 0.18f, 0.18f);   // disabled — darkest
            else if (potsHere.Count > 0)
                bg = new Color(0.30f, 0.30f, 0.36f);   // occupied — medium dark
            else
                bg = new Color(0.55f, 0.55f, 0.55f);   // empty — lightest

            GUI.backgroundColor = bg;
            Rect rect = GUILayoutUtility.GetRect(CELL_SIZE, CELL_SIZE,
                GUILayout.Width(CELL_SIZE), GUILayout.Height(CELL_SIZE));

            if (GUI.Button(rect, GUIContent.none, CellStyle))
            {
                if (!enabled) return;
                if (Event.current.button == 1 && potsHere.Count > 0)
                {
                    Undo.RecordObject(currentLevel, "Remove Pots at Tile");
                    currentLevel.pots.RemoveAll(p => p.tilePosition == pos);
                    SaveAndMarkDirty();
                    Event.current.Use();
                    Repaint();
                }
            }

            if (potsHere.Count > 0)
            {
                DrawPotIcon(rect);
                DrawPotCountBadge(rect, potsHere.Count);
            }

            GUI.backgroundColor = Color.white;
        }


        /// <summary>Entry point for pot initialization — confirms if pots exist, then runs the selected algorithm.</summary>
        private void TryInitPots()
        {
            if (!currentLevel.IsGridInitialized())
            {
                EditorUtility.DisplayDialog("Grid Not Initialized",
                    "Set up the grid in Grid Mode before initializing pots.", "OK");
                return;
            }

            if (currentLevel.pots.Count > 0)
            {
                if (!EditorUtility.DisplayDialog("Re-initialize Pots",
                    "This will clear all existing pots. Continue?", "Continue", "Cancel"))
                    return;
            }

            Undo.RecordObject(currentLevel, "Init Pots");

            switch (currentLevel.potDistributionMethod)
            {
                case PotDistributionMethod.RandomShuffle:
                    InitPotsRandomShuffle();
                    break;
                case PotDistributionMethod.EvenlySpreadByColor:
                    InitPotsEvenlySpread();
                    break;
            }

            SaveAndMarkDirty();
            Repaint();
        }

        /// <summary>
        /// Distributes pots randomly: builds a shuffled pot list and assigns each pot
        /// to the first tile that does not already have a pot of that color.
        /// </summary>
        private void InitPotsRandomShuffle()
        {
            ImageData img = currentLevel.imageData;
            List<Vector2Int> tiles = currentLevel.GetEnabledTiles();
            ShuffleList(tiles);

            var result = new List<PotData>();
            int nextID = 1;

            // Build per-color pot list with requiredAmount already assigned
            var potsToPlace = new List<PotData>();
            foreach (PotColorConfig cfg in currentLevel.potColorConfigs)
            {
                int count = UnityEngine.Random.Range(cfg.minPots, cfg.maxPots + 1);
                int totalPixels = img.GetColorCount(cfg.color);
                if (totalPixels < 3) count = 1;
                int[] amounts = DistributeAmount(totalPixels, count);

                for (int i = 0; i < count; i++)
                    potsToPlace.Add(new PotData(0, cfg.color, Vector2Int.zero, amounts[i]));
            }

            foreach (PotData proto in potsToPlace)
            {
                bool placed = false;
                foreach (Vector2Int tile in tiles)
                {
                    if (!result.Exists(p => p.tilePosition == tile && LevelData.Color32Equals(p.color, proto.color)))
                    {
                        proto.potID = nextID++;
                        proto.tilePosition = tile;
                        result.Add(proto);
                        placed = true;
                        break;
                    }
                }
                if (!placed)
                    Debug.LogWarning($"[Init Pots] No valid tile for #{proto.color.r:X2}{proto.color.g:X2}{proto.color.b:X2} — " +
                                     "try increasing tile count or reducing pot count.");
            }

            currentLevel.pots = result;
        }


        /// <summary>
        /// Distributes pots using a greedy max-distance spread per color:
        /// each new pot is placed on the tile furthest from the nearest already-placed pot of the same color.
        /// </summary>
        private void InitPotsEvenlySpread()
        {
            ImageData img = currentLevel.imageData;
            var result = new List<PotData>();
            int nextID = 1;

            foreach (PotColorConfig cfg in currentLevel.potColorConfigs)
            {
                int count = UnityEngine.Random.Range(cfg.minPots, cfg.maxPots + 1);
                Color32 color = cfg.color;
                int totalPixels = img.GetColorCount(color);
                if (totalPixels < 3) count = 1;
                int[] amounts = DistributeAmount(totalPixels, count);

                for (int i = 0; i < count; i++)
                {
                    List<Vector2Int> valid = currentLevel.GetEnabledTiles()
                        .Where(t => !result.Exists(p =>
                            p.tilePosition == t && LevelData.Color32Equals(p.color, color)))
                        .ToList();

                    if (valid.Count == 0)
                    {
                        Debug.LogWarning($"[Init Pots] No more valid tiles for #{color.r:X2}{color.g:X2}{color.b:X2}.");
                        break;
                    }

                    List<PotData> placed = result.Where(p => LevelData.Color32Equals(p.color, color)).ToList();

                    Vector2Int best;
                    if (placed.Count == 0)
                        best = valid[UnityEngine.Random.Range(0, valid.Count)];
                    else
                        best = valid.OrderByDescending(t =>
                            placed.Min(p => Vector2Int.Distance(p.tilePosition, t))
                        ).First();

                    result.Add(new PotData(nextID++, color, best, amounts[i]));
                }
            }

            currentLevel.pots = result;
        }


        /// <summary>Fisher-Yates shuffle for any List type.</summary>
        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }


        /// <summary>
        /// Distributes totalAmount as evenly as possible across potCount buckets.
        /// Remainder is spread one extra unit to the first buckets.
        /// e.g. 10 across 3 → [4, 3, 3]
        /// </summary>
        private static int[] DistributeAmount(int totalAmount, int potCount)
        {
            int[] amounts = new int[potCount];
            int baseAmount = totalAmount / potCount;
            int remainder = totalAmount % potCount;
            for (int i = 0; i < potCount; i++)
                amounts[i] = baseAmount + (i < remainder ? 1 : 0);
            return amounts;
        }



        #endregion

        #region State Mode

        /// <summary>
        /// Draws the State Mode UI: interactive grid for tile selection
        /// and a side panel showing each pot on the selected tile with its state controls.
        /// </summary>
        private void DrawStateMode()
        {
            if (currentLevel.pots.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No pots placed yet. Initialize pots in Pot Mode first.",
                    MessageType.Warning);
                return;
            }

            if (!currentLevel.IsGridInitialized()) return;

            DrawStateProbabilityPanel();
            EditorGUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Left Click: Select tile    Right Click: Clear selection",
                MessageType.Info);
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            DrawGrid(DrawStateModeCell);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);
            DrawStateSidePanel();

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the probability weight sliders for all 4 PotStates and the Apply button.
        /// Weights are saved into currentLevel.stateProbability.
        /// </summary>
        private void DrawStateProbabilityPanel()
        {
            StateProbabilityConfig cfg = currentLevel.stateProbability;
            if (cfg == null) { currentLevel.stateProbability = new StateProbabilityConfig(); cfg = currentLevel.stateProbability; }

            EditorGUILayout.LabelField("🎲  State Probability", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            int total = cfg.TotalWeight;

            // Helper to compute the display percentage string
            string Pct(int w) => total > 0 ? $"~{(w * 100f / total):F0}%" : "--";

            // Row data: (label, emoji, get weight, set weight)
            // Drawn as: [emoji label | IntSlider | ~XX%]
            DrawWeightRow("None", "", cfg.noneWeight, v => { Undo.RecordObject(currentLevel, "Set None Weight"); cfg.noneWeight = v; SaveAndMarkDirty(); }, Pct(cfg.noneWeight));
            DrawWeightRow("Frozen", "❄", cfg.frozenWeight, v => { Undo.RecordObject(currentLevel, "Set Frozen Weight"); cfg.frozenWeight = v; SaveAndMarkDirty(); }, Pct(cfg.frozenWeight));
            DrawWeightRow("Locked", "🔒", cfg.lockedWeight, v => { Undo.RecordObject(currentLevel, "Set Locked Weight"); cfg.lockedWeight = v; SaveAndMarkDirty(); }, Pct(cfg.lockedWeight));

            DrawWeightRow("Decay", "💀", cfg.decayWeight, v => { Undo.RecordObject(currentLevel, "Set Decay Weight"); cfg.decayWeight = v; SaveAndMarkDirty(); }, Pct(cfg.decayWeight));
            if (cfg.decayWeight > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                int newMin = EditorGUILayout.IntField("💀 Min Decay Time (s)", Mathf.RoundToInt(cfg.minDecayTime));
                int newMax = EditorGUILayout.IntField("💀 Max Decay Time (s)", Mathf.RoundToInt(cfg.maxDecayTime));

                if (EditorGUI.EndChangeCheck())
                {
                    newMin = Mathf.Max(1, newMin);
                    newMax = Mathf.Max(newMin, newMax);
                    Undo.RecordObject(currentLevel, "Set Decay Time Range");
                    cfg.minDecayTime = newMin;
                    cfg.maxDecayTime = newMax;
                    SaveAndMarkDirty();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);

            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
            if (GUILayout.Button("⚡  Apply to All Pots", GUILayout.Height(32)))
            {
                bool hasNonNone = currentLevel.pots.Exists(p => p.state != PotState.None);
                if (!hasNonNone || EditorUtility.DisplayDialog(
                    "Apply State Probability",
                    "This will overwrite all current pot states. Continue?",
                    "Continue", "Cancel"))
                {
                    Undo.RecordObject(currentLevel, "Apply State Probability");
                    currentLevel.ApplyStateProbabilities();
                    SaveAndMarkDirty();
                    Repaint();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        /// <summary>Draws one weight row: emoji + name label, IntSlider, effective % label.</summary>
        private void DrawWeightRow(string stateName, string emoji, int weight, System.Action<int> onChanged, string pctLabel)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"{emoji} {stateName}", GUILayout.Width(70));

            EditorGUI.BeginChangeCheck();
            int newWeight = EditorGUILayout.IntSlider(weight, 0, 100);
            if (EditorGUI.EndChangeCheck())
                onChanged(newWeight);

            GUILayout.Label(pctLabel, EditorStyles.miniLabel, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Draws one cell in State Mode: highlights the selected tile; left-click selects, right-click clears.</summary>
        private void DrawStateModeCell(int x, int y)
        {
            GridData grid = currentLevel.GridData;
            Vector2Int pos = new Vector2Int(x, y);
            bool enabled = grid.IsEnabled(x, y);
            List<PotData> potsHere = currentLevel.GetPotsAtTile(pos);
            bool isSelected = selectedTile == pos;

            Color bg;
            if (!enabled)
                bg = new Color(0.18f, 0.18f, 0.18f);   // disabled — darkest
            else if (potsHere.Count > 0)
                bg = new Color(0.30f, 0.30f, 0.36f);   // occupied — medium dark
            else
                bg = new Color(0.55f, 0.55f, 0.55f);   // empty — lightest

            if (isSelected) bg = Color.Lerp(bg, Color.white, 0.4f);

            GUI.backgroundColor = bg;
            Rect rect = GUILayoutUtility.GetRect(CELL_SIZE, CELL_SIZE,
                GUILayout.Width(CELL_SIZE), GUILayout.Height(CELL_SIZE));

            if (GUI.Button(rect, GUIContent.none, CellStyle))
            {
                if (!enabled) return;
                if (Event.current.button == 0)
                {
                    selectedTile = pos;
                    Event.current.Use();
                    Repaint();
                }
                else
                {
                    selectedTile = new Vector2Int(-1, -1);
                    Event.current.Use();
                    Repaint();
                }
            }

            if (potsHere.Count > 0)
            {
                DrawPotIcon(rect);
                DrawPotCountBadge(rect, potsHere.Count);
            }

            PotState firstNonNone = potsHere
                .Select(p => p.state)
                .FirstOrDefault(s => s != PotState.None);
            if (firstNonNone != PotState.None)
                DrawStateIconOverlay(rect, firstNonNone);

            if (isSelected)
                EditorGUI.DrawRect(
                    new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2),
                    new Color(1f, 1f, 1f, 0.25f));

            GUI.backgroundColor = Color.white;
        }


        /// <summary>
        /// Draws the right-side panel for the selected tile.
        /// Lists each PotData with a state EnumPopup and decay time controls when relevant.
        /// </summary>
        private void DrawStateSidePanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(350), GUILayout.ExpandHeight(false));

            if (selectedTile.x < 0)
            {
                GUILayout.Label("Click a tile to inspect its pots.", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            List<PotData> potsHere = currentLevel.GetPotsAtTile(selectedTile);
            GUILayout.Label(
                $"Tile ({selectedTile.x}, {selectedTile.y})  —  {potsHere.Count} pot(s)",
                EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (potsHere.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No pots on this tile — place pots in Pot Mode first.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (PotData pot in potsHere)
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();

                // Colored swatch
                Rect swatchRect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
                EditorGUI.DrawRect(swatchRect, new Color32(pot.color.r, pot.color.g, pot.color.b, 255));

                GUILayout.Label($"ID {pot.potID}", GUILayout.Width(46));

                // State popup
                EditorGUI.BeginChangeCheck();
                PotState newState = (PotState)EditorGUILayout.EnumPopup(pot.state, GUILayout.Width(80));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(currentLevel, "Set Pot State");
                    pot.state = newState;
                    if (newState == PotState.Decay && pot.decayTime < 1f) pot.decayTime = 5f;
                    SaveAndMarkDirty();
                }

                EditorGUILayout.EndHorizontal();

                // Decay time controls — only shown when state is Decay
                if (pot.state == PotState.Decay)
                {
                    EditorGUILayout.Space(2);
                    EditorGUILayout.LabelField("Decay Time", EditorStyles.miniLabel);

                    EditorGUI.BeginChangeCheck();
                    float newTime = EditorGUILayout.Slider(pot.decayTime, 1f, 60f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(currentLevel, "Set Decay Time");
                        pot.SetDecayTime(newTime);
                        SaveAndMarkDirty();
                    }

                    EditorGUILayout.LabelField($"Current: {pot.decayTime:F1}s", EditorStyles.boldLabel);
                    EditorGUILayout.Space(2);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("5s", GUILayout.Height(22))) SetPotDecay(pot, 5f);
                    if (GUILayout.Button("10s", GUILayout.Height(22))) SetPotDecay(pot, 10f);
                    if (GUILayout.Button("30s", GUILayout.Height(22))) SetPotDecay(pot, 30f);
                    if (GUILayout.Button("60s", GUILayout.Height(22))) SetPotDecay(pot, 60f);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>Applies a decay time preset to a pot and saves the asset.</summary>
        private void SetPotDecay(PotData pot, float time)
        {
            Undo.RecordObject(currentLevel, "Set Decay Preset");
            pot.SetDecayTime(time);
            SaveAndMarkDirty();
            Repaint();
        }

        #endregion

        #region Pot State Helpers

        /// <summary>Draws a small state emoji in the top-right corner of a cell rect.</summary>
        private void DrawStateIconOverlay(Rect cellRect, PotState state)
        {
            string emoji = GetPotStateEmoji(state);
            if (string.IsNullOrEmpty(emoji)) return;

            if (_overlayStyle == null)
            {
                _overlayStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.UpperRight
                };
            }

            Rect iconRect = new Rect(cellRect.xMax - 18, cellRect.y + 2, 16, 16);
            GUI.Label(iconRect, emoji, _overlayStyle);
        }

        /// <summary>Returns the emoji used to represent the given PotState.</summary>
        private static string GetPotStateEmoji(PotState state)
        {
            switch (state)
            {
                case PotState.Frozen: return "❄";
                case PotState.Locked: return "🔒";
                case PotState.Decay: return "💀";
                default: return "";
            }
        }

        #endregion

        #region Settings Mode

        /// <summary>Draws the Settings Mode UI: background colors, power-ups, coin reward, and star rating.</summary>
        private void DrawSettingsMode()
        {
            DrawBackgroundColorSettings();
            EditorGUILayout.Space(15);
            DrawConveyorSlot();
            EditorGUILayout.Space(15);
            DrawPowerUpSettings();
            EditorGUILayout.Space(15);
            DrawCoinRewardSettings();
            EditorGUILayout.Space(15);
            DrawStarRatingSettings();
        }

        /// <summary>Draws ColorField controls for the level's background gradient top and bottom colors.</summary>
        private void DrawBackgroundColorSettings()
        {
            EditorGUILayout.LabelField("🎨 Background Colors", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            // Capture new values into temps first so Undo.RecordObject runs before any field is modified
            EditorGUI.BeginChangeCheck();
            Color newTop = EditorGUILayout.ColorField("Top Color", currentLevel.backgroundTopColor);
            Color newBottom = EditorGUILayout.ColorField("Bottom Color", currentLevel.backgroundBottomColor);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentLevel, "Change Background Colors");
                currentLevel.backgroundTopColor = newTop;
                currentLevel.backgroundBottomColor = newBottom;
                SaveAndMarkDirty();
            }

            EditorGUILayout.EndVertical();
        }


        /// <summary>
        /// Draw the conveyor slot
        /// </summary>
        private void DrawConveyorSlot()
        {
            EditorGUI.BeginChangeCheck();
            int newConveyorSlot = EditorGUILayout.IntField("Conveyor Slot", currentLevel.conveyorSlot);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentLevel, "Change Conveyor Slot");
                currentLevel.conveyorSlot = Mathf.Max(1, newConveyorSlot);
                SaveAndMarkDirty();
            }
        }


        /// <summary>Draws a card for each power-up with cost and unlock level sliders. Shows an init button if list is empty.</summary>
        private void DrawPowerUpSettings()
        {
            EditorGUILayout.LabelField("⚡ Power-Up Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.HelpBox("Configure cost and unlock level for each power-up.", MessageType.Info);

            if (currentLevel.powerUps == null || currentLevel.powerUps.Count == 0)
            {
                EditorGUILayout.Space(4);
                GUI.backgroundColor = new Color(0.6f, 0.9f, 1f);
                if (GUILayout.Button("Initialize Default Power-Ups", GUILayout.Height(30)))
                {
                    Undo.RecordObject(currentLevel, "Initialize Power-Ups");
                    currentLevel.InitializeDefaultPowerUps();
                    SaveAndMarkDirty();
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(6);
            foreach (PowerUpData powerUp in currentLevel.powerUps)
            {
                DrawPowerUpCard(powerUp);
                EditorGUILayout.Space(6);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>Draws a single power-up card with cost and unlock level IntSliders.</summary>
        private void DrawPowerUpCard(PowerUpData powerUp)
        {
            EditorGUILayout.BeginVertical("box");

            if (_powerUpHeaderStyle == null)
                _powerUpHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

            EditorGUILayout.LabelField(
                $"{GetPowerUpEmoji(powerUp.powerUpType)}  {powerUp.powerUpType}",
                _powerUpHeaderStyle);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("💰 Cost:", GUILayout.Width(110));
            EditorGUI.BeginChangeCheck();
            int newCost = EditorGUILayout.IntSlider(powerUp.cost, 0, 200);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentLevel, $"Change {powerUp.powerUpType} Cost");
                powerUp.cost = newCost;
                SaveAndMarkDirty();
            }
            EditorGUILayout.LabelField($"{powerUp.cost}", GUILayout.Width(36));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔓 Unlock Level:", GUILayout.Width(110));
            EditorGUI.BeginChangeCheck();
            int newUnlock = EditorGUILayout.IntSlider(powerUp.unlockLevel, 1, 100);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentLevel, $"Change {powerUp.powerUpType} Unlock Level");
                powerUp.unlockLevel = newUnlock;
                SaveAndMarkDirty();
            }
            EditorGUILayout.LabelField($"{powerUp.unlockLevel}", GUILayout.Width(36));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>Returns the emoji associated with a given power-up type.</summary>
        private static string GetPowerUpEmoji(PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.Slot: return "⚡";
                case PowerUpType.Magnet: return "🧲";
                case PowerUpType.Hammer: return "🔨";
                case PowerUpType.Key: return "🗝️";
                case PowerUpType.Cooler: return "❄️";
                default: return "⚡";
            }
        }

        /// <summary>Draws min/max coin reward sliders with a live runtime range preview.</summary>
        private void DrawCoinRewardSettings()
        {
            EditorGUILayout.LabelField("💰 Coin Reward", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.HelpBox(
                "Coins awarded per pot cleared. A random value between Min and Max is chosen at runtime.",
                MessageType.Info);
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min Reward:", GUILayout.Width(110));
            EditorGUI.BeginChangeCheck();
            int newMin = EditorGUILayout.IntSlider(currentLevel.minCoinReward, 0, 100);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentLevel, "Change Min Coin Reward");
                currentLevel.minCoinReward = newMin;
                currentLevel.maxCoinReward = Mathf.Max(newMin, currentLevel.maxCoinReward);
                SaveAndMarkDirty();
            }
            EditorGUILayout.LabelField($"{currentLevel.minCoinReward}", GUILayout.Width(36));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max Reward:", GUILayout.Width(110));
            EditorGUI.BeginChangeCheck();
            int newMax = EditorGUILayout.IntSlider(currentLevel.maxCoinReward, 0, 100);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentLevel, "Change Max Coin Reward");
                currentLevel.maxCoinReward = Mathf.Max(currentLevel.minCoinReward, newMax);
                SaveAndMarkDirty();
            }
            EditorGUILayout.LabelField($"{currentLevel.maxCoinReward}", GUILayout.Width(36));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(
                $"Runtime range:  {currentLevel.minCoinReward} – {currentLevel.maxCoinReward} coins",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        /// <summary>Draws star rating percentage sliders and shows the calculated time thresholds in seconds.</summary>
        private void DrawStarRatingSettings()
        {
            EditorGUILayout.LabelField("⭐ Star Rating (Time-based)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.HelpBox(
                "Higher percentages mean the player needs more time remaining to earn that star.",
                MessageType.Info);
            EditorGUILayout.Space(6);

            DrawSettingsFloat("Total Level Time", "Maximum seconds to complete the level",
                ref currentLevel.totalLevelTime, 30f, 600f, "Change Total Level Time", "s");
            EditorGUILayout.Space(6);
            DrawSettingsFloat("1 ⭐  Time %", "% of time remaining required for 1 star",
                ref currentLevel.oneStarPercent, 0f, 100f, "Change One Star %", "%");
            EditorGUILayout.Space(4);
            DrawSettingsFloat("2 ⭐  Time %", "% of time remaining required for 2 stars",
                ref currentLevel.twoStarPercent, 0f, 100f, "Change Two Star %", "%");
            EditorGUILayout.Space(4);
            DrawSettingsFloat("3 ⭐  Time %", "% of time remaining required for 3 stars",
                ref currentLevel.threeStarPercent, 0f, 100f, "Change Three Star %", "%");

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⏱️ Calculated Time Thresholds", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  1 ⭐   {currentLevel.GetStarTimeThreshold(1):F1}s or more remaining");
            EditorGUILayout.LabelField($"  2 ⭐   {currentLevel.GetStarTimeThreshold(2):F1}s or more remaining");
            EditorGUILayout.LabelField($"  3 ⭐   {currentLevel.GetStarTimeThreshold(3):F1}s or more remaining");
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        /// <summary>Draws a labeled float slider with undo support. Used for time and percentage settings.</summary>
        private void DrawSettingsFloat(string label, string tooltip,
            ref float value, float min, float max, string undoLabel, string suffix = "")
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"ℹ️  {tooltip}", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.Slider(value, min, max);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(currentLevel, undoLabel);
                value = newValue;
                SaveAndMarkDirty();
            }
            string display = string.IsNullOrEmpty(suffix) ? $"{value:F1}" : $"{value:F1}{suffix}";
            EditorGUILayout.LabelField(display, GUILayout.Width(72));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Shared Grid Renderer

        /// <summary>Renders the full grid top-to-bottom, calling drawCell(x, y) for each tile.</summary>
        private void DrawGrid(System.Action<int, int> drawCell)
        {
            GridData grid = currentLevel.GridData;
            for (int y = grid.height - 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < grid.width; x++) drawCell(x, y);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(CELL_SPACING);
            }
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Pot Color Helpers

        /// <summary>Draws a small dark badge in the bottom-right of a cell rect showing the pot count. Only shown for count > 1.</summary>
        private void DrawPotCountBadge(Rect cellRect, int count)
        {
            if (count <= 1) return;

            if (_badgeStyle == null)
            {
                _badgeStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.LowerRight,
                    normal = { textColor = Color.white }
                };
            }

            Rect badgeRect = new Rect(cellRect.xMax - 18, cellRect.yMax - 16, 16, 14);
            EditorGUI.DrawRect(badgeRect, new Color(1f, 1f, 1f, 0.55f));
            GUI.Label(badgeRect, count.ToString(), _badgeStyle);
        }


        /// <summary>
        /// Draws a centered built-in cylinder icon inside a cell rect to represent a pot.
        /// </summary>
        /// <param name="cellRect"></param>
        private static void DrawPotIcon(Rect cellRect)
        {
            Texture icon = EditorGUIUtility.IconContent("d_PreMatCylinder").image;
            if (icon == null) return;
            const float ICON_SIZE = 28f;
            Rect iconRect = new Rect(
                cellRect.x + (cellRect.width - ICON_SIZE) * 0.5f,
                cellRect.y + (cellRect.height - ICON_SIZE) * 0.5f,
                ICON_SIZE, ICON_SIZE);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
        }


        #endregion

        #region Grid Bulk Actions

        /// <summary>Enables all tiles in the current grid.</summary>
        private void FillAllTiles()
        {
            Undo.RecordObject(currentLevel, "Fill All Tiles");
            currentLevel.GridData.FillAll();
            SaveAndMarkDirty();
        }

        /// <summary>Disables all tiles and removes all pots from the current level.</summary>
        private void ClearAll()
        {
            Undo.RecordObject(currentLevel, "Clear All");
            currentLevel.GridData.Clear();
            currentLevel.pots.Clear();
            SaveAndMarkDirty();
        }

        /// <summary>
        /// Resizes the grid to newWidth × newHeight, preserving tile states where possible
        /// and pruning any pots that fall outside the new bounds.
        /// </summary>
        private void ResizeGrid(int newWidth, int newHeight)
        {
            Undo.RecordObject(currentLevel, "Resize Grid");

            GridData old = currentLevel.GridData?.Clone();
            currentLevel.InitializeGrid(newWidth, newHeight);
            GridData grid = currentLevel.GridData;

            // Restore tile states from the old grid; new tiles default to enabled
            for (int y = 0; y < grid.height; y++)
                for (int x = 0; x < grid.width; x++)
                    grid.SetEnabled(x, y,
                        old != null && old.IsValidPosition(x, y) ? old.IsEnabled(x, y) : true);

            // Remove pots that are now out of bounds
            currentLevel.pots.RemoveAll(p => !grid.IsValidPosition(p.tilePosition));

            MarkDirty();
        }

        #endregion

        #region Level Management

        /// <summary>Reloads allLevels from the asset database, sorted by level number.</summary>
        private void RefreshLevelList()
        {
            allLevels = AssetDatabase
                .FindAssets("t:LevelData")
                .Select(guid => AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(l => l != null)
                .OrderBy(l => l.levelNumber)
                .ToList();
        }

        /// <summary>Sets currentLevel and resets editor selection state.</summary>
        private void SelectLevel(LevelData level)
        {
            currentLevel = level;
            selectedTile = new Vector2Int(-1, -1);
            if (_previewTex != null) { DestroyImmediate(_previewTex); _previewTex = null; }
            GUI.FocusControl(null);
        }

        /// <summary>Creates a new LevelData asset with default grid and power-ups, then selects it.</summary>
        private void CreateNewLevel()
        {
            int number = allLevels.Count == 0 ? 1 : allLevels.Max(l => l.levelNumber) + 1;

            LevelData level = CreateInstance<LevelData>();
            level.levelNumber = number;
            level.InitializeGrid(gridWidth, gridHeight);
            level.GridData.FillAll();
            level.InitializeDefaultPowerUps();

            if (!System.IO.Directory.Exists(ASSET_FOLDER))
                System.IO.Directory.CreateDirectory(ASSET_FOLDER);

            string path = $"{ASSET_FOLDER}Level_{number}.asset";
            AssetDatabase.CreateAsset(level, path);
            AssetDatabase.SaveAssets();

            RefreshLevelList();
            SelectLevel(level);
        }

        /// <summary>Shows a confirmation dialog then deletes the level asset from disk.</summary>
        private void DeleteLevel(LevelData level)
        {
            if (EditorUtility.DisplayDialog("Delete Level",
                $"Delete Level {level.levelNumber}? This cannot be undone.", "Delete", "Cancel"))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(level));
                if (currentLevel == level) currentLevel = null;
                RefreshLevelList();
            }
        }

        /// <summary>Marks the current level asset as dirty so Unity knows to save it.</summary>
        private void MarkDirty()
        {
            if (currentLevel == null) return;
            EditorUtility.SetDirty(currentLevel);
        }

        /// <summary>Marks dirty and immediately saves all assets to disk.</summary>
        private void SaveAndMarkDirty()
        {
            if (currentLevel == null) return;
            EditorUtility.SetDirty(currentLevel);
            AssetDatabase.SaveAssets();
        }

        #endregion
    }
}
#endif