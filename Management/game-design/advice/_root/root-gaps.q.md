# Soát lỗ hổng node gốc (vòng 1)

Người nhờ: @GameDesigner, mode Full Auto. Node: gốc (`ai/idea/data.md`, `ai/idea/CLAUDE.md`, đã đính kèm).
Bên hỏi muốn: **tìm lỗ hổng và đề xuất cách làm chắc**, không viết lại cả node.

## Câu hỏi
1. **Vòng lặp chơi và cảm giác chính** (mục "Cảm giác chính" và "Vòng lặp chơi" trong `CLAUDE.md`): có chỗ hở nào khiến lượt chơi nhàm, hoặc thiếu quyết định đáng nghĩ (ví dụ: lúc nào cũng mua cùng một thứ, kĩ năng thành bấm ngay khi hồi chiêu xong, chọn đội có một đáp án tốt nhất)? Chỗ hở nào cần vá ở vòng 1, chỗ nào để vòng sau lo được?
2. **Độ khác biệt** với game thủ thành cùng loại: ba điểm khác hiện có là (a) thành bị kẹp hai phía, (b) không xây, không điều khiển hero, (c) quyết định nằm ở chọn đội, canh kĩ năng thành, chi tiền. Người lạ đọc xong đã phân biệt được game này với game cùng thể loại chưa? Nếu chưa, thiếu gì?
3. **Các ý đang yếu**, xét từng ý: `hero-types` (AI 6), `endless-score` (AI 6), `run-reset` (AI 6), `single-map` (AI 6), `target-player` (AI 7). Với mỗi ý: giữ nguyên, sửa thế nào, hay vòng 1 còn thiếu một ý nào.

## Ràng buộc phải giữ
- Ba ý người đã chấm lại, **không đề xuất ngược**:
  - `hero-auto-spawn`: đội hero cố định chọn trước mỗi lượt (đã loại: thành tự sinh hero theo thời gian).
  - `castle-role`: thành tự đánh và có kĩ năng hồi chiêu do người chơi bấm (đã loại: thành không đánh).
  - `upgrade-between-waves`: nâng cấp ngay lúc đang đánh, trận không dừng (đã loại: chỉ nâng cấp giữa hai đợt).
- Màn ngang 1920x1080, game 2D nhìn ngang, hai style `ui-flat` (giao diện) và `world-cartoon` (thế giới trận).
- Vòng 1 không có con số, danh sách feature hay định nghĩa (để vòng sau). Không ghi dữ liệu theo giai đoạn (bản đầu, MVP, làm trước / để sau).
- Cả cây đã tới vòng 4 và đã chốt. Đổi `data.md` của gốc làm **mọi node bên dưới phải làm lại**, nên chỉ đề xuất thay đổi thật sự đáng giá.

## Cách trả lời
- Mỗi góp ý một mục rời `### <id góp ý> | <tiêu đề>`, một chủ đề mỗi mục.
- Trong mỗi mục ghi rõ: **loại** (đổi quyết định trong `data.md` / chỉ câu chữ hoặc diễn giải trong `CLAUDE.md` / để vòng sau lo), ý nào bị ảnh hưởng (id), và vì sao đáng đổi so với cái giá làm lại cả cây.
- Câu ngắn, tiếng Việt.
