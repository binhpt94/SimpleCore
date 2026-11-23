# Kết quả lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lượt kết thúc vì thua thì có một popup thua (`feature-run/run-flow`); bỏ lượt thì không có.
- Điểm, kỉ lục, cờ phá kỉ lục đọc từ `feature-run/run-score`.
- Popup hiện qua `feature-popup`; nút bấm ở `feature-popup-lose/lose-actions`.

## Định nghĩa
- `glossary#score` (của lượt vừa xong)

## Danh sách
Trên popup, từ trên xuống:
- Tiêu đề báo thành đã bị phá.
- Điểm của lượt.
- Kỉ lục.
- Lời báo kỉ lục mới (chỉ khi phá kỉ lục).

## Luật cơ bản
- Phá kỉ lục: hiện lời báo kỉ lục mới, điểm và kỉ lục bằng nhau.
- Không phá kỉ lục: không có lời báo, kỉ lục giữ như cũ.
- Popup chỉ hiện, không đổi điểm hay kỉ lục.

## Đã tự quyết
- **Hiện kỉ lục sau khi cập nhật.** Người chơi thấy ngay số mới; lời báo kỉ lục mới đã đủ nói "vừa vượt". Đã loại: hiện kỉ lục cũ rồi đổi số.
- **Không hiện chi tiết khác của lượt** (số quái hạ, tiền đã kiếm). Điểm là số đợt (quyết định chung). Đã loại: bảng thống kê lượt.

## Chưa rõ
- Không có điểm chặn.
