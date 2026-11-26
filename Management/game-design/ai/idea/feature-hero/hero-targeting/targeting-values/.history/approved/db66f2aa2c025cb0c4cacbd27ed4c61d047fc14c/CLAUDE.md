# Thông số nhắm và di chuyển của hero

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
Mỗi khung hình, với mỗi hero đang đứng (không gục, không trong 0.3s hồi sinh):
1. Trạng thái **Đánh**: mục tiêu còn sống và khoảng cách ≤ tầm thì giữ mục tiêu, ra đòn theo nhịp; không thì sang bước 2.
2. Chọn mục tiêu: quái còn sống, tâm trong [-16u, +16u], khoảng cách nhỏ nhất.
3. Có mục tiêu: khoảng cách ≤ tầm thì sang **Đánh** (đứng yên); không thì **Đi tới** mục tiêu với tốc độ chạy, không vượt [-15u, +15u].
4. Không có mục tiêu: **Đứng chờ**: đi về điểm xuất phát của chỗ mình rồi đứng yên.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Vùng quái được nhắm | tâm x từ -16 tới +16 | u | Bằng màn hình; không đánh thứ người chơi chưa thấy |
| Vùng hero đi được | tâm x từ -15 tới +15 | u | Bộ số chung; hero luôn nằm trọn trong màn |
| Tốc độ chạy | theo từng hero, mỗi hero một mức (2.0-3.5) | u/s | Giữ ở `feature-hero/hero-list/hero-roster-values`; node này chỉ dùng |
| Điểm đứng chờ | -1.5 / +1.5 / -1.5 theo chỗ 1 / 2 / 3 | x (u) | Trùng điểm xuất phát |
| Chọn khi bằng khoảng cách | gần thành hơn, rồi ra trước | | Ưu tiên con nguy hơn cho thành |
| Hero chắn nhau | không | | Luật vòng 3; hero đi xuyên qua nhau |

Thời gian dự kiến: Hiệp sĩ (2.0u/s) từ x=1.5u đi tới quái mới vào ở x=16u (mép trong 15.5u) mất khoảng (15.5-1.2-2)/2.0 ≈ 6.2s; quái cũng đi vào nên gặp sớm hơn, khoảng 3.5s. Kiếm khách (3.5u/s) cùng quãng đó mất khoảng 3.5s, gặp quái sau khoảng 2.5s. Trong đội cùng lượt, hero nhanh tới quái trước và đứng chắn một mình một lúc.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Luật nhắm | như bảng | như bảng | như bảng |
| Vùng nhắm / vùng đi | ±16u / ±15u | ±16u / ±15u | chỉnh nếu đổi khung màn |
| Quay mặt | lật sprite theo hướng đi hoặc hướng mục tiêu | như IDEA | như IDEA, có anim quay |

## Trường hợp biên
- Mục tiêu đang được đánh bị đòn khác hạ: khung hình sau chọn lại ngay; nhịp đòn không đặt lại.
- Mục tiêu bị Băng giá làm chậm: không đổi gì với cách nhắm.
- Cùng lúc có quái hai bên cách bằng nhau: chọn con gần thành hơn; nếu vẫn bằng thì con ra trước.
- Hero đứng chồng lên quái (khoảng cách 0): đánh được, không phải lùi lại.
- Mục tiêu ở ngoài ±15u và ngoài tầm khi hero đứng ở mốc: hero đứng yên ở mốc, vẫn chọn lại mỗi khung hình; có quái khác gần hơn thì đổi.
- Quái mới vào màn khi hero đang về điểm đứng chờ: chọn quái đó ngay, không cần về tới nơi.
- Giữa hai đợt còn quái sót: vẫn nhắm và đánh như thường.
- Tạm dừng: hero đứng yên, giữ trạng thái và mục tiêu.

## Đã tự quyết
- **Tốc độ chạy khác nhau giữa các hero (2026-11-25, theo yêu cầu của người).** Mỗi hero chạy theo tốc độ riêng ở node danh sách hero; luật nhắm và đi không đổi. Đã rà ví dụ thời gian đi theo số mới.
- **Không nhắm quái ngoài màn (tâm quá ±16u).** Tránh hero cận chiến đánh quái người chơi không thấy ở x=±17u. Đã loại: nhắm mọi quái đã sinh.
- **Bằng khoảng cách thì ưu tiên quái gần thành.** Đó là con nguy cho thành hơn.
- **Đứng chờ ở điểm xuất phát, không ở tâm thành.** Hai bên đều có hero chờ sẵn. Đã loại: đứng ở x=0 (cả đội chồng một chỗ).

## Chưa rõ
- Không có điểm chặn.
