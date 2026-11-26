# Giá và luật mua

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Mỗi mục (`feature-upgrade/upgrade-list/item-set`) có giá cấp sau tính từ cấp hiện tại L theo công thức của loại mục.
- Nhận một yêu cầu mua (một chạm từ `feature-hud-upgrade`) cho một mục:
  1. Trận không chạy (đang tạm dừng, hoặc lượt đã kết thúc): từ chối, không đổi gì.
  2. Xin ví trừ giá cấp sau (`feature-wallet/balance-spend/wallet-rules`).
  3. Ví trả "không đủ": từ chối, không đổi gì.
  4. Ví trả "được": mục lên L + 1, báo bên giữ chỉ số (`feature-upgrade/upgrade-effect/effect-numbers`), tính giá cấp sau mới.
- Xử lí yêu cầu lần lượt theo thứ tự tới; yêu cầu sau luôn thấy số dư và giá đã đổi của yêu cầu trước.
- Đưa cho thanh nâng cấp, với mỗi mục: cấp hiện tại, giá cấp sau, mua được hay không. Gửi lại cho mọi mục mỗi khi số dư đổi, một cấp đổi, hoặc trạng thái tạm dừng/kết thúc đổi.
- **Mua được** = trận đang chạy (không tạm dừng, chưa kết thúc) và số dư >= giá cấp sau.

## Thông số
Công thức (giá trị dự kiến; nằm trong asset cấu hình của `feature-balance`, không viết cứng):

| Mục | Giá cấp L → L+1 | Lí do |
|---|---|---|
| Cấp hero (mỗi hero riêng) | làm tròn lên 30 × 1,35^(L − 1) | Rẻ nhất, tăng chậm nhất: hero là trọng tâm |
| Máu thành | làm tròn lên 40 × 1,4^(L − 1) | Đắt hơn hero một chút |
| Sức đánh thành | làm tròn lên 40 × 1,4^(L − 1) | Cùng giá máu để chọn theo nhu cầu, không theo giá |
| Kĩ năng thành | làm tròn lên 60 × 1,5^(L − 1) | Một cấp tăng cả ba kĩ năng nên đắt và tăng nhanh nhất |

Bảng giá (tiền):

| Lên cấp | Hero | Máu / Sức đánh thành | Kĩ năng thành |
|---|---|---|---|
| 1 → 2 | 30 | 40 | 60 |
| 2 → 3 | 41 | 56 | 90 |
| 3 → 4 | 55 | 79 | 135 |
| 4 → 5 | 74 | 110 | 203 |
| 5 → 6 | 100 | 154 | 304 |
| 6 → 7 | 135 | 216 | 456 |
| 7 → 8 | 182 | 302 | 684 |
| 8 → 9 | 246 | 422 | 1026 |
| 9 → 10 | 331 | 591 | 1538 |
| Tổng từ cấp 1 lên cấp 10 | 1194 | 1970 | 4496 |

Cách tính: tính số thực, làm tròn tới 6 chữ số thập phân để bỏ sai số dấu phẩy động, rồi làm tròn lên số nguyên (ví dụ 40 × 1,4 tính ra 56,00000000000001 phải ra 56, không ra 57).

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Giá gốc hero / máu, sức đánh / kĩ năng | 30 / 40 / 60 | như IDEA, chỉnh theo chơi thử | Chốt theo `feature-balance` |
| Hệ số tăng giá hero / máu, sức đánh / kĩ năng | 1,35 / 1,4 / 1,5 | như IDEA, chỉnh theo chơi thử | Chốt |
| Cấp tối đa | không | không | không |
| Mua lúc nghỉ giữa đợt | được | được | được |

## Trường hợp biên
- **Bấm mua hai lần rất nhanh, chỉ đủ tiền một lần**: hai yêu cầu xử lí lần lượt; lần đầu thành công, lần sau xin ví trừ giá mới (cao hơn) với số dư đã giảm, ví trả "không đủ", không đổi gì.
- **Số dư đúng bằng giá**: mua được, số dư về 0.
- **Hero đang gục**: mua được như thường; chỉ cấp đổi (`effect-numbers`).
- **Thua đúng lúc đang mua**: từ khung hình thành về 0 máu, lượt đã kết thúc, mọi yêu cầu mua sau đó bị từ chối. Yêu cầu tới trước khung hình đó đã xử lí xong; mua máu thành không cứu được thành đã về 0 máu.
- **Đang tạm dừng**: từ chối, kể cả khi đủ tiền; thanh nâng cấp hiện mọi nút ở trạng thái không mua được.
- **Lúc nghỉ giữa hai đợt**: mua được.
- **Tiền vào cùng khung hình với yêu cầu mua**: theo thứ tự ví nhận; không bao giờ trừ khi chưa đủ.
- **Giá rất lớn** (ví dụ hero cấp 30 lên 31 giá khoảng 181.000 tiền): giá vẫn là số nguyên; cách rút gọn khi hiển thị thuộc `feature-hud-upgrade/button-state/button-display`.

## Đã tự quyết
- **Dùng đúng bộ số chung.** Giá 30/40/60, hệ số 1,35/1,4/1,5. Đã loại: một công thức chung cho mọi mục (mất khác biệt hero rẻ, kĩ năng đắt).
- **Làm tròn sau khi bỏ sai số.** Để giá trong test khớp bảng trên. Đã loại: làm tròn lên thẳng (ra 57 thay vì 56).
- **Từ chối ngay, không xếp hàng chờ tiền.** Chạm khi không đủ tiền không được nhớ lại. Đã loại: đặt mua trước, tự mua khi đủ tiền.

## Chưa rõ
- Đề xuất cho người: thưởng dọn đợt tăng tuyến tính (đợt 20 được 120) còn giá tăng theo cấp số nhân; sau khoảng đợt 15 mỗi đợt chỉ còn mua được 1 cấp. Nếu chơi thử thấy về sau quá bí, cân nhắc hạ hệ số hero còn 1,3. Hiện vẫn dùng 1,35.
