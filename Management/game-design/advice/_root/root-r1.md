---
advice: root-r1
node: (gốc)
table: ai/idea
hash: 469d9009792c
asked: 2026-11-24 15:24:16
by: gpt (Codex CLI)
---
# Góp ý root-r1

## Câu hỏi

# Làm lại vòng 1 cùng GPT: ý nào thiếu, mơ hồ, sai

Người nhờ: @GameDesigner, mode Full Auto. Người bảo làm lại vòng 1 (node gốc) cùng GPT.
Node: gốc (`ai/idea/data.md`, `ai/idea/CLAUDE.md`, đã đính kèm).

## Câu hỏi
Với node gốc hiện tại, xét đủ các mặt của vòng 1:
- thể loại và góc nhìn;
- người chơi là ai;
- cảm giác chính;
- vòng lặp chơi (làm gì, được gì, lặp lại thế nào);
- điều kiện thua và cách tính điểm.

Hãy chỉ ra:
1. **Thêm:** quyết định vòng 1 nào còn thiếu.
2. **Làm rõ:** ý nào còn mơ hồ, đọc riêng chưa chấm được.
3. **Thay:** ý nào sai hoặc yếu, cần một quyết định khác.

Ý đã ổn thì không viết gì.

## Ràng buộc phải giữ
- Ba ý người đã chấm lại, **không đề xuất ngược**:
  - `hero-auto-spawn`: đội hero cố định, chọn trước mỗi lượt. Người đã loại: thành tự sinh hero theo thời gian.
  - `castle-role`: thành tự đánh, có kĩ năng hồi chiêu do người chơi bấm. Người đã loại: thành không đánh.
  - `upgrade-between-waves`: nâng cấp ngay lúc đang đánh, trận không dừng. Người đã loại: chỉ nâng cấp giữa hai đợt.
- Màn ngang 1920x1080, game 2D nhìn ngang, hai style `ui-flat` (giao diện) và `world-cartoon` (thế giới trận).
- Luật vòng 1: không con số, không danh sách feature, không định nghĩa, không dữ liệu theo giai đoạn (bản đầu, MVP, làm trước / để sau).
- Mỗi góp ý sẽ thành một ý trong `data.md` của gốc. Đổi gốc làm cả cây (đã tới vòng 4) phải rà lại. Vì vậy **chỉ đề xuất điều thật sự là quyết định của vòng 1**. Chi tiết của vòng sau (nhịp thả quái, cân bằng giá, thời điểm hồi chiêu, cách hiển thị) để nhánh dưới.
- Đã hỏi hai lần trước: `advice/_root/root-gaps.md` và `advice/_root/root-gaps-r2.md`. Đừng lặp lại y nguyên các góp ý chỉ là diễn giải đã đưa vào `CLAUDE.md` của gốc (ví dụ kể một lần cứu nguy, đội có điểm yếu, giữ đội để thử lại, đọc được nguy cơ). Ý áp lực đổi cánh đã được ghi thành câu hỏi mở cho nhánh `feature-wave/spawning`; không đề xuất lại ở gốc.

## Bên hỏi muốn
Danh sách quyết định vòng 1 còn thiếu hoặc cần sửa, mỗi góp ý một quyết định, để đưa thẳng thành ý của node gốc.

## Góp ý của GPT

### nguoi-chi-huy | thêm | Người chơi là người chỉ huy phòng thủ tòa thành.
Vai trò này gắn việc chọn đội, mua nâng cấp và kích hoạt kĩ năng thành vào cùng một trách nhiệm là giữ thành.

### ton-that-qua-dot | thêm | Khi sang đợt quái kế tiếp, thành giữ nguyên mức hư hại.
Thắng một đợt với ít tổn thất giúp người chơi trụ lâu hơn. Nhờ vậy, quyết định bảo vệ thành trong đợt hiện tại có giá trị cả sau khi đợt đó kết thúc.

### cam-giac-chu-dao | thêm | Cảm giác chủ đạo là tự xoay chuyển thế thủ khi thành đang bị dồn ép.
Điều này đặt ưu tiên cho nhịp trận. Sức ép cần đủ rõ để người chơi nhận ra quyết định của mình đã giúp đội giành lại thế thủ.
