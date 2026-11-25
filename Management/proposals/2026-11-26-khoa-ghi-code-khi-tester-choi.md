# Đề xuất: không ghi code vào UnityProject khi tester đang chơi thử

Người đề xuất: @Manager · 2026-11-26 · Chờ người duyệt

## Vấn đề
@UnityAutoPlay giữ khoá Unity Editor suốt lúc Play. Khoá này chỉ chặn mở, sửa, lưu scene, prefab và Play của phiên khác. Nó không chặn phiên feature ghi `.cs` hay asset bằng Edit/Write.

Ngày 2026-11-26 lúc 00:36, đợt chơi thử PROTOTYPE 1 (`Tester/UnityAutoPlay/20261126-003105`) bị ngắt giữa chừng. Lí do là GameFlow, TeamSelect và Home đang ghi code cho luồng màn. Unity nạp lại code giữa lúc Play nên tester mất hết số đo. Đợt phải chốt ở trạng thái dở: 5 trong 9 mục chưa chơi, 2 mục phải chơi lại.

Luật hiện có ("No .cs writes during Play") chỉ nằm trong ghi nhớ của các phiên, guard không ép.

## Đề xuất (chọn một)
- **A (đề xuất):** guard chặn Edit/Write vào `UnityProject/Assets/**` của mọi phiên khác khi `editor-lock.json` ghi chủ khoá là @UnityAutoPlay. Câu chặn ghi tester giữ tới mấy giờ. Phiên bị chặn thì dừng, chờ rồi làm lại; nếu gấp thì báo @Manager.
- **B:** `dispatch.py` không giao tin việc code cho feature, service, @Core trong lúc tester giữ khoá. Tin vẫn được lưu và tự giao khi khoá được nhả. Phiên đang chạy dở thì không bị chặn.
- **C:** không sửa tool. @Manager chỉ giao đợt chơi thử khi hộp thư không còn tin việc code nào đang chạy.

## Ảnh hưởng
- A chặn chắc nhất, nhưng làm các feature chờ trong lúc tester chơi, thường 5 tới 15 phút.
- B nhẹ hơn, nhưng không chặn được phiên đã nhận việc trước đó.
- Cả A và B đổi `.claude/hooks/guard.py` hoặc `Management/tools/dispatch.py`. Không đổi code game.
