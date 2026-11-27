---
based-on: ca84fa282d14
by: field-ground-line=gpt@r2-1#mep-dat-theo-moc, field-layer-order=gpt@r2-1#thu-tu-lop-nen, field-decor-flat=gpt@r2-1#decor-tren-duong-di, field-fill-frame=gpt@r2-1#nen-phu-khung-tran, field-touch-through=gpt@r2-1#cham-xuyen-nen, field-battle-only=gpt@r2-1#nen-theo-man-tran
---
# Nền chiến trường

Phông nền của trận: trời, cảnh xa, dải đất và decor mà thành, hero, quái đứng lên. Nền chỉ để nhìn, không có luật chơi, nằm dưới mọi thứ khác trong trận.

## Ý cốt lõi

### field-backdrop | AI 8 | Trận có một phông nền gồm trời, cảnh xa, dải đất chạy ngang qua thành và decor
Người chơi thấy ngay đây là một bãi chiến hai bên thành, không phải khoảng trống. Thiếu nền thì thành, hero, quái lơ lửng trên màn trống.

### field-ground-line | AI 8 | Mép trên dải đất trùng đường mặt đất do nhánh thân thành cấp, từ mép màn trái tới mép màn phải
Dải đất liền qua dưới chân thành tới cả hai nơi quái xuất hiện. `feature-castle` cấp mốc, nền vẽ theo mốc; hero và quái tự đặt nhân vật của mình trên đó.

### field-look-only | AI 8 | Nền chỉ để nhìn: không chặn, không đổi đường đi, không ảnh hưởng trận
Vị trí và luật của thành, hero, quái giữ ở feature của chúng; nền vẽ theo các mốc đó.

### field-behind-all | AI 8 | Nền nằm dưới mọi thứ khác của trận, HUD và popup che lên trên nền
Nền không bao giờ che unit, hiệu ứng hay HUD.

### field-layer-order | AI 8 | Thứ tự vẽ các lớp nền từ sau ra trước là trời, cảnh xa, dải đất, decor
Cả bốn lớp cùng nằm sau thành, hero, quái và hiệu ứng.

### field-decor-flat | AI 7 | Trên dải hero và quái đi, decor chỉ là hoạ tiết phẳng trên mặt đất
Người chơi không nhầm vật trang trí với vật cản hay lối vào; đường quái tiến vào thành vẫn đọc rõ.

### field-fill-frame | AI 8 | Nền phủ kín khung hình trận ở mọi tỉ lệ màn hỗ trợ, kể cả phần sau HUD và thanh nâng cấp
Không có dải trống khi màn rộng hơn. Vùng dành cho nút vẫn do `feature-hud` và `feature-hud-upgrade` quy định.

### field-touch-through | AI 8 | Mọi lớp nền cho thao tác chạm đi xuyên qua
Nút kĩ năng, nút nâng cấp và việc chặn chạm của popup chạy như không có nền.

### field-battle-only | AI 7 | Nền hiện ở mọi khung hình của màn trận và chỉ ở màn trận
Khung hình đầu khi vào trận đã có nền; nền vẫn hiện dưới popup. Rời màn trận thì nền mất cùng trận.

### field-one-map | AI 7 | Chỉ có một nền, giống nhau ở mọi lượt
Khớp "một bản đồ duy nhất" ở vòng 1. Đã loại: đổi nền theo đợt.
