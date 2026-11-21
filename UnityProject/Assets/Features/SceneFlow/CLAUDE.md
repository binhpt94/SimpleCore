# SceneFlow

Đưa người chơi từ scene Loading sang scene Game. Không feature nào khác được nạp hay gỡ scene.

- Dùng `ISessionScene` (SessionScene): tham gia phiên của cả hai scene, và gọi `EndAsync` của phiên Loading.
- Chưa có Contracts: chưa feature nào cần gọi SceneFlow. Ai cần thì đề xuất.
- Một lớp installer, hai asset trong `FeatureAssets/Public`: `SceneFlowLoadingInstaller` và `SceneFlowGameInstaller`.
- Bên Loading (`LoadingToGameFlow`): hiện màn hình chờ, nạp Game chồng lên, chờ cổng, kết thúc phiên Loading, gỡ scene Loading.
- Bên Game (`GameReadySignal`): đặt Game làm scene chính ngay ở `LoadAsync` (để view các feature tạo trong `Run` không rơi vào scene Loading), mở cổng khi tới `Run`, báo lỗi khi phiên Failed. Phải cài SAU CÙNG trong Boot của scene Game.
- Hai scene có registry riêng. `SceneGate` đi qua object `SceneGateCarrier`; bên Loading tìm nó trong các scene đã nạp.
- Game lỗi thì scene Loading ở lại và màn hình chờ hiện thông báo trong `SceneFlowConfig.asset`.
- Tên scene và thông báo: `SceneFlowConfig.asset` trong `FeatureAssets/Private`, lúc chạy chỉ đọc.
- Việc đụng Unity (nạp scene, tạo prefab) nằm sau các interface trong `UnityPorts.cs`.
- `LoadingScreen.prefab` mới có vỏ: hình ảnh và `errorLabel` cần người dựng trong Unity.
- Phải được cài sau SessionScene.

Test: assembly `Game.SceneFlow.Tests` (EditMode), dùng phiên, bộ nạp scene và màn hình chờ giả.
