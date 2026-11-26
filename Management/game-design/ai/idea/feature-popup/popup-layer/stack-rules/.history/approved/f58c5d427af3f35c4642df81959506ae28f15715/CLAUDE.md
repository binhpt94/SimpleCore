# Luật xếp chồng và chặn bấm

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Lớp popup của scene Game giữ một chồng popup, từ 0 tới 3 cái.
- Mở popup: kiểm id chưa mở và chồng chưa đủ 3; đặt nền che rồi popup lên trên cùng; popup cũ trên cùng thôi nhận bấm.
- Đóng popup: bỏ popup và nền che của nó; popup ngay dưới (nếu có) nhận bấm lại; không còn popup thì lớp popup không chặn gì.
- Khi có popup mở: HUD, màn chơi và các popup không ở trên cùng không nhận bấm. Nút quay lại của điện thoại chỉ được chuyển cho feature của popup trên cùng.

Các tình huống trong game hiện tại:

| Tình huống | Chồng popup (dưới → trên) | Nhận bấm |
|---|---|---|
| Đang đánh | (trống) | HUD, màn chơi |
| Thua | popup thua | popup thua |
| Thua, lượt vừa phá kỉ lục | popup thua, popup mời đánh giá | popup mời đánh giá; đóng nó thì popup thua |

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số popup chồng tối đa | 3 | popup | Game hiện cần 2 (popup thua và popup mời đánh giá); dư cho popup sau này, quá 3 là dấu hiệu lỗi |
| Số bản mỗi id cùng lúc | 1 | bản | Chặn mở trùng do bấm nhanh |
| Độ tối của một nền che | 60 (dự kiến) | % | Đủ thấy đang có popup mà vẫn nhận ra trận phía sau |
| Thời gian hiện, đóng popup | 0 | giây | Tức thì, nhận bấm ngay |
| Tác dụng khi bấm nền che | không | — | Tránh bấm nhầm mất popup thua |
| Tác dụng nút quay lại của điện thoại khi có popup | chuyển cho feature của popup trên cùng; dịch vụ không tự làm gì | — | Mỗi popup tự quyết (popup thua: không tác dụng) |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Hiệu ứng mở popup | Không, hiện tức thì | Phóng to từ 90% lên 100% trong 0,15 giây (dự kiến) | Như PROTOTYPE, chỉnh theo art |
| Hiệu ứng đóng popup | Không, mất tức thì | Mất tức thì | Thu nhỏ và mờ 0,1 giây (dự kiến) |
| Nhận bấm trong lúc hiệu ứng mở | Không có hiệu ứng | Chặn bấm tới khi hiệu ứng xong | Như PROTOTYPE |
| Độ tối nền che | 60% (dự kiến) | 60% | Chỉnh theo art |
| Số popup chồng tối đa | 3 | 3 | 3 |
| Nút quay lại của điện thoại | Chuyển cho popup trên cùng | Chuyển cho popup trên cùng | Chuyển cho popup trên cùng |

## Trường hợp biên
- **Mở popup thứ 4**: từ chối, ghi lỗi nêu id; ba popup đang mở giữ nguyên.
- **Mở id đang mở**: từ chối, ghi cảnh báo; không đưa popup đó lên trên cùng.
- **Bấm hai nút của popup trên cùng trong cùng khung hình (hai ngón)**: chỉ lần bấm đầu được chuyển cho feature gọi; feature gọi chịu trách nhiệm bỏ qua bấm sau khi đã xử lí (xem `feature-popup/popup-request/request-rules`).
- **Đóng popup không ở trên cùng**: chỉ popup đó và nền của nó mất; popup phía trên vẫn ở trên cùng và vẫn nhận bấm.
- **Trận đứng yên (thua)**: popup và hiệu ứng của popup không đứng theo; popup không dựa vào đồng hồ của trận.
- **Người chơi rời game lúc có popup**: quay lại thì chồng popup y như cũ.
- **Không có popup**: lớp popup không chặn bấm, không làm tối gì.

## Đã tự quyết
- **Giới hạn 3.** Dư chỗ cho popup sau này, bắt được lỗi mở lặp. Đã loại: không giới hạn; giới hạn 1 (không chồng được popup mới).
- **Một bản mỗi id.** Đơn giản nhất để chặn bấm đúp. Đã loại: cho mở trùng.
- **Nền chồng thì tối thêm.** Mỗi popup một nền theo vòng 3; tối thêm giúp thấy popup nào đang hoạt động. Đã loại: chỉ nền trên cùng làm tối.
- **Hiện tức thì.** Hiệu ứng không cần để thấy gameplay.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ popup tạm dừng khỏi bảng tình huống, nút quay lại và trận đứng yên; bỏ mục đã tự quyết về hộp xác nhận bỏ lượt.

## Chưa rõ
- Nút quay lại của điện thoại khi không có popup: việc của `feature-run/run-flow`, không thuộc dịch vụ này.
