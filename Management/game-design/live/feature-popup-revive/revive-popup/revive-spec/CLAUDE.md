# Chữ, nút và trạng thái của popup hồi sinh

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Khi `feature-run` nhờ mở `PopupRevive/Main` qua dịch vụ popup: điền tiêu đề, dòng hồi sinh, hai nút; ẩn dòng chờ.
- Chạm "Xem quảng cáo": gửi một yêu cầu tới `feature-ads` (kèm mã đề nghị), hiện dòng chờ, làm mờ và khoá hai nút.
- Có kết quả: đóng popup, rồi báo `feature-run` "hồi sinh" hoặc "quảng cáo không xem được".
- Chạm "Kết thúc lượt": đóng popup, rồi báo `feature-run` "không hồi sinh".

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Khung popup | 800 x 700 ở x=560, y=190 | px | Khung chuẩn của `feature-popup/popup-layer/stack-rules` |
| Tiêu đề | "Thành đã bị phá", y=40, cao 90 | px | Giống popup thua |
| Dòng hồi sinh | "Xem quảng cáo để thành hồi 30% máu và chơi tiếp", y=160, cao 70 | px | 30% từ `feature-castle/castle-core/castle-body-values` |
| Dòng chờ | "Đang chờ quảng cáo...", y=340, cao 70 | px | Cùng chỗ dòng "Kỉ lục mới!" của popup thua |
| Nút "Xem quảng cáo" | x=50, y=540, 400 x 120 | px | Nút chính, cùng chỗ "Chơi lại" |
| Nút "Kết thúc lượt" | x=490, y=540, 260 x 120 | px | Cùng chỗ "Về màn bắt đầu" |
| Cao nút | 120 | px | `tap` của `feature-hud/hud-layout/layout-spec` |
| Số lần chạm có tác dụng mỗi lần mở | 1 | lần | Chặn bấm đúp |

Số tính ra: 50 + 400 + 40 + 260 + 50 = 800 (bề rộng khung).

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Popup hồi sinh | Không có | Có đủ chữ, hai nút, dòng chờ | Có, kèm hiệu ứng mở 0.2 giây (dự kiến) |
| Nút mờ khi chờ | | Đổi độ mờ | Như PROTOTYPE, thêm vòng xoay nhỏ cạnh dòng chờ |

## Trường hợp biên
- **Chạm hai nút gần như cùng lúc:** chạm được xử lí trước tính.
- **Rời game khi đang chờ quảng cáo:** quảng cáo là ứng dụng ngoài; quay lại thì popup vẫn ở trạng thái chờ tới khi có kết quả.
- **Rời game khi đang chờ chọn:** quay lại popup y như cũ; không mở popup tạm dừng.
- **Kết quả tới khi popup đã đóng (không xảy ra vì nút khoá):** bỏ qua theo mã đề nghị.

## Đã tự quyết
- **Một frame có dòng chờ.** Trạng thái chờ chọn chỉ ẩn dòng chờ và làm nút sáng; theo cách của popup thua (một frame đủ dòng). Đã loại: hai frame.
- **Chữ ghi rõ 30%.** Người chơi biết đổi lấy gì; theo ý `revive-shows-gain` của vòng 2.
- **Hai nút cùng chỗ hai nút của popup thua.** Hai popup nối tiếp nhau khi thua; tay không phải tìm chỗ mới.

## Chưa rõ
- Không có điểm chặn.
