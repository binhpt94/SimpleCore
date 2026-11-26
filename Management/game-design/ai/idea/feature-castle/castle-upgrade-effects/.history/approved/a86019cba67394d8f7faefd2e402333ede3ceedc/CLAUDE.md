# Hiệu ứng nâng cấp thành

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT thành có BA mục nâng cấp; mỗi mục MỘT cấp riêng.
- Một lần mua tăng MỘT cấp của MỘT mục.
- Mục "kĩ năng" tác động CẢ BA kĩ năng của thành.
- Giá, việc trừ tiền, giá tăng dần: `feature-upgrade`. Nút mua: `feature-hud-upgrade`. Mức tăng mỗi cấp: `feature-balance`.

## Định nghĩa
- `glossary#level`

## Danh sách
| Mục | Tăng gì | Không đổi gì |
|---|---|---|
| Máu | Máu tối đa; máu hiện tại tăng thêm đúng phần tăng | Phần máu đã mất |
| Sức đánh | Sức đánh của đòn tự bắn | Tốc độ đánh, tầm bắn |
| Kĩ năng | Hiệu quả cả ba kĩ năng | Hồi chiêu |

## Luật cơ bản
- Mua xong là có hiệu lực ngay, kể cả đang đánh hay kĩ năng đang hồi chiêu.
- Cấp càng cao thì thành càng mạnh ở mục đó.
- Cấp và hiệu ứng giữ tới hết lượt; lượt mới đặt lại do `feature-run`.
- Thưởng-phạt: tiền vào thành là tiền không vào hero; nâng máu giữ thành lâu hơn nhưng không giúp hạ quái.

## Đã tự quyết
- **Nâng máu chỉ cộng phần tăng, không hồi đầy.** Giữ luật máu thành không tự hồi; nếu hồi đầy thì người chơi chỉ mua máu lúc sắp thua. Đã loại: hồi đầy khi nâng, chỉ tăng máu tối đa mà không tăng máu hiện tại (mua xong không thấy gì thay đổi).
- **Một mục kĩ năng chung cho cả ba kĩ năng.** Ít nút, hợp người chơi phổ thông. Đã loại: một mục cho mỗi kĩ năng.
- **Nâng kĩ năng không giảm hồi chiêu.** Mạnh hơn dễ thấy hơn hồi nhanh hơn; hồi chiêu giữ nhịp bấm ổn định. Đã loại: giảm hồi chiêu.
- **Mỗi mục chỉ tăng một thứ.** Người chơi đọc tên mục là biết mua gì.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ luật "lúc tạm dừng không mua được".

## Chưa rõ
- Không có điểm chặn.
