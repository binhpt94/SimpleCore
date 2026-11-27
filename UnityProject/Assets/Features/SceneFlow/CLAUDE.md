# SceneFlow
Tầng: 6 · Features

Đưa người chơi từ scene Loading sang scene Game, và chuyển, tải lại scene cho panel cheat. Không feature nào khác được nạp hay gỡ scene.
Thiết kế: `live/feature-scene-flow/scene-list/boot-loading` (màn Loading), `scene-request/switch-rules` (một lần chuyển), cheat ở `feature-scene-flow/cheat`; `ISceneSwitch` theo `feature-cheat-panel/cheat-states/states-rules`.

## Contracts

### IGameSceneReady
- Dùng để: feature của scene Game biết lúc scene sẵn sàng cho người chơi (mọi tham gia đã chạy, lớp che đang được bỏ).
- `IsReady`: true từ lúc `Ready` đã phát trong lần nạp này của scene Game.
- `Ready`: phát đúng một lần mỗi lần nạp scene Game, ở bước chạy, khi cổng mở; phiên Failed thì không phát.
- Do `GameReadySignal` hiện thực, chỉ đăng ký bên Game. SceneFlow cài sau cùng nên feature khác lấy nó trễ (ở `Run`), không lấy trong `Install`.
- Feature dùng: các feature của scene Game cần biết lúc màn đầu đã sẵn sàng (định tuyến màn).

### ISceneSwitch
- Dùng để: panel cheat nạp scene trực tiếp. Đường chơi thường không dùng nó, vẫn xin chuyển như cũ (trùng scene đang chạy thì bỏ qua).
- Đăng ký ở cả scene Loading và Game, mọi loại build (không sau cờ dev).
- `IsSwitching`: true từ lúc một lần chuyển bắt đầu (lớp che hiện) tới khi scene mới sẵn sàng (lớp che bỏ). Lần chuyển lỗi thì giữ true. Chung cho cả game: lần chuyển bắt đầu ở scene cũ vẫn thấy true ở scene mới tới khi xong.
- `SwitchingChanged`: phát mỗi lần `IsSwitching` đổi (true lúc bắt đầu, false lúc xong). Người nghe của một scene bị bỏ khi phiên scene đó bắt đầu huỷ.
- `Reload(scene)`: nạp scene đó từ đầu qua màn Loading như một lần chuyển thường, kể cả khi trùng scene đang chạy. Tên lạ: từ chối, ghi lỗi nêu tên. Đang chuyển: không làm gì.
- `Restart()`: chạy lại game từ scene đầu tiên (Loading) như lúc mới mở: nạp Loading, rồi Loading tự đi tiếp sang Game. Đang chuyển: không làm gì.
- Do `SceneSwitchService` hiện thực, bọc `SceneSwitcher` dùng chung.
- Feature dùng: CheatPanel.

## Cách chạy
- Dùng `ISessionScene` của `Game.Core`: tham gia phiên của cả hai scene, và gọi `EndAsync` của phiên scene cũ.
- Dùng `ISessionLoadProgress` (contract của SessionScene) của scene mới cho thanh tiến độ và tên feature tải lỗi; installer cả hai bên đòi nó đã đăng ký.
- Một lớp installer, hai asset trong `FeatureAssets/Public`: `SceneFlowLoadingInstaller` và `SceneFlowGameInstaller`. Cả hai cần `config`, `loadingScreenPrefab`, `gateCarrierPrefab`, `switchHostPrefab`.
- `SceneSwitcher` sống suốt game, một cái cho mọi scene: scene đầu tiên boot tạo nó và gắn lên `SceneSwitchHost.prefab` (`DontDestroyOnLoad`); scene sau tìm lại. Ngoài Play (test EditMode) mỗi lần Install có switcher riêng, không tạo object.
- Một lần chuyển (theo `scene-request/switch-rules`): kiểm tên (lạ: lỗi; đang chuyển: cảnh báo, không xếp hàng; trùng scene đang chạy: bỏ qua, trừ `Reload`) → hiện lớp che → kết thúc phiên scene cũ → nạp scene mới bằng `LoadSceneMode.Single` → chờ cổng của scene mới và thời gian ít nhất → bỏ lớp che. Lớp che chính là `LoadingScreen.prefab`, `DontDestroyOnLoad`. Chuyển quá `slowSwitchWarningSeconds` (5) thì ghi cảnh báo hiệu năng, vẫn chuyển tiếp. Lỗi thì lớp che ở lại với dòng lỗi, không quay về.
- Mỗi scene có một cổng (`SceneGate`, mang `ISessionLoadProgress` của scene đó) đi qua object `SceneGateCarrier`; lần chuyển tìm nó trong các scene đã nạp. Hai scene có registry riêng.
- Bên Loading (`LoadingToGameFlow`): tham gia duy nhất của scene Loading; ở `Run` mở cổng Loading rồi xin chuyển sang Game. Tới Loading bằng một lần chuyển (`Restart`) thì chờ lần đó xong rồi mới xin sang Game. Không đóng lớp che khi phiên Loading kết thúc.
- Bên Game (`GameReadySignal`): đặt Game làm scene chính ngay ở `LoadAsync` (để view tạo trong `Run` không rơi vào scene cũ), mở cổng khi tới `Run`, báo lỗi khi phiên Failed. Phải cài SAU CÙNG trong Boot của scene Game.
- Game lỗi: lớp che ở lại, thanh đứng yên, tên game vẫn hiện, dòng lỗi "Lỗi tải: <tên>" khi biết feature lỗi, không thì thông báo chung. Giới hạn 10 giây mỗi feature là của SessionScene.
- Prefab màn Loading phải có `GraphicRaycaster` ở gốc và `Background` phủ kín, đục, `raycastTarget` bật: đó là thứ chặn chạm (có test).
- `SceneFlowConfig.asset` (`FeatureAssets/Private`, lúc chạy chỉ đọc): tên scene (cũng là danh sách scene được xin chuyển), thông báo chung, mẫu `featureFailedMessage` ("{0}" là tên feature), `minLoadingScreenSeconds`, `slowSwitchWarningSeconds`.
- Việc đụng Unity (nạp scene, tạo prefab, tìm host, chờ thời gian thật) nằm sau các interface trong `UnityPorts.cs`.
- `LoadingScreen.prefab`: Title (tên game), ProgressBar/Fill (Image Filled, cần sprite), Error; bố cục theo frame `boot-loading/main.frame`. Hình nền và logo để giai đoạn FULL.
- Sprite thanh tiến độ: order `claudeimage/orders/SceneFlow/loading-bar` (bar-track, bar-fill). Bản PROTOTYPE (style Colorful): order `codex/orders/SceneFlow/loading-bar-colorful`, đã chấm Đạt, chép ở `FeatureAssets/Private/Generated/loading-bar-colorful`; chưa gắn vào prefab.
- Phải được cài sau SessionScene.

## Cheat (chỉ `UNITY_EDITOR || DEVELOPMENT_BUILD`)
- Installer hiện thực `ICheatStateSource` của Core: `CheatFeatureTitle` = "Chuyển scene"; asset Loading khai state `SceneFlow/LoadError` ("Màn Loading dừng vì một feature tải lỗi"), asset Game không khai state nào.
- State `LoadError` (`LoadingCheat.cs`): màn Loading đứng ở 40% với dòng "Lỗi tải: Wallet", không nạp scene Game; dựng xong thì mở cổng Loading (lần chuyển vào Loading kết thúc).
- Chọn state: lúc Install lấy `ICheatSelection` từ registry (không có thì như chưa chọn). `HasPick` thì bỏ trường `cheat` trên asset, chỉ dựng nếu `PickedKey` là `SceneFlow/LoadError`; chưa chọn thì theo trường `cheat`. Mỗi lần dựng state gọi `ReportRunning(key)`.
- Asset dùng chung phải để `cheat` = `None` (có test kiểm).

Test: assembly `Game.SceneFlow.Tests` (EditMode), dùng phiên, tiến độ, đồng hồ, bộ nạp scene và màn hình chờ giả; `CheatSelection` thật của Core. Assembly này không tham chiếu `UnityEngine.UI`: test prefab đọc giá trị qua `SerializedObject`.
