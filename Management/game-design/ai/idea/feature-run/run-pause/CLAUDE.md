# Tạm dừng trận

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Nút tạm dừng ở `feature-hud`; bấm thì feature này chuyển sang tạm dừng và nhờ `feature-popup-pause` hiện popup.
- `feature-popup-pause` báo người chơi chọn tiếp tục hay bỏ lượt; feature này đổi trạng thái theo.
- Lúc tạm dừng, feature này báo cho các feature trong trận dừng lại: `feature-wave`, `feature-enemy`, `feature-hero`, `feature-castle`.
- `feature-upgrade` và `feature-castle` (kĩ năng) không nhận thao tác khi đang tạm dừng; `feature-hud-upgrade` và nút kĩ năng của `feature-hud` bị popup che (`feature-popup/popup-block`).

## Định nghĩa
- **Đang đánh**: trận chạy, người chơi mua nâng cấp và bấm kĩ năng được.
- **Tạm dừng**: trận đứng yên, popup tạm dừng đang mở.
- **Kết thúc**: lượt đã xong (`feature-run/run-flow`), không quay lại đang đánh.
- **Rời khỏi game**: game không còn ở trên màn hình (chuyển ứng dụng, khoá máy, cuộc gọi).

## Danh sách
Đứng yên khi tạm dừng:
- Quái: không đi, không đánh.
- Hero: không đi, không đánh, đồng hồ hồi sinh đứng.
- Thành: không đánh, hồi chiêu kĩ năng đứng.
- Đợt: không thả quái, lúc thở giữa hai đợt đứng.

Bị chặn khi tạm dừng:
- Mua nâng cấp.
- Bấm kĩ năng thành.

## Luật cơ bản
- Đang đánh → tạm dừng: bấm nút tạm dừng, hoặc rời khỏi game.
- Tạm dừng → đang đánh: bấm tiếp tục.
- Tạm dừng → kết thúc: bấm bỏ lượt.
- Quay lại game sau khi rời thì vẫn ở tạm dừng, chờ người chơi bấm tiếp tục.
- Trận đứng ở đúng chỗ lúc bắt đầu tạm dừng; tiếp tục là chạy tiếp từ đó.

## Đã tự quyết
- **Lúc tạm dừng không mua nâng cấp, không bấm kĩ năng.** Theo quyết định chung của game; chốt hướng đã ghi ở `feature-run`.
- **Tự tạm dừng khi rời khỏi game.** Trả lời câu "Chưa rõ" của `feature-popup-pause`. Không tự dừng thì người chơi điện thoại mất lượt vì một cuộc gọi. Đã loại: trận vẫn chạy khi rời game.
- **Quay lại không tự chạy tiếp.** Người chơi cần một lúc để nhìn lại trận.

## Chưa rõ
- Không có điểm chặn.
