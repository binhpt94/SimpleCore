---
screen: landscape
by: player-commander=gpt@root-r1#nguoi-chi-huy, castle-damage-carries=gpt@root-r1#ton-that-qua-dot, main-feel=gpt@root-r1#cam-giac-chu-dao, art-shared-palette=gpt@style-colorful#bang-mau-chung, art-battle-reference=gpt@style-colorful#canh-tran-mau, art-light-from-top=gpt@style-colorful#anh-sang-tu-tren, art-outline-body-color=gpt@style-colorful#vien-theo-mau-than, art-popup-content-from-node=gpt@style-colorful#noi-dung-theo-node, art-ui-text-font=gpt@style-colorful#chu-dat-trong-ui, art-ad-button-pink=gpt@style-colorful#mau-nut-quang-cao, art-hud-text-contrast=gpt@style-colorful#do-tuong-phan-hud, art-faction-silhouette=gpt@style-colorful#duong-bao-hai-phe, art-unit-outline-contrast=gpt@style-colorful#nen-nhuong-vien-don-vi, art-castle-edge-solid=gpt@style-colorful#mep-thanh-de-doc, art-effect-overlay-limit=gpt@style-colorful#gioi-han-che-boi-hieu-ung, art-skill-color-match=gpt@style-colorful#mau-ky-nang-xuyen-suot
---
# Thủ thành anh hùng (tên tạm)

Game thủ thành nhìn ngang: một tòa thành đứng giữa màn hình, đội hero chọn trước sinh ra từ thành và tự đánh quái kéo tới theo từng đợt từ hai bên. Người chơi bấm kĩ năng của thành và nâng cấp hero, thành ngay trong trận để trụ lâu nhất có thể; thành bị phá là thua. Khác game thủ thành thường gặp: thành bị kẹp từ hai phía, và người chơi không điều khiển hero hay xây tháp mà chỉ chọn đội, canh kĩ năng thành và mua nâng cấp giữa trận.

## Ý cốt lõi

### target-player | AI 7 | Người chơi phổ thông, điện thoại cầm ngang, lượt chơi ngắn
Thao tác trong trận ít: bấm kĩ năng của thành và mua nâng cấp. Cái hay nằm ở chọn đội hero, chọn nâng cấp và canh lúc dùng kĩ năng. Đã loại: game nặng tính toán, thao tác nhanh tay.

### player-commander | AI 6 | Người chơi là người chỉ huy phòng thủ tòa thành
Chọn đội, mua nâng cấp và bấm kĩ năng thành gắn vào cùng một trách nhiệm: giữ thành.

### main-feel | AI 7 | Cảm giác chủ đạo là tự xoay chuyển thế thủ khi thành đang bị dồn ép
Sức ép phải đủ rõ để người chơi thấy quyết định của mình vừa giúp đội giành lại thế thủ.

### layout | AI 8 | Nhìn ngang từ bên cạnh, thành đứng yên giữa màn hình, chiến trường là dải đất hai bên
Mọi trận đánh diễn ra trên một dải đất chạy ngang qua thành. Đã loại: nhìn từ trên xuống, thành ở một mép màn hình.

### enemy-two-sides | AI 8 | Quái kéo tới theo đợt từ mép trái và mép phải
Đã loại: bốn phía (khó nhìn trên màn hình thấp), một phía (mất cảm giác bị kẹp).

### hero-auto-spawn | AI 7 | Đội hero cố định, chọn trước mỗi lượt, gục thì hồi sinh ở thành
Vào trận cả đội sinh ra ở thành; trong trận không có thêm hero. Hero gục thì sau một lúc hồi sinh ở thành. Đã loại (người loại): thành tự sinh hero theo thời gian.

### hero-auto-fight | AI 7 | Hero tự đánh, người chơi không điều khiển hero
Người chơi tác động vào trận qua nâng cấp và kĩ năng của thành. Đã loại: kéo thả hero, chọn mục tiêu.

### hero-types | AI 6 | Có nhiều hero với cách đánh khác nhau để chọn vào đội
Ví dụ cận chiến chắn quái, bắn xa đứng sau. Có nhiều hero hơn số chỗ trong đội để mỗi lượt phải chọn; mọi hero chọn được ngay. Đã loại: mở khoá hero dần.

### castle-role | AI 7 | Thành tự đánh quái và có kĩ năng hồi chiêu do người chơi bấm
Thành vừa là thứ phải giữ vừa là một chiến binh. Kĩ năng của thành là lúc người chơi tự tay cứu nguy. Đã loại (người loại): thành không đánh.

### upgrade-between-waves | AI 7 | Hạ quái được tiền, dùng tiền nâng cấp ngay trong trận, trận không dừng
Mua lúc nào cũng được, kể cả đang đánh; mạnh lên liền. Đã loại (người loại): chỉ nâng cấp giữa hai đợt.

### upgrade-targets | AI 7 | Nâng cấp được từng hero trong đội và nâng cấp được thành

### lose-condition | AI 9 | Thua khi thành bị phá

### ad-revive | AI 7 | Thành bị phá lần đầu trong lượt thì người chơi được chọn xem quảng cáo để hồi sinh thành và chơi tiếp
Mỗi lượt một lần; thành hồi sinh với một phần máu, quái và đợt giữ nguyên. Không xem thì thua như thường. Người yêu cầu (2026-11-24).

### castle-damage-carries | AI 7 | Sang đợt kế tiếp, thành giữ nguyên mức hư hại
Trụ qua một đợt mà ít tổn thất thì trụ được lâu hơn; giữ thành trong đợt này còn có giá trị sau khi đợt kết thúc.

### endless-score | AI 6 | Không có màn thắng: chơi vô tận, trụ càng nhiều đợt càng cao điểm, lưu kỉ lục
Đợt sau khó hơn đợt trước. Đã loại: thắng khi qua đủ một số đợt.

### run-reset | AI 6 | Mỗi lượt chơi bắt đầu lại từ đầu, chỉ giữ kỉ lục
Tiền và nâng cấp mất khi thua; lượt sau chọn lại đội hero. Đã loại: nâng cấp vĩnh viễn giữa các lượt.

### single-map | AI 6 | Game có một bản đồ duy nhất
Đã loại: nhiều màn, nhiều bản đồ.

### art-shared-palette | AI 7 | Hai style hình ảnh dùng chung một bảng màu ghi mã màu thân, vùng sáng, bóng và viền của từng màu chủ đạo
Lấy màu gốc từ ảnh tham chiếu người chọn, thêm màu của trận trên cùng bảng. Chữ "tươi" để mỗi công cụ tự chọn màu sẽ làm lệch sắc qua mỗi lần vẽ.

### art-battle-reference | AI 6 | Asset của cả hai công cụ vẽ được đối chiếu với cùng một khung trận ngang mẫu đã duyệt, có đủ UI và các lớp chiến trường
Ảnh tham chiếu chỉ có popup. Khung trận mẫu có thành, hero, quái, nền, hiệu ứng, HUD và thanh nâng cấp ở kích thước hiển thị thật, theo bố cục đã chốt của nền chiến trường và HUD.

### art-light-from-top | AI 7 | Mặt nổi trong cả hai style sáng nhất ở mép trên và tối dần về mép dưới
Cùng một hướng sáng cho khung, nút, icon, giáp, thân thành, để hình AI và hình SVG lên khối giống nhau khi ghép vào trận.

### art-outline-body-color | AI 8 | Viền ngoài của vật thể và nút là sắc đậm hơn của chính màu thân sát viền
Nút xanh và khối hồng giữ được màu riêng ở rìa. Đã loại: viền đen chung.

### art-popup-content-from-node | AI 8 | Popup lấy hình thức từ ảnh tham chiếu, lấy nội dung (chữ, số, nút) từ thiết kế của chính popup đó
Ảnh có ô thưởng và nút hồi sinh trong popup thua; game này để nội dung popup thua và popup hồi sinh theo node của từng popup.

### art-ui-text-font | AI 7 | Chữ và số do game hiển thị bằng một font tròn có đủ dấu tiếng Việt; ảnh và SVG để trống chỗ chữ
Hai công cụ dùng chung khung, ruy băng mà chữ vẫn đồng nhất; số thay đổi trong trận và dấu tiếng Việt đọc rõ ở kích thước thật.

### art-ad-button-pink | AI 7 | Nút xem quảng cáo luôn màu hồng, kể cả khi là nút chính của popup
Chốt màu ưu tiên cho popup hồi sinh: nhận diện quảng cáo đứng trên quy tắc nút chính màu xanh lá.

### art-hud-text-contrast | AI 7 | Chữ và số trên HUD đủ tương phản với nền ngay sau nó, kiểm trên khung trận ghép
Trời, cảnh xa, mặt đất sáng khác nhau và hiệu ứng có thể chạy sau HUD; máu thành, tiền, giá nâng cấp, số giây hồi chiêu luôn phải đọc được.

### art-faction-silhouette | AI 6 | Hero và quái phân biệt được chỉ bằng hình bóng tô đen ở kích thước trong trận
Cung thủ có ở cả hai phe nên vũ khí không đủ phân biệt phe; hình tổng thể phải khác khi hai phe dồn sát thành.

### art-unit-outline-contrast | AI 6 | Viền thành, hero và quái đủ tương phản với lớp nền ngay sau chúng ở cả hai cánh
Cùng một sprite có thể nổi trên trời sáng rồi chìm trước cảnh xa; chỉnh màu nền hoặc viền để dải đi lại đọc được.

### art-castle-edge-solid | AI 5 | Phần thân thành ngang tầm hero dùng mảng màu liền ở hai mép giáp dải đất
Đó là chỗ hero xuất hiện và quái áp sát; mảng liền giữ hình bóng nhân vật rõ khi đứng sát thành.

### art-effect-overlay-limit | AI 6 | Hiệu ứng vẽ đè lên thành, hero hay quái có giới hạn độ đục cộng gộp để vẫn thấy đơn vị bên dưới
Điểm sáng mạnh để ở vùng trống quanh mục tiêu.

### art-skill-color-match | AI 7 | Mỗi kĩ năng thành dùng cùng một màu chủ đạo trên nút HUD và trên hiệu ứng trong trận
Người chơi nối được nút vừa bấm với hiệu ứng vừa hiện giữa nhiều đòn đánh.
