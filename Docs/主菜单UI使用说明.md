# 情侣小熊主菜单

主菜单及每日奖励、图鉴收藏、商店、设置使用本次提供的五张参考图。原图保存在 `Assets/BuBuYiEr/UI/Art/`，通过 Unity Sprite Editor 数据接口切分为命名精灵；主菜单分成顶部、标题、双熊房间、开始按钮、导航、草地六个独立图层。子页面保留原图装饰底图，领取状态、数值、滑条和交互控件放在独立层。裁切素材和原生网格直接采样原图。

## 打开与编辑

1. 打开 `Assets/Diamond_Loop/Scenes/Home.unity`，运行游戏。
2. Home 场景中的 ViewManager 指向新预制体 `Assets/BuBuYiEr/UI/Prefabs/BearHomeView.prefab`。
3. 打开新预制体，在 `BearMenuUI` 下编辑各页面；原模板的预制体仍位于 `Assets/Diamond_Loop/Prefabs/Views/HomeView.prefab`。
4. 更换原图、修改切片坐标或布局代码后，执行 **Tools → BuBuYiEr → 应用参考图主菜单** 重新生成。

生成器会重建新预制体的界面层级。需要反复生成的布局调整应写进 `BearMenuFactory.cs`，切图坐标在 `Editor/BearMenuBuilder.cs`；直接手工修改生成层级后，不要再次生成，以免覆盖这些手工调整。

## 已接入的行为

- **开始游戏**：进入原有 Level 选关场景，继续使用现有玩法。
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

主菜单设计尺寸为 1024×1536，其他页面为 941×1672。布局保留原图比例，并适配安全区域；较长手机屏幕会增加主菜单标题上下的背景留白，双熊、桌面和开始按钮保持衔接，不拉长小熊和按钮。横屏使用居中的完整竖屏界面。

动态中文采用 [Google Fonts 收录的站酷快乐体](https://github.com/google/fonts/tree/main/ofl/zcoolkuaile)，数字采用 [Baloo 2](https://github.com/google/fonts/tree/main/ofl/baloo2) 的 700 字重静态实例（实例字体名为 BuBu Menu Round）。两份字体的 OFL 许可文件随资源保存在 `Assets/BuBuYiEr/UI/Fonts/`。原图内的标题、角色和装饰保留原始像素。

界面入口代码为 `BearHomeMenuController.cs`，原生控件和布局由 `BearMenuFactory.cs` 生成。原有调试面板和自动打关仍独立使用。

## 预览与验证

- [主菜单（参考图比例）](Previews/home-reference-size.png) / [主菜单（9:16 长屏）](Previews/home-portrait.png)
- [每日奖励](Previews/daily.png) / [图鉴收藏](Previews/collection.png) / [商店](Previews/shop.png) / [设置](Previews/settings.png)

2026-11-02 使用 Unity 2022.3.62f2 的独立工程副本验证，使用独立测试存档。通过 45 项检查，覆盖五页面生成、真实数值更新、实际按钮射线命中、首次及第七天奖励、七天循环重置、重复领取拦截、音量与开关保存、故事解锁、未配置商品处理、进入选关及返回主菜单。生成九张渲染图并核对布局，以上保留六张主要预览。运行时代码、Editor 代码及移除 Editor 定义后的运行时代码均编译通过；手机真机触摸和商店支付仍需在目标平台联调。
