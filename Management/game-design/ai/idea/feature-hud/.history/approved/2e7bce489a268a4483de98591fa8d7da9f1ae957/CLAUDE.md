# HUD trận

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Liếc một cái là biết đang an toàn hay nguy, có bao nhiêu tiền, đang ở đợt mấy.
- Bấm kĩ năng thành bằng một chạm.

## Liên quan
- `feature-castle`: tình trạng thành, kĩ năng thành.
- `feature-wallet`: số tiền.
- `feature-wave`: đợt đang đánh.
- `feature-hud-upgrade`: cụm thanh nâng cấp, tách riêng.

## Đã tự quyết
- **HUD là feature riêng, thanh nâng cấp là feature thứ hai.** Luật vòng 2: HUD lớn thì tách cụm lớn. Thanh nâng cấp là cụm lớn nhất và có thao tác mua; phần còn lại (thông tin, nút kĩ năng) gộp một feature. Đã loại: một HUD duy nhất (quá lớn cho một phiên); tách riêng nút kĩ năng (quá mỏng).
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ nút tạm dừng và liên quan tới `feature-popup-pause`.

## Chưa rõ
- Vị trí từng phần trên màn hình: vòng 3.
