# Nhân vật Spine thử: bộ mẫu, chỗ đứng, animation và nút

Dữ liệu cốt lõi: [data.md](data.md) · UI cần có: [ui.md](ui.md)

## Làm gì
- Khi màn trận mở, phiên `SpineTest` đặt một nhân vật Spine (bộ mẫu spineboy) trên mặt đất ở x=+9u, cao 1.5u, quay mặt trái, chạy idle lặp.
- Bốn nút "Đứng", "Chạy", "Nhảy", "Lật" và một nhãn trạng thái hiện trên màn trận (bố cục do @UIDesigner xếp, không chồng HUD, không chồng nút của feature-button-test).
- Đổi animation luôn chuyển mượt 0.2 giây. jump chạy một lần rồi tự về idle.
- Nhân vật không chạm, không chặn; quái, hero đi xuyên qua. Trận dừng thì nhân vật đứng nguyên, nút không nhận chạm (tín hiệu dừng tới qua nơi lắp màn).
- Chỉ có trong Unity Editor và bản Development build.
- Mỗi lần thử, tester đo p95 thời gian khung hình 60 giây trận đông, bật và tắt nhân vật, ghi vào đợt chơi thử.
- Toạ độ u, mặt đất, cỡ màn theo `feature-battlefield/battlefield-layers/layer-values`; 1u = 60 px ở màn chuẩn.

## Thông số
Bảng `Params` (kind `params`) trong `.excel/`: mỗi dòng một thông số (`key`, tên, đơn vị, giá trị ba giai đoạn, lí do): bộ xương mẫu, chiều cao, toạ độ chân, mặt quay lúc mở, tên ba animation, thời gian chuyển mượt, tốc độ, thứ tự vẽ. Thêm `perf_sample` (độ dài đoạn đo hiệu năng).

## Thông số theo giai đoạn
Cột `idea`, `prototype`, `full` của bảng `Params`. Mọi thông số có từ IDEA; PROTOTYPE và FULL ghi `như IDEA`.

## Trường hợp biên
- Bấm "Chạy" khi đang run, bấm "Đứng" khi đang idle: không làm gì, không chuyển mượt lại.
- Bấm "Nhảy" khi đang jump: jump chạy lại từ đầu, chuyển mượt 0.2 giây.
- Bấm "Lật" giữa jump: lật ngay, jump chạy tiếp, xong vẫn về idle với mặt mới.
- Bấm một nút animation khác giữa jump: bỏ jump, chuyển mượt sang animation đó.
- Popup mở mà trận vẫn chạy: nền che phủ nhân vật, nút không bấm được, animation chạy tiếp.
- Trận dừng (có hay chưa có popup): nhân vật đứng nguyên kể cả giữa jump hay giữa chuyển mượt; chạm nút bị bỏ, không để dành tới lúc chạy lại.
- Đổi animation giữa lúc chuyển mượt: chuyển mượt 0.2 giây từ tư thế đang hiện.
- Lật mặt: gốc ở chân, chân giữ x=+9u, y=0.
- Màn 21:9: nhân vật giữ 90 px, đứng đúng x=+9u; màn rộng chỉ thấy thêm chiến trường.
- Runtime spine-unity chưa cài hoặc thiếu bộ mẫu: feature không chạy được; đây là việc của người và @CodeLead, thiết kế không có phương án thay thế bằng sprite.

## Đã tự quyết
- **spineboy**: bộ mẫu chính thức, ai cài runtime cũng có; đã loại: tự làm bộ xương (không có công cụ).
- **x=+9u**: đã loại x=−9u (đối xứng được nhưng cánh trái đã có khối thử ở −5u và nút thử của button-test ở góc trái dưới, ảnh chụp rối hơn).
- **Theo trận dừng, chạy** (`match-time`, thay ý `real-time` cũ): theo góp ý GPT `r2-core#dung-dung-khung` ở node feature; Spine dùng cho unit thì phải đứng khi trận dừng. Tín hiệu đi qua nơi lắp màn nên vẫn không gọi thẳng feature khác. Đã loại: thời gian thực bất kể trận dừng.
- **Đo hiệu năng không có ngưỡng** (`perf-measure`): thiết kế chưa có ngân sách FPS và máy yếu nhất; ghi số để người quyết.
- **Ngay trên hero, quái**: đã loại dưới hero, quái (khối thử giữa đã thử trường hợp bị che).
- **Bấm lại animation lặp không làm gì**: tránh giật khi bấm nhầm hai lần.

## Chưa rõ
- Giấy phép Spine và bản runtime: người và @CodeLead.
- Ngân sách FPS, máy yếu nhất được hỗ trợ: chưa có node nào chốt.
