# Nút tạm dừng

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một HUD trận, một nút tạm dừng.
- Bấm nút: báo `feature-run/run-pause`; bên đó chuyển trận sang tạm dừng và nhờ `feature-popup-pause` hiện popup.
- Vị trí nút ở `feature-hud/hud-layout`.

## Định nghĩa
- **Nút tạm dừng**: nút có biểu tượng tạm dừng quen thuộc (hai vạch đứng).

## Danh sách
- Nút tạm dừng.

## Luật cơ bản
- Trận đang đánh: bấm nút là tạm dừng.
- Trận đang tạm dừng hoặc đã kết thúc: nút không có tác dụng.

## Đã tự quyết
- **Biểu tượng hai vạch đứng.** Người chơi điện thoại đã quen. Đã loại: chữ "Dừng".
- **Nút xa vùng bấm nhiều.** Bấm nhầm tạm dừng làm đứt nhịp trận.
- **Popup che nút theo `feature-popup/popup-layer` (2026-11-23).** Ý `pause-button-active` trước trỏ tới node không có (`popup-block`); luật che và chặn bấm nằm ở `feature-popup/popup-layer`.

## Chưa rõ
- Không có điểm chặn.
