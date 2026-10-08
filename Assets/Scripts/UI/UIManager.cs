using System.Collections.Generic;
using Relicfall.Boss;
using Relicfall.Items;
using Relicfall.Save;
using Relicfall.Feedback;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Relicfall.UI
{
    // 唯一的页面管理器：常驻 Canvas，场景业务组件在启停时绑定/解绑。
    [DefaultExecutionOrder(-300)]
    public sealed class UIManager : MonoBehaviour
    {
        private static UIManager instance;
        private static bool quitting;
        public static UIManager Instance
        {
            get
            {
                if (quitting) return null;
                if (instance == null)
                {
                    instance = FindAnyObjectByType<UIManager>();
                    if (instance == null)
                    {
                        GameObject prefab = Resources.Load<GameObject>("UI/RelicfallUI");
                        if (prefab != null) instance = Instantiate(prefab).GetComponent<UIManager>();
                        else instance = new GameObject("UIManager").AddComponent<UIManager>();
                    }
                    instance.Initialize();
                }
                return instance;
            }
        }
        public static UIManager Existing => instance;
        public static bool BlocksGameplay => instance != null && (instance.loading ||
            instance.Overlay == GameOverlay.Pause || instance.Overlay == GameOverlay.Death ||
            instance.Overlay == GameOverlay.Victory);
        public GameOverlay Overlay { get; private set; }
        public bool IsInventoryOpen { get; private set; }
        public bool ShowingControls { get; private set; }
        public InventoryUguiView InventoryView { get; private set; }
        public UIInputRouter Inputs { get; private set; }
        public InventoryPresenter InventoryPresenter { get; private set; }
        public InventoryUI InventoryController => inventorySource;

        private Canvas canvas;
        private RectTransform safeArea, damageRoot;
        private CanvasGroup menu, controls, overlayPage, loadingPage, bossPage, noticePage, pickupPage;
        private MainMenuUI mainSource;
        private InventoryUI inventorySource;
        private GameUIScreen gameSource;
        private BossEncounterUI bossSource;
        private DragonKnightBoss boss;
        private Text overlayHeading, overlayDetail, retryLabel, loadingDetail, bossPhase, noticeText, pickupText;
        private Button continueButton, pauseContinue, saveButton, restartButton, menuButton, retryButton;
        private Slider loadingBar, bossBar;
        private RawImage backdrop;
        private RectTransform noticePanel;
        private CanvasGroup backdropGroup;
        private readonly List<DamageNumber> numbers = new();
        private bool initialized, loading, bossAppeared, gateDirty;
        private float overlayAt, noticeUntil, bossAt;
        private Vector2 lastScreen;
        private Rect lastSafe;

        private sealed class DamageNumber
        {
            public Text Text;
            public Vector3 Point;
            public float Born;
            public bool Active;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            quitting = false;
            // 禁用域重载时也确保新一轮游戏不会继承上次暂停的时间倍率。
            GameTime.ClearTransient();
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            gameObject.layer = LayerMask.NameToLayer("UI");
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            Initialize();
        }
        private void OnEnable() => SceneManager.activeSceneChanged += SceneChanged;
        private void OnDisable() => SceneManager.activeSceneChanged -= SceneChanged;
        private void OnApplicationQuit() => quitting = true;
        private void OnDestroy() { if (instance == this) instance = null; }

        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            Inputs = UguiTheme.Component<UIInputRouter>(gameObject);
            InventoryPresenter = UguiTheme.Component<InventoryPresenter>(gameObject);
            gameObject.layer = LayerMask.NameToLayer("UI");
            RectTransform canvasRect = UguiTheme.Node(transform, "Canvas");
            canvas = UguiTheme.Component<Canvas>(canvasRect.gameObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = UguiTheme.Component<CanvasScaler>(canvasRect.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;
            UguiTheme.Component<GraphicRaycaster>(canvasRect.gameObject);
            backdropGroup = UguiTheme.Page(canvasRect, "Backdrop");
            backdrop = UguiTheme.Component<RawImage>(backdropGroup.gameObject);
            backdrop.texture = Resources.Load<Texture2D>("DarkForest1.2/main_background");
            backdrop.raycastTarget = false;
            safeArea = UguiTheme.Stretch(canvasRect, "SafeArea");
            BuildMenu();
            InventoryView = UguiTheme.Component<InventoryUguiView>(gameObject);
            InventoryView.Build(this, safeArea);
            BuildBoss();
            damageRoot = UguiTheme.Stretch(safeArea, "DamageNumbers");
            for (int i = 0; i < 20; i++) CreateNumber();
            BuildOverlay();
            noticePage = UguiTheme.Page(safeArea, "Notice");
            RectTransform notice = UguiTheme.Panel(noticePage.transform, "Panel", -210, -180, 420, 42);
            noticePanel = notice;
            notice.anchorMin = notice.anchorMax = new Vector2(0.5f, 0);
            noticeText = UguiTheme.Label(notice, "Message", 12, 4, 396, 34, "", 16,
                UguiTheme.Gold, TextAnchor.MiddleCenter);
            pickupPage = UguiTheme.Page(safeArea, "PickupPrompt");
            RectTransform pickup = UguiTheme.Panel(pickupPage.transform, "Panel", -210, -230, 420, 42);
            pickup.anchorMin = pickup.anchorMax = new Vector2(0.5f, 0);
            pickupText = UguiTheme.Label(pickup, "Message", 12, 4, 396, 34, "", 16,
                UguiTheme.Gold, TextAnchor.MiddleCenter);
            BuildLoading();
            Transform eventRoot = transform.Find("EventSystem");
            if (eventRoot == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                go.transform.SetParent(transform, false);
                eventRoot = go.transform;
            }
            var module = eventRoot.GetComponent<InputSystemUIInputModule>();
            // 默认操作绑定鼠标、键盘与手柄，不依赖旧输入系统。
            if (Application.isPlaying) module.AssignDefaultActions();
            RefreshPages();
        }

        private void BuildMenu()
        {
            menu = UguiTheme.Page(safeArea, "MainMenu");
            RectTransform panel = UguiTheme.Panel(menu.transform, "Panel", 106, 99, 482, 523);
            UguiTheme.Header(panel, "Title", 36, 54, 410, 70, "遗物之陨", 46);
            UguiTheme.Label(panel, "Subtitle", 39, 131, 408, 29, "暗影深处，遗物正在苏醒", 18);
            continueButton = UguiTheme.Button(panel, "Continue", 59, 216, 364, 46, "继续旅程", () => StartJourney(true));
            UguiTheme.Button(panel, "Start", 59, 278, 364, 46, "开始旅程", () => StartJourney(false));
            UguiTheme.Button(panel, "Controls", 59, 340, 364, 46, "操作说明", () => SetControls(true));
            UguiTheme.Button(panel, "Exit", 59, 402, 364, 46, "退出游戏", () => mainSource?.Exit());
            controls = UguiTheme.Page(safeArea, "Controls", true);
            RectTransform help = CenterPanel(controls.transform, "Panel", 500, 400);
            UguiTheme.Header(help, "Title", 30, 20, 440, 50, "操作说明");
            UguiTheme.Label(help, "Keys", 40, 86, 420, 215,
                "A / D  移动    W  跳跃\nJ  攻击        Shift  冲刺\nE  拾取        I  背包\nH  药水        F5  保存\n1 / 2 / Q  武器切换\nEsc  暂停", 18);
            UguiTheme.Button(help, "Back", 85, 326, 330, 46, "返回", () => SetControls(false));
        }

        private static RectTransform CenterPanel(Transform parent, string name, float w, float h)
        {
            RectTransform rect = UguiTheme.Panel(parent, name, -w * 0.5f, -h * 0.5f, w, h);
            rect.anchorMin = rect.anchorMax = Vector2.one * 0.5f;
            return rect;
        }

        private void BuildOverlay()
        {
            overlayPage = UguiTheme.Page(safeArea, "GameOverlay", true);
            RectTransform panel = CenterPanel(overlayPage.transform, "Panel", 500, 430);
            overlayHeading = UguiTheme.Header(panel, "Heading", 40, 40, 420, 66, "", 40);
            overlayDetail = UguiTheme.Label(panel, "Detail", 35, 118, 430, 32, "", 18,
                UguiTheme.Pale, TextAnchor.MiddleCenter);
            pauseContinue = UguiTheme.Button(panel, "Continue", 86, 192, 328, 46, "继续游戏", () => SetOverlay(GameOverlay.None));
            saveButton = UguiTheme.Button(panel, "Save", 86, 245, 328, 46, "保存进度", () => GameSaveController.Instance?.SaveNow());
            restartButton = UguiTheme.Button(panel, "Restart", 86, 298, 328, 46, "重新开始", () => Transition("GameScene"));
            retryLabel = restartButton.transform.Find("Label").GetComponent<Text>();
            menuButton = UguiTheme.Button(panel, "MainMenu", 86, 351, 328, 46, "返回主菜单", () => Transition("MainMenuScene"));
        }

        private void BuildLoading()
        {
            loadingPage = UguiTheme.Page(safeArea, "Loading", true);
            RectTransform panel = CenterPanel(loadingPage.transform, "Panel", 700, 180);
            UguiTheme.Header(panel, "Title", 40, 20, 620, 55, "踏入遗物之陨");
            loadingDetail = UguiTheme.Label(panel, "Detail", 40, 80, 620, 28, "", 16,
                UguiTheme.Pale, TextAnchor.MiddleCenter);
            loadingBar = UguiTheme.Bar(panel, "Progress", 70, 132, 560, 18, UguiTheme.Gold);
            retryButton = UguiTheme.Button(panel, "Retry", 200, 126, 300, 38, "重试", () => LoadingManager.Retry());
        }

        private void BuildBoss()
        {
            bossPage = UguiTheme.Page(safeArea, "Boss");
            RectTransform panel = UguiTheme.Panel(bossPage.transform, "Panel", -255, 20, 510, 82);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1);
            UguiTheme.Label(panel, "Name", 20, 9, 300, 32, "龙焰骑士", 18, UguiTheme.Gold);
            bossPhase = UguiTheme.Label(panel, "Phase", 325, 11, 164, 28, "", 14,
                UguiTheme.Muted, TextAnchor.MiddleRight);
            bossBar = UguiTheme.Bar(panel, "Health", 21, 46, 468, 18, UguiTheme.Red);
        }

        public void BindMainMenu(MainMenuUI source)
        {
            mainSource = source;
            continueButton.gameObject.SetActive(JsonSaveManager.HasSave);
            ShowingControls = false;
            Overlay = GameOverlay.None;
            RefreshPages();
        }
        public void UnbindMainMenu(MainMenuUI source) { if (mainSource == source) { mainSource = null; ShowingControls = false; } }
        public void BindInventory(InventoryUI source)
        {
            if (inventorySource != source) { IsInventoryOpen = false; inventorySource?.ApplyOpen(false); }
            inventorySource = source;
            InventoryPresenter.Bind(source, InventoryView);
            gateDirty = true;
            ApplyGameplayGate();
        }
        public void UnbindInventory(InventoryUI source)
        {
            if (inventorySource != source) return;
            source.ApplyOpen(false);
            inventorySource = null;
            InventoryPresenter.Bind(null, InventoryView);
            IsInventoryOpen = false;
        }
        public void BindGame(GameUIScreen source) { gameSource = source; Overlay = GameOverlay.None; gateDirty = true; ApplyGameplayGate(); }
        public void UnbindGame(GameUIScreen source) { if (gameSource == source) gameSource = null; }
        public void BindBoss(BossEncounterUI source, DragonKnightBoss target) { bossSource = source; boss = target; bossAppeared = false; }
        public void UnbindBoss(BossEncounterUI source) { if (bossSource == source) { bossSource = null; boss = null; bossAppeared = false; } }

        public void SetControls(bool show) { if (mainSource == null || loading) return; ShowingControls = show; RefreshPages(); }
        private void StartJourney(bool resume) { if (mainSource == null || loading) return; mainSource.StartJourney(resume); }
        private void Transition(string scene)
        {
            if (loading) return;
            GameSaveController.ContinueRequested = false;
            SceneTransition.Load(scene);
        }

        public void SetInventoryOpen(InventoryUI source, bool open)
        {
            if (source != inventorySource) return;
            if (open && (BlocksGameplay || source.IsDead)) return;
            IsInventoryOpen = open;
            source.ApplyOpen(open);
            ApplyGameplayGate();
            RefreshPages();
        }
        public void CloseInventory()
        {
            IsInventoryOpen = false;
            inventorySource?.ApplyOpen(false);
            ApplyGameplayGate();
        }
        public void InventoryAction(InventoryUIAction action)
        {
            if (loading || BlocksGameplay || inventorySource == null || inventorySource.IsDead) return;
            inventorySource.Handle(action);
        }
        public void SelectInventorySlot(int index)
        {
            if (IsInventoryOpen && !BlocksGameplay) inventorySource?.SelectSlot(index);
        }
        public void SetInventoryFilter(int filter)
        {
            if (IsInventoryOpen && !BlocksGameplay) inventorySource?.SetFilter(filter);
        }
        public void SetOverlay(GameOverlay value)
        {
            if (loading) return;
            if (Overlay == GameOverlay.Victory && value == GameOverlay.DeathPending) return;
            if ((Overlay == GameOverlay.Death || Overlay == GameOverlay.DeathPending) && value == GameOverlay.Victory) return;
            CloseInventory();
            Overlay = value;
            overlayAt = Time.unscaledTime;
            ApplyGameplayGate();
            RefreshPages();
        }
        public void BeginLoading()
        {
            loading = true;
            CloseInventory();
            ApplyGameplayGate();
            RefreshPages();
        }
        public void EndLoading()
        {
            loading = false;
            Overlay = GameOverlay.None;
            ShowingControls = false;
            ApplyGameplayGate();
            RefreshPages();
        }
        private void ApplyGameplayGate()
        {
            bool blocked = BlocksGameplay;
            // 开关背包不打断战斗的短暂停顿，仅阻塞状态变化时修改时间。
            if (Application.isPlaying) GameTime.SetPaused(blocked);
            ConfigureInput();
            if (gameSource != null) gameSource.ApplyGameplayGate(blocked, IsInventoryOpen);
            else inventorySource?.ApplyInputGate(blocked);
        }
        private void ConfigureInput()
        {
            Inputs?.Configure(inventorySource != null && !inventorySource.IsDead && mainSource == null,
                IsInventoryOpen, BlocksGameplay, loading,
                mainSource != null || Overlay == GameOverlay.Pause || Overlay == GameOverlay.Death || Overlay == GameOverlay.Victory);
        }

        public void ShowNotice(string message, float seconds = 2.5f)
        {
            noticeText.text = message;
            noticeUntil = Time.unscaledTime + seconds;
        }
        private void CreateNumber()
        {
            Text text = UguiTheme.Label(damageRoot, "Number" + numbers.Count, 0, 0, 100, 40,
                "", 26, UguiTheme.Gold, TextAnchor.MiddleCenter);
            ((RectTransform)text.transform).anchorMin = ((RectTransform)text.transform).anchorMax = Vector2.one * 0.5f;
            ((RectTransform)text.transform).pivot = Vector2.one * 0.5f;
            text.gameObject.SetActive(false);
            numbers.Add(new DamageNumber { Text = text });
        }
        public void ShowDamageNumber(Vector3 point, int damage, bool player)
        {
            DamageNumber number = numbers.Find(n => !n.Active);
            if (number == null) { CreateNumber(); number = numbers[numbers.Count - 1]; }
            number.Point = point;
            number.Born = Time.unscaledTime;
            number.Active = true;
            number.Text.text = damage.ToString();
            number.Text.color = player ? UguiTheme.Red : UguiTheme.Gold;
        }

        private void SceneChanged(Scene previous, Scene next)
        {
            // 新场景组件可能已经 OnEnable，只清除属于旧场景的引用。
            if (mainSource != null && mainSource.gameObject.scene != next) mainSource = null;
            if (inventorySource != null && inventorySource.gameObject.scene != next) { inventorySource = null; IsInventoryOpen = false; }
            InventoryPresenter.Bind(inventorySource, InventoryView);
            if (gameSource != null && gameSource.gameObject.scene != next) gameSource = null;
            if (bossSource != null && bossSource.gameObject.scene != next) { bossSource = null; boss = null; bossAppeared = false; }
            foreach (DamageNumber number in numbers) number.Active = false;
            noticeUntil = 0;
            Overlay = GameOverlay.None;
            GameTime.ClearTransient();
            ApplyGameplayGate();
        }

        private void Update()
        {
            if (!initialized) return;
            if (Overlay == GameOverlay.DeathPending && Time.unscaledTime - overlayAt >= 0.85f)
                SetOverlay(GameOverlay.Death);
            if (loading || !Inputs.ConsumeCancel()) return;
            if (mainSource != null)
            {
                if (ShowingControls) SetControls(false);
            }
            else if (gameSource != null)
            {
                if (Overlay == GameOverlay.None)
                {
                    if (IsInventoryOpen) CloseInventory();
                    else SetOverlay(GameOverlay.Pause);
                }
                else if (Overlay == GameOverlay.Pause) SetOverlay(GameOverlay.None);
            }
        }

        private static void Visible(CanvasGroup page, bool show)
        {
            if (page.gameObject.activeSelf == show) return;
            EventSystem events = Application.isPlaying ? EventSystem.current : null;
            if (!show && events != null && events.currentSelectedGameObject != null &&
                events.currentSelectedGameObject.transform.IsChildOf(page.transform)) events.SetSelectedGameObject(null);
            page.gameObject.SetActive(show);
            if (show && events != null && (page.name == "MainMenu" || page.name == "Controls" || page.name == "GameOverlay"))
                events.SetSelectedGameObject(null);
        }

        private void FocusMenuOnNavigation()
        {
            if (!Inputs.ConsumeNavigation()) return;
            EventSystem events = EventSystem.current;
            if (events == null) return;
            CanvasGroup page = controls.gameObject.activeInHierarchy ? controls :
                menu.gameObject.activeInHierarchy ? menu : overlayPage.gameObject.activeInHierarchy ? overlayPage : null;
            if (page == null) return;
            GameObject selected = events.currentSelectedGameObject;
            if (selected != null && selected.activeInHierarchy && selected.transform.IsChildOf(page.transform) &&
                selected.TryGetComponent(out Button current) && current.IsInteractable()) return;
            foreach (Button button in page.GetComponentsInChildren<Button>())
                if (button.IsActive() && button.IsInteractable()) { events.SetSelectedGameObject(button.gameObject); break; }
        }

        private void RefreshPages()
        {
            if (!initialized) return;
            ConfigureInput();
            Visible(backdropGroup, loading || mainSource != null);
            Visible(menu, mainSource != null && !ShowingControls && !loading);
            Visible(controls, mainSource != null && ShowingControls && !loading);
            Visible(InventoryView.Hud, inventorySource != null && !IsInventoryOpen && !BlocksGameplay);
            Visible(InventoryView.Bag, inventorySource != null && IsInventoryOpen && !BlocksGameplay);
            bool showOverlay = !loading && (Overlay == GameOverlay.Pause || Overlay == GameOverlay.Death || Overlay == GameOverlay.Victory);
            Visible(overlayPage, showOverlay);
            Visible(loadingPage, loading);
            Visible(noticePage, !loading && !string.IsNullOrEmpty(noticeText.text) && Time.unscaledTime < noticeUntil);
            WorldItem focused = WorldItem.Focused;
            Visible(pickupPage, inventorySource != null && !IsInventoryOpen && !BlocksGameplay &&
                !loading && focused != null && focused.Item != null);
            Visible(bossPage, !loading && !BlocksGameplay && !IsInventoryOpen && bossAppeared && boss != null && boss.State != BossState.Dead);
            if (showOverlay)
            {
                bool pause = Overlay == GameOverlay.Pause;
                overlayHeading.text = pause ? "暂  停" : Overlay == GameOverlay.Death ? "旅途终结" : "胜  利";
                overlayDetail.text = pause ? "片刻休整，再次踏入暗影" : Overlay == GameOverlay.Death ? "火光熄灭，但旅途仍可重来" : "龙焰已熄，遗物仍在前方";
                overlayPage.alpha = Mathf.Clamp01((Time.unscaledTime - overlayAt) * 3f);
                pauseContinue.gameObject.SetActive(pause);
                saveButton.gameObject.SetActive(pause);
                retryLabel.text = pause ? "重新开始" : Overlay == GameOverlay.Death ? "再次挑战" : "再来一局";
                ((RectTransform)restartButton.transform).anchoredPosition = new Vector2(86, pause ? -298 : -227);
                ((RectTransform)menuButton.transform).anchoredPosition = new Vector2(86, pause ? -351 : -295);
                EventSystem events = EventSystem.current;
                if (events != null && events.currentSelectedGameObject != null && !events.currentSelectedGameObject.activeInHierarchy)
                    events.SetSelectedGameObject(null);
            }
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            GameTime.Tick();
            // 场景内组件 Awake 顺序不确定，初始化完成后再次同步输入。
            if (gateDirty) { gateDirty = false; ApplyGameplayGate(); }
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            if (screenSize != lastScreen || Screen.safeArea != lastSafe)
            {
                lastScreen = screenSize;
                lastSafe = Screen.safeArea;
                safeArea.anchorMin = new Vector2(lastSafe.xMin / Mathf.Max(1, Screen.width), lastSafe.yMin / Mathf.Max(1, Screen.height));
                safeArea.anchorMax = new Vector2(lastSafe.xMax / Mathf.Max(1, Screen.width), lastSafe.yMax / Mathf.Max(1, Screen.height));
                if (backdrop.texture != null)
                {
                    float imageAspect = (float)backdrop.texture.width / backdrop.texture.height;
                    float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                    float w = Mathf.Min(1, aspect / imageAspect), h = Mathf.Min(1, imageAspect / aspect);
                    backdrop.uvRect = new Rect((1 - w) * 0.5f, (1 - h) * 0.5f, w, h);
                }
            }
            // 提示放在面板下方，避免盖住背包或暂停菜单的按钮。
            noticePanel.anchoredPosition = new Vector2(-210, IsInventoryOpen || BlocksGameplay ? 66 : 180);
            if (boss != null && boss.State != BossState.Dormant)
            {
                if (!bossAppeared) { bossAppeared = true; bossAt = Time.unscaledTime; }
                bossPhase.text = boss.PhaseTwo ? "第二阶段" : "首领";
                bossPhase.color = boss.PhaseTwo ? new Color(1, 0.48f, 0.29f) : UguiTheme.Muted;
                bossBar.SetValueWithoutNotify((float)boss.Health.CurrentHealth / Mathf.Max(1, boss.Health.MaxHealth));
                bossBar.fillRect.GetComponent<Image>().color = boss.PhaseTwo ? new Color(0.92f, 0.34f, 0.17f) : UguiTheme.Red;
                bossPage.alpha = Mathf.Clamp01((Time.unscaledTime - bossAt) * 2.5f);
            }
            loadingDetail.text = LoadingManager.Error ?? "正在穿越暗影…";
            loadingBar.SetValueWithoutNotify(LoadingManager.Progress);
            loadingBar.gameObject.SetActive(LoadingManager.Error == null);
            retryButton.gameObject.SetActive(LoadingManager.Error != null);
            RefreshPages();
            InventoryPresenter.Tick();
            // 等 InputSystem 的本帧导航事件处理完，再建立首次焦点，避免一次方向键跳过首个按钮。
            FocusMenuOnNavigation();
            if (inventorySource != null && !IsInventoryOpen && !BlocksGameplay)
            {
                WorldItem focused = WorldItem.Focused;
                if (focused != null && focused.Item != null)
                {
                    pickupText.text = $"E 拾取  {focused.Item.DisplayName} ×{focused.Count}";
                }
            }
            Camera camera = Camera.main;
            foreach (DamageNumber number in numbers)
            {
                float age = Time.unscaledTime - number.Born;
                if (age > 0.72f) number.Active = false;
                bool show = number.Active && camera != null && !BlocksGameplay && !IsInventoryOpen;
                if (show)
                {
                    Vector3 screen = camera.WorldToScreenPoint(number.Point);
                    Vector2 local = Vector2.zero;
                    show = screen.z >= 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        damageRoot, screen, null, out local);
                    if (show) ((RectTransform)number.Text.transform).anchoredPosition = local + Vector2.up * age * 55f;
                }
                if (number.Text.gameObject.activeSelf != show) number.Text.gameObject.SetActive(show);
            }
        }
    }
}
