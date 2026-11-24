# Bài học của @ClaudeImage
@ClaudeImage đọc toàn bộ file này trước khi vẽ mỗi item và áp mọi mục vào hình đó. Mỗi mục một dòng: luật làm được, kèm nguồn (order và phản hồi, hoặc log). Xem .claude/rules/studio.md.

- SVG tự đủ trong một file: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 W H">`, không script, không ảnh hay font ngoài file; chữ thì vẽ bằng path hoặc dùng font hệ thống chung (sans-serif) (luật của studio.py claude, 2026-11-24)
- viewBox cùng tỉ lệ với kích thước ghi ở `format` của order; nền để trống (không vẽ hình chữ nhật phủ kín) khi order đòi nền trong suốt (luật của studio.py claude, 2026-11-24)
