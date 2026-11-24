# SessionScene (dịch vụ)

Giữ vòng đời một phiên của scene: Init → Loading → Running → Disposing → Disposed, hoặc Failed.
Không biết feature nào; feature tự xin tham gia bằng `ISessionScene.Join` trong `Install`.

- Vòng đời (`ISessionScene`, `ISessionParticipant`, `SessionState`) nằm ở `Game.Core`. Contract riêng: `ISessionLoadProgress` (`ParticipantCount`, `LoadedCount`, `FailedParticipant`, `LoadProgressChanged`) cho thanh tiến độ. Dùng UniTask.
- Thiết kế: `live/feature-session-scene/session-steps/participant-order`.
- Loading: tải lần lượt theo thứ tự `Join`; mỗi participant tối đa `loadTimeLimitSeconds` (installer, mặc định 10 giây), đếm qua `ILoadTimer`. Lỗi hoặc quá giờ: không tải cái sau, ghi `FailedParticipant` (tên feature lấy từ namespace), huỷ ngược các cái đã tới (kể cả cái lỗi), phiên thành Failed.
- Yêu cầu kết thúc lúc đang tải: huỷ token, chờ cái đang tải xong (tối đa tới giới hạn), rồi huỷ ngược các cái đã tới. Join trùng thì lỗi ngay.
- Running: gọi `Run` theo thứ tự tham gia. View của feature chỉ được tạo từ đây.
- Disposing: gọi `DisposeAsync` theo thứ tự ngược, từng cái một. Failed cũng dọn như vậy rồi dừng ở Failed.
- Logic nằm trong `SessionSceneLogic` (C# thuần). `SessionSceneDriver` (View) chỉ chuyển `Start`/`OnDestroy` thành tín hiệu.
- Installer tạo driver từ `SessionSceneDriver.prefab`; chỉ tạo khi đang Play.
- Phải được cài trước mọi feature gọi `Join`.
- Thoát game đột ngột: Unity không chờ, chỉ phần đồng bộ của `DisposeAsync` chắc chắn chạy.

Test: assembly `Game.Services.SessionScene.Tests` (EditMode), dùng participant giả.
