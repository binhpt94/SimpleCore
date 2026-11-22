# Thành tự bắn

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT thành bắn MỘT quái mỗi phát.
- Thành xét quái ở CẢ HAI bên; chỉ một mục tiêu tại một lúc.
- NHIỀU quái có thể ở trong tầm; thành chỉ chọn con gần thành nhất.

## Định nghĩa
- **Đòn tự bắn**: đòn thường của thành, tự bắn theo nhịp tốc độ đánh, không cần thao tác.
- **Tầm bắn**: khoảng cách tính từ thành ra mỗi bên; quái trong khoảng này mới bị bắn.
- **Mục tiêu**: quái còn sống gần thành nhất trong tầm.
- Sức đánh, tốc độ đánh, tầm đánh: theo định nghĩa chung ở `feature-hero/hero-stats`.

## Danh sách
- Chỉ số của thành: máu (`feature-castle/castle-core`), sức đánh, tốc độ đánh, tầm đánh. Không có tốc độ di chuyển.

## Luật cơ bản
- Có quái trong tầm thì thành bắn con gần nhất; không có thì thành không bắn.
- Mỗi phát trừ máu quái theo sức đánh của thành.
- Mục tiêu bị hạ hoặc ra khỏi tầm thì thành chọn lại con gần nhất.
- Quái bị thành hạ vẫn cho tiền như mọi quái khác (`feature-enemy/enemy-reward`).
- Thành bắn suốt trận, kể cả lúc giữa hai đợt nếu còn quái.
- Sức đánh tăng qua nâng cấp (`feature-castle/castle-upgrade-effects`).

## Đã tự quyết
- **Nhắm quái gần thành nhất, không theo bên.** Quái gần nhất là quái sắp đánh thành; luật đơn giản, giống cách hero nhắm (`feature-hero/hero-targeting`). Đã loại: luân phiên trái phải, nhắm quái máu thấp nhất.
- **Đòn thường một mục tiêu.** Đánh nhiều quái để dành cho kĩ năng thành bấm tay. Đã loại: đòn nổ vùng.
- **Tầm ngắn hơn khoảng đất mỗi bên.** Giữ vai trò cho hero, và thành giống "tuyến cuối". Đã loại: bắn khắp chiến trường.
- **Nâng cấp sức đánh không tăng tốc độ đánh hay tầm.** Mục nâng cấp thành chỉ có máu, sức đánh, kĩ năng (quyết định chung của game).

## Chưa rõ
- Không có điểm chặn.
