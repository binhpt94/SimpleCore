---
based-on: ca84fa282d14
group: test
---
# Panel cheat

Nút cheat nổi trên màn hình, chạm vào thì mở panel cheat để người test nhảy ngay tới các state cheat của các feature. Nút và panel chạy được trong bản build, không chỉ trong Editor, và một config bật hoặc tắt cả hai.

## Ý cốt lõi

### panel-test-tool | AI 8 | Panel cheat là công cụ cho người làm game test, không phải tính năng của người chơi
Không tính vào vòng chơi, không thưởng phạt, không lưu gì.

### panel-own-feature | AI 8 | Nút và panel cheat là một feature riêng, không đặt trong feature nào có sẵn
Nút nổi ở mọi scene và gom state của nhiều feature, nên cắt ngang nhiều feature. Đã loại: đặt trong `feature-hud` (HUD chỉ có trong trận), đặt trong `feature-popup` (dịch vụ popup không biết state của feature khác).

### panel-in-build | AI 8 | Nút và panel chạy được trong bản build cài lên máy, không chỉ trong Editor
Người test cầm máy thật là nhảy được tới state cần xem, không cần mở Unity.

### panel-config-switch | AI 8 | Một config bật hoặc tắt cả nút lẫn panel cheat
Tắt thì nút không hiện và không ai mở được panel.

### panel-lists-states | AI 8 | Panel liệt kê các state cheat mà các feature đã khai ở node cheat của mình
Panel không tự đặt ra state; mỗi state vẫn do feature sở hữu dựng.

### panel-apply-state | AI 7 | Chạm một state trong panel thì game chuyển ngay tới state đó
Thay cho việc chọn state trên asset trong Editor rồi bấm Play.
