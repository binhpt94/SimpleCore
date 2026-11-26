# Ba ô thông tin trận và thông báo đợt

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Khi trận dựng xong: hiện 3 ô với giá trị đầu lượt (1000/1000, 50, "Đợt 1").
- Nghe `feature-castle` (máu hiện tại, máu tối đa), `feature-wallet` (tiền), `feature-wave` (đợt đang đánh hoặc sắp tới, đợt bắt đầu) và cập nhật ngay.
- Khi `feature-wave` báo một đợt bắt đầu: hiện thông báo "Đợt N" trong 1.5 giây.
- Khi trận bị huỷ: huỷ cả 3 ô và thông báo.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số ô thông tin | 3 (máu, tiền, đợt) | ô | `hud-status` |
| Chữ máu | "hiện tại/tối đa" | máu | Bộ số chung yêu cầu số và thanh |
| Làm tròn máu | xuống, số nguyên | | Thấy "0" là biết rất nguy; xem trường hợp biên |
| Chữ tiền | số nguyên đầy đủ, kèm 1 biểu tượng xu | xu | |
| Chữ đợt | "Đợt N" | | Người chơi quen nhìn "đợt mấy" |
| Thời gian hiện thông báo đợt | 1.5 (dự kiến) | giây | Ngắn hơn nghỉ 3 giây, đủ đọc |
| Thông báo đợt chặn bấm | không | | Trận không dừng |
| Độ trễ cập nhật | 0, ngay khung hình nhận báo | | `status-read-only` |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Máu thành | Thanh và số | Thanh và số | Thanh và số, thanh tụt dần 0.3 giây khi mất máu (dự kiến) |
| Tiền | Số kèm biểu tượng xu | Như IDEA | Số nảy nhẹ khi nhận tiền |
| Ô đợt | "Đợt N" | Như IDEA | Như IDEA |
| Thông báo "Đợt N" đầu đợt | Không bắt buộc | Có, 1.5 giây | Có, kèm hiện dần và mờ dần |

## Trường hợp biên
- **Máu còn 0.4 (lẻ):** số làm tròn xuống thành 0 nhưng thành chưa bị phá; vẫn hiện "0/1000" và thanh còn một vệt rất nhỏ. Thành chỉ bị phá khi `feature-castle` báo.
- **Máu xuống dưới 0 trong tính toán:** hiện 0, thanh rỗng.
- **Máu vượt máu tối đa (không nên xảy ra):** thanh đầy, số hiện đúng giá trị nhận được.
- **Tiền rất lớn (ví dụ 12345):** hiện đủ số, không rút gọn.
- **Đợt mới bắt đầu khi thông báo đợt trước còn hiện:** thay ngay bằng thông báo mới, đếm lại 1.5 giây.
- **Lượt kết thúc:** 3 ô giữ giá trị cuối (máu 0) cho tới khi trận bị huỷ; thông báo đợt đang hiện thì tắt.

## Đã tự quyết
- **Kèm con số với thanh máu.** Trả lời "Có kèm con số hay không" của node cha; bộ số chung yêu cầu cả số và thanh.
- **Thông báo "Đợt N" thuộc HUD.** `feature-wave/wave-gap` để việc báo đợt mới cho `feature-hud`. Không chặn bấm vì trận không dừng.
- **Làm tròn xuống.** Người chơi thấy "0" là biết rất nguy. Đã loại: làm tròn lên (thấy "1" mà thành đã bị phá ở khung sau).
- **`ui.md` (2026-11-25, quy trình mới):** 6 phần tử, trùng id hình trong `main.frame`. Máu (thanh và số) và số tiền là chính, vì là thứ người chơi liếc để quyết bấm kĩ năng hay mua; biểu tượng xu, ô đợt, thông báo đợt là phụ. Không phần tử nào nhận chạm.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ trường hợp biên tạm dừng khi thông báo đợt đang hiện.

## Chưa rõ
- `feature-wave` cần báo hai việc riêng: "đợt hiện tại/sắp tới là N" (đổi ngay khi dọn sạch) và "đợt N bắt đầu ra quái" (để hiện thông báo).
