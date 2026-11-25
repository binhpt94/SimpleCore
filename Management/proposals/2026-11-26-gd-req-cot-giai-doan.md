# Đề xuất: sổ yêu cầu sinh yêu cầu cho cột giai đoạn đang làm

Người đề xuất: @Manager · 2026-11-26 · Chờ người duyệt

## Vấn đề
Khi người đổi giai đoạn từ IDEA sang PROTOTYPE, sổ yêu cầu (`gd_req.py`) không có yêu cầu mới cho phần PROTOTYPE của các node đã làm.

Mỗi node vòng 4 có bảng "Thông số theo giai đoạn" với cột IDEA, PROTOTYPE, FULL. Cả bảng đó chỉ là một yêu cầu `#params`, và yêu cầu này đã được ghi "đã kiểm" theo giá trị IDEA. Sang PROTOTYPE thì sổ vẫn báo đủ, dù code chưa làm phần PROTOTYPE.

Ví dụ thấy được trong đợt này:
- Enemy: hiệu ứng Trùm phóng to khi vào màn có ở cột PROTOTYPE, chưa có code, nhưng sổ Enemy vẫn ghi `#params` đã kiểm.
- HudUpgrade: tiếng mua, nút mờ rung 0,2 giây, dấu hero đang gục đều ở cột PROTOTYPE, nhưng sổ ghi 30/30.
- Combat: `combat_enabled` bật ở PROTOTYPE, nhưng Enemy, Hero, Castle vẫn tự trừ máu, không đi qua Combat.

Hiện chỉ phát hiện được khi feature tự rà, hoặc khi @CodeLead đọc bảng trước lúc đánh tiến độ PROTOTYPE done.

## Đề xuất (chọn một)
- **A (đề xuất):** `gd_req.py` sinh thêm một yêu cầu `<node>#stage-<giai đoạn>` cho mỗi node có bảng "Thông số theo giai đoạn", với giai đoạn đang làm. Nội dung của yêu cầu là cột của giai đoạn đó. Cột đổi thì yêu cầu thành "đổi thiết kế" như mọi yêu cầu khác. Người đổi giai đoạn thì các yêu cầu mới hiện ra là việc dev, và @Manager giao theo id.
- **B:** giữ sổ như cũ. Khi đổi giai đoạn, `#params` của mọi node có cột giai đoạn mới khác cột cũ tự về "đổi thiết kế", buộc feature ghi lại `done` với test đúng giá trị mới.
- **C:** không sửa tool. @CodeLead chỉ đánh tiến độ giai đoạn done cho node lá khi đã đọc bảng và có test đúng giá trị của giai đoạn đó, như đang làm tạm.

## Ảnh hưởng
- A và B đổi `Management/tools/reqbook.py` và `gd_req.py`, cùng trang sổ yêu cầu. Không đổi code game.
- Chọn A hoặc B thì ngay khi áp dụng, sổ của nhiều feature sẽ có thêm việc PROTOTYPE. Đó là đúng việc còn thiếu, hiện đang bị ẩn.
