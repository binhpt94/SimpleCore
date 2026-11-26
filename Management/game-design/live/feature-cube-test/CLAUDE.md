# Khối thử 3D

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Không có gì cho lối chơi. Người làm game xem được model 3D đứng giữa sprite 2D của trận: có lệch cỡ, lệch sáng, lệch viền, sai thứ tự vẽ hay không, trước khi quyết có dùng model 3D cho thứ khác hay không.

## Liên quan
- `feature-battlefield`: khối đứng trên dải đất, theo camera và thứ tự vẽ của nền chiến trường.
- `feature-castle`, `feature-hero`, `feature-enemy`: chỉ để đặt cạnh khi xem thứ tự vẽ; khối không đọc, không gửi gì cho chúng.
- `feature-hud`, `feature-popup`: HUD và popup vẽ trên khối như trên mọi thứ của trận.
- Không feature nào gọi tới feature này, và nó không gọi feature nào.

## Đã tự quyết
- **Đặt trên chiến trường của màn trận.** Chỉ ở đó mới thấy đủ các lớp để so. Đã loại: màn riêng chỉ có khối thử.
- **Feature riêng**, không nằm trong `feature-battlefield`: thứ để thử, bỏ được mà không đụng nền. Đã loại: thêm khối vào nền chiến trường.
- **Khối là hình lập phương**: hình đơn giản nhất mà vẫn thấy được ba mặt, sáng tối và viền. Đã loại: model nhân vật (phải làm art trước khi thử được).
- **Không chặn ai, không chạm được.** Nếu khối chặn quái thì nó đổi lối chơi, sai mục đích thử.
- Style `world-cartoon` ghi "không phối cảnh 3D": khối 3D ở đây chính là để xem khoảng cách giữa 3D và style đó, không phải đổi style.
- `code.json`: `CubeTest`, theo luật đặt tên của vòng 2.

## Chưa rõ
- Khi nào bỏ feature này khỏi game: người quyết sau khi xem kết quả thử.
- Hỏi GPT vòng 2: đã nhờ @DesignAdvisor; lệnh hỏi đang lỗi config Codex, chờ người sửa.
