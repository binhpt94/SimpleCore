# SceneFlow

Đưa người chơi từ scene Loading sang scene Game. Không feature nào khác được nạp hay gỡ scene.
Thiết kế: `live/feature-scene-flow/scene-list/boot-loading` (màn Loading), cheat ở `feature-scene-flow/cheat`.
- Cheat (`LoadingCheat.cs`, chỉ chạy ở `UNITY_EDITOR || DEVELOPMENT_BUILD`): chọn trường `cheat` = `LoadError` trên `SceneFlowLoadingInstaller.asset` thì màn Loading đứng ở 40% với dòng "Lỗi tải: Wallet", không nạp scene Game. Asset dùng chung phải để `None` (có test kiểm).
- Sprite thanh tiến độ: order `claudeimage/orders/SceneFlow/loading-bar` (bar-track, bar-fill); chưa giao thì thanh vẫn là ô màu tạm.

- Dùng `ISessionScene` của `Game.Core`: tham gia phiên của cả hai scene, và gọi `EndAsync` của phiên Loading.
- Dùng `ISessionLoadProgress` (contract của SessionScene) của scene Game cho thanh tiến độ và tên feature tải lỗi.
- Chưa có Contracts: chưa feature nào cần gọi SceneFlow. Ai cần thì đề xuất.
- Một lớp installer, hai asset trong `FeatureAssets/Public`: `SceneFlowLoadingInstaller` và `SceneFlowGameInstaller`.
- Một lần chuyển (`SceneSwitcher`, theo `scene-request/switch-rules`): kiểm tên (lạ: lỗi; đang chuyển: cảnh báo, không xếp hàng; trùng scene đang chạy: bỏ qua) → hiện lớp che → kết thúc phiên scene cũ → nạp scene mới bằng `LoadSceneMode.Single` (Unity gỡ scene cũ) → chờ cổng của scene mới và thời gian ít nhất → bỏ lớp che. Lớp che chính là `LoadingScreen.prefab`, `DontDestroyOnLoad`, nên sống qua lúc gỡ scene Loading. Chuyển quá `slowSwitchWarningSeconds` (5) thì ghi cảnh báo hiệu năng, vẫn chuyển tiếp. Lỗi thì lớp che ở lại với dòng lỗi, không quay về.
- Bên Loading (`LoadingToGameFlow`): tham gia duy nhất của scene Loading; ở `Run` xin chuyển sang Game. Không đóng lớp che khi phiên Loading kết thúc.
- Prefab màn Loading phải có `GraphicRaycaster` ở gốc và `Background` phủ kín, đục, `raycastTarget` bật: đó là thứ chặn chạm (có test).
- Bên Game (`GameReadySignal`): đặt Game làm scene chính ngay ở `LoadAsync` (để view các feature tạo trong `Run` không rơi vào scene Loading), mở cổng khi tới `Run`, báo lỗi khi phiên Failed. Phải cài SAU CÙNG trong Boot của scene Game; installer bên Game đòi `ISessionLoadProgress` đã đăng ký.
- Hai scene có registry riêng. `SceneGate` (mang cả `ISessionLoadProgress` của Game) đi qua object `SceneGateCarrier`; bên Loading tìm nó trong các scene đã nạp.
- Game lỗi: scene Loading ở lại, thanh đứng yên, tên game vẫn hiện, dòng lỗi "Lỗi tải: <tên>" khi biết feature lỗi, không thì thông báo chung. Giới hạn 10 giây mỗi feature là của SessionScene, không phải ở đây.
- `SceneFlowConfig.asset` (`FeatureAssets/Private`, lúc chạy chỉ đọc): tên scene (cũng là danh sách scene được xin chuyển), thông báo chung, mẫu `featureFailedMessage` ("{0}" là tên feature), `minLoadingScreenSeconds`, `slowSwitchWarningSeconds`.
- Việc đụng Unity (nạp scene, tạo prefab, chờ thời gian thật) nằm sau các interface trong `UnityPorts.cs`.
- `LoadingScreen.prefab`: Title (tên game), ProgressBar/Fill (Image Filled, cần sprite để fill có tác dụng), Error; bố cục theo frame `boot-loading/main.frame`. Hình nền và logo để giai đoạn FULL.
- Phải được cài sau SessionScene.

Test: assembly `Game.SceneFlow.Tests` (EditMode), dùng phiên, tiến độ, đồng hồ, bộ nạp scene và màn hình chờ giả. Assembly này không tham chiếu `UnityEngine.UI`: test prefab đọc giá trị qua `SerializedObject`.
