# Nút thử

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Không có gì cho lối chơi. Người làm game được một feature nhỏ nhất để chạy thử trọn quy trình: thiết kế tới vòng 4, @UIDesigner xếp frame, phiên `ButtonTest` code, @CodeLead kiểm, @UnityAutoPlay chơi thử.

## Liên quan
- `feature-hud`: nút thử nằm trên cùng màn trận, không được che phần nào của HUD.
- `feature-popup`: popup mở thì che và chặn bấm cả nút thử, như mọi thứ bên dưới.
- Không feature nào gọi tới feature này, và nó không gọi feature nào.

## Đã tự quyết
- **Đặt trên màn trận** (người nói "ở game scene"). Scene Game có cả màn bắt đầu, chọn đội và trận; chọn màn trận vì đó là màn chơi chính và có HUD để thử xếp chỗ cạnh nhau. Đã loại: màn bắt đầu (dễ tới hơn nhưng ít thứ để thử cạnh nhau; node cheat đã cho dựng thẳng state, nên đường tới màn trận không phải vấn đề).
- **Feature riêng, không nằm trong `feature-hud`.** Nó là thứ để thử, sẽ bỏ được mà không đụng HUD. Đã loại: thêm một nút vào HUD.
- **Không nối với feature nào.** Một feature đứng một mình là đủ để chạy hết quy trình, và kiểm được luật ranh giới feature. Đã loại: nút gửi tin cho feature khác (thêm contract, quá mục đích thử).
- `code.json`: `ButtonTest`, theo luật đặt tên của vòng 2.

## Chưa rõ
- Khi nào bỏ feature này khỏi game: người quyết sau khi chạy thử xong quy trình.
