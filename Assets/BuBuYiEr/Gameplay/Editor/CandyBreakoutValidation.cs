using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

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
                CheckDataAndAllocations();
                CheckThemeAudio();
                CheckCollisions();
                CheckSafeAreas();
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
        private static void CheckDataAndAllocations()
        {
            bool allocationCounter=HasAllocationCounter();
            var data=new PlayerData { TotalCoins=753,CurrentLevel=17,TargetLevel=12,CurrentRewardIndex=3 };
            for(int i=1;i<=101;i++)data.ListStarData.Add(new StarData(i,i%3+1));
            PlayerDataHandler.SaveData(data);
            string json=PlayerPrefs.GetString(PlayerDataHandler.PLAYER_DATA);
            data.TotalCoins=0;
            Check(PlayerDataHandler.GetTotalCoin()==753,"save keeps caller snapshot semantics");
            PlayerDataHandler.UpdateStar(1,1);
            Check(PlayerDataHandler.GetStar(1)==2,"best stars retained by cached save");
            PlayerDataHandler.UpdateTotalCoin(754);
            var saved=JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(PlayerDataHandler.PLAYER_DATA));
            Check(saved.TotalCoins==754 && saved.ListStarData.Count==101 && saved.CurrentLevel==17 && saved.TargetLevel==12,"save format and all fields preserved");
            for(int i=0;i<10;i++){PlayerDataHandler.GetTotalCoin();PlayerDataHandler.GetStar(1);}
            object cachedSnapshot=typeof(PlayerDataHandler).GetField("cachedData",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null);
            var timer=System.Diagnostics.Stopwatch.StartNew();
            long start=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++){PlayerDataHandler.GetTotalCoin();PlayerDataHandler.GetCurrentLevel();PlayerDataHandler.GetStar(i%101+1);PlayerDataHandler.GetTimeOfLastReward();}
            long cachedBytes=GC.GetAllocatedBytesForCurrentThread()-start;
            timer.Stop();double cachedMs=timer.Elapsed.TotalMilliseconds;
            Check(ReferenceEquals(cachedSnapshot,typeof(PlayerDataHandler).GetField("cachedData",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).GetValue(null)),"save reads reuse one deserialized graph");
            timer.Restart();
            start=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)JsonUtility.FromJson<PlayerData>(json);
            long oldBytes=GC.GetAllocatedBytesForCurrentThread()-start;
            timer.Stop();double oldMs=timer.Elapsed.TotalMilliseconds;
            if(allocationCounter)Check(cachedBytes==0,"cached save getters allocate zero bytes after warmup");
            var board=CollisionBoard(new Vector2(350,240),new Vector2(321,277));
            board.Bricks[0].Health=board.Bricks[0].MaxHealth=100000000;
            for(int i=1;i<24;i++)board.Balls.Add(new CandyBreakoutSimulation.Ball { Kind=i%6,Position=new Vector2(100+i*20,600),Velocity=new Vector2(230+i,280-i) });
            for(int i=0;i<120;i++)board.Step(1f/120);
            start=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)board.Step(1f/120);
            long physicsBytes=GC.GetAllocatedBytesForCurrentThread()-start;
            if(allocationCounter)Check(physicsBytes==0,"24-ball simulation has no steady-state managed allocations");
            PlayerPrefs.DeleteKey(PlayerDataHandler.PLAYER_DATA);
            Check(!PlayerDataHandler.HasData(),"deleting save clears cached session data");
            Debug.Log("PERFORMANCE: 1000 cached read batches "+cachedMs.ToString("F2")+" ms; 1000 prior JSON reads "+oldMs.ToString("F2")+" ms; cache graph reuse and 24-ball simulation PASS; "+
                (allocationCounter?"managed allocations: reads "+cachedBytes+" B, prior JSON "+oldBytes+" B, simulation "+physicsBytes+" B":"this Mono runtime has no working per-thread allocation counter; no zero-allocation claim"));
        }
        public static bool HasAllocationCounter()
        {
            long before=GC.GetAllocatedBytesForCurrentThread();
            var probe=new byte[4096];GC.KeepAlive(probe);
            return GC.GetAllocatedBytesForCurrentThread()>before;
        }
        private static void CheckThemeAudio()
        {
            string[] music={"HomeLoop","CandyDay","MilkTea","Garden","StarryCandy","BigCandy"};
            foreach(string name in music)
            {
                var clip=Resources.Load<AudioClip>("BearAudio/"+name);
                Check(clip!=null && clip.channels==2 && clip.frequency==44100 && clip.length>30,"complete stereo music master "+name);
                Check(clip.loadType==AudioClipLoadType.Streaming,"mobile music uses streaming "+name);
            }
            var clips=Resources.LoadAll<AudioClip>("BearAudio");
            Check(clips.Length==27,"six music loops and 21 effects imported");
            foreach(var clip in clips)
            {
                if(music.Contains(clip.name))continue;
                Check(clip.channels==1 && clip.loadType==AudioClipLoadType.DecompressOnLoad && clip.length<2,"short mono effect import "+clip.name);
                var samples=new float[clip.samples];
                Check(clip.GetData(samples,0),"effect samples available without streaming delay "+clip.name);
                float peak=0;foreach(float sample in samples)peak=Mathf.Max(peak,Mathf.Abs(sample));
                Check(peak>.01f && peak<.5f,"effect contains audio with headroom "+clip.name);
            }
            Debug.Log("AUDIO ASSETS: six 44.1kHz stereo streaming loops and 21 preloaded mono effects, non-silent PCM and headroom PASS");
        }
        private static CandyBreakoutSimulation CollisionBoard(Vector2 position, Vector2 velocity, int kind = 0, int hp = 10000)
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.levelNumber = 1;
            var board = new CandyBreakoutSimulation(level);
            UnityEngine.Object.DestroyImmediate(level);
            board.Bricks.Clear();
            board.Bricks.Add(new CandyBreakoutSimulation.Brick { Bounds = new Rect(400,300,100,50), Health = hp, MaxHealth = hp });
            board.Balls[0].Position = position;
            board.Balls[0].Velocity = velocity;
            board.Balls[0].Kind = kind;
            return board;
        }
        private static void CheckSafeAreas()
        {
            foreach(Vector2 viewport in new[]{new Vector2(1080,1920),new Vector2(1080,2400),new Vector2(1170,2532),new Vector2(1536,2048)})
            {
                var parent=new GameObject("BearMenuUI",typeof(RectTransform)).GetComponent<RectTransform>();
                parent.sizeDelta=viewport;
                var frame=BearMenuFactory.Node(parent,"Settings",0,0,941,1672);
                var layout=frame.gameObject.AddComponent<BearMenuLayout>();
                Rect safe=new Rect(0,90,viewport.x,viewport.y-240);
                layout.Refresh(safe,viewport);
                float width=frame.rect.width*frame.localScale.x,height=frame.rect.height*frame.localScale.y;
                Check(width<=safe.width+.1f && height<=safe.height+.1f,"frame stays within notch and home indicator");
                Check(Mathf.Abs(height-safe.height)<.1f,"menu fills safe height");
                Check(Mathf.Abs(frame.anchoredPosition.y+30)<.1f,"asymmetric safe-area centre");
                UnityEngine.Object.DestroyImmediate(parent.gameObject);
            }
            Debug.Log("MOBILE LAYOUT: four viewport sizes with simulated notch/home indicator PASS");
        }
        private static void CheckCollisions()
        {
            var side = CollisionBoard(new Vector2(350,325),new Vector2(600,0));
            side.Step(.1f);
            Check(side.Balls[0].Velocity.x < 0 && Mathf.Abs(side.Balls[0].Position.x-368)<.1f,"side hit uses remaining travel after reflection");
            Check(side.Bricks[0].Health==9999,"single contact damages exactly once");
            var top = CollisionBoard(new Vector2(450,250),new Vector2(0,600));
            top.Step(.1f);
            Check(top.Balls[0].Velocity.y<0 && top.Balls[0].Position.y<289,"top face separates and reflects");
            var corner = CollisionBoard(new Vector2(370,270),new Vector2(400,400));
            corner.Step(.1f);
            Check(corner.Balls[0].Velocity.x<0 && corner.Balls[0].Velocity.y<0,"rounded corner normal reflects both components");
            Check(Mathf.Abs(corner.Balls[0].Velocity.magnitude-new Vector2(400,400).magnitude)<.01f,"elastic collision conserves speed");
            var leaving = CollisionBoard(new Vector2(389.5f,325),new Vector2(-600,0));
            leaving.Step(.05f);
            Check(leaving.Bricks[0].Health==10000 && leaving.Balls[0].Velocity.x<0,"overlap moving away cannot re-hit or reverse");
            var fast = CollisionBoard(new Vector2(100,325),new Vector2(30000,0));
            fast.Step(1f/120);
            fast.Step(1f/120);
            Check(fast.Bricks[0].Health<10000 && fast.Balls[0].Position.x<389,"continuous collision prevents tunnelling at extreme speed");
            var pierce = CollisionBoard(new Vector2(350,325),new Vector2(600,0),4,1);
            pierce.Step(.1f);
            Check(!pierce.Bricks[0].Alive && pierce.Balls[0].Velocity.x>0,"cocoa pierces a destroyed brick");
            var star = CollisionBoard(new Vector2(100,680),new Vector2(430,0),2);
            star.Step(.1f);
            Check(star.Balls[0].Velocity==new Vector2(430,0),"star does not steer towards bricks");
            var thirty=CollisionBoard(new Vector2(350,240),new Vector2(321,277));
            var sixty=CollisionBoard(new Vector2(350,240),new Vector2(321,277));
            var oneTwenty=CollisionBoard(new Vector2(350,240),new Vector2(321,277));
            for(int i=0;i<300;i++)thirty.Step(1f/30);
            for(int i=0;i<600;i++)sixty.Step(1f/60);
            for(int i=0;i<1200;i++)oneTwenty.Step(1f/120);
            Check(Vector2.Distance(thirty.Balls[0].Position,sixty.Balls[0].Position)<.02f && Vector2.Distance(thirty.Balls[0].Position,oneTwenty.Balls[0].Position)<.02f,"30/60/120 Hz produce identical motion");
            foreach(int kind in Enumerable.Range(0,6))
            {
                var b=CollisionBoard(new Vector2(390,280),new Vector2(431,317),kind);
                b.Bricks.Add(new CandyBreakoutSimulation.Brick { Bounds=new Rect(506,300,100,50),Health=10000,MaxHealth=10000 });
                for(int tick=0;tick<3600;tick++)
                {
                    b.Step(1f/120);
                    var ball=b.Balls[0];
                    Check(!float.IsNaN(ball.Position.x) && ball.Position.x>=10.9f && ball.Position.x<=889.1f && ball.Position.y>=10.9f && ball.Position.y<=749.1f,"ball remains inside walls");
                    foreach(var brick in b.Bricks)
                    {
                        Vector2 nearest=new Vector2(Mathf.Clamp(ball.Position.x,brick.Bounds.xMin,brick.Bounds.xMax),Mathf.Clamp(ball.Position.y,brick.Bounds.yMin,brick.Bounds.yMax));
                        Check(!brick.Alive || Vector2.Distance(ball.Position,nearest)>=10.9f,"no embedding at seams, kind "+kind);
                    }
                }
            }
            Debug.Log("MOBILE PHYSICS: faces, corners, overlap exit, high speed, pierce, no attraction, frame rates and 180 seconds seam soak PASS");
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
            var audioManager=ServicesManager.Instance.SoundManager;
            var homeMusic=(AudioSource)PrivateField(audioManager,"musicSource").GetValue(audioManager);
            var soundEffects=(AudioSource)PrivateField(audioManager,"soundSource").GetValue(audioManager);
            CandyBreakoutValidation.Check(homeMusic.clip!=null && homeMusic.clip.name=="HomeLoop" && homeMusic.loop && homeMusic.isPlaying,"home music is bound and playing");
            CandyBreakoutValidation.Check(Mathf.Approximately(homeMusic.volume,PlayerDataHandler.GetMusicValue()*audioManager.MusicMixGain) && audioManager.MusicMixGain==.25f,"background music uses quieter base mix without changing player setting");
            CandyBreakoutValidation.Check(soundEffects.ignoreListenerPause && !homeMusic.ignoreListenerPause,"pause UI remains audible while music pauses");
            CandyBreakoutValidation.Check(audioManager.Button.name=="UI_Click" && audioManager.LevelCompleted.name=="LevelComplete","retained scene audio references use new theme");
            for(int cue=0;cue<(int)GameSoundCue.Count;cue++)CandyBreakoutValidation.Check(audioManager.GetCue((GameSoundCue)cue)!=null,"every theme cue is resolved");
            float savedSound=PlayerDataHandler.GetSoundValue(),savedMusic=PlayerDataHandler.GetMusicValue();
            try
            {
                PlayerDataHandler.UpdateSoundValue(0);PlayerDataHandler.UpdateMusicValue(0);audioManager.UpdateVolumes();
                audioManager.PlayCue(GameSoundCue.UiClick);
                CandyBreakoutValidation.Check(soundEffects.volume==0 && homeMusic.volume==0,"existing sound/music settings mute new audio");
            }
            finally
            {
                PlayerDataHandler.UpdateSoundValue(savedSound);PlayerDataHandler.UpdateMusicValue(savedMusic);audioManager.UpdateVolumes();
            }
            yield return CheckServiceLifetimes();
            var menu=FindObjectOfType<BearHomeMenuController>();
            foreach(int page in new[]{0,1,2,3,4})
            {
                menu.OpenPage(page);
                Render(menu.themeRoot.gameObject,"mobile-menu-"+page+"-1080x2400.png",1080,2400);
                if(page>0)
                {
                    Transform menuFrame=menu.pages[page].transform;
                    var arrow=(menuFrame.Find("BackArrow") ?? menuFrame.Find("Header/BackArrow")) as RectTransform;
                    var paper=(menuFrame.Find("BackPaper") ?? menuFrame.Find("Header/BackPanel")) as RectTransform;
                    var corners=new Vector3[4];arrow.GetWorldCorners(corners);
                    foreach(Vector3 corner in corners)
                        CandyBreakoutValidation.Check(paper.rect.Contains(paper.InverseTransformPoint(corner)),"back arrow stays inside return button, page "+page);
                }
            }
            menu.OpenPage(0);
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
            var roundMusic=(AudioSource)PrivateField(audioManager,"musicSource").GetValue(audioManager);
            CandyBreakoutValidation.Check(roundMusic.clip!=null && roundMusic.clip.name=="CandyDay","first game music reference uses new theme");
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
            CandyBreakoutValidation.Check(first.Health == hp, "press alone causes no damage");
            graphic.OnPointerUp(pointer);
            CandyBreakoutValidation.Check(first.Health == hp - 1, "pointer position damages intended brick");
            game.SetPaused(true);
            hp = first.Health;
            graphic.OnPointerDown(pointer);
            graphic.OnPointerUp(pointer);
            CandyBreakoutValidation.Check(first.Health == hp, "paused pointer cannot damage");
            float elapsed = game.Board.Elapsed;
            int honey = game.Board.Honey;
            yield return new WaitForSecondsRealtime(.2f);
            CandyBreakoutValidation.Check(game.Board.Elapsed == elapsed && !game.CanInteract, "pause freezes board and input");
            int dirties=0;
            UnityEngine.Events.UnityAction countDirty=()=>dirties++;
            Canvas.ForceUpdateCanvases();
            graphic.RegisterDirtyVerticesCallback(countDirty);
            yield return new WaitForSecondsRealtime(.18f);
            graphic.UnregisterDirtyVerticesCallback(countDirty);
            CandyBreakoutValidation.Check(dirties==0,"paused arena does not request mesh rebuilds");
            game.SetPaused(false);
            yield return CheckTouchGestures(game, graphic);
            var buy = views.IngameViewController.GetComponentsInChildren<Button>(true).First(x => x.name == "Buy");
            buy.onClick.Invoke();
            CandyBreakoutValidation.Check(game.Board.Balls.Count == 2 && game.Board.Honey < honey, "actual shop button spends honey");
            yield return null;
            Render(views.IngameViewController.gameObject, "polished-1080x1920.png", 1080, 1920);
            Render(views.IngameViewController.gameObject, "polished-1080x2400.png", 1080, 2400);
            Render(views.IngameViewController.gameObject, "polished-1024x1536.png", 1024, 1536);
            Render(views.IngameViewController.gameObject, "polished-tablet.png", 1536, 2048);
            Render(views.IngameViewController.gameObject, "polished-landscape.png", 1920, 1080);
            var shopScroll=views.IngameViewController.transform.Find("CandyBreakoutUI/CandyRoom/BallShop").GetComponent<ScrollRect>();
            shopScroll.horizontalNormalizedPosition=1;
            Canvas.ForceUpdateCanvases();
            Render(views.IngameViewController.gameObject,"mobile-shop-last-three.png",1080,1920);
            CandyBreakoutValidation.Check(shopScroll.content.anchoredPosition.x < -900,"swipe reveals last three ball types");
            shopScroll.horizontalNormalizedPosition=0;
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
            CandyBreakoutValidation.Check(graphic.Board==null,"hidden persistent gameplay view releases the previous simulation");
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
            var gameplay=views.IngameViewController.GetComponent<CandyBreakoutView>();
            var labelRoot=views.IngameViewController.transform.Find("CandyBreakoutUI/CandyRoom/CandyBoard/Arena/BrickHealthLabels");
            int pooledLabels=labelRoot.childCount;
            gameplay.Bind(IngameManager.Instance);gameplay.Bind(IngameManager.Instance);
            CandyBreakoutValidation.Check(labelRoot.childCount==pooledLabels,"rebind reuses health labels without Instantiate/Destroy churn");
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
            CandyBreakoutValidation.Check(roundMusic.clip!=null && roundMusic.clip.name=="BigCandy","boss uses gentle dedicated loop");
            views.IngameViewController.GetComponent<BearPausePanel>().ReturnHome();
            yield return new WaitForSecondsRealtime(2);
            CandyBreakoutValidation.Check(views.ActiveViewType == ViewType.HOME_VIEW && IngameManager.Instance == null && Time.timeScale == 1, "return home restores state");
            CandyBreakoutValidation.Check(originalScaler.referenceResolution==originalReference && originalScaler.matchWidthOrHeight==originalMatch,"other views retain their original scaler");
        }
        private static Vector2 ScreenPoint(RectTransform rect, Vector2 local)
            => RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(local));
        private static System.Reflection.FieldInfo PrivateField(object target,string name)
            => target.GetType().GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        private static IEnumerator CheckServiceLifetimes()
        {
            var services=ServicesManager.Instance;var views=ViewManager.Instance;
            var duplicateServices=new GameObject("DuplicateServicesProbe");duplicateServices.AddComponent<ServicesManager>();
            var duplicateViews=new GameObject("DuplicateViewsProbe");duplicateViews.AddComponent<ViewManager>();
            CandyBreakoutValidation.Check(!duplicateServices.activeSelf && !duplicateViews.activeSelf,"duplicate services deactivate before Start");
            yield return null;
            CandyBreakoutValidation.Check(duplicateServices==null && duplicateViews==null && ServicesManager.Instance==services && ViewManager.Instance==views,"destroying duplicates preserves persistent singleton ownership");

            var parent=new GameObject("BearMenuUI",typeof(RectTransform)).GetComponent<RectTransform>();parent.sizeDelta=new Vector2(1080,2400);
            var frame=BearMenuFactory.Node(parent,"Settings",0,0,941,1672);
            var layout=frame.gameObject.AddComponent<BearMenuLayout>();
            Rect safe=new Rect(0,90,1080,2160);Vector2 viewport=new Vector2(1080,2400);
            for(int i=0;i<10;i++)layout.Refresh(safe,viewport);
            long start=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)layout.Refresh(safe,viewport);
            long layoutBytes=GC.GetAllocatedBytesForCurrentThread()-start;
            bool allocationCounter=CandyBreakoutValidation.HasAllocationCounter();
            if(allocationCounter)CandyBreakoutValidation.Check(layoutBytes==0,"stable layout refresh has no managed allocations");
            DestroyImmediate(parent.gameObject);

            var iapObject=new GameObject("IAPTaskProbe");var iap=iapObject.AddComponent<IAPManager>();
            var pending=new System.Threading.Tasks.TaskCompletionSource<bool>();
            PrivateField(iap,"initializationTask").SetValue(iap,pending.Task);
            CandyBreakoutValidation.Check(ReferenceEquals(iap.InitializePurchasing(),pending.Task) && ReferenceEquals(iap.InitializePurchasing(),pending.Task),"concurrent IAP initialization shares one task");
            DestroyImmediate(iapObject);pending.SetResult(true);

            var adObject=new GameObject("AdRetryProbe");var ad=adObject.AddComponent<UnityAdController>();
            string placement=(string)typeof(UnityAdController).GetProperty("InterstitialID",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(ad);
            ad.OnUnityAdsFailedToLoad(placement,(UnityEngine.Advertisements.UnityAdsLoadError)0,"validation");
            object retry=PrivateField(ad,"interstitialRetry").GetValue(ad);
            ad.OnUnityAdsFailedToLoad(placement,(UnityEngine.Advertisements.UnityAdsLoadError)0,"validation");
            CandyBreakoutValidation.Check(retry!=null && ReferenceEquals(retry,PrivateField(ad,"interstitialRetry").GetValue(ad)),"ad load failures coalesce retries");
            ad.OnUnityAdsAdLoaded(placement);
            CandyBreakoutValidation.Check(PrivateField(ad,"interstitialRetry").GetValue(ad)==null,"successful load cancels pending retry");
            adObject.SetActive(false);
            CandyBreakoutValidation.Check(PrivateField(ad,"rewardedRetry").GetValue(ad)==null,"disabled ad owner releases retries");
            DestroyImmediate(adObject);

            var leaderboardObject=new GameObject("LeaderboardParseProbe");var leaderboard=leaderboardObject.AddComponent<LeaderboardManager>();
            var parse=typeof(LeaderboardManager).GetMethod("ParseLeaderboard",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var rows=(System.Collections.Generic.List<LeaderboardParams>)parse.Invoke(leaderboard,new object[]{"alice|3\nbroken\nbob|bad\ncarol|7\n"});
            CandyBreakoutValidation.Check(rows.Count==2 && rows[0].Username=="carol","malformed leaderboard rows do not abort valid results or ranking");
            DestroyImmediate(leaderboardObject);

            string snapshot=PlayerPrefs.GetString(PlayerDataHandler.PLAYER_DATA);
            try
            {
                PlayerDataHandler.UpdateTimeOfLastReward(DateTime.Now.AddHours(-25).ToString("yyyy:M:d:H:m:s",System.Globalization.CultureInfo.InvariantCulture));
                PlayerDataHandler.UpdateCurrentRewardIndex(services.DailyRewardManager.DailyRewardConfigurations.Length);
                int resets=0;Action onReset=()=>resets++;
                services.DailyRewardManager.RewardsReset+=onReset;
                try
                {
                    CandyBreakoutValidation.Check(services.DailyRewardManager.TimeRemainsTillNextReward()==0 && PlayerDataHandler.GetCurrentRewardIndex()==0,"daily reward format and rollover preserved");
                    services.DailyRewardManager.TimeRemainsTillNextReward();
                    CandyBreakoutValidation.Check(resets==1,"daily reward rollover emits one notification");
                }
                finally { services.DailyRewardManager.RewardsReset-=onReset; }
            }
            finally { PlayerDataHandler.SaveData(JsonUtility.FromJson<PlayerData>(snapshot)); }

            var menu=FindObjectOfType<BearHomeMenuController>();
            try
            {
                menu.OpenPage(4);
                menu.soundSlider.value=.37f;
                CandyBreakoutValidation.Check(Mathf.Approximately(PlayerDataHandler.GetSoundValue(),.37f) && (bool)PrivateField(menu,"preferencesDirty").GetValue(menu),"volume changes apply immediately while disk saves are coalesced");
                menu.OpenPage(0);
                CandyBreakoutValidation.Check(!(bool)PrivateField(menu,"preferencesDirty").GetValue(menu),"leaving settings flushes pending preferences");
                menu.Hide();menu.Show(views.HomeViewController);
                var subscribers=(Action<int>)typeof(CoinManager).GetField("TotalCoinChanged",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(services.CoinManager);
                CandyBreakoutValidation.Check(subscribers.GetInvocationList().Count(x=>ReferenceEquals(x.Target,menu) && x.Method.Name=="OnCoins")==1,"menu hide/show retains exactly one coin subscription");
            }
            finally
            {
                var restored=JsonUtility.FromJson<PlayerData>(snapshot);
                PlayerDataHandler.SaveData(restored);
                menu.soundSlider.SetValueWithoutNotify(restored.SoundValue);menu.musicSlider.SetValueWithoutNotify(restored.MusicValue);
                services.SoundManager.UpdateVolumes();menu.OpenPage(0);
            }

            var audioObject=new GameObject("SoundTransitionProbe");
            var music=audioObject.AddComponent<AudioSource>();var effects=audioObject.AddComponent<AudioSource>();
            var sound=audioObject.AddComponent<SoundManager>();
            PrivateField(sound,"musicSource").SetValue(sound,music);PrivateField(sound,"soundSource").SetValue(sound,effects);
            var clipA=AudioClip.Create("transition A",48000,1,48000,false);var clipB=AudioClip.Create("transition B",48000,1,48000,false);
            float originalMusic=PlayerDataHandler.GetMusicValue();
            try
            {
                sound.PlayMusic(clipA,.02f);sound.StopMusic(.08f);sound.PlayMusic(clipB,.03f);
                yield return new WaitForSecondsRealtime(.14f);
                float fullMusic=PlayerDataHandler.GetMusicValue()*sound.MusicMixGain;
                CandyBreakoutValidation.Check(music.clip==clipB && music.isPlaying && Mathf.Approximately(music.volume,fullMusic),"old music fade cannot stop a newer track or bypass music mix gain");
                sound.PlayCue(GameSoundCue.BrickHit);
                yield return new WaitForSecondsRealtime(.045f);
                CandyBreakoutValidation.Check(music.volume<fullMusic*.85f && PlayerDataHandler.GetMusicValue()==originalMusic,"collision ducks music without modifying saved volume");
                for(int i=0;i<4;i++){sound.PlayCue(GameSoundCue.BrickBreak);yield return new WaitForSecondsRealtime(.04f);}
                float duckGain=(float)PrivateField(sound,"collisionMusicGain").GetValue(sound);
                CandyBreakoutValidation.Check(music.volume>=fullMusic*duckGain-.001f,"repeated collisions hold one duck instead of multiplying attenuation");
                yield return new WaitForSecondsRealtime(.42f);
                CandyBreakoutValidation.Check(Mathf.Approximately(music.volume,fullMusic),"music smoothly returns to its quieter mix after collisions");
                float originalSound=PlayerDataHandler.GetSoundValue();
                try
                {
                    PlayerDataHandler.UpdateSoundValue(0);sound.UpdateVolumes();sound.PlayCue(GameSoundCue.BrickHit);
                    yield return new WaitForSecondsRealtime(.06f);
                    CandyBreakoutValidation.Check(Mathf.Approximately(music.volume,fullMusic),"muted collisions do not duck music");
                }
                finally {PlayerDataHandler.UpdateSoundValue(originalSound);sound.UpdateVolumes();}
                sound.PlayMusic(clipA,.25f);
                var outgoing=PrivateField(sound,"outgoingMusic").GetValue(sound) as AudioSource;
                bool previousListenerPause=AudioListener.pause;
                try
                {
                    AudioListener.pause=true;
                    PlayerDataHandler.UpdateMusicValue(0);sound.UpdateVolumes();
                    CandyBreakoutValidation.Check(music.volume==0 && (outgoing==null || outgoing.volume==0),"mute immediately affects both crossfade sources even while paused");
                }
                finally { AudioListener.pause=previousListenerPause; }
                yield return new WaitForSecondsRealtime(.06f);
                CandyBreakoutValidation.Check(music.volume==0 && (outgoing==null || outgoing.volume==0),"crossfade cannot restore audio after mute");
                PlayerDataHandler.UpdateMusicValue(originalMusic);sound.UpdateVolumes();
                sound.PauseMusic(.02f);yield return new WaitForSecondsRealtime(.05f);
                sound.ResumeMusic(.03f);yield return new WaitForSecondsRealtime(.06f);
                CandyBreakoutValidation.Check(music.isPlaying,"music pause/resume preserved");
                sound.StopMusic(.2f);sound.enabled=false;
                CandyBreakoutValidation.Check(PrivateField(sound,"musicFade").GetValue(sound)==null,"disabled sound owner cancels transition coroutine");
            }
            finally { PlayerDataHandler.UpdateMusicValue(originalMusic);DestroyImmediate(audioObject);DestroyImmediate(clipA);DestroyImmediate(clipB); }
            Debug.Log("LIFECYCLE: singleton ownership, stable layout"+(allocationCounter?" "+layoutBytes+" B":" (allocation counter unavailable)")+", IAP task coalescing, ad retry cleanup, leaderboard parsing, daily rollover, quieter music mix, collision duck/recovery, mute, interrupted music and pause/resume PASS");
        }
        private static void TouchAt(Touchscreen device, Vector2 point, UnityEngine.InputSystem.TouchPhase phase)
            => InputSystem.QueueStateEvent(device,new TouchState { touchId=1,position=point,phase=phase });
        private static IEnumerator CheckTouchGestures(IngameManager game, CandyBreakoutGraphic graphic)
        {
            // Feed real touch events through InputSystemUIInputModule, including UI raycasts.
            var background=InputSystem.settings.backgroundBehavior;
            var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var device=InputSystem.AddDevice<Touchscreen>();
            var first=game.Board.Bricks[0];var second=game.Board.Bricks[1];
            int firstHealth=first.Health,secondHealth=second.Health;
            var velocities=game.Board.Balls.Select(x=>x.Velocity).ToArray();
            foreach(var ball in game.Board.Balls)ball.Velocity=Vector2.zero;
            first.Health+=1000;second.Health+=1000;
            Vector2 a=ScreenPoint(graphic.rectTransform,graphic.ToLocalPoint(first.Bounds.center));
            Vector2 b=ScreenPoint(graphic.rectTransform,graphic.ToLocalPoint(second.Bounds.center));
            var upgrade=graphic.transform.root.GetComponentsInChildren<Button>().First(x=>x.name=="ClickUpgrade").GetComponent<RectTransform>();
            var pause=graphic.transform.root.GetComponentsInChildren<Button>().First(x=>x.name=="PauseButton").GetComponent<RectTransform>();
            Vector2 buttonA=ScreenPoint(upgrade,upgrade.rect.center),buttonB=ScreenPoint(pause,pause.rect.center);
            try
            {
                yield return null;
                int hp=first.Health;
                TouchAt(device,a,UnityEngine.InputSystem.TouchPhase.Began);
                yield return new WaitForSecondsRealtime(.3f);
                CandyBreakoutValidation.Check(first.Health==hp,"holding finger produces no damage");
                TouchAt(device,a,UnityEngine.InputSystem.TouchPhase.Ended);
                yield return new WaitForSecondsRealtime(.12f);
                CandyBreakoutValidation.Check(first.Health==hp-1,"touch release on same brick damages exactly once");
                hp=first.Health;int other=second.Health;
                TouchAt(device,a,UnityEngine.InputSystem.TouchPhase.Began);
                yield return new WaitForSecondsRealtime(.04f);
                TouchAt(device,b,UnityEngine.InputSystem.TouchPhase.Moved);
                yield return new WaitForSecondsRealtime(.04f);
                TouchAt(device,b,UnityEngine.InputSystem.TouchPhase.Ended);
                yield return new WaitForSecondsRealtime(.12f);
                CandyBreakoutValidation.Check(first.Health==hp && second.Health==other,"drag between bricks damages neither brick");
                TouchAt(device,a,UnityEngine.InputSystem.TouchPhase.Began);
                yield return new WaitForSecondsRealtime(.04f);
                TouchAt(device,b,UnityEngine.InputSystem.TouchPhase.Moved);
                yield return new WaitForSecondsRealtime(.04f);
                TouchAt(device,a,UnityEngine.InputSystem.TouchPhase.Moved);
                yield return new WaitForSecondsRealtime(.04f);
                TouchAt(device,a,UnityEngine.InputSystem.TouchPhase.Ended);
                yield return new WaitForSecondsRealtime(.12f);
                CandyBreakoutValidation.Check(first.Health==hp && second.Health==other,"dragging back to original brick remains cancelled");
                int honey=game.Board.Honey,clickLevel=game.Board.ClickLevel;
                TouchAt(device,a,UnityEngine.InputSystem.TouchPhase.Began);
                yield return new WaitForSecondsRealtime(.04f);
                TouchAt(device,buttonA,UnityEngine.InputSystem.TouchPhase.Moved);
                yield return new WaitForSecondsRealtime(.04f);
                TouchAt(device,buttonA,UnityEngine.InputSystem.TouchPhase.Ended);
                yield return new WaitForSecondsRealtime(.12f);
                CandyBreakoutValidation.Check(first.Health==hp && game.Board.Honey==honey && game.Board.ClickLevel==clickLevel,"drag from board onto upgrade button cannot activate it");
                TouchAt(device,buttonA,UnityEngine.InputSystem.TouchPhase.Began);
                yield return new WaitForSecondsRealtime(.04f);
                TouchAt(device,buttonB,UnityEngine.InputSystem.TouchPhase.Moved);
                yield return new WaitForSecondsRealtime(.04f);
                TouchAt(device,buttonB,UnityEngine.InputSystem.TouchPhase.Ended);
                yield return new WaitForSecondsRealtime(.12f);
                CandyBreakoutValidation.Check(!game.IsPaused && game.Board.Honey==honey && game.Board.ClickLevel==clickLevel,"drag between buttons activates neither button");
                Vector2 empty=ScreenPoint(graphic.rectTransform,graphic.ToLocalPoint(new Vector2(450,650)));
                TouchAt(device,empty,UnityEngine.InputSystem.TouchPhase.Began);
                yield return new WaitForSecondsRealtime(.04f);
                TouchAt(device,a,UnityEngine.InputSystem.TouchPhase.Moved);
                yield return new WaitForSecondsRealtime(.04f);
                TouchAt(device,a,UnityEngine.InputSystem.TouchPhase.Ended);
                yield return new WaitForSecondsRealtime(.12f);
                CandyBreakoutValidation.Check(first.Health==hp,"dragging in from empty field cannot click a brick");
                TouchAt(device,a,UnityEngine.InputSystem.TouchPhase.Began);
                yield return new WaitForSecondsRealtime(.04f);
                game.SetPaused(true);
                yield return null;
                game.SetPaused(false);
                TouchAt(device,a,UnityEngine.InputSystem.TouchPhase.Ended);
                yield return new WaitForSecondsRealtime(.12f);
                CandyBreakoutValidation.Check(first.Health==hp,"pause cancels pending touch");
                TouchAt(device,buttonB,UnityEngine.InputSystem.TouchPhase.Began);
                yield return new WaitForSecondsRealtime(.04f);
                CandyBreakoutValidation.Check(!game.IsPaused,"button press alone does not activate pause");
                TouchAt(device,buttonB,UnityEngine.InputSystem.TouchPhase.Ended);
                yield return new WaitForSecondsRealtime(.08f);
                CandyBreakoutValidation.Check(game.IsPaused,"normal button tap still works");
                game.SetPaused(false);
                Debug.Log("CLICK INPUT: real touch hold, release, brick drag, drag back, board-to-button, button-to-button, empty-to-brick, pause cancellation and normal button tap PASS");
            }
            finally
            {
                game.SetPaused(false);
                first.Health=firstHealth;second.Health=secondHealth;
                for(int i=0;i<velocities.Length;i++)game.Board.Balls[i].Velocity=velocities[i];
                InputSystem.RemoveDevice(device);
                InputSystem.settings.backgroundBehavior=background;
                InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            }
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
                if(!text.canvasRenderer.cull && !string.IsNullOrWhiteSpace(text.text))
                    CandyBreakoutValidation.Check(text.cachedTextGenerator.vertexCount>=4,"text actually generates visible glyphs: "+text.name+" "+text.text);
            foreach(var action in view.GetComponentsInChildren<CandyBreakoutButton>())
            {
                var rect = (RectTransform)action.transform;
                CandyBreakoutValidation.Check(rect.rect.width>=144 && rect.rect.height>=132, "touch target size");
            }
            var graphic=view.GetComponentInChildren<CandyBreakoutGraphic>();
            Vector2 a=graphic.ToLocalPoint(new Vector2(100,100)),x=graphic.ToLocalPoint(new Vector2(200,100)),y=graphic.ToLocalPoint(new Vector2(100,200));
            CandyBreakoutValidation.Check(Mathf.Abs(Vector2.Distance(a,x)-Vector2.Distance(a,y))<.001f,"uniform field scale preserves collision angles");
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
