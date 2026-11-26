---
advice: r2-core
node: feature-spine-test
table: ai/idea
hash: 5033d18c216a
also: feature-battlefield, feature-hero, feature-hud, feature-popup
asked: 2026-11-26 20:53:04
by: gpt (Codex CLI)
---
# Góp ý r2-core

## Câu hỏi

# Feature thử Spine: đủ ý cốt lõi chưa, thiếu kiểm tra nào, nên tách gộp node con nào, ranh giới với chiến trường, hero, HUD, popup

`feature-spine-test` là feature thử nhân vật Spine (animation xương 2D) trên màn trận của game 2D nhìn ngang. Nó không phải tính năng chơi của game.

## Ràng buộc của người (người yêu cầu ngày 2026-11-26, phải giữ)
- Là một feature riêng.
- Chỉ để thử, không thêm gì vào lối chơi.

## Câu hỏi
1. **Ý cốt lõi đã đủ chưa?** So với mục đích thử Spine trước khi quyết có dùng Spine cho hero, quái hay không: node còn thiếu ý cốt lõi nào (ví dụ: tiêu chí đạt hay không đạt của cả lần thử, kết quả thử được ghi lại thế nào, ai xem và quyết)? Ý nào còn mơ hồ?
2. **Còn thiếu điều cần kiểm nào** về Spine trong game 2D nhìn ngang? Xét các node con hiện có (danh sách điều cần kiểm, cheat) và các rủi ro thường gặp khi trộn Spine với sprite: thứ tự vẽ, material và shader, viền và bảng màu so với style 2D, cỡ ở các tỉ lệ màn, quay mặt, chuyển và trộn animation, hiệu năng, nhân vật bị che hay bị cắt bởi camera. Điều nào thiếu, điều nào thừa so với mức cần để thử?
3. **Node con:** các node con hiện có đã chia đúng chưa? Nên tách, gộp, hay thêm node con nào?
4. **Quan hệ với `feature-battlefield`, `feature-hero`, `feature-hud`, `feature-popup`:** cần làm rõ gì để ranh giới không mơ hồ? Ví dụ: nhân vật thử theo lớp vẽ và camera của chiến trường thế nào, đứng ở đâu để không chồng hero, quái và hai feature thử khác; so cỡ với hero theo tiêu chí gì; HUD và popup che nhân vật thế nào khi trận dừng; nút đổi animation nằm ở đâu so với HUD.

Bên hỏi (@GameDesigner) cần các góp ý cụ thể để đưa vào node trước khi đi tiếp vòng 3.

## Góp ý của GPT

### chi-hien-ban-dev | thêm | Nhân vật, nút và nhãn Spine chỉ xuất hiện trên màn trận của bản dev.
Quyết định `feature-spine-test/spine-checks#shown-with-battle` áp dụng trong bản dev. Người thử vào đúng màn trận để quan sát, còn bản dành cho người chơi không có điều khiển thử.

### moc-dat-lan-thu | thêm | Lần thử đạt khi mọi mục bắt buộc đều đạt trên bản dev ở cấu hình đã chốt trước khi thử.
Một mục bắt buộc chưa có bằng chứng thì lần thử còn dang dở. Độ hợp phong cách của bộ mẫu được ghi riêng để chủ project phân biệt lỗi tích hợp với khác biệt mỹ thuật.

### phieu-ket-qua | thêm | Mỗi lần thử có một phiếu ngoài game ghi cấu hình, kết quả từng mục và bằng chứng.
Ghi phiên bản bản dev, runtime, bộ mẫu, máy, tỉ lệ màn và cách dựng cảnh. Dùng ảnh cho cỡ và lớp vẽ, clip cho lỗi chuyển động, số đo cho hiệu năng. Phiếu nằm ngoài phạm vi của `feature-save`.

### nguoi-chot-spine | thêm | Chủ project xem phiếu thử rồi chốt riêng việc dùng Spine cho hero và cho quái.
Một mẫu cho thấy Spine hoạt động trong trận. Quyết định cho `feature-hero` và `feature-enemy` có thể khác nhau vì số nhân vật cùng xuất hiện và yêu cầu hình ảnh khác nhau.

### ngo-canh-hien-thi | làm rõ spine-no-effect | Spine nhận camera, mốc đất và tín hiệu dừng trận qua nơi lắp màn, không gọi trực tiếp các feature khác.
`UnityProject/Assets/_Composition` lắp Spine vào camera trận dùng chung và cấp mốc đất theo `feature-battlefield` và `feature-castle`. Tín hiệu dừng đến từ `feature-run` qua cùng nơi lắp. Hero và quái vẫn đi xuyên qua Spine, giao chiến giữ nguyên.

### khoang-trong-mac-dinh | làm rõ spine-on-battle | Ở cảnh chuẩn, toàn bộ vùng hoạt ảnh Spine nằm ngoài thân thành, vùng xuất hiện của đơn vị và khối thử.
Giữ mốc `x=+9u` trong `feature-spine-test/spine-checks/spine-spec` nếu khung trận đáp ứng tiêu chí. Đối chiếu với `feature-castle`, `feature-hero`, `feature-enemy` và `feature-cube-test`. Hero và quái đi ngang qua Spine trong trận là ca kiểm lớp vẽ riêng.

### co-theo-hero | thêm | Ở tư thế đứng yên, đo từ đường đất đến đỉnh đầu Spine và so với mốc hộp hero 1,5u.
Dùng cùng camera và khung hình với `feature-hero`. Mốc 90 px trong `feature-spine-test/spine-checks/spine-spec` được đọc trên khung chuẩn. Ghi thêm chênh lệch so với chiều cao nhìn thấy của hero để phát hiện mẫu đúng cỡ hộp nhưng trông quá to hoặc quá nhỏ.

### o-dieu-khien-thu | thêm | Cụm nút và nhãn Spine có một ô riêng ở mép vùng an toàn, ngoài dải chiến trường.
Bản bố cục trong `feature-spine-test/spine-checks/spine-spec` ghi mép neo và khung chứa cụm. Kiểm cả hình lẫn vùng chạm ở tỉ lệ hẹp nhất để cụm không đè lên `feature-hud`, `feature-hud-upgrade` hay `feature-button-test`.

### dung-dung-khung | thêm | Spine đứng nguyên tư thế khi trận dừng và tiếp tục hoạt ảnh từ đúng thời điểm đã dừng.
Thử lúc đang nhảy và lúc đang trộn hai hoạt ảnh. Theo mốc dừng của `feature-run` ngay cả trước khi `feature-popup-pause`, `feature-popup-revive` hoặc `feature-popup-lose` hiện. Khi trận chạy, giữ tốc độ thực đã đề xuất ở `feature-spine-test/spine-checks/spine-spec`.

### khoa-cham-khi-dung | thêm | Nút Spine chỉ nhận chạm khi trận đang chạy và không có popup chặn thao tác.
Kiểm cả khoảng trận đã dừng nhưng popup chưa hiện, và khoảng popup đã đóng nhưng trận chưa chạy lại. Dùng mốc của `feature-run` và lớp chặn chạm của `feature-popup` để thao tác bị chặn không biến thành lệnh đổi hoạt ảnh khi tiếp tục.

### thu-tu-khi-giao-hinh | thêm | Khi hero hoặc quái đi ngang qua, toàn bộ Spine giữ cùng thứ tự vẽ so với từng sprite qua mọi khung hình.
Kiểm thân, tay và phụ kiện trong cùng đoạn clip để bắt lỗi một phần Spine lọt ra sau sprite hoặc đổi lớp giữa chừng. Dùng thứ tự đã ghi ở `feature-spine-test/spine-checks/spine-spec` khi đặt cạnh `feature-hero` và `feature-enemy`.

### chat-lieu-tren-nen-tran | thêm | Material của mẫu Spine giữ đúng màu và mép trong suốt trên cả vùng sáng lẫn vùng tối của nền trận.
So với ảnh mẫu trên thiết bị chạy bản dev, ở cả hai hướng quay. Ghi shader và material khi thấy mảng hồng, ô đục, quầng viền hoặc chi tiết mất khi lật. Dùng nền thật của `feature-battlefield` để lỗi ở rìa hình dễ lộ.

### so-voi-khung-my-thuat | thêm | Độ dễ đọc của mẫu Spine được chấm ở cỡ trong trận, cạnh sprite hero trên khung trận mẫu.
Đối chiếu hình bóng, độ rõ của viền và độ lệch bảng màu theo `_root/style-colorful` khi đặt cạnh `feature-hero`. Chênh lệch màu hoặc viền của bộ mẫu là đầu vào cho quyết định làm art riêng sau khi thử runtime.

### khung-camera-hep | thêm | Camera giữ trọn hình Spine qua cả ba hoạt ảnh và hai hướng quay ở tỉ lệ màn ngang hẹp nhất được hỗ trợ.
Đo tới đỉnh cú nhảy và điểm xa nhất của phụ kiện so với mép khung nhìn. Ca này bổ sung ca màn rộng ở `feature-spine-test/spine-checks` và tách lỗi cắt bởi camera khỏi che khuất có chủ ý bởi `feature-hud` hoặc `feature-popup`.

### lat-giu-moc-dat | thêm | Lật mặt giữ nguyên tọa độ gốc của Spine trên chiến trường.
Đặt dấu tại mốc chân trên đường đất rồi lật qua lại ở cùng tư thế để phát hiện mẫu bị trượt ngang hoặc nhấc khỏi đất. Đối chiếu với mốc đất của `feature-battlefield` và vị trí ở `feature-spine-test/spine-checks/spine-spec`.

### doi-giua-luc-tron | thêm | Khi có lệnh đổi mới giữa lúc trộn, Spine chuyển từ tư thế đang hiện sang hoạt ảnh vừa chọn.
Thử bấm Chạy → Nhảy → Đứng trong khoảng 0,2 giây đã đề xuất ở `feature-spine-test/spine-checks/spine-spec`. Tư thế phải nối tiếp qua từng lần bấm và lần Nhảy bị ngắt không được tự kéo nhân vật về Đứng khi đã chuyển sang hoạt ảnh khác.

### do-chi-phi-trong-tran | thêm | Trên máy yếu nhất được hỗ trợ, thời gian xử lý khung hình p95 khi bật Spine phải nằm trong ngân sách FPS đã chốt.
Đo hai lượt cùng máy, đội, seed và thao tác, một bật và một tắt Spine. Lấy cùng 60 giây ở đoạn trận đông sau khi máy đã chạy ổn định, ghi cả p95 và phần tăng khi bật. Dùng mốc tái lập của `feature-sim-clock` để giữ cảnh so sánh nhất quán.

### ca-cheat-giao-hinh | thêm | Cheat có một ca dựng sẵn Spine chồng hình với hero và quái trong khung trận có HUD.
Giữ `feature-spine-test/cheat#run-flipped` làm ca xem trạng thái chạy quay phải. Ca mới giúp chụp và lặp lại lỗi lớp vẽ mà không cần chờ `feature-hero` và `feature-enemy` đi tới đúng vị trí trong lượt thật.

### node-cach-kiem | thêm | Node mới: Quy trình kiểm Spine ghi cảnh thử, dấu hiệu đạt và bằng chứng cho từng mục bắt buộc.
`feature-spine-test/spine-checks` giữ hành vi đã chốt, `feature-spine-test/spine-checks/spine-spec` giữ thông số mẫu và `feature-spine-test/cheat` giữ cách dựng nhanh. Node mới ghi mục nào cần xác nhận trong lượt thật, nhất là lúc dừng trận, mở popup và đo hiệu năng.
