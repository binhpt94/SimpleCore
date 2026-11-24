# SceneFlow

Đưa người chơi từ scene Loading sang scene Game. Không feature nào khác được nạp hay gỡ scene.
Thiết kế: `live/feature-scene-flow/scene-list/boot-loading` (màn Loading), cheat ở `feature-scene-flow/cheat` (chưa làm).

- Dùng `ISessionScene` của `Game.Core`: tham gia phiên của cả hai scene, và gọi `EndAsync` của phiên Loading.
- Dùng `ISessionLoadProgress` (contract của SessionScene) của scene Game cho thanh tiến độ và tên feature tải lỗi.
- Chưa có Contracts: chưa feature nào cần gọi SceneFlow. Ai cần thì đề xuất.
- Một lớp installer, hai asset trong `FeatureAssets/Public`: `SceneFlowLoadingInstaller` và `SceneFlowGameInstaller`.
- Bên Loading (`LoadingToGameFlow`): hiện màn hình chờ (thanh ở 0%), nạp Game chồng lên, cho thanh chạy theo `LoadedCount / ParticipantCount` của scene Game, chờ cổng và chờ màn đã hiện đủ thời gian ít nhất (tính từ lúc hiện), rồi kết thúc phiên Loading và gỡ scene Loading.
- Bên Game (`GameReadySignal`): đặt Game làm scene chính ngay ở `LoadAsync` (để view các feature tạo trong `Run` không rơi vào scene Loading), mở cổng khi tới `Run`, báo lỗi khi phiên Failed. Phải cài SAU CÙNG trong Boot của scene Game; installer bên Game đòi `ISessionLoadProgress` đã đăng ký.
- Hai scene có registry riêng. `SceneGate` (mang cả `ISessionLoadProgress` của Game) đi qua object `SceneGateCarrier`; bên Loading tìm nó trong các scene đã nạp.
- Game lỗi: scene Loading ở lại, thanh đứng yên, tên game vẫn hiện, dòng lỗi "Lỗi tải: <tên>" khi biết feature lỗi, không thì thông báo chung. Giới hạn 10 giây mỗi feature là của SessionScene, không phải ở đây.
- `SceneFlowConfig.asset` (`FeatureAssets/Private`, lúc chạy chỉ đọc): tên scene, thông báo chung, mẫu `featureFailedMessage` ("{0}" là tên feature), `minLoadingScreenSeconds`.
- Việc đụng Unity (nạp scene, tạo prefab, chờ thời gian thật) nằm sau các interface trong `UnityPorts.cs`.
- `LoadingScreen.prefab`: Title (tên game), ProgressBar/Fill (Image Filled, cần sprite để fill có tác dụng), Error; bố cục theo frame `boot-loading/main.frame`. Hình nền và logo để giai đoạn FULL.
- Phải được cài sau SessionScene.

Test: assembly `Game.SceneFlow.Tests` (EditMode), dùng phiên, tiến độ, đồng hồ, bộ nạp scene và màn hình chờ giả. Assembly này không tham chiếu `UnityEngine.UI`: test prefab đọc giá trị qua `SerializedObject`.
