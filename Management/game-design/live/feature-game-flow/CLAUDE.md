# Điều hướng màn

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Đi qua các màn mượt và nhất quán: chơi, chọn đội, vào trận, thua rồi chơi lại hay về màn bắt đầu luôn đi đúng một đường.

## Liên quan
- `feature-run`: lượt chơi trong trận; báo đã dọn trận xong để điều hướng màn đổi màn.
- `feature-home`, `feature-team-select`: hai màn trước lượt; gửi lựa chọn của người chơi.
- `feature-popup-lose`, `feature-popup-pause`: lựa chọn rời trận.
- `feature-scene-flow`: bàn giao khi scene Game sẵn sàng.

## Đã tự quyết
- **Tạo feature theo đề xuất của GPT (2026-11-24, `advice/_general/r2-all-2.md`, góp ý `tach-dieu-huong-man`; cùng hướng với `tach-luong-man-game` ở lần hỏi r2-3 của `feature-run`).** Khai `node-by`.
- **Điểm AI 6 cho ý chính.** `feature-run` đang ôm cả luồng màn lẫn trận; tách ra giúp ranh giới rõ (màn trước lượt không thuộc lượt, khớp nghĩa từ "Lượt"). Đổi lại: nhánh luồng màn đã chốt ở `feature-run` phải chuyển dần sang đây ở vòng 3-4. Các ý cũ của `feature-run` (`run-routes`, `run-switches-screens`) vẫn còn và đang "trái nhau" với ý mới; điểm sẽ chốt một bên.
- `code.json`: `GameFlow`, theo luật đặt tên vòng 2.

- **Đưa góp ý GPT lần hỏi `r2-1` (2026-11-24, 13 góp ý).** Mọi quyền chọn đích và đổi màn trong scene Game về đây (AI 7). Lựa chọn rời trận đi qua `feature-run` trước để dọn trận, rồi mới tới đây (`leave-choice-after-dispose`, đặt cạnh `flow-buttons-send-choice`, hai ý trái nhau ở đường rời trận). `back-from-team` (AI 6): thêm nút Quay lại mà nhánh chỗ trong đội của `feature-team-select` chưa có. `retry-on-build-fail` (AI 5): gần mức trường hợp biên của vòng 4. Góp ý `quyen-chon-man-dich` đề nghị bỏ ý `run-routes-after-lose` ở `feature-popup`; ý đó giữ tới khi điểm chốt một bên.

- **Người chấm `flow-buttons-send-choice` 2 (2026-11-24).** Xoá ý đó; giữ `leave-choice-after-dispose`: lựa chọn rời trận đi qua lượt chơi để dọn trận trước. Nút Chơi và Vào trận vẫn gửi thẳng cho điều hướng màn (`play-opens-team`, `start-hands-team`).

- **Độ quan trọng 6, không phải 8 (2026-11-25, theo câu hỏi của @Manager).** IDEA chỉ có màn trận: mở game vào thẳng trận, "Chơi lại" vào trận mới; hai việc này nằm ở nhánh luồng màn của `feature-run` (bảng giai đoạn của nó ghi rõ). Điều hướng màn chỉ cần khi có màn bắt đầu và màn chọn đội, tức từ PROTOTYPE; vòng 3-4 viết trước khi vào PROTOTYPE. Đã loại: viết vòng 3-4 sớm cho IDEA (chuyển luồng màn đã chốt và đang code của `feature-run` mà IDEA không cần).

## Chưa rõ
- Chuyển nhánh luồng màn đã chốt của `feature-run` sang đây thế nào: vòng 3.
