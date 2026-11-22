# Thả quái

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một lượt có nhiều đợt nối tiếp; mỗi lúc chỉ có MỘT đợt đang đánh.
- Một đợt có NHIỀU quái, thuộc MỘT hay NHIỀU loại quái.
- Mỗi con quái ra ở MỘT bên (trái hoặc phải), vào MỘT dải đất duy nhất (một làn).
- Danh sách quái của đợt (bao nhiêu con, loại nào) lấy từ `feature-wave/difficulty-scaling`; loại quái và chỉ số gốc của nó là của `feature-enemy`.
- Node này chỉ đưa quái vào trận; quái ra rồi thì đi, đánh, bị hạ theo luật của `feature-enemy`.

## Định nghĩa
- **Đợt**: một danh sách quái ra trong một khoảng của trận; đợt đang đánh là đợt có quái đang ra hoặc còn sống.
- **Bên ra**: mép trái hoặc mép phải của dải đất.
- **Lúc ra**: thời điểm con quái xuất hiện, tính từ đầu đợt.
- **Đã ra hết**: mọi con trong danh sách của đợt đã xuất hiện trong trận.
- **Nhịp thả**: khoảng cách giữa hai lần ra quái liên tiếp.

## Danh sách
Mỗi mục trong danh sách đợt gồm:
- Loại quái (một loại trong danh sách của `feature-enemy`).
- Bên ra: trái hoặc phải.
- Lúc ra.

## Luật cơ bản
- Đầu đợt, danh sách quái được lập theo số đợt; rồi thứ tự và bên ra được xáo.
- Mỗi bên đều có quái; số quái hai bên gần bằng nhau.
- Quái ra lần lượt theo nhịp thả, cho tới khi đã ra hết.
- Đợt sau có nhịp thả dày hơn đợt trước (mức dày hơn bao nhiêu là của `feature-wave/difficulty-scaling` và `feature-balance`).
- Quái mới ra mang chỉ số theo đợt đang đánh.
- Lượt chơi kết thúc (`feature-run`) thì ngừng thả.

## Đã tự quyết
- **Đợt là danh sách lập sẵn đầu đợt.** Nhờ vậy biết chắc khi nào "đã ra hết" để `feature-wave/wave-clear` xét dọn sạch. Đã loại: thả liên tục theo đồng hồ.
- **Chia gần đều hai bên.** Giữ cảm giác bị kẹp, và kĩ năng thành, hero không bị lệch về một bên. Đã loại: lệch bên theo đợt.
- **Thả từng con theo nhịp.** Dễ đọc trên màn hình thấp. Đã loại: cụm lớn một lúc.
- **Ngẫu nhiên chỉ ở thứ tự và bên ra.** Độ khó của một đợt luôn như nhau, cân bằng được. Đã loại: số quái dao động.
- **Viết lại theo luật vòng 3 mới (2026-11-23).** Bỏ mọi con số (khoảng thả, giới hạn quái trên màn, số con liên tiếp cùng bên), bỏ trường hợp biên (tạm dừng, đợt một con, đủ giới hạn), bỏ chữ "Đợt N" giữa màn hình (hiển thị là của `feature-hud`).

## Chưa rõ
- Có giới hạn số quái còn sống cùng lúc trên màn hình hay không: để vòng 4 cùng thông số.
