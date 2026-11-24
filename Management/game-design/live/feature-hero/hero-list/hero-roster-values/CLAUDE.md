# Năm hero: chỉ số, kích thước, kiểu đòn

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Cung cấp danh sách hero (mã, tên tạm, nhóm, kiểu đòn, chỉ số lv1, hộp) cho `feature-team-select` và cho việc sinh hero trong trận.
- Ra đòn theo kiểu đòn của hero, dùng luật đo khoảng cách, nhịp đánh và sát thương ở `feature-hero/hero-stats/hero-level-values`.
- Hiện hero bằng sprite 2D gen bằng AI, chân trên mặt đất (y=0), quay mặt về phía mục tiêu hoặc hướng đang đi.

## Thông số
| Hero | Nhóm | Máu | Sát thương | s mỗi đòn | Tầm (u) | Chạy (u/s) | Hộp (u) | Kiểu đòn | Sát thương/s |
|---|---|---|---|---|---|---|---|---|---|
| Hiệp sĩ | Cận chiến | 300 | 12 | 1.0 | 1.2 | 2.5 | 1×1.5 | Một mục tiêu | 12.0 |
| Đấu sĩ rìu | Cận chiến | 240 | 14 | 1.4 | 1.3 | 2.5 | 1×1.5 | Chém 1.5u phía trước | 10.0 mỗi quái |
| Kiếm khách | Cận chiến | 180 | 16 | 0.7 | 1.2 | 3.5 | 1×1.5 | Một mục tiêu | 22.9 |
| Cung thủ | Bắn xa | 120 | 14 | 0.8 | 7 | 2.5 | 1×1.5 | Một mục tiêu | 17.5 |
| Pháp sư | Bắn xa | 100 | 18 | 1.6 | 6 | 2.2 | 1×1.5 | Nổ bán kính 1.5u | 11.25 mỗi quái |

Mọi giá trị là cấp 1, dự kiến, theo bộ số chung. Lí do chung: cận chiến máu 180-300, bắn xa 100-120; hero đánh vùng có sát thương/s mỗi quái thấp hơn hero một mục tiêu (luật `hero-tradeoff` vòng 3).

Kiểu đòn:
| Kiểu | Trúng ai |
|---|---|
| Một mục tiêu | Chỉ mục tiêu |
| Chém phía trước | Mục tiêu và mọi quái có hộp chạm đoạn dài 1.5u bắt đầu từ mép trước của hero, về phía mục tiêu |
| Nổ quanh mục tiêu | Mục tiêu và mọi quái có mép hộp cách tâm mục tiêu không quá 1.5u, cả hai phía của mục tiêu |

Hiệu ứng (mức tối thiểu):
| Hiệu ứng | Xuất hiện | Biến mất |
|---|---|---|
| Nhún khi đi (0.1u, chu kì 0.4s) | bắt đầu đi | dừng đi |
| Nhích khi ra đòn (0.2u về phía mục tiêu, 0.15s) | mỗi đòn | sau 0.15s |
| Nháy khi bị đánh | mỗi đòn trúng | sau 0.1s, đòn mới đặt lại |
| Vệt chém (Đấu sĩ rìu) | mỗi đòn, phủ đoạn 1.5u | sau 0.2s |
| Vòng nổ (Pháp sư) | lúc trúng, bán kính 1.5u quanh mục tiêu | sau 0.3s |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Số hero trong danh sách | 3 (Hiệp sĩ, Đấu sĩ rìu, Cung thủ) | 5 | 5 |
| Số chỗ trong đội | 3 (cả 3 hero, không cần chọn; đúng đội mặc định của `feature-team-select/team-memory/memory-rules`) | 3 chọn từ 5 | 3 chọn từ 5 |
| Kiểu đòn có | Một mục tiêu, Chém phía trước | cả ba | cả ba |
| Chỉ số | như bảng | như bảng | theo số đã chỉnh sau chơi thử |
| Hình | 1 sprite mỗi hero, anim bằng move/scale/nháy | như IDEA, thêm sprite khi đánh | đủ anim đi, đánh, gục; âm thanh |

## Trường hợp biên
- Đấu sĩ rìu chém: chỉ trúng quái phía trước (bên có mục tiêu); quái chồng lên chính Đấu sĩ rìu (khoảng cách 0) cũng tính là phía trước nếu ở cùng bên với mục tiêu theo tâm.
- Pháp sư nổ: tính vùng quanh tâm mục tiêu lúc ra đòn; mục tiêu bị đòn khác hạ cùng khung hình trước khi Pháp sư ra đòn thì Pháp sư chọn lại mục tiêu, không nổ vào chỗ trống.
- Đòn vùng hạ nhiều quái cùng lúc: mỗi quái cho tiền một lần (`feature-enemy/enemy-reward`).
- Đòn vùng không trúng thành, không trúng hero.
- Đội chưa đủ 3 hero (lỗi từ màn chọn): không vào trận; `feature-team-select` phải bảo đảm đủ 3.

## Đã tự quyết
- **IDEA dùng Hiệp sĩ, Đấu sĩ rìu, Cung thủ (2026-11-24, sửa lệch theo @Manager và @CodeLead).** Bản trước ghi Hiệp sĩ, Cung thủ, Pháp sư, trái với đội mặc định cố định ở IDEA (`feature-team-select/team-memory/memory-rules`, `feature-run/run-start/start-values`, cheat của `feature-team-select`: Hiệp sĩ, Đấu sĩ rìu, Cung thủ). IDEA không có màn chọn đội nên danh sách phải đúng bằng đội đó. Vẫn đủ chắn, đòn vùng (chém) và bắn xa một mục tiêu. Đã loại: đổi đội mặc định sang Pháp sư (phải sửa data ba node đã chốt và cheat có ảnh); thêm Đấu sĩ rìu thành 4 hero (Pháp sư không bao giờ vào trận ở IDEA).
- **Mọi hero cùng hộp 1×1.5u.** Bộ số chung; khác nhau ở chỉ số, không ở kích thước.
- **Vùng chém đo từ mép trước hero.** Hợp với tầm đánh 1.3u: quái đứng sau mục tiêu tới 1.5u vẫn bị chém.

## Chưa rõ
- Kiếm khách 22.9 sát thương/s cao gần gấp đôi Hiệp sĩ trong khi máu chỉ kém 40%: có thể lấn vai. Giữ theo bộ số chung; đề xuất xem lại sau chơi thử (ví dụ 0.9s mỗi đòn, 17.8 sát thương/s).
