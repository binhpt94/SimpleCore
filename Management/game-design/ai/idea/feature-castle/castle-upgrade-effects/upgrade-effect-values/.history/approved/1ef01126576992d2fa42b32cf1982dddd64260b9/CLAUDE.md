# Mức tăng mỗi cấp nâng thành

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ cấp hiện tại của ba mục: máu, sức đánh, kĩ năng (bắt đầu lượt ở cấp 1).
- Khi `feature-upgrade` báo một mục vừa mua xong: tăng cấp mục đó lên 1 và áp hiệu lực ngay.
  - Máu: cộng 150 vào máu tối đa và 150 vào máu hiện tại (`feature-castle/castle-core/castle-body-values`).
  - Sức đánh: tính lại sát thương đòn tự bắn (`feature-castle/castle-attack/auto-shot-values`).
  - Kĩ năng: tính lại hiệu lực cả ba kĩ năng (`feature-castle/castle-skills/skill-values`).
- Cho `feature-upgrade` và `feature-hud-upgrade` đọc cấp hiện tại để tính và hiện giá.

## Thông số
| Mục | Công thức ở cấp n | Đơn vị | Ví dụ cấp 1 / 2 / 5 | Lí do |
|---|---|---|---|---|
| Máu tối đa | 1000 + 150×(n-1) | máu | 1000 / 1150 / 1600 | 150 ≈ 15% máu đầu, thấy rõ trên thanh máu |
| Sát thương tự bắn | 10 × (1 + 0.2×(n-1)) | máu/phát | 10 / 12 / 18 | Bộ số chung |
| Sấm sét | 80 × (1 + 0.2×(n-1)) | máu/quái | 80 / 96 / 144 | Bộ số chung |
| Băng giá thời gian chậm | 5 × (1 + 0.2×(n-1)) | s | 5 / 6 / 9 | Bộ số chung |
| Cổ vũ lượng hồi | 30% × (1 + 0.2×(n-1)) | % máu tối đa của hero | 30% / 36% / 54% | Bộ số chung |
| Hồi chiêu ba kĩ năng | không đổi (20s / 35s / 25s) | s | | Luật vòng 3 |

Giá tham khảo (của `feature-upgrade`, dự kiến): máu và sức đánh 40×1.4^(n-1), kĩ năng 60×1.5^(n-1), làm tròn lên. Cấp máu 2 giá 40, cấp 5 cần tổng 40+56+79+110=285.

Cân bằng dự kiến: Băng giá cấp 31 thì chậm 35s bằng hồi chiêu (chậm liên tục); giá cấp 31 ≈ 60×1.5^30, không thể đạt trong một lượt bình thường.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Mục mua được | Máu, Sức đánh, Kĩ năng | như IDEA | cả ba |
| Máu mỗi cấp | +150 | như IDEA | theo số đã chỉnh sau chơi thử |
| Sức đánh mỗi cấp | +20% lv1 | như IDEA | theo số đã chỉnh |
| Kĩ năng mỗi cấp | +20% hiệu lực lv1 | như IDEA | theo số đã chỉnh |
| Phản hồi khi lên cấp | thành phóng to 1.1 lần rồi về cỡ cũ trong 0.3s | như IDEA | hiệu ứng riêng mỗi mục, âm thanh |

## Trường hợp biên
- Mua nâng kĩ năng khi Băng giá đang làm chậm quái: quái đang chậm giữ thời gian cũ; lần bấm sau mới dùng thời gian mới.
- Mua nâng kĩ năng khi kĩ năng đang hồi: hồi chiêu không đổi; lần bấm sau mạnh hơn.
- Mua nâng sức đánh khi hình đạn đang bay: phát đó đã tính sát thương lúc bắn; phát sau mới dùng số mới.
- Mua nâng máu khi thành còn 1 máu: thành thành 151 máu, máu tối đa +150.
- Cổ vũ vượt 100% máu tối đa (cấp ≥ 13): hero hồi tới đầy, không vượt.
- Tạm dừng hoặc thành đã bị phá: không nhận lệnh mua (`feature-upgrade` chặn).
- Không có hoàn tiền, không có hạ cấp.

## Đã tự quyết
- **Cộng tuyến tính theo giá trị lv1.** Người chơi tính được trong đầu; giá tăng theo hàm mũ đã đủ hãm. Đã loại: nhân theo phần trăm của cấp trước (tăng quá nhanh).
- **Nâng kĩ năng không đổi hiệu lực đang chạy.** Tránh kéo dài hiệu ứng giữa chừng khó đọc.
- **IDEA có đủ ba mục nâng thành, phản hồi phóng to 1.1 lần trong 0.3s (2026-11-24, sửa lệch).** Bản trước ghi IDEA chưa có nâng thành và phản hồi 0.2s chỉ từ PROTOTYPE, lệch với bảng mức tăng mỗi cấp của nhánh nâng cấp (ba mục thành có từ IDEA, phản hồi 0.3s) và với danh sách mục (3 mục thành ở IDEA). Phiên Castle đã code theo 1.1 lần, 0.3s. Thống nhất một số cho cả game: thời lượng 0.3s như phản hồi lên cấp của hero; thành phóng 1.1 lần (nhỏ hơn hero 1.2 lần vì thành to, phóng nhiều sẽ che chiến trường). Đã loại: giữ 0.2s và chỉ từ PROTOTYPE (lệch với các node khác và code đã có).

## Chưa rõ
- Không có điểm chặn.
