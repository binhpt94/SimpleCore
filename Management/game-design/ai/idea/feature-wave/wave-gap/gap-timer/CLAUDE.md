# Đồng hồ lúc thở

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Nhận "đã dọn sạch đợt w" từ `feature-wave/wave-clear/clear-rules`: số đợt thành w + 1 ngay, báo `feature-hud`; bắt đầu đếm 3 giây.
- Trong lúc đếm không thả quái. Không chặn hay đổi bất kì luật nào của `feature-upgrade`, `feature-castle`, `feature-hero`.
- Đếm xong: gọi `feature-wave/difficulty-scaling/wave-formula` (với số đợt w + 1) và `feature-wave/spawning/spawn-order` lập và thả đợt mới; báo `feature-hud` "đợt w + 1 bắt đầu" để hiện thông báo đợt.

## Thông số
| Thông số | Giá trị (dự kiến) | Đơn vị | Lí do |
|---|---|---|---|
| Độ dài lúc thở | 3 | s | Theo bộ số chuẩn; ngắn hơn hẳn đợt (đợt ngắn nhất thả trong 7 s), đủ để thấy "đã dọn sạch" |
| Độ dài thay đổi theo đợt | không | | Nhịp quen thuộc, dễ đoán |

Ước tính độ dài một lượt (dự kiến): mỗi đợt mất thời gian thả cộng thời gian dọn quái tồn, khoảng 15 s ở đợt 1 tới 35–45 s ở đợt 10–20; cộng 3 s mỗi lúc thở. Một lượt gãy ở đợt 15 dài khoảng 7–8 phút.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Độ dài lúc thở | 3 s | 3 s, chỉnh sau chơi thử | chốt sau chơi thử |
| Báo đợt mới cho `feature-hud` | không bắt buộc | có | có |
| Tạm dừng đóng băng đồng hồ | có | có | có |

## Trường hợp biên
- **Tạm dừng trong lúc thở:** đồng hồ đứng yên; tiếp tục thì đếm phần còn lại.
- **Thành bị phá trong lúc thở** (chỉ có thể do đạn quái còn bay): lượt kết thúc, đồng hồ dừng, không bắt đầu đợt mới.
- **Mua nâng cấp đúng lúc hết 3 giây:** nâng cấp có hiệu lực ngay; đợt mới vẫn bắt đầu đúng giờ, không chờ.
- **Hero đang hồi sinh khi đợt mới bắt đầu:** hồi sinh tiếp tục theo đồng hồ 8 s riêng của hero; đợt mới không chờ.
- **Lúc thở trước đợt 1:** không có; đợt 1 bắt đầu ngay khi trận bắt đầu (`feature-wave/spawning/spawn-order`).

## Đã tự quyết
- **3 giây cố định.** Theo bộ số chuẩn. Đã loại: lúc thở dài dần theo đợt (làm lượt dài thêm mà không thêm quyết định).
- **Số đợt tăng ngay lúc dọn sạch, thả quái sau 3 giây (2026-11-25, làm rõ, theo câu hỏi của @CodeLead).** Bản trước ghi "hết 3 giây thì số đợt tăng 1", đọc được thành số người chơi thấy chỉ đổi sau 3 giây, trái với `feature-hud/hud-status/status-spec` (`wave-label`: trong lúc thở hiện đợt sắp tới) và node cha ở nhánh HUD ("lúc thở hiện đợt sắp tới"). Tách thành hai ý: `gap-next-number` (số đợt đổi ngay) và `gap-start-next` (thả quái sau 3 giây). Không đổi hành vi code hiện có (Wave đặt đợt hiện tại bằng w + 1 lúc dọn sạch). Đã loại: số đợt đổi sau 3 giây (ô đợt hiện đợt đã dọn suốt lúc thở).
- **Đợt mới không chờ hero hồi sinh xong.** Hero gục là hậu quả của đợt trước; chờ thì làm nhẹ áp lực. Đã loại: chờ cả đội đứng dậy.

## Chưa rõ
- Không có điểm chặn.
