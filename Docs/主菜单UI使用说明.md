# 情侣小熊主菜单

主菜单使用 `Assets/BuBuYiEr/UI/Art/SceneArt.png` 中已切好的命名精灵。标题、双熊、窗户、窗帘、盆栽、桌面、糖果、按钮、图标和草地均为独立 Image，可直接修改 RectTransform；顶部数值和导航文字为独立 Text。面板与桌面使用九宫格缩放，CanvasScaler 使用 Scale With Screen Size（1024×1536、按宽度匹配）。设置页使用 `SettingArt.png`，每日奖励使用 `DailyArt.png`；共用元素直接引用 SettingArt 和 SceneArt。

## 打开与编辑

1. 打开 `Assets/Diamond_Loop/Scenes/Home.unity`，不需要 Play，Scene 和 Game 窗口即可显示主菜单。Scene 窗口会切换到 2D 并定位 UI。
2. 在 Hierarchy 选中 `HomeMenuCanvas`，展开 `BearMenuUI` 编辑各页面。Inspector 的“编辑模式页面预览”按钮可切换主菜单、每日奖励、图鉴、商店和设置；预览不读取或修改玩家存档。
3. 场景中的 `HomeMenuCanvas` 是 `Assets/BuBuYiEr/UI/Prefabs/BearHomeView.prefab` 的实例。运行时 ViewManager 直接复用该实例；返回主页会移除新加载的重复 UI 和 EventSystem。
4. 需要给 Home 场景补回 UI、绑定引用或清理旧三维展示物体时，执行 **Tools → BuBuYiEr → 将主菜单生成到 Home 场景**。这项操作保留现有 UI 布局。
5. 更换原图、修改切片坐标或布局代码后，执行 **Tools → BuBuYiEr → 应用参考图主菜单** 重新生成预制体并同步 Home 场景。
6. 修改 SceneArt 切图后，执行 **Tools → BuBuYiEr → 使用 SceneArt 重建主菜单**，只重建主页并更新场景实例。该操作会覆盖主页的手工布局调整，其余四个页面保留。
7. 修改 SettingArt 切图后，执行 **Tools → BuBuYiEr → 使用 SettingArt 重建设置面板**，只重建设置页。切片名称、矩形和标识均保留；面板和滑条补充九宫格边框。
8. 修改 DailyArt 切图后，执行 **Tools → BuBuYiEr → 使用 DailyArt 重建每日奖励**，只重建每日奖励页。保留现有切片名称、矩形及标识。
9. 修改 ShopArt 后，执行 **Tools → BuBuYiEr → 使用 ShopArt 重建商店**，只重建商店页。图集尚无切片时创建命名切片；已有切片时保留原切片。

设置页的背景、标题底板、条目底板、图标、音量滑条、客服按钮和蝴蝶结均可独立编辑。滑条绑定原有音效/音乐设置，震动和通知按钮继续使用原有保存逻辑。SettingArt 独立保存在 `BearMenuArt.settingsSprites` 中，以 `Setting/切片名` 引用，避免其 `TitleBg` 与 SceneArt 的同名精灵冲突。返回按钮、金币、加号、双熊、窗帘和花草复用 SceneArt。

每日奖励在 `BearMenuUI/DailyReward` 下按 Day1—Day7 分组，卡片背景、日期、奖励图标、数量和领取控件均可独立编辑。DailyArt 保存在 `BearMenuArt.dailySprites`，以 `Daily/切片名` 引用，避免同名金币和爱心覆盖其他图集。面板、日期底板、领取按钮和蝴蝶结复用 SettingArt；房间、双熊、顶部栏和花草复用 SceneArt。运行时显示实际领取状态与冷却；编辑模式预览显示第 1 天已领取、第 2 天可领取，不读写玩家存档。此次仅修改工程文件，未运行测试或使用 computer-use。

商店在 `BearMenuUI/Shop` 下按分类标签、NewPlayerPack 和 Product1—Product4 分组。ShopArt 独立保存到 `BearMenuArt.shopSprites`，以 `Shop/切片名` 引用；面板、蝴蝶结和箭头复用 SettingArt，奖励图标复用 DailyArt，房间、顶部栏和草地复用 SceneArt。商品图、名称、数量、价格、购买按钮均可独立编辑。礼包分类显示全部五张商品卡片，其余分类只显示对应商品。未填写商品 ID 时使用参考价格并提示“暂未上架”；填写商品 ID 后显示真实本地化价格，等待商店连接时显示省略号，购买继续调用原 IAPManager。此次商店修改直接保存到预制体和 Home 场景，未运行测试或使用 computer-use。

生成器会重建新预制体的界面层级。需要反复生成的布局调整应写进 `BearMenuFactory.cs`，切图坐标在 `Editor/BearMenuBuilder.cs`；直接手工修改生成层级后，不要再次生成，以免覆盖这些手工调整。

Home 场景已移除旧 Environment、传送带、立柱、边框、Background 模型及 Directional Light；HomeManager 只保留存档初始化和主页启动，旧传送带滚动逻辑已删除。小熊菜单预制体内停用的旧模板 UI 及其组件也已移除。保留 Main Camera 的二维背景与 AudioListener，以及其他页面和玩法仍然依赖的 ViewManager、ServicesManager、音频、金币、广告等服务；关卡场景和共享资源不受此次清理影响。

## 已接入的行为

- **开始游戏**：进入 Level 选关场景，游玩新的糖果打砖块。
- **顶部金币与关卡**：显示实际存档；参考图上的 3726 和 Lv.2 不会写进玩家存档。
- **每日奖励**：沿用模板的领取序号及 24 小时冷却，禁止重复领取。奖励按照参考图为金币100、爱心3、四叶草1、小花5、装饰券1、金币300、糖果罐1。
- **图鉴故事**：可选章节、查看故事，按当前关卡解锁（1、5、10、15、20、25关）。其他表情和动画页目前提供主题空状态，等待加入对应内容素材。
- **设置**：音效与音乐滑条接入 SoundManager 并保存；震动偏好保存到本地。通知关闭后会取消已安排的通知，并停止后续安排。
- **商店**：包含五个分类和商品点击入口，保留参考图商品外观。
- **返回与关闭**：回到新主菜单，信息弹窗可关闭。

新增主题物品暂以 `BUBU_UI_EXTRA_` 前缀保存在 PlayerPrefs 中，属于主题收藏库存，尚未接入关内消耗或生命系统。金币继续使用原有 CoinManager。每日奖励序号与冷却继续使用原有 PlayerDataHandler / DailyRewardManager。

## 上架前需要填写的内容

打开 `Assets/BuBuYiEr/UI/BearMenuArt.asset`：

- `productIds`：按新人礼包、金币、爱心、装饰券、图鉴券的顺序填入真实 IAP 商品 ID，并在现有 IAPManager 中配置相同商品及奖励。配置后价格使用商店返回的本地化价格。
- 商品 ID 留空时，点击显示“暂未上架”，不发起购买、不扣款，也不会模拟发货。参考图的人民币价格只作为初始美术展示。
- `privacyUrl` 和 `supportUrl`：填入真实隐私政策、客服地址；留空时显示尚未开放。
- 多语言、表情资源、动画资源及主题物品在玩法中的用途需要后续配置或开发。

## 尺寸与资源

主菜单设计尺寸为 1024×1536，其他页面为 941×1672。主菜单使用顶部、居中和底部锚点分配各区域间距，并适配安全区域；角色、图标和按钮保持各自比例，桌面和面板使用九宫格伸缩。横屏使用居中的完整竖屏界面。

动态中文采用 [Google Fonts 收录的站酷快乐体](https://github.com/google/fonts/tree/main/ofl/zcoolkuaile)，数字采用 [Baloo 2](https://github.com/google/fonts/tree/main/ofl/baloo2) 的 700 字重静态实例（实例字体名为 BuBu Menu Round）。两份字体的 OFL 许可文件随资源保存在 `Assets/BuBuYiEr/UI/Fonts/`。原图内的标题、角色和装饰保留原始像素。

当前关内玩法已替换为糖果打砖块，详见 [糖果打砖块玩法说明](糖果打砖块玩法说明.md)。

界面入口代码为 `BearHomeMenuController.cs`，原生控件和布局由 `BearMenuFactory.cs` 生成。原有调试面板和自动打关仍独立使用。

## 预览与验证

以下图片来自先前的参考图版本；当前 SceneArt 版主菜单以 Home 场景和 BearHomeView 预制体为准。此次 SceneArt 重建按要求仅修改工程文件，没有运行测试。

- [主菜单（参考图比例）](Previews/home-reference-size.png) / [主菜单（9:16 长屏）](Previews/home-portrait.png)
- [每日奖励](Previews/daily.png) / [图鉴收藏](Previews/collection.png) / [商店](Previews/shop.png) / [设置](Previews/settings.png)

2026-11-02 使用 Unity 2022.3.62f2 的独立工程副本验证，使用独立测试存档。通过 45 项检查，覆盖五页面生成、真实数值更新、实际按钮射线命中、首次及第七天奖励、七天循环重置、重复领取拦截、音量与开关保存、故事解锁、未配置商品处理、进入选关及返回主菜单。生成九张渲染图并核对布局，以上保留六张主要预览。运行时代码、Editor 代码及移除 Editor 定义后的运行时代码均编译通过；手机真机触摸和商店支付仍需在目标平台联调。
