# Thứ tự và cách thả quái

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
1. Đầu đợt nhận từ `feature-wave/difficulty-scaling/wave-formula`: số quái N, nhịp thả, hệ số chỉ số, số con từng loại.
2. Lập danh sách N mục (loại, bên, lúc ra):
   - Xáo thứ tự các loại không phải Trùm.
   - Nếu đợt có Trùm: đặt Trùm ở vị trí cuối (thứ N).
   - Gán bên: đúng N/2 trái, N/2 phải, ngẫu nhiên, không có chuỗi quá 3 mục liên tiếp cùng bên.
   - Lúc ra dự kiến của mục thứ i (đếm từ 1) là (i − 1) × nhịp thả, tính từ đầu đợt, cộng thời gian bị hoãn.
3. Thả lần lượt theo danh sách. Mỗi con mới ra mang chỉ số đã nhân hệ số của đợt đang đánh.
4. Báo "đã ra hết" cho `feature-wave/wave-clear/clear-rules` khi mục cuối đã ra.

## Thông số
| Thông số | Giá trị (dự kiến) | Đơn vị | Lí do |
|---|---|---|---|
| Điểm ra bên trái / phải | x = −17 / +17, trên mặt đất của làn | u | Ngoài mép màn hình (±16u) 1u, quái đi vào khung hình |
| Số quái mỗi bên | N/2 | con | Kẹp đều hai phía |
| Vị trí Trùm | N (con cuối) | thứ tự | Cuối đợt; luật này do node này giữ |
| Chuỗi cùng bên tối đa | 3 | con | Không bỏ trống một bên lâu |
| Con đầu tiên ra | 0 | s từ đầu đợt | Đợt bắt đầu là thấy quái ngay |
| Giới hạn quái sống | 40 | con | Màn hình điện thoại vẫn đọc được |
| Thời gian thả cả đợt (không hoãn) | (N − 1) × nhịp | s | Đợt 1: 7 s, đợt 5: 12 s, đợt 10: 13.75 s, đợt 20: 13.5 s |

"Quái sống" là quái đã ra và chưa bị hạ, ở bất kì vị trí nào (kể cả còn ngoài màn hình).

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Điểm ra | x = ±17u | x = ±17u | x = ±17u |
| Chia bên | N/2 mỗi bên | N/2 mỗi bên | N/2 mỗi bên |
| Chuỗi cùng bên tối đa | 3 con | 3 con | chỉnh sau chơi thử |
| Vị trí Trùm | không có Trùm | con cuối (N) | chỉnh sau chơi thử |
| Giới hạn quái sống | 40 con | 40 con | chỉnh theo hiệu năng máy thật |
| Xáo thứ tự, bên | có | có | có |

## Trường hợp biên
- **Đủ 40 quái sống đúng lúc tới nhịp:** con đó chưa ra; đồng hồ nhịp dừng. Một con bị hạ (còn 39) thì con đó ra ngay, đồng hồ đếm lại một nhịp đầy đủ cho con kế tiếp.
- **Thành bị phá giữa lúc thả hoặc lúc đang hoãn:** ngừng thả ngay; các mục chưa ra bị bỏ; quái đang sống đứng theo luật kết thúc của `feature-run`.
- **Hai con ra cùng chỗ** (con trước còn đứng ở x = ±17u): vẫn ra, cho chồng lên nhau; `feature-enemy` lo việc tách khi đi.
- **Không xếp được bên để chuỗi ≤ 3:** với N/2 trái, N/2 phải luôn xếp được; nếu bước ngẫu nhiên ra chuỗi 4 thì đổi bên mục thứ 4 với mục gần nhất phía sau thuộc bên kia.
- **Trùm và giới hạn 40:** Trùm cũng phải chờ dưới 40 như mọi con; vẫn là con cuối của đợt.
- **Mua nâng cấp, dùng kĩ năng thành giữa lúc thả:** không ảnh hưởng danh sách hay nhịp.
- **Đợt 1:** bắt đầu ngay lúc `feature-run` báo trận bắt đầu, con đầu ra ở 0 s; quãng đi khoảng 9.3 s của Lính (`feature-enemy/enemy-behavior/enemy-move-values`) đủ để đội hero ra khỏi thành.

## Đã tự quyết
- **Trùm là con cuối của đợt, vị trí cố định (2026-11-23, sửa lệch).** Bản cũ đặt Trùm ở giữa đợt, trái với `feature-enemy/enemy-list/enemy-type-values` (ý `boss-single`: ra cuối đợt). Theo node của enemy: người chơi dọn bớt quái thường rồi mới gặp Trùm, đợt dồn lên một đỉnh rõ. Mức IDEA không có Trùm (theo node enemy). Đã loại: Trùm giữa đợt, Trùm ngẫu nhiên.
- **Luật "Trùm ra cuối đợt" và "Trùm chờ dưới 40" thuộc node này (2026-11-25).** Node loại quái của `feature-enemy` đã bỏ bản chép của hai luật này (theo hướng A người chọn); node đó chỉ còn hộp, kiểu đánh và việc Trùm không có đòn riêng. Không đổi luật nào ở đây.
- **Lính đi khoảng 9.3 s tới mép thành (2026-11-23).** Lấy theo `feature-enemy/enemy-behavior/enemy-move-values`; bản cũ ghi khoảng 10 s.
- **Chuỗi cùng bên tối đa 3.** Ý `spawn-both-sides` cần cả hai bên luôn có áp lực. Đã loại: xen kẽ trái phải tuyệt đối (dễ đoán), ngẫu nhiên hoàn toàn (có lúc 6 con cùng bên).
- **Hoãn khi đủ 40 rồi thả ngay khi bớt.** Không mất quái nào của đợt, đợt vẫn đủ số. Đã loại: bỏ con thừa (đợt nhẹ đi khi đội yếu, thưởng người chơi kém).
- **Xáo theo seed của lượt (2026-11-25, người yêu cầu ngẫu nhiên tất định theo seed).** Ý `seeded-shuffle`: hai bước "xáo thứ tự" và "gán bên" ở mục "Làm gì" lấy số từ dòng ngẫu nhiên của đợt quái (`feature-sim-clock/seeded-random`), không dùng bộ ngẫu nhiên của engine. Không đổi luật xáo nào. Đã loại: ghi vào node thả quái vòng 3 (luật chung ở đó không đổi, chỉ nguồn số đổi).
- **Không có đếm ngược trước đợt 1.** Node `feature-wave/wave-gap` chỉ có lúc thở sau dọn sạch; quãng đi của quái đã đủ làm lúc chuẩn bị.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ trường hợp biên tạm dừng giữa lúc thả.

## Chưa rõ
- Giới hạn 40 có đủ nhẹ cho máy yếu không: cần đo trên máy thật.
