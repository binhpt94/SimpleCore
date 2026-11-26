# Nút của popup thua

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một popup thua, hai nút.
- Nút báo lựa chọn cho `feature-run/run-flow`; chuyển scene do `feature-scene-flow` khi `feature-run` xin.
- Chơi lại dẫn tới `feature-team-select` (đội lượt trước đã xếp sẵn, `feature-team-select/team-memory`); về màn bắt đầu dẫn tới `feature-home`.
- Popup che và chặn bấm phần bên dưới (`feature-popup`).

## Định nghĩa
- Không dùng thuật ngữ riêng.

## Danh sách
- Nút chơi lại (nút chính).
- Nút về màn bắt đầu.

## Luật cơ bản
- Bấm một nút: popup đóng, chuyển màn theo nút đó.
- Không bấm nút nào thì popup vẫn mở.
- Popup thua không có nút hồi sinh; đề nghị hồi sinh đã đi trước nó ở `feature-popup-revive`.

## Đã tự quyết
- **Chơi lại là nút chính.** Khớp vòng lặp "thua thì chơi lại ngay".
- **Không đóng bằng chạm ngoài.** Đóng mà không chọn thì người chơi kẹt ở trận đã dừng.

## Chưa rõ
- Không có điểm chặn.
