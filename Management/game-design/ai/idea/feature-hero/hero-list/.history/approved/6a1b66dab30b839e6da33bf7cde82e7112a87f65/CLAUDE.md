# Danh sách hero

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Danh sách có NHIỀU hero hơn số chỗ trong MỘT đội.
- MỘT hero có MỘT cách đánh, không đổi trong trận.
- Đòn một mục tiêu: MỘT hero trúng MỘT quái mỗi đòn. Đòn vùng: MỘT hero trúng NHIỀU quái mỗi đòn.
- Chọn hero nào vào đội: `feature-team-select`. Giá trị chỉ số cụ thể: `feature-balance`.

## Định nghĩa
- `glossary#fighting-style`

## Danh sách
| Hero | Nhóm | Kiểu đòn | Vai so với các hero khác |
|---|---|---|---|
| Hiệp sĩ | Cận chiến | Một mục tiêu | Máu nhiều nhất, sức đánh thấp; đứng chắn |
| Đấu sĩ rìu | Cận chiến | Chém vùng trước mặt | Máu khá, đánh chậm; dọn quái đông |
| Kiếm khách | Cận chiến | Một mục tiêu | Máu ít hơn Hiệp sĩ, đánh nhanh, đi nhanh; hạ nhanh từng con |
| Cung thủ | Bắn xa | Một mục tiêu | Máu ít, tầm xa nhất, đánh nhanh; hạ quái trâu và quái bắn xa |
| Pháp sư | Bắn xa | Nổ vùng quanh mục tiêu | Máu ít nhất, đánh chậm nhất; dọn quái dồn cụm |

## Luật cơ bản
- Hero nào cũng tự đánh theo cùng một luật nhắm (`feature-hero/hero-targeting`); khác nhau chỉ ở chỉ số và kiểu đòn.
- Hero bắn xa vẫn bị quái đánh nếu quái tới gần.
- Hero cận chiến có máu nhiều hơn hero bắn xa.
- Hero có đòn vùng đánh chậm hơn hoặc trừ ít máu hơn mỗi quái so với hero một mục tiêu.
- Hero không có kĩ năng chủ động; kĩ năng người chơi bấm là của thành (`feature-castle/castle-skills`).

## Đã tự quyết
- **Năm hero, ba cận chiến và hai bắn xa.** Đủ để danh sách nhiều hơn số chỗ trong đội và mỗi kiểu đòn có đại diện. Cận chiến nhiều hơn vì nhóm này gánh vai chắn. Đã loại: hero hồi máu (thêm luật nhắm đồng đội), hero triệu hồi (gần với "thành tự sinh hero" người đã loại).
- **Ba kiểu đòn.** Quyết định chung cho phép cách đánh khác "một mục tiêu"; hai kiểu vùng cho cận chiến và bắn xa mỗi bên một. Đã loại: đòn xuyên hàng (trùng vai Pháp sư), đòn nảy.
- **Đòn vùng bị đổi bằng tốc độ hoặc sức đánh thấp hơn.** Giữ cân bằng ở mức định nghĩa, số cụ thể để vòng 4.
- **Tên hero là tên tạm theo vai.** Đổi tên không đổi luật.

## Chưa rõ
- Không có điểm chặn. Số chỗ trong đội (ít hơn năm) quyết ở vòng 4, cùng `feature-team-select`.
