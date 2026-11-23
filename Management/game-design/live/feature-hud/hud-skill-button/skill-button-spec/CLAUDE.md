# Ba nút kĩ năng thành

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Lấy danh sách kĩ năng từ `feature-castle` (3 kĩ năng), tạo một nút cho mỗi kĩ năng theo thứ tự danh sách.
- Mỗi khung hình hiện theo trạng thái `feature-castle` báo: sẵn sàng, hoặc đang hồi kèm thời gian còn lại và tổng hồi chiêu.
- Chạm nút sẵn sàng: báo `feature-castle`; `feature-castle` quyết dùng được hay không.
- Khi trạng thái đổi từ đang hồi sang sẵn sàng: chạy hiệu ứng nảy một lần.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số nút | 3 | nút | `feature-castle/castle-skills` |
| Thứ tự | Sấm sét, Băng giá, Cổ vũ | | Theo danh sách kĩ năng |
| Hồi chiêu Sấm sét | 20 (dự kiến) | giây | Bộ số chung |
| Hồi chiêu Băng giá | 35 (dự kiến) | giây | Bộ số chung; dài nhất |
| Hồi chiêu Cổ vũ | 25 (dự kiến) | giây | Bộ số chung |
| Cách hiện số giây | số nguyên, làm tròn lên | giây | Không hiện "0" khi chưa dùng được |
| Lớp phủ | tỉ lệ còn lại / tổng hồi chiêu | | Liếc là thấy sắp xong chưa |
| Hiệu ứng hồi xong | phóng 1.0 → 1.2 → 1.0 trong 0.3 giây (dự kiến), một lần | lần, giây | Anim bằng scale |
| Trạng thái lúc vào trận | cả 3 sẵn sàng | | `feature-run/run-start/start-values` |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Số nút | 3 | 3 | 3 |
| Số giây còn lại | Có | Có | Có |
| Lớp phủ vơi dần | Không bắt buộc | Có | Có |
| Hiệu ứng nảy khi hồi xong | Không bắt buộc | Có | Có, kèm âm thanh ngắn |
| Biểu tượng | Sprite 2D gen bằng AI hoặc chữ tên kĩ năng | Sprite 2D gen bằng AI | Sprite hoàn thiện |

## Trường hợp biên
- **Hai nút được chạm cùng lúc:** cả hai đều báo `feature-castle`; mỗi kĩ năng dùng độc lập.
- **Chạm đúng khung hình vừa hồi xong:** nếu `feature-castle` đã báo sẵn sàng thì dùng; chưa thì bỏ qua.
- **`feature-castle` từ chối dùng (ví dụ lượt vừa kết thúc):** nút giữ nguyên trạng thái `feature-castle` báo, không tự chuyển sang đang hồi.
- **Tạm dừng khi đang nảy:** hiệu ứng nảy đứng yên cùng trận.
- **Nâng cấp kĩ năng:** không đổi hồi chiêu (`feature-castle/castle-skills`), nút không đổi.
- **Hồi chiêu tổng bằng 0 (cấu hình sai):** nút luôn sẵn sàng, không hiện số, không phủ.

## Đã tự quyết
- **Làm tròn lên.** Hiện "0" mà bấm không được gây khó hiểu.
- **Nảy bằng phóng to 1.2 lần.** Anim scale đơn giản, kiểm được. Đã loại: nhấp nháy liên tục tới khi bấm (rối mắt khi cả 3 cùng sẵn sàng).
- **HUD không tự đếm giờ.** Theo node cha; thời gian chỉ có một nguồn ở `feature-castle`.

## Chưa rõ
- `feature-castle` cần cấp cho mỗi kĩ năng: tên, biểu tượng, trạng thái, thời gian còn lại, tổng hồi chiêu.
