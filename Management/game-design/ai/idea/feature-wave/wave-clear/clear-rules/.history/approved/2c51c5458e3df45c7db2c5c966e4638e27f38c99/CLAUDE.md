# Luật dọn sạch và thưởng đợt

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Đếm số quái sống của đợt: +1 khi một con ra, −1 khi một con bị hạ.
- Mỗi lần một con bị hạ: nếu `feature-wave/spawning/spawn-order` đã báo "đã ra hết" và số sống = 0, và thành chưa bị phá trong cùng lúc đó, thì đợt dọn sạch.
- Khi dọn sạch: cộng thưởng vào `feature-wallet`, báo `feature-run` tăng điểm 1, rồi giao cho `feature-wave/wave-gap/gap-timer`.

## Thông số
| Thông số | Giá trị (dự kiến) | Đơn vị | Lí do |
|---|---|---|---|
| Thưởng dọn đợt | 20 + 5×w | xu | Nhỏ hơn tiền hạ quái của cùng đợt (đợt 1: 25 so với 40, đợt 10: 70 so với 272), giữ tiền hạ quái là nguồn chính |
| Điểm thêm mỗi lần dọn | 1 | điểm | Điểm = số đợt dọn sạch |

Ví dụ: đợt 1: 25 xu; đợt 5: 45 xu; đợt 10: 70 xu; đợt 15: 95 xu; đợt 20: 120 xu.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Thưởng dọn đợt | 20 + 5×w xu | như IDEA, chỉnh sau chơi thử | chốt sau chơi thử |
| Điểm mỗi lần dọn | 1 | 1 | 1 |
| Báo dọn sạch cho `feature-hud` | không bắt buộc | có | có |

## Trường hợp biên
- **Thành bị phá cùng lúc con cuối bị hạ** (ví dụ con cuối đánh thành rồi chết vì Sấm sét trong cùng một bước cập nhật): đợt chưa được tính; tin dọn sạch do `feature-run` giữ. Lượt chốt thua (không hồi sinh): không thưởng, không điểm. Hồi sinh thành công: `feature-run` thả tin, feature này cộng thưởng dọn đợt và bắt đầu lúc thở 3 giây như mọi lần dọn sạch. (Sửa 2026-11-26 khi chuyển ý held-clear từ nhánh thứ tự hồi sinh ở popup hồi sinh về lượt chơi; ý đó nay ở `feature-run/run-flow/flow-rules`.)
- **Con cuối bị hạ trước khi ra hết** (đang hoãn vì đủ 40 hoặc còn mục chưa tới nhịp): chưa dọn sạch, chờ mục cuối ra và bị hạ.
- **Đạn của quái bắn xa còn bay sau khi đợt đã dọn sạch:** vẫn gây sát thương. Nếu nó phá thành trong lúc thở thì lượt thua, nhưng đợt vừa dọn vẫn được tính (điểm và thưởng đã cộng).
- **Tạm dừng đúng lúc dọn sạch:** việc cộng thưởng, báo điểm đã xảy ra trước khi dừng; đồng hồ lúc thở bắt đầu đếm khi bỏ tạm dừng.
- **Quái bị hạ khi còn ngoài màn hình** (ví dụ ở x = ±16.5u bởi Cung thủ): vẫn tính, vẫn cho tiền.

## Đã tự quyết
- **Thua xét trước dọn sạch.** Theo luật node cha: thành bị phá thì đợt đang đánh không được tính. Đã loại: dọn sạch xét trước (người chơi được điểm dù đã thua).
- **Thưởng dọn đợt tăng tuyến tính 5 xu mỗi đợt.** Theo bộ số chuẩn; tăng chậm hơn giá nâng cấp (tăng theo hệ số mũ).

## Chưa rõ
- Không có điểm chặn.
