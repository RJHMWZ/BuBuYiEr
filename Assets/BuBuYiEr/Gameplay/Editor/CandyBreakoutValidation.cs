using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ClawbearGames.EditorTools
{
    // Run in an isolated project copy with -executeMethod ...CandyBreakoutValidation.Run.
    [InitializeOnLoad]
    public static class CandyBreakoutValidation
    {
        private const string Active = "CandyBreakoutValidation.Active";
        static CandyBreakoutValidation()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Active, false))
                    new GameObject("BreakoutValidation").AddComponent<CandyBreakoutFlowValidation>();
            };
        }
        public static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception("BREAKOUT CHECK FAILED: " + message);
        }
        public static void Run()
        {
            try
            {
                Check(Application.productName == "BuBuYiErBreakoutValidation" &&
                    Application.dataPath.Replace("\\", "/").EndsWith("/Temp/BreakoutValidation/Assets"),
                    "validation requires the isolated Temp/BreakoutValidation project and product name");
                PlayerPrefs.DeleteKey("CBGAMES_PLAYER_DATA");
                var levels = Resources.LoadAll<LevelData>("Levels");
                Check(levels.Length == 101, "101 retained levels");
                foreach (var level in levels)
                {
                    var b = new CandyBreakoutSimulation(level);
                    Check(b.Total > 0 && b.Balls.Count == 1 && b.Honey == level.startingHoney, "initial board " + level.name);
                    Check(b.Bricks.All(x => x.Bounds.xMin > 0 && x.Bounds.xMax < 900 && x.Bounds.yMax < 760), "board bounds");
                    Check(!b.Buy(-1) && !b.Upgrade(6), "invalid purchase rejected");
                }
                foreach (var level in levels)
                {
                    var preview = new GameObject("PresentationCheck", typeof(RectTransform), typeof(CandyBreakoutGraphic));
                    var graphic = preview.GetComponent<CandyBreakoutGraphic>();
                    graphic.Board = new CandyBreakoutSimulation(level);
                    graphic.rectTransform.sizeDelta = new Vector2(976,632);
                    graphic.FitPresentation();
                    foreach (var brick in graphic.Board.Bricks)
                    {
                        Vector2 point = graphic.ToBoardPoint(graphic.ToLocalPoint(brick.Bounds.center));
                        Check(Vector2.Distance(point,brick.Bounds.center)<.002f,"presentation/inverse centre " + level.name);
                        Rect visual = graphic.VisualRect(brick.Bounds);
                        Check(visual.width>0 && visual.height>0 && graphic.rectTransform.rect.Contains(visual.center),"visible brick " + level.name);
                    }
                    for (int y=0;y<=760;y+=10)
                        Check(Vector2.Distance(new Vector2(450,y),graphic.ToBoardPoint(graphic.ToLocalPoint(new Vector2(450,y))))<.002f,"inverse board coordinate");
                    UnityEngine.Object.DestroyImmediate(preview);
                }
                Debug.Log("UI POLISH: 101 level presentation mappings PASS");
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene("Assets/Diamond_Loop/Scenes/Home.unity");
                SessionState.SetBool(Active, true);
                EditorApplication.isPlaying = true;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        public static void Finish(bool ok)
        {
            SessionState.SetBool(Active, false);
            Debug.Log(ok ? "BREAKOUT ALL CHECKS PASSED" : "BREAKOUT FLOW FAILED");
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }

    public sealed class CandyBreakoutFlowValidation : MonoBehaviour
    {
        private bool errors;
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.logMessageReceived += OnLog;
        }
        private void OnLog(string condition, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error) errors = true;
            if (type == LogType.Exception) EditorApplication.delayCall += () => CandyBreakoutValidation.Finish(false);
        }
        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1);
            yield return RunFlow();
            CandyBreakoutValidation.Finish(!errors);
        }
        private IEnumerator RunFlow()
        {
            var views = ViewManager.Instance;
            var originalScaler=views.GetComponent<CanvasScaler>();
            Vector2 originalReference=originalScaler.referenceResolution;
            float originalMatch=originalScaler.matchWidthOrHeight;
            CandyBreakoutValidation.Check(views != null && views.ActiveViewType == ViewType.HOME_VIEW, "home boot");
            PlayerDataHandler.UpdateTargetLevel(1);
            // Enter through Loading, then check the retained level-selection UI.
            views.HomeViewController.LoadScene("Level", 0);
            yield return new WaitForSecondsRealtime(2);
            CandyBreakoutValidation.Check(views.ActiveViewType == ViewType.LEVEL_VIEW, "loading to level map");
            views.LevelViewController.ShowConfirmPanel(1);
            views.LevelViewController.OnClickStartButton();
            yield return new WaitForSecondsRealtime(2);
            var game = IngameManager.Instance;
            CandyBreakoutValidation.Check(game != null && game.Board != null && views.ActiveViewType == ViewType.INGAME_VIEW, "start selected breakout level");
            CandyBreakoutValidation.Check(!game.IsRoundStarted && game.Board.Elapsed == 0, "intro blocks simulation");
            var begin = views.IngameViewController.GetComponentsInChildren<Button>(true).Single(x => x.name == "Begin");
            begin.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.2f);
            CandyBreakoutValidation.Check(game.IsRoundStarted && game.Board.Elapsed > 0, "intro begin button");
            var graphic = views.IngameViewController.GetComponentInChildren<CandyBreakoutGraphic>();
            var first = game.Board.Bricks[0];
            Vector2 local = graphic.ToLocalPoint(first.Bounds.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, graphic.rectTransform.TransformPoint(local));
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = screen };
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
            CandyBreakoutValidation.Check(hits.Count > 0 && hits[0].gameObject == graphic.gameObject, "arena receives real UI raycast");
            int hp = first.Health;
            graphic.OnPointerDown(pointer);
            CandyBreakoutValidation.Check(first.Health == hp - 1, "pointer position damages intended brick");
            game.SetPaused(true);
            hp = first.Health;
            graphic.OnPointerDown(pointer);
            CandyBreakoutValidation.Check(first.Health == hp, "paused pointer cannot damage");
            float elapsed = game.Board.Elapsed;
            int honey = game.Board.Honey;
            yield return new WaitForSecondsRealtime(.2f);
            CandyBreakoutValidation.Check(game.Board.Elapsed == elapsed && !game.CanInteract, "pause freezes board and input");
            game.SetPaused(false);
            var buy = views.IngameViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "Buy");
            buy.onClick.Invoke();
            CandyBreakoutValidation.Check(game.Board.Balls.Count == 2 && game.Board.Honey < honey, "actual shop button spends honey");
            yield return null;
            Render(views.IngameViewController.gameObject, "polished-1080x1920.png", 1080, 1920);
            Render(views.IngameViewController.gameObject, "polished-1080x2400.png", 1080, 2400);
            Render(views.IngameViewController.gameObject, "polished-1024x1536.png", 1024, 1536);
            Render(views.IngameViewController.gameObject, "polished-tablet.png", 1536, 2048);
            Render(views.IngameViewController.gameObject, "polished-landscape.png", 1920, 1080);
            int coins = ServicesManager.Instance.CoinManager.TotalCoins;
            int safety = 0;
            while (!game.Board.Complete && safety++ < 12000)
            {
                var b = game.Board;
                for (int i = 5; i >= 0; i--) if (b.Counts[i] < 3) b.Buy(i);
                for (int i = 0; i < 6; i++) if (b.Upgrades[i] < 4) b.Upgrade(i);
                foreach (var brick in b.Bricks) if (brick.Alive) { b.Click(brick.Bounds.center); break; }
                b.Step(.1f);
            }
            yield return new WaitForSecondsRealtime(1);
            CandyBreakoutValidation.Check(game.IngameState == IngameState.Ingame_CompleteLevel && views.ActiveViewType == ViewType.ENDGAME_VIEW, "real win reaches outcome");
            CandyBreakoutValidation.Check(ServicesManager.Instance.CoinManager.TotalCoins == coins + game.Level.coinReward, "coin reward committed once");
            game.CompletedLevel();
            CandyBreakoutValidation.Check(ServicesManager.Instance.CoinManager.TotalCoins == coins + game.Level.coinReward, "duplicate settlement blocked");
            CandyBreakoutValidation.Check(PlayerDataHandler.GetCurrentLevel() == 2, "progress unlock");
            Render(views.EndGameViewController.gameObject, "polished-result.png", 1080, 1920);
            views.EndGameViewController.OnClickPlayButton();
            yield return new WaitForSecondsRealtime(2);
            CandyBreakoutValidation.Check(IngameManager.Instance.CurrentLevel == 2 && !IngameManager.Instance.IsRoundStarted, "next level fresh round");
            views.IngameViewController.GetComponent<BearPausePanel>().Restart();
            yield return new WaitForSecondsRealtime(2);
            CandyBreakoutValidation.Check(IngameManager.Instance.CurrentLevel == 2 && IngameManager.Instance.Board.Destroyed == 0, "retry clean board");
            GameDebugService.JumpToLevel(6);
            yield return new WaitForSecondsRealtime(2);
            views.IngameViewController.GetComponentsInChildren<Button>(true).Single(x=>x.name=="Begin").onClick.Invoke();
            yield return new WaitForSecondsRealtime(.2f);
            Render(views.IngameViewController.gameObject,"polished-level6.png",1080,1920);
            game=IngameManager.Instance;
            game.SetPaused(true);
            yield return null;
            Render(views.IngameViewController.gameObject,"polished-pause.png",1080,1920);
            game.SetPaused(false);
            for(int tick=0;tick<55;tick++)
            {
                foreach(var brick in game.Board.Bricks) if(brick.Alive) {game.Board.Click(brick.Bounds.center);break;}
                game.Board.Step(.1f);
            }
            yield return new WaitForSecondsRealtime(.4f);
            Render(views.IngameViewController.gameObject,"polished-progress.png",1080,1920);
            var upgradeFace = views.IngameViewController.GetComponentsInChildren<CandyBreakoutButton>().First(x=>x.name=="Upgrade");
            upgradeFace.SetState(false,new Color32(190,220,189,255),"已满级","MAX",true);
            Render(views.IngameViewController.gameObject,"polished-states.png",1080,1920);
            GameDebugService.JumpToLevel(5);
            yield return new WaitForSecondsRealtime(2);
            views.IngameViewController.GetComponentsInChildren<Button>(true).Single(x=>x.name=="Begin").onClick.Invoke();
            yield return new WaitForSecondsRealtime(.2f);
            Render(views.IngameViewController.gameObject,"polished-boss.png",1080,1920);
            views.IngameViewController.GetComponent<BearPausePanel>().ReturnHome();
            yield return new WaitForSecondsRealtime(2);
            CandyBreakoutValidation.Check(views.ActiveViewType == ViewType.HOME_VIEW && IngameManager.Instance == null && Time.timeScale == 1, "return home restores state");
            CandyBreakoutValidation.Check(originalScaler.referenceResolution==originalReference && originalScaler.matchWidthOrHeight==originalMatch,"other views retain their original scaler");
        }
        private static void CheckLayout(GameObject view)
        {
            var frame = view.transform.Find("CandyBreakoutUI/CandyRoom") as RectTransform;
            var board = frame.Find("CandyBoard") as RectTransform;
            var shop = frame.Find("BallShop") as RectTransform;
            var primary = frame.Find("MainAction") as RectTransform;
            CandyBreakoutValidation.Check(board.rect.height >= 490, "minimum board height");
            CandyBreakoutValidation.Check(-board.anchoredPosition.y + board.rect.height < -primary.anchoredPosition.y, "board does not overlap primary");
            CandyBreakoutValidation.Check(-primary.anchoredPosition.y + primary.rect.height < -shop.anchoredPosition.y, "primary does not overlap shop");
            CandyBreakoutValidation.Check(-shop.anchoredPosition.y + shop.rect.height <= frame.rect.height-20, "shop fits safe frame");
            foreach(var text in view.GetComponentsInChildren<Text>())
                if(!string.IsNullOrWhiteSpace(text.text))
                    CandyBreakoutValidation.Check(text.cachedTextGenerator.vertexCount>=4,"text actually generates visible glyphs: "+text.name+" "+text.text);
            foreach(var action in view.GetComponentsInChildren<CandyBreakoutButton>())
            {
                var rect = (RectTransform)action.transform;
                CandyBreakoutValidation.Check(rect.rect.width>=144 && rect.rect.height>=132, "touch target size");
            }
        }
        private static void Render(GameObject view, string name, int width, int height)
        {
            var renderRoot = new GameObject("PreviewCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = BearGameTheme.Cream;
            camera.orthographic = true;
            var rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            var canvas = renderRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10;
            var scaler = renderRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080,1920); scaler.matchWidthOrHeight = 0;
            Transform original = view.transform.parent; int sibling = view.transform.GetSiblingIndex();
            view.transform.SetParent(renderRoot.transform, false);
            Canvas.ForceUpdateCanvases();
            foreach (var layout in view.GetComponentsInChildren<BearMenuLayout>(true)) layout.Refresh();
            var polished = view.GetComponent<CandyBreakoutView>();
            if (polished != null) polished.RefreshLayout(true);
            Canvas.ForceUpdateCanvases();
            foreach (var text in view.GetComponentsInChildren<Text>())
                if (text.font != null) text.font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
            foreach (var text in view.GetComponentsInChildren<Text>()) text.SetAllDirty();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var texture = new Texture2D(width,height,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,width,height),0,0); texture.Apply();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","UIPolishPreviews"));
            Directory.CreateDirectory(output);
            File.WriteAllBytes(Path.Combine(output,name), texture.EncodeToPNG());
            if (polished != null) CheckLayout(view);
            RenderTexture.active = previous;
            view.transform.SetParent(original,false); view.transform.SetSiblingIndex(sibling);
            DestroyImmediate(texture); camera.targetTexture = null; rt.Release(); DestroyImmediate(rt);
            DestroyImmediate(cameraObject); DestroyImmediate(renderRoot);
        }
    }
}
