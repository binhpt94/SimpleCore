# Hai nút của popup thua

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Hiện 2 nút dưới phần kết quả (`feature-popup-lose/lose-result/result-spec`).
- Chạm nút: báo lựa chọn cho `feature-run/run-flow/flow-rules` một lần, rồi khoá cả 2 nút.
- Popup không tự đóng và không tự chuyển màn; `feature-run` đóng nó qua `feature-popup`.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số nút | 2 | nút | `actions-two` |
| Nút chính | "Chơi lại" | | Vòng lặp thua rồi chơi lại ngay |
| Nút phụ | "Về màn bắt đầu" | | |
| Số lần báo mỗi lần popup mở | 1 | lần | `first-tap-only` |
| Cách đóng khác | không có | | `actions-only-exit` |
| Nút hồi sinh | không có | | Bộ số chung |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Nút "Chơi lại" | Có; vào thẳng trận mới với đội mặc định | Có; sang màn chọn đội | Có; sang màn chọn đội |
| Nút "Về màn bắt đầu" | Không có (chưa có màn bắt đầu) | Có | Có |
| Hiệu ứng chạm nút | Không | Không | Nút lún nhẹ (thu 0.95 lần, 0.1 giây, dự kiến) |

## Trường hợp biên
- **Chạm hai nút gần như cùng lúc:** chỉ nút được xử lí trước tính.
- **Rời game khi popup thua đang mở:** quay lại popup vẫn như cũ; không mở popup tạm dừng.
- **Tắt game khi popup đang mở:** kỉ lục đã lưu trước khi popup hiện (`feature-run/run-score/score-rules`).
- **Popup khác chồng lên popup thua (không nên xảy ra):** chỉ popup trên cùng nhận chạm (`feature-popup/popup-layer`); 2 nút này chờ tới khi popup trên đóng.

## Đã tự quyết
- **Khoá cả 2 nút sau lần chạm đầu.** Đổi màn có thể mất một khung hình; không để chuyển màn hai lần.
- **Nút quay lại của máy không làm gì.** Không có lựa chọn thứ ba; tránh kẹt ở trận đã dừng.
- **Frame vẽ cả popup (2026-11-23).** `main.frame` có nền che toàn màn, khung popup (`overlap` vì nằm trên nền che), ba dòng chữ lấy số từ `feature-popup-lose/lose-result/result-spec`, và hai nút. Node này sở hữu số của hai nút: `btn-y` = 540, `replay-x` = 50, `replay-w` = 400, `home-x` = 490, `home-w` = 260 (trong khung 800x700: cách mép 50, cách nhau 40, 50+400+40+260+50 = 800); cao bằng `tap` của `feature-hud/hud-layout/layout-spec`. Nút "Chơi lại" rộng hơn nút phụ theo ý `actions-replay-main` của node cha ("to và nổi hơn"); "Về màn bắt đầu" 260 px vẫn đủ chỗ cho chữ ở cỡ 1080p. Bố cục giống `main.frame` của result-spec. Claude-concept của frame này chỉ vẽ thành phần hai nút (số do node này `let`); khung, ba dòng chữ giữ hình cơ bản vì mượn số (2026-11-24, theo luật claude-concept mới). Đã loại: hộp giữ chỗ "Chữ kết quả".

## Chưa rõ
- Không có điểm chặn.
