# SessionScene (dịch vụ)

Giữ vòng đời một phiên của scene: Init → Loading → Running → Disposing → Disposed, hoặc Failed.
Không biết feature nào; feature tự xin tham gia bằng `ISessionScene.Join` trong `Install`.

- Vòng đời (`ISessionScene`, `ISessionParticipant`, `SessionState`) nằm ở `Game.Core`. Contract riêng: `ISessionLoadProgress` (`ParticipantCount`, `LoadedCount`, `FailedParticipant`, `LoadProgressChanged`) cho thanh tiến độ. Dùng UniTask.
- Thiết kế: `live/feature-session-scene/session-steps/participant-order`.
- Loading: tải lần lượt theo thứ tự `Join`; mỗi participant tối đa `loadTimeLimitSeconds` (installer, mặc định 10 giây), đếm qua `ILoadTimer`. Lỗi hoặc quá giờ: không tải cái sau, ghi `FailedParticipant` (tên feature lấy từ namespace), huỷ ngược các cái đã tới (kể cả cái lỗi), phiên thành Failed.
- Yêu cầu kết thúc lúc đang tải: huỷ token, chờ cái đang tải xong (tối đa tới giới hạn), rồi huỷ ngược các cái đã tới. Join trùng (cùng object) thì lỗi ngay.
- Một feature có hai participant (cùng kiểu, ví dụ installer có hai lần trong Boot): lúc Begin, trước bước tải, phiên thành Failed (Init → Failed), ghi `FailedParticipant`, không tải ai. Danh sách rỗng: vẫn qua ba bước, ghi cảnh báo (`reportWarning`).
- Running: gọi `Run` theo thứ tự tham gia. View của feature chỉ được tạo từ đây.
- Disposing: gọi `DisposeAsync` theo thứ tự ngược, từng cái một. Failed cũng dọn như vậy rồi dừng ở Failed.
- Kiểm dọn sạch: sau bước huỷ, `ParticipantUsageCounter` đọc `ISessionParticipantUsage` (Core, `SessionParticipantBase` hiện thực; participant không hiện thực thì coi là 0) của từng participant đã tới; còn view hay việc thì ghi lỗi nêu tên feature và số. Chỉ bật ở Editor và bản dev (installer). Lớp che chuyển scene không được `TrackView` nên không bị đếm.
- Logic nằm trong `SessionSceneLogic` (C# thuần). `SessionSceneDriver` (View) chỉ chuyển `Start`/`OnDestroy` thành tín hiệu.
- Installer tạo driver từ `SessionSceneDriver.prefab`; chỉ tạo khi đang Play.
- Phải được cài trước mọi feature gọi `Join`.
- Thoát game đột ngột: Unity không chờ, chỉ phần đồng bộ của `DisposeAsync` chắc chắn chạy.

Test: assembly `Game.Services.SessionScene.Tests` (EditMode), dùng participant giả.
