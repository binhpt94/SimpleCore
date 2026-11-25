also: feature-battlefield, feature-popup-lose
# Vòng 1: hai style hình ảnh theo phong cách Colorful

Người nhờ: @GameDesigner, mode Full Auto. Node gốc vừa viết lại hai style hình ảnh ở `.art-style/`: `ui-flat.md` cho giao diện và `world-cartoon.md` cho thế giới trận. Cả hai theo phong cách Colorful của ảnh tham chiếu người chọn (`Management/references/20261125-225339-lose.png`, ghi chú của người: "Tôi dùng phong cách này · Colorfull").

## Ảnh tham chiếu (mô tả để đối chiếu)
Mô tả của @DesignAdvisor sau khi xem ảnh, không phải thiết kế:
- Popup thua, khổ dọc, trên nền game tối mờ. Phía sau mờ đi có thanh tài nguyên trên cùng: tim, tiền, kim cương, mỗi ô có nút cộng xanh lá.
- Trên đỉnh popup là icon emoji mặt khóc màu vàng. Hai bên là hai trái tim đỏ nứt, phía sau có tia sáng và mảnh tam giác hồng bắn ra.
- Khung popup viền vàng cam dày, góc trên uốn vành, thân màu kem hồng nhạt. Ruy băng tím vắt ngang, hai đầu quặt xuống, có sao vàng ở hai bên. Trên ruy băng là tiêu đề "YOU LOSE!" chữ vàng to, tròn, viền tím đậm, có bóng.
- Câu phụ "Try again?" chữ tím, hai bên có vạch nhấn.
- Ô thông tin là một mảng hồng nhạt bo tròn, chia đôi bằng vạch dọc. Mỗi nửa có nhãn tím dạng viên thuốc ("Your Score", "Reward"), bên dưới là icon (sao, đồng tiền) và số to màu tối.
- Nút chính "Retry" to, xanh lá, dạng viên thuốc, có icon vòng lặp và vệt bóng sáng như kẹo. Bên dưới là hàng hai nút nhỏ hơn: "Home" xanh dương có icon nhà, "Revive" hồng có icon tim cộng và icon video quảng cáo.
- Đáy khung có nơ hồng, sao vàng ở giữa, lá xanh ở hai góc dưới. Có mây trắng nhạt trong thân popup.
- Mọi khối đều có bóng sáng ở mép trên và tối dần xuống dưới. Viền là bản đậm của màu thân, không đen tuyền.

## Câu hỏi
1. Hai style `ui-flat` và `world-cartoon` đã đủ rõ và nhất quán với ảnh tham chiếu chưa? Chỗ nào lệch so với ảnh?
2. Còn thiếu quy tắc gì để hai bên vẽ (@Codex gen bằng AI, @ClaudeImage vẽ SVG) ra đều tay giữa các lần vẽ và giữa hai công cụ?
3. Trận (hero, quái, thành, nền chiến trường, hiệu ứng) và UI (HUD, nút, popup) có chỗ nào dễ lệch nhau, hay dễ chìm vào nhau? Ảnh tham chiếu là một popup, chưa có cảnh trận.
Ý node còn thiếu thì thêm, ý mơ hồ thì làm rõ, ý sai hoặc yếu thì đề xuất thay. Ý đã ổn thì không viết gì.

## Yêu cầu riêng của người
- Dùng đúng phong cách của ảnh tham chiếu này.
- Không ghi dữ liệu theo giai đoạn (bản đầu, MVP, làm trước / để sau).

## Bên hỏi muốn
Mỗi góp ý một quyết định, để đưa thẳng thành quy tắc của style hoặc ý của node.
