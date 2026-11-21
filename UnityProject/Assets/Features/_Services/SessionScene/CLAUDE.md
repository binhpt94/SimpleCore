# SessionScene (dịch vụ)

Giữ vòng đời một phiên của scene: Init → Loading → Running → Disposing → Disposed, hoặc Failed.
Không biết feature nào; feature tự xin tham gia bằng `ISessionScene.Join` trong `Install`.

- Contract: `ISessionScene` (`State`, `StateChanged`, `Join`, `EndAsync`), `ISessionParticipant`, `SessionState`. Dùng UniTask.
- Loading: mọi participant tải song song. Một cái lỗi thì huỷ các cái còn lại và phiên thành Failed.
- Running: gọi `Run` theo thứ tự tham gia. View của feature chỉ được tạo từ đây.
- Disposing: gọi `DisposeAsync` theo thứ tự ngược, từng cái một. Failed cũng dọn như vậy rồi dừng ở Failed.
- Logic nằm trong `SessionSceneLogic` (C# thuần). `SessionSceneDriver` (View) chỉ chuyển `Start`/`OnDestroy` thành tín hiệu.
- Installer tạo driver từ `SessionSceneDriver.prefab`; chỉ tạo khi đang Play.
- Phải được cài trước mọi feature gọi `Join`.
- Thoát game đột ngột: Unity không chờ, chỉ phần đồng bộ của `DisposeAsync` chắc chắn chạy.

Test: assembly `Game.Services.SessionScene.Tests` (EditMode), dùng participant giả.
