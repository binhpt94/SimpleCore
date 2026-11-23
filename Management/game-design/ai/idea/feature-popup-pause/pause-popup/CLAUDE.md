# Nội dung popup tạm dừng

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lần tạm dừng, một popup tạm dừng.
- `feature-run/run-pause` nhờ hiện popup khi trận vào tạm dừng; popup báo lại người chơi chọn gì.
- Popup hiện qua `feature-popup`, che và chặn bấm HUD, thanh nâng cấp và chiến trường (`feature-popup/popup-layer`).
- Bỏ lượt dẫn tới `feature-home` qua `feature-run/run-flow`.

## Định nghĩa
- `glossary#quit-run`

## Danh sách
Trên popup:
- Tiêu đề "Tạm dừng".
- Nút tiếp tục (nút chính).
- Nút bỏ lượt; khi chạm thì đổi thành hỏi xác nhận, có nút đồng ý bỏ và nút không bỏ.

## Luật cơ bản
- Tiếp tục: popup đóng, trận chạy tiếp.
- Bỏ lượt → đồng ý: popup đóng, lượt kết thúc, về màn bắt đầu.
- Bỏ lượt → không bỏ: quay lại hai nút ban đầu, trận vẫn tạm dừng.
- Popup chỉ đóng bằng tiếp tục hoặc đồng ý bỏ lượt.

## Đã tự quyết
- **Có bước xác nhận bỏ lượt.** Mất lượt vì chạm nhầm là phạt nặng với người chơi phổ thông.
- **Xác nhận nằm trong cùng popup.** Tránh hai popup chồng nhau (`feature-popup` còn chưa rõ cách xử lí hai popup).
- **Không có nút chơi lại.** Giữ đúng hai lựa chọn của ý pause-actions ở `feature-popup-pause`.
- **Tự mở khi rời khỏi game giữa trận.** Luật ở `feature-run/run-pause`.

## Chưa rõ
- Không có điểm chặn.
