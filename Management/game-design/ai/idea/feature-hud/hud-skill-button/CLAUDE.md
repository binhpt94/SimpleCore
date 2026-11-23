# Nút kĩ năng thành

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một kĩ năng thành, một nút.
- `feature-castle` báo danh sách kĩ năng, trạng thái từng kĩ năng và thời gian hồi chiêu còn lại; nút chỉ hiện.
- Bấm nút: báo cho `feature-castle`; `feature-castle` dùng kĩ năng và bắt đầu hồi chiêu.
- Lúc tạm dừng, hồi chiêu đứng và nút không nhận bấm (`feature-run/run-pause`).
- Vị trí nút ở `feature-hud/hud-layout`.

## Định nghĩa
- `glossary#castle-skill`
- `glossary#cooldown`

## Danh sách
Trên mỗi nút:
- Biểu tượng kĩ năng.
- Trạng thái sẵn sàng hoặc đang hồi chiêu.
- Thời gian hồi chiêu còn lại (khi đang hồi).
- Lớp phủ vơi dần theo hồi chiêu.

## Luật cơ bản
- Sẵn sàng → bấm → đang hồi chiêu.
- Đang hồi chiêu → hết thời gian → sẵn sàng, kèm tín hiệu báo.
- Bấm lúc đang hồi chiêu: không có gì xảy ra.

## Đã tự quyết
- **Hiện cả số thời gian còn lại lẫn lớp phủ.** Quyết định chung yêu cầu hiện thời gian còn lại; lớp phủ cho liếc nhanh.
- **HUD không tự đếm hồi chiêu.** Thời gian do `feature-castle` giữ để chỉ có một nơi đúng.
- **Có tín hiệu lúc hồi xong.** Canh lúc bấm kĩ năng là cảm giác chính ở vòng 1; người chơi không nên phải nhìn chằm chằm vào nút.

## Chưa rõ
- Số kĩ năng và biểu tượng từng kĩ năng chờ vòng 3 của `feature-castle`.
