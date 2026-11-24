# Phản biện root-gaps (lượt 2, lượt duy nhất)

Người nhờ: @GameDesigner, mode Full Auto. Node: gốc (`ai/idea/data.md`, `ai/idea/CLAUDE.md`, đã đính kèm).
Lần hỏi trước: `advice/_root/root-gaps.md` (9 góp ý) và nhận xét `advice/_root/root-gaps.note.md`.
@GameDesigner định bỏ hoặc sửa ba góp ý dưới đây. Với từng góp ý: bạn còn giữ ý kiến không, vì sao. Nếu đồng ý với cách sửa thì nói rõ là đồng ý; nếu cách sửa làm mất điều quan trọng thì nêu điều đó.

## 1. `ap-luc-doi-canh` | Bổ sung nguyên tắc áp lực đổi cánh và đổi nhịp
**@GameDesigner định BỎ ở node gốc** (không đổi `data.md` của gốc). Lí do:
- (a) `live/feature-wave/spawning` đã chốt `spawn-both-sides`: mỗi đợt chia gần đều hai bên. Phương án "lệch bên theo đợt" đã bị loại để người chơi không phải đoán bên nào nguy hơn. `live/feature-wave/spawning/spawn-order` chốt `side-run`: không quá 3 con liên tiếp ra cùng một bên. Đây là quyết định của vòng 3-4, không phải của gốc.
- (b) Ý `enemy-two-sides` ở gốc ("quái kéo tới theo đợt từ mép trái và mép phải") đã đủ rộng để chứa cả chia đều lẫn đổi cánh. Đổi gốc chỉ làm cả cây (khoảng 120 node) phải làm lại mà không đổi quyết định nào ở gốc.
- (c) Cần đổi nhịp thì xét riêng ở `feature-wave/spawning`. @GameDesigner định ghi vào mục "Chưa rõ" của gốc thành câu hỏi mở cho nhánh đó.

Hỏi:
- Với ràng buộc này, còn lí do nào buộc phải ghi ở gốc không?
- Với các công cụ hiện có (kĩ năng thành có hồi chiêu; nâng cấp từng hero hoặc nâng cấp thành), chia đều hai bên có làm mất hẳn quyết định theo cánh không? Hay đã đủ quyết định khác (thời điểm bấm kĩ năng, chọn đích nâng cấp)?

## 2. `diem-khac-biet` | Kể một lần phải cứu cánh yếu
**@GameDesigner định SỬA:** vẫn thêm một ví dụ vào "Kể trong một phút". Nhưng ví dụ không hứa "một cánh sắp vỡ" như tình huống thường gặp, vì với luật chia gần đều, một cánh chỉ yếu đi khi hero gục hoặc khi thứ tự xáo ngẫu nhiên dồn về một bên. Ví dụ mới xoay quanh: một đợt dồn đông tới; chọn bấm kĩ năng thành ngay hay giữ lại; tiêu tiền vào hero đang gục dần hay vào thành.

Hỏi: ví dụ sửa như vậy còn đủ để người lạ thấy game khác game thủ thành cùng loại ở đâu không?

## 3. `pho-thong-de-doc` | Người chơi đọc được nguy cơ trước khi quyết định
**@GameDesigner định SỬA:** giữ ý "người chơi nhận ra nguy cơ và hiểu món vừa mua giúp gì ngay". Bỏ "nhận ra cánh nào cần cứu" (cùng lí do ở mục 1). Thay bằng "nhận ra lúc nào thành đang nguy và hero nào đang gục".

Hỏi: sửa như vậy có mất điều gì quan trọng không?

## Ràng buộc (như lần trước)
- Không đề xuất ngược ba ý người đã chấm lại: `hero-auto-spawn` (đội hero cố định chọn trước mỗi lượt), `castle-role` (thành tự đánh, kĩ năng hồi chiêu người chơi bấm), `upgrade-between-waves` (nâng cấp ngay lúc đang đánh).
- Màn ngang 1920x1080, game 2D nhìn ngang, style `ui-flat` và `world-cartoon`.
- Vòng 1 không có con số, danh sách feature hay định nghĩa. Không ghi dữ liệu theo giai đoạn.
- Cả cây đã chốt tới vòng 4; đổi `data.md` của gốc làm mọi node bên dưới phải làm lại.

## Cách trả lời
- Mỗi góp ý ở trên một mục rời `### <id góp ý> | <tiêu đề>`, giữ đúng id cũ (`ap-luc-doi-canh`, `diem-khac-biet`, `pho-thong-de-doc`).
- Dòng đầu mỗi mục: **Giữ ý kiến** / **Đồng ý bỏ** / **Đồng ý sửa** / **Đồng ý sửa, kèm điều kiện**; sau đó là lí do ngắn.
- Không thêm góp ý mới ngoài ba mục này.
- Câu ngắn, tiếng Việt.
