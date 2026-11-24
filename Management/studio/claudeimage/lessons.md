# Bài học của @ClaudeImage
@ClaudeImage đọc toàn bộ file này trước khi vẽ mỗi item và áp mọi mục vào hình đó. Mỗi mục một dòng: luật làm được, kèm nguồn (order và phản hồi, hoặc log). Xem .claude/rules/studio.md.

- SVG tự đủ trong một file: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 W H">`, không script, không ảnh hay font ngoài file; chữ viết `font-family="Roboto"` (studio.py tự nhúng font khi đổi ra PNG) (luật của studio.py claude, 2026-11-24)
- Chữ của nút và nhãn trên claude-concept do tool in bằng Roboto: không vẽ chữ vào phần, trừ khi muốn chữ theo style, lúc đó khai `"text": false` cho phần ấy trong bảng phần (luật của studio.py claude, 2026-11-24)
- Claude-concept: không vẽ ảnh cả màn; chia frame thành phần theo `gd_frame.py`, một kiểu hình một phần (9-slice khi hình khác cỡ), tool ghép (luật của studio.py claude, 2026-11-24)
- Không ghi check.md bằng tay và không cần mở ảnh ra xem: `studio.py claude` tự kiểm kích thước và ghi check.md; tiêu chí của order chỉ áp cho phần được vẽ, hình cơ bản tool giữ lại trong ảnh ghép không thuộc tiêu chí (luật của studio.py claude, 2026-11-24)
- viewBox cùng tỉ lệ với kích thước ghi ở `format` của order; nền để trống (không vẽ hình chữ nhật phủ kín) khi order đòi nền trong suốt (luật của studio.py claude, 2026-11-24)
- Concept có popup hay UI phủ lên cảnh: các vật order yêu cầu thấy ở cảnh sau (hero, quái...) phải đặt ngoài vùng khung UI, kiểm bằng toạ độ frame trước khi chạy (claudeimage/orders/_design/feature-popup-lose.cheat/lose-popup-claude: tự kiểm lượt 1, hero bị popup che hết)
- Ghép lại theo yêu cầu đổi frame: tool đọc frame ở `live`, không đọc `ai/idea`; trước khi deliver, đối chiếu dòng "Chữ tool in" trong check.md (và `gd_frame.py live/<node>`) với điều bên yêu cầu muốn, chưa khớp thì không giao mà báo lại (claudeimage/orders/_design/feature-hud-upgrade.cheat/upgrade-cheat-claude: lần ghép thứ 3, nhãn nút vẫn in vì bản sửa chưa Sync sang live)
