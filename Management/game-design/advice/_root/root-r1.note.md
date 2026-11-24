# Nhận xét của @DesignAdvisor về root-r1

Nhận xét cho người chấm. Không phải góp ý của GPT, và không phải lí do để bỏ góp ý.

- Không góp ý nào đi ngược ba ý người đã chấm lại: `hero-auto-spawn`, `castle-role`, `upgrade-between-waves`.
- Không góp ý nào trùng ý đã bị người loại.

## Từng góp ý
- **`ton-that-qua-dot`** (thành giữ nguyên mức hư hại qua các đợt): cây đã chốt đúng quyết định này ở vòng 3-4, không trái nhau.
  - `live/feature-castle/castle-core`, ý `castle-no-regen`: máu thành không tự hồi trong trận. Đã loại: hồi đầy khi dọn sạch đợt.
  - `live/feature-wave/wave-gap`: lúc thở giữa hai đợt, thành không hồi máu.
  - `live/feature-castle/castle-skills`: kĩ năng không hồi máu thành.

  Đưa ý này lên gốc là nâng một quyết định đã có lên vòng 1. Các node bên dưới đã khớp, nên lúc rà lại có lẽ chỉ cần đổi `based-on`.
- **`cam-giac-chu-dao`** (tự xoay chuyển thế thủ khi bị dồn ép): gần với hai dòng đang có trong mục "Cảm giác chính" của `CLAUDE.md` gốc: "Tự tay cứu nguy" và "Hồi hộp khi thành sắp bị phá". Điểm mới là chọn đây làm cảm giác **chủ đạo**, đứng trên các cảm giác khác.
- **`nguoi-chi-huy`** (người chơi là người chỉ huy phòng thủ thành): là cách đặt vai trò cho người chơi. Ý này khớp với `target-player` và `hero-auto-fight`, không đổi luật nào ở dưới.
