# Năm hero: chỉ số, kích thước, kiểu đòn

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Cung cấp danh sách hero (mã, tên tạm, nhóm, kiểu đòn, chỉ số lv1, hộp) cho `feature-team-select` và cho việc sinh hero trong trận.
- Ra đòn theo kiểu đòn của hero, dùng luật đo khoảng cách, nhịp đánh và sát thương ở `feature-hero/hero-stats/hero-level-values`.
- Hiện hero bằng sprite 2D gen bằng AI, chân trên mặt đất (y=0), quay mặt về phía mục tiêu hoặc hướng đang đi.

## Thông số
Số liệu nằm ở các bảng trong `.excel/` (xem: `python Management/tools/gd_excel.py show ai/idea/feature-hero/hero-list/hero-roster-values`):
- `Heroes` (loại `list`): năm hero, mỗi hero một dòng: nhóm, máu, sát thương, giây mỗi đòn, tầm, tốc độ chạy và lí do, hộp, kiểu đòn, cỡ vùng đòn, sát thương/s. Cột `stage` là giai đoạn hero bắt đầu có.
- `AttackKinds` (loại `list`): ba kiểu đòn và đòn trúng ai; cột `attack_kind` của `Heroes` trỏ tới bảng này.
- `Effects` (loại `list`): hiệu ứng tối thiểu, khi nào hiện, khi nào mất.

Mọi giá trị là cấp 1, dự kiến, theo bộ số chung.

So với quái: Lính 1.5, Sói 3.0 u/s (`feature-enemy`). Chỉ Kiếm khách chạy nhanh hơn Sói; Cung thủ bằng Sói. Lí do chung: cận chiến máu 180-300, bắn xa 100-120; hero đánh vùng có sát thương/s mỗi quái thấp hơn hero một mục tiêu (luật `hero-tradeoff` vòng 3).

## Thông số theo giai đoạn
Bảng `StageParams` trong `.excel/` (loại `params`): danh sách hero có gì ở IDEA, PROTOTYPE, FULL. Hero và kiểu đòn nào có từ giai đoạn nào: cột `stage` của `Heroes` và `AttackKinds`.

## Trường hợp biên
- Đấu sĩ rìu chém: chỉ trúng quái phía trước (bên có mục tiêu); quái chồng lên chính Đấu sĩ rìu (khoảng cách 0) cũng tính là phía trước nếu ở cùng bên với mục tiêu theo tâm.
- Pháp sư nổ: tính vùng quanh tâm mục tiêu lúc ra đòn; mục tiêu bị đòn khác hạ cùng khung hình trước khi Pháp sư ra đòn thì Pháp sư chọn lại mục tiêu, không nổ vào chỗ trống.
- Đòn vùng hạ nhiều quái cùng lúc: mỗi quái cho tiền một lần (`feature-enemy/enemy-reward`).
- Đòn vùng không trúng thành, không trúng hero.
- Đội chưa đủ 3 hero (lỗi từ màn chọn): không vào trận; `feature-team-select` phải bảo đảm đủ 3.

## Đã tự quyết
- **IDEA dùng Hiệp sĩ, Đấu sĩ rìu, Cung thủ (2026-11-24, sửa lệch theo @Manager và @CodeLead).** Bản trước ghi Hiệp sĩ, Cung thủ, Pháp sư, trái với đội mặc định cố định ở IDEA (`feature-team-select/team-memory/memory-rules`, `feature-run/run-start/start-values`, cheat của `feature-team-select`: Hiệp sĩ, Đấu sĩ rìu, Cung thủ). IDEA không có màn chọn đội nên danh sách phải đúng bằng đội đó. Vẫn đủ chắn, đòn vùng (chém) và bắn xa một mục tiêu. Đã loại: đổi đội mặc định sang Pháp sư (phải sửa data ba node đã chốt và cheat có ảnh); thêm Đấu sĩ rìu thành 4 hero (Pháp sư không bao giờ vào trận ở IDEA).
- **Mỗi hero một tốc độ chạy riêng (2026-11-25, người yêu cầu "Các Hero sẽ có tốc độ di chuyển khác nhau", qua @Manager).** Bản trước có ba hero cùng 2.5u/s. Node này là nơi duy nhất giữ tốc độ chạy; nhánh nhắm và di chuyển của hero, `feature-balance` chỉ tham chiếu. Chọn theo độ nặng của vai: Hiệp sĩ 2.0, Đấu sĩ rìu 2.4, Pháp sư 2.6, Cung thủ 3.0, Kiếm khách 3.5. Có từ IDEA (ba hero của đội mặc định đã khác nhau). Đã loại: giữ chung 2.5u/s cho ba hero (trái yêu cầu); cho hero bắn xa chậm hơn mọi hero cận chiến (Cung thủ cần đổi chỗ bắn nhanh khi quái hai bên).
- **Mọi hero cùng hộp 1×1.5u.** Bộ số chung; khác nhau ở chỉ số, không ở kích thước.
- **Vùng chém đo từ mép trước hero.** Hợp với tầm đánh 1.3u: quái đứng sau mục tiêu tới 1.5u vẫn bị chém.

## Chưa rõ
- Kiếm khách 22.9 sát thương/s cao gần gấp đôi Hiệp sĩ trong khi máu chỉ kém 40%: có thể lấn vai. Giữ theo bộ số chung; đề xuất xem lại sau chơi thử (ví dụ 0.9s mỗi đòn, 17.8 sát thương/s).
