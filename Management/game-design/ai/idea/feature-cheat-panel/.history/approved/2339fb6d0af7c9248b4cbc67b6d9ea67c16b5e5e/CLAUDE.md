# Panel cheat
Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Người chơi thật không được gì: đây là feature test (`group: test`). Người được là người làm game và người test.
- Người test cầm bản build trên máy, chạm nút cheat, chọn một state (ví dụ popup thua đang mở với kỉ lục mới) và thấy ngay state đó, không phải chơi tới nơi.

## Liên quan
- Các feature có node `cheat`: panel lấy danh sách state từ chúng; mỗi feature vẫn tự dựng state của mình.
- `feature-scene-flow`: chuyển scene khi state nằm ở scene khác scene đang mở.
- `feature-session-scene`: vòng đời scene; nút cheat có mặt ở mọi scene.

## Chưa rõ
- Panel có thêm thao tác cheat lẻ (nhảy đợt, đặt máu thành) ngoài các state hay không: chưa có yêu cầu, chưa thiết kế.

## Đã tự quyết
- Feature riêng, `group: test`, thư mục code `CheatPanel`: nút và panel cắt ngang mọi scene và mọi feature có cheat. Đã loại: node trong `feature-hud` (chỉ có trong trận), service mới (panel có giao diện và luật riêng, không phải dịch vụ dùng chung).
- Không hỏi GPT: feature `group: test` được miễn theo luật vòng 2.
- Panel chỉ làm việc với các state cheat sẵn có, theo phạm vi Manager giao ngày 2026-11-27.
