# Đề xuất: feature SceneFlow (hai scene Loading và Game)

Ngày: 2026-11-21. Trạng thái: chờ duyệt. Chưa có dòng code nào được sửa.
Bổ sung cho `2026-11-21-session-scene.md` và trả lời câu hỏi 3 ở đó: SceneFlow là bên gọi `EndAsync()`.

## Đã chốt với chủ dự án

- Game có hai scene: Loading và Game.
- Scene Loading nạp scene Game chồng lên (additive). Màn hình chờ còn hiện cho tới khi phiên của Game tới Running.

## Diễn biến

| Bước | Scene Loading | Scene Game |
|---|---|---|
| 1 | Boot cài installer; phiên Loading: Init → Loading → Running | chưa nạp |
| 2 | SceneFlow (`Run`) nạp scene Game chồng lên | Boot cài installer; phiên Game: Init |
| 3 | màn hình chờ vẫn hiện | các feature chạy `LoadAsync` |
| 4 | | phiên Game tới Running; các feature mở view; SceneFlow chạy `Run` sau cùng và báo "sẵn sàng" |
| 5 | SceneFlow nhận tín hiệu, gọi `EndAsync()` của phiên Loading, chờ tới Disposed | |
| 6 | SceneFlow gỡ scene Loading | thành scene chính; người chơi thao tác |

Phiên Game bị Failed: SceneFlow bên Loading nhận tín hiệu lỗi, giữ màn hình chờ và hiện thông báo lỗi. Không gỡ scene Loading.

## Đặt ở đâu

Feature mới `UnityProject/Assets/Features/SceneFlow/`. Không có Contracts: chưa feature nào cần gọi nó. Không sửa `Core`.

- Cài ở cả hai scene, bằng hai asset installer khác vai: bên Loading (nạp Game, chờ, gỡ Loading) và bên Game (báo sẵn sàng).
- Bên Game phải đứng cuối danh sách installer, để `Run` của nó chạy sau khi mọi feature khác đã mở view.
- Tên scene nằm trong asset cấu hình ở `FeatureAssets/Private`.
- Luật chuyển scene nằm trong lớp C# thuần. Việc nạp và gỡ scene đi qua một interface trong Internal để test thay bằng đồ giả.
- Màn hình chờ là view của SceneFlow, chỉ hiển thị.

## Điểm khó nhất: nối hai phiên mà không dùng static

Hai scene có hai registry riêng, nên bên Game không gọi được bên Loading qua registry.

Cách đề nghị: nối bằng chính object của SceneFlow trong scene Game.

- Scene Game chứa một object "cổng" của SceneFlow. Bên Game bật trạng thái của cổng (sẵn sàng hoặc lỗi) khi tới `Run` hoặc khi phiên Failed.
- Bên Loading, sau khi nạp xong scene Game, lấy cổng từ các object gốc của scene vừa nạp và chờ trạng thái đó.
- Cổng nhớ trạng thái, nên bên Loading tới trễ vẫn đọc được. Không phụ thuộc thứ tự.

Phần này nên dựng thử trước, vì chưa kiểm được trong Unity thứ tự giữa lúc nạp scene xong và lúc `Start` của scene mới chạy.

## Ảnh hưởng tới SessionScene

- `EndAsync()` có bên gọi thật, nên giữ trong contract.
- SessionScene phải chịu được `EndAsync()` gọi khi đang ở Running lẫn khi các feature khác còn đang `Run`.
- Bên Game cần biết phiên bị Failed. Cần thêm vào `ISessionParticipant` một hàm báo lỗi, hoặc để `ISessionScene` có sự kiện đổi trạng thái. Đề nghị cách thứ hai; chốt khi dựng SessionScene.

## Những chỗ cần người làm hoặc duyệt

| Việc | Vì sao AI không tự làm được |
|---|---|
| Tạo thư mục `Features/SceneFlow/` | guard chỉ nhận `FEATURE` trỏ tới thư mục đã có |
| Tạo hai scene `Loading.unity`, `Game.unity` trong `_Composition`, mỗi scene một Boot | `_Composition` phải hỏi; scene nên tạo bằng Unity Editor, không viết tay |
| Tách danh sách installer theo scene. Loading: SessionScene, SceneFlow. Game: SessionScene, Popup, Wallet, Shop, SceneFlow | sửa `Boot.prefab` hoặc tạo biến thể; `_Composition` phải hỏi |
| Viết lại `BootTests` (đang giả định một Boot với 3 installer) | `_Composition` phải hỏi |
| Thêm hai scene vào Build Settings, Loading đứng đầu | `ProjectSettings` phải hỏi từng lần |
| Thêm tham chiếu SessionScene Contracts và UniTask vào asmdef của SceneFlow | sửa `*.asmdef` phải hỏi |

## Chưa gồm

- Thanh tiến độ tải: cần SessionScene báo tiến độ, hiện contract chưa có. Bản đầu chỉ hiện màn hình chờ.
- Quay từ Game về Loading, hay scene thứ ba.
- Thử lại sau khi lỗi.

## Thứ tự làm (thay cho thứ tự ở đề xuất trước)

1. Người thêm UniTask, sửa `rules.json`, tạo hai thư mục `_Services/SessionScene` và `SceneFlow`.
2. Dựng SessionScene kèm test (phiên `FEATURE=_Services/SessionScene`).
3. Dựng SceneFlow kèm test bằng phiên giả và bộ nạp scene giả (phiên `FEATURE=SceneFlow`).
4. Chuyển Wallet sang dùng phiên, xoá `WalletRuntime` (phiên `FEATURE=Wallet`).
5. Người tạo hai scene, tách danh sách installer, sửa Build Settings; sửa `BootTests`.
6. Chạy thử trong Unity: Loading → Game, và trường hợp một feature tải lỗi.
