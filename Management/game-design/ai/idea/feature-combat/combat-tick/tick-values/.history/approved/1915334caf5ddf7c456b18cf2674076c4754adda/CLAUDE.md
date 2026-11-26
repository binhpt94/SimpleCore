# Thông số nhịp giao chiến

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Mỗi bước nhịp chung của trận (0.025 giây, theo `feature-sim-clock`): chạy một nhịp theo thứ tự ở `tick-order`.
- Nhận đòn trúng từ hero, quái, thành (đòn tự bắn, Sấm sét), lọc theo trạng thái đầu nhịp, cộng theo bên trúng.
- Chuyển tổng cho feature sở hữu; chờ mọi bên trừ máu và báo xong; báo nhịp xong cho `feature-wave` và `feature-run`.

## Thông số
Bảng `TickParams` trong `.excel/` (loại `params`, chuyển từ bảng markdown cũ ngày 2026-11-25, giữ nguyên số trừ bước cập nhật): `combat_enabled`, `step_seconds`, `max_steps_per_frame`, `damage_rounding`, `lightning_hits`, `kill_reward_times`. Bước và trần lấy từ nhịp chung của `feature-sim-clock`; Sấm sét theo `feature-castle/castle-skills/skill-values`; nhịp đòn nhanh nhất theo `feature-hero/hero-list/hero-roster-values`.

## Thông số theo giai đoạn
Ba cột `idea`, `prototype`, `full` của bảng `TickParams`. IDEA không có giao chiến (mỗi bên trừ máu ngay khi trúng); từ PROTOTYPE mỗi bước nhịp chung chạy một nhịp giao chiến.

## Trường hợp biên
- **Hero và quái kết liễu nhau cùng nhịp:** cả hai hết máu; hero gục, quái bị hạ, quái vẫn cho tiền.
- **Thành và con quái cuối cùng hết máu cùng nhịp:** thành bị phá xét trước; luồng hồi sinh giữ tin dọn sạch.
- **Đòn vào quái đã ra ngoài màn (±16u):** vẫn trúng nếu bên ra đòn đã chọn nó; luật chọn là của bên ra đòn.
- **Cổ vũ hồi máu cùng nhịp hero nhận đòn:** Cổ vũ không qua giao chiến; hồi máu áp ở bước "đi và nhắm" trước khi trừ máu.

## Đã tự quyết
- **Bước theo nhịp chung 0.025 giây (sửa 2026-11-25, người yêu cầu nhịp chung 40 lần mỗi giây).** Thay cho bước riêng 0.02 giây (50 bước mỗi giây) và trần 5 bước mỗi khung của giao chiến; nay bước, trần và cách chạy do `feature-sim-clock` giữ cho mọi logic gameplay. Giữ `id` `fixed-step`. Code của Combat bỏ đồng hồ bước riêng sau khi node chốt (@CodeLead).
- **Cổ vũ áp trước khi trừ máu.** Người chơi bấm cứu đúng lúc thì hero được cứu.

## Chưa rõ
- Giao chiến là dịch vụ riêng hay một bước trong vòng lặp trận: phiên code cùng @CodeLead.
