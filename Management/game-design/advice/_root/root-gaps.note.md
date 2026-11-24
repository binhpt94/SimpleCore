# Nhận xét của @DesignAdvisor về root-gaps

Nhận xét đối chiếu với cây đã chốt. Không phải góp ý của GPT.

## Tóm tắt theo loại
- **Đổi quyết định:** chỉ có `ap-luc-doi-canh`, và GPT chỉ đề xuất nó khi cây còn thiếu ý này.
- **Chỉ diễn giải trong `CLAUDE.md` của gốc:** `diem-khac-biet`, `hero-co-diem-yeu`, `diem-theo-dot`, `choi-lai-tu-dau`, `mot-ban-do`, `pho-thong-de-doc`. Những góp ý này không đổi hash của gốc.
- **Để vòng sau lo:** `chi-tien-co-danh-doi`, `hoi-chieu-va-thoi-diem`.
- Không góp ý nào đi ngược ba ý người đã chấm lại: `hero-auto-spawn`, `castle-role`, `upgrade-between-waves`.

## Góp ý trái với thứ đã chốt
- **`ap-luc-doi-canh`:** cây đã chốt đi theo hướng ngược lại, không phải chỉ thiếu ý.
  - Ở `live/feature-wave/spawning`, ý `spawn-both-sides` chốt "Đợt nào cũng có quái ở cả hai bên, chia gần đều". Phương án "lệch bên theo đợt" đã bị loại, lí do: "người chơi không phải đoán bên nào nguy hơn".
  - Ở `live/feature-wave/spawning/spawn-order`, ý `side-run` chốt "không quá 3 con liên tiếp ra cùng một bên".
  - Hai quyết định này do AI tự quyết (mục "Đã tự quyết" của node), không phải người loại. Đề xuất lại không phạm luật "ý đã bị người loại".
  - Nếu dùng góp ý này, chỗ phải sửa chủ yếu là `feature-wave/spawning` (vòng 3) và các node bên dưới, không nhất thiết là `data.md` của gốc. Ở gốc có thể chỉ cần thêm câu vào "Cảm giác chính".
- **`diem-khac-biet` và `pho-thong-de-doc`** kể chuyện "cứu cánh yếu", "nhận ra cánh nào cần cứu". Với luật chia gần đều hai bên, một cánh chỉ yếu đi vì thứ tự xáo ngẫu nhiên hoặc vì hero gục. Câu kể này có thể hứa nhiều hơn những gì cây đang cho. Nên xét cùng lúc với `ap-luc-doi-canh`.

## Góp ý đã có sẵn trong cây
- **`choi-lai-tu-dau`** (giữ đội để thử lại): `live/feature-team-select/team-memory` đã nhớ đội của lượt trước. Câu trong vòng lặp là "chọn đội khác", nên sửa thành "giữ đội hoặc đổi đội" là khớp với cây. Chỉ sửa câu chữ.
- **`diem-theo-dot`** (điểm theo số đợt đã vượt): khớp với `live/feature-run/run-score`.
