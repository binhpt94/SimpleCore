# Luật số dư

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ một số dư nguyên, không âm, cho lượt đang chơi.
- Ba thao tác:
  - **Cộng(n)**: n > 0 thì số dư tăng n; n = 0 thì bỏ qua; n < 0 thì từ chối và ghi lỗi.
  - **Trừ(giá)**: giá < 0 thì từ chối và ghi lỗi; số dư >= giá thì trừ, trả "được"; ngược lại trả "không đủ", số dư giữ nguyên.
  - **Đặt lại**: số dư về tiền đầu lượt (50), luôn báo số mới.
- Sau mỗi thao tác làm số dư đổi: báo số dư mới cho mọi bên theo dõi (`feature-hud`, `feature-hud-upgrade`) ngay trong khung hình đó.
- Xử lí tuần tự: một yêu cầu xong (kể cả báo số mới) mới tới yêu cầu sau. Không có hàng đợi trì hoãn, không có "giữ chỗ" tiền.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Tiền đầu lượt | 50 (dự kiến) | tiền | Mua được một cấp hero (30) ngay, chưa đủ cho hai món; bộ số chung |
| Số dư nhỏ nhất | 0 | tiền | Luật không âm |
| Số dư lớn nhất | 999.999.999 | tiền | Tránh tràn số; không chạm tới khi chơi thường |
| Thời điểm báo số mới | cùng khung hình | khung hình | HUD phải khớp ngay với số tiền |

Tiền đầu lượt là số cấu hình của ví (asset cấu hình), không viết cứng.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Tiền đầu lượt | 50 | 50, chỉnh theo chơi thử | Chốt theo `feature-balance` |
| Ghi log khi cộng/trừ bị từ chối | có | có | có |
| Số dư lớn nhất | 999.999.999 | 999.999.999 | 999.999.999 |

## Trường hợp biên
- **Hai lần bấm mua rất nhanh, chỉ đủ tiền một lần**: hai yêu cầu trừ tới lần lượt; lần đầu "được", lần sau thấy số dư mới và trả "không đủ". Số dư không âm.
- **Tiền vừa đủ đúng bằng giá**: trừ được, số dư về 0, báo số 0.
- **Cộng và trừ trong cùng khung hình**: xử lí theo thứ tự tới; mỗi lần đổi báo một lần (bên theo dõi có thể nhận hai số trong một khung hình, số cuối là đúng).
- **Cộng làm vượt 999.999.999**: số dư dừng ở 999.999.999, báo số đó.
- **Tiền vào sau khi lượt kết thúc** (ví dụ quái gục cùng lúc thành bị phá): ví vẫn cộng bình thường; không ảnh hưởng gì vì lượt mới sẽ đặt lại về 50. Ví không cần biết lượt đã kết thúc.
- **Đặt lại khi số dư đang đúng bằng 50**: vẫn báo một lần để HUD lượt mới chắc chắn hiện đúng.
- **Yêu cầu trừ giá 0**: số dư >= 0 luôn đúng nên trả "được", số dư không đổi, không báo. Trong game không có món giá 0.

## Đã tự quyết
- **Tiền đầu lượt 50.** Theo bộ số chung. Đủ cho một quyết định ngay đầu trận.
- **Xử lí tuần tự, không khoá theo thời gian.** Chặn chi quá tay bằng thứ tự xử lí, không bằng thời gian chờ giữa hai chạm. Đã loại: khoá nút 0.5 s sau mỗi chạm (làm chậm người mua liên tiếp khi đủ tiền).
- **Số âm là lỗi, không phải "trừ ngược".** Giữ hai thao tác rõ nghĩa. Đã loại: cho phép cộng số âm thay cho trừ.
- **Có trần số dư.** Chỉ để an toàn số học. Đã loại: không trần.

## Chưa rõ
- Không có điểm chặn. Tiền đầu lượt 50 hợp lí với giá 30/40/60.
