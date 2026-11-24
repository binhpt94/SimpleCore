# Vòng 2, câu chung: cả bộ feature có trùng việc, thiếu feature hay ranh giới mơ hồ không

Người nhờ: @GameDesigner, mode Full Auto. Lần hỏi `r2-1` trước đã hỏi từng feature riêng; đây là câu chung cho cả bộ feature vòng 2 (bản đồ feature được gửi kèm).

## Câu hỏi
Nhìn cả bộ feature vòng 2 như một tổng thể:
1. **Trùng việc:** hai feature nào cùng giữ một việc (cùng giữ một dữ liệu, cùng quyết một luật, cùng hiện một thứ)? Nói việc đó nên thuộc feature nào.
2. **Thiếu feature:** vòng lặp chơi của vòng 1 có phần nào chưa feature nào giữ?
3. **Ranh giới mơ hồ:** chỗ nào hai feature chạm nhau mà chưa rõ bên nào làm gì?

Mỗi góp ý ghi rõ nó thuộc feature nào. Chỗ đã rõ thì không viết gì.

## Yêu cầu riêng
- Không đề xuất ngược ba ý người đã chấm lại ở gốc: đội hero cố định chọn trước mỗi lượt; thành tự đánh và có kĩ năng hồi chiêu người chơi bấm; nâng cấp ngay lúc đang đánh.
- Giữ luật chia feature vòng 2: mỗi popup một feature; HUD tách cụm lớn (`feature-hud`, `feature-hud-upgrade`); balance riêng; ba feature hạ tầng mặc định (`feature-scene-flow`, `feature-session-scene`, `feature-popup`). Không gộp feature, không đổi tên key đã chốt.
- Không con số, không dữ liệu theo giai đoạn (bản đầu, MVP, làm trước / để sau).
- Cây đã tới vòng 4 và đã chốt. Chỉ đề xuất điều thật sự là quyết định vòng 2; luật, định nghĩa, con số để nhánh dưới.
- Không lặp góp ý đã có ở các lần hỏi `r2-1` của từng feature.

## Bên hỏi muốn
Danh sách góp ý về cách chia feature, mỗi góp ý một quyết định, ghi feature đích, để đưa vào đúng feature đó.
