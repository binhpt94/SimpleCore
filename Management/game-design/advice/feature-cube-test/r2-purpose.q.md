also: feature-battlefield, feature-button-test, feature-popup

# Feature khối thử 3D: mục đích, ranh giới, các trường hợp hiển thị đáng thử

`feature-cube-test` là feature mới. Người yêu cầu nó ngày 2026-11-26, với mục đích: **thử model 3D hiển thị thế nào giữa các sprite 2D của game**, ngay trên màn trận. Nó không phải tính năng chơi của game.

## Ràng buộc của người (người yêu cầu, phải giữ)
- Feature để thử model 3D hiển thị trong game, không thêm gì vào lối chơi.

## Câu hỏi
So với mục đích trên, node feature này:
1. **Mục đích:** còn thiếu quyết định nào để lần thử này thật sự trả lời được câu "model 3D đặt giữa sprite 2D trông và chạy ra sao"? Ví dụ kết quả của lần thử là gì, ai xem, xem thế nào thì coi là đạt hay không đạt.
2. **Ranh giới:** chỗ giao với `feature-battlefield` (mặt đất, các lớp chiến trường, thứ tự vẽ), `feature-popup` (popup mở đè lên) và `feature-button-test` (feature thử khác cùng màn trận) có điểm nào mơ hồ? Ai giữ chỗ đặt khối trên chiến trường, ai quyết thứ tự vẽ, khối có phải biết feature khác có mặt không? Có trùng việc với feature nào không?
3. **Các trường hợp hiển thị đáng thử:** trường hợp nào nên có (thứ tự vẽ trước và sau thành, hero, quái; ánh sáng, bóng; viền và bảng màu so với style 2D; cỡ ở các tỉ lệ màn; nằm dưới popup; hiệu ứng vẽ đè...), trường hợp nào thừa, trường hợp nào còn thiếu?
4. **Lệch hoặc thừa:** ý nào kéo feature thành tính năng chơi, hoặc rộng hơn mức cần để thử? Nên giữ thành một feature riêng, gộp vào feature có sẵn, hay tách thêm?

Bên hỏi (@GameDesigner) cần các góp ý cụ thể để đưa vào node trước khi đi tiếp vòng 3.
