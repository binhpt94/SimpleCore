# Lúc thở giữa hai đợt

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT lúc thở nằm giữa HAI đợt liền nhau; bắt đầu khi `feature-wave/wave-clear` báo dọn sạch, kết thúc khi đợt sau bắt đầu ở `feature-wave/spawning`.
- Trong lúc thở, `feature-upgrade`, kĩ năng `feature-castle`, hồi sinh `feature-hero` chạy như mọi lúc khác.
- Số đợt đang đánh hiện ở `feature-hud`; báo đợt mới bằng hình hay chữ là việc của `feature-hud`.

## Định nghĩa
- **Lúc thở**: khoảng thời gian từ lúc dọn sạch một đợt tới lúc đợt sau bắt đầu ra quái.
- **Đợt sau bắt đầu**: lúc danh sách quái của đợt kế tiếp được lập và con đầu tiên bắt đầu ra theo nhịp thả.

## Danh sách
Những gì thay đổi trong lúc thở:
- Không có quái mới ra.

Những gì giữ như mọi lúc:
- Mua nâng cấp.
- Kĩ năng thành dùng và hồi chiêu.
- Hero gục vẫn hồi sinh theo thời gian.
- Máu thành giữ nguyên, không hồi.

## Luật cơ bản
- Lúc thở chỉ có sau khi dọn sạch đợt.
- Lúc thở có độ dài cố định, ngắn hơn hẳn một đợt.
- Hết lúc thở thì đợt sau tự bắt đầu.
- Mỗi lúc chỉ có một đợt đang đánh.

## Đã tự quyết
- **Có lúc thở ngắn.** Để thấy rõ đã qua một đợt và có cảm giác nhẹ nhõm (vòng 1, cảm giác chính). Đã loại: nối liền đợt.
- **Không có nút gọi đợt sớm.** Thêm một thao tác và một luật thưởng ngoài vòng 1. Đã loại: gọi sớm lấy thêm tiền.
- **Thành không hồi máu trong lúc thở.** Theo quyết định chung: thành không tự hồi; máu mất kéo áp lực sang đợt sau.
- **Node mới thay `feature-wave/break-phase` đã xoá (2026-11-23).** Màn nghỉ để nâng cấp mâu thuẫn với "nâng cấp ngay trong trận" của vòng 1.

## Chưa rõ
- Không có điểm chặn.
