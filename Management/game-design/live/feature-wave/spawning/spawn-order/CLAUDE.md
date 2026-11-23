# Thứ tự và cách thả quái

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
1. Đầu đợt nhận từ `feature-wave/difficulty-scaling/wave-formula`: số quái N, nhịp thả, hệ số chỉ số, số con từng loại.
2. Lập danh sách N mục (loại, bên, lúc ra):
   - Xáo thứ tự các loại không phải Trùm.
   - Nếu đợt có Trùm: chèn Trùm vào vị trí N/2 + 1.
   - Gán bên: đúng N/2 trái, N/2 phải, ngẫu nhiên, không có chuỗi quá 3 mục liên tiếp cùng bên.
   - Lúc ra dự kiến của mục thứ i (đếm từ 1) là (i − 1) × nhịp thả, tính từ đầu đợt, cộng thời gian bị hoãn.
3. Thả lần lượt theo danh sách. Mỗi con mới ra mang chỉ số đã nhân hệ số của đợt đang đánh.
4. Báo "đã ra hết" cho `feature-wave/wave-clear/clear-rules` khi mục cuối đã ra.

## Thông số
| Thông số | Giá trị (dự kiến) | Đơn vị | Lí do |
|---|---|---|---|
| Điểm ra bên trái / phải | x = −17 / +17, trên mặt đất của làn | u | Ngoài mép màn hình (±16u) 1u, quái đi vào khung hình |
| Số quái mỗi bên | N/2 | con | Kẹp đều hai phía |
| Vị trí Trùm | N/2 + 1 | thứ tự | Giữa đợt |
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
| Vị trí Trùm | N/2 + 1 | N/2 + 1 | chỉnh sau chơi thử |
| Giới hạn quái sống | 40 con | 40 con | chỉnh theo hiệu năng máy thật |
| Xáo thứ tự, bên | có | có | có |

## Trường hợp biên
- **Đủ 40 quái sống đúng lúc tới nhịp:** con đó chưa ra; đồng hồ nhịp dừng. Một con bị hạ (còn 39) thì con đó ra ngay, đồng hồ đếm lại một nhịp đầy đủ cho con kế tiếp.
- **Thành bị phá giữa lúc thả hoặc lúc đang hoãn:** ngừng thả ngay; các mục chưa ra bị bỏ; quái đang sống đứng theo luật kết thúc của `feature-run`.
- **Tạm dừng giữa lúc thả:** đồng hồ nhịp đứng yên; bỏ tạm dừng thì đếm tiếp phần còn lại của nhịp, không thả bù.
- **Hai con ra cùng chỗ** (con trước còn đứng ở x = ±17u): vẫn ra, cho chồng lên nhau; `feature-enemy` lo việc tách khi đi.
- **Không xếp được bên để chuỗi ≤ 3:** với N/2 trái, N/2 phải luôn xếp được; nếu bước ngẫu nhiên ra chuỗi 4 thì đổi bên mục thứ 4 với mục gần nhất phía sau thuộc bên kia.
- **Trùm và giới hạn 40:** Trùm cũng phải chờ dưới 40 như mọi con; tới lượt thì ra, không chen trước.
- **Mua nâng cấp, dùng kĩ năng thành giữa lúc thả:** không ảnh hưởng danh sách hay nhịp.
- **Đợt 1:** bắt đầu ngay lúc `feature-run` báo trận bắt đầu, con đầu ra ở 0 s; quãng đi khoảng 10 s của Lính đủ để đội hero ra khỏi thành.

## Đã tự quyết
- **Trùm ở giữa đợt, vị trí cố định.** Kiểm được, không phụ thuộc ngẫu nhiên. Đã loại: Trùm ra cuối đợt (dồn với quái tồn), Trùm ngẫu nhiên.
- **Chuỗi cùng bên tối đa 3.** Ý `spawn-both-sides` cần cả hai bên luôn có áp lực. Đã loại: xen kẽ trái phải tuyệt đối (dễ đoán), ngẫu nhiên hoàn toàn (có lúc 6 con cùng bên).
- **Hoãn khi đủ 40 rồi thả ngay khi bớt.** Không mất quái nào của đợt, đợt vẫn đủ số. Đã loại: bỏ con thừa (đợt nhẹ đi khi đội yếu, thưởng người chơi kém).
- **Không có đếm ngược trước đợt 1.** Node `feature-wave/wave-gap` chỉ có lúc thở sau dọn sạch; quãng đi của quái đã đủ làm lúc chuẩn bị.

## Chưa rõ
- Giới hạn 40 có đủ nhẹ cho máy yếu không: cần đo trên máy thật.
