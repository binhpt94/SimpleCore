# Nút "Thử" và dòng "Đã bấm: N"

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Khi màn trận hiện: dựng nút "Thử" và nhãn "Đã bấm: 0" (phần tử ở `ui.md`, chỗ ở frame của @UIDesigner).
- Mỗi lần chạm rồi nhả trên nút: N tăng 1, nhãn đổi chữ trong cùng khung hình.
- Rời màn trận: ẩn cả hai, bỏ N.
- Không gửi, không đọc gì của feature khác. Không lưu gì.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Chữ trên nút | Thử | | Một từ, rõ là nút thử |
| Chữ nhãn | Đã bấm: N | | N là số lần chạm, ghi đủ số |
| N lúc màn trận mở | 0 | lần | Không lưu giữa các lần mở |
| Mức tăng mỗi lần chạm | 1 | lần | |
| Trễ từ lúc nhả tới lúc nhãn đổi | 0 | khung hình | Phản hồi tức thì để thấy rõ |
| Cạnh chạm tối thiểu của nút | như nút HUD ở `feature-hud/hud-layout/layout-spec` | px | Cùng ngưỡng chạm với cả game |
| Số phần HUD bị che | 0 | | `no-hud-overlap` |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Nút "Thử" và nhãn "Đã bấm: N" trên màn trận | Có, đủ các ý của node | như IDEA | như IDEA |
| Art của nút | Hình cơ bản (claude-concept là đủ) | như IDEA | như IDEA |

## Trường hợp biên
- **Bấm nhanh liên tục:** mỗi lần nhả trên nút tính một, không bỏ lần nào, không giới hạn tốc độ.
- **Hai ngón chạm nút cùng lúc:** mỗi lần nhả tính một (hai ngón là hai lần).
- **Chạm rồi kéo ra ngoài nút:** không tính.
- **N rất lớn:** không có trần; nhãn ghi đủ số, chữ dài ra thì nhãn tự co hoặc giãn trong chỗ của nó.
- **Popup mở:** lớp popup chặn chạm (`feature-popup/popup-layer/stack-rules`); N giữ nguyên tới khi popup đóng.
- **Trận đứng yên mà không có popup:** nút vẫn đếm (`works-when-still`).
- **Hồi sinh thành rồi chơi tiếp:** vẫn là màn trận đó, N giữ nguyên.
- **Thua rồi chơi lại:** màn trận mở lại, N về 0.

## Đã tự quyết
- **Đếm cả khi trận đứng yên.** Khoá nút theo trạng thái trận thì nút phải đọc `feature-run`, thêm một chỗ nối mà mục đích thử không cần. Thực tế các lúc đứng yên đều có popup che. Đã loại: khoá nút khi trận không đánh.
- **Góc trái dưới, ngay trên thanh dưới của HUD** (người quyết 2026-11-26, thay cho bản đặt sát mép phải). Nút nằm trong dải chiến trường, đáy nút sát mép trên của mép dưới HUD, không đè lên nó; chỗ cụ thể để @UIDesigner quyết. Nút có che một góc nhỏ của cánh trái chiến trường, chấp nhận được vì nó chỉ để thử. Đã loại: sát mép phải (bản trước), mép trái giữa màn (bản frame đầu).
- **Dòng số đếm bên phải nút, cùng hàng.** Số dài ra thì chữ giãn sang phải mà nút đứng yên. Đã loại: đặt phía trên nút.
- **Không có hiệu ứng nhấn riêng.** Phản hồi của node là số đếm; hiệu ứng nhấn chung của nút (nếu có) theo style `ui-flat`.

## Chưa rõ
- Không có điểm chặn.
