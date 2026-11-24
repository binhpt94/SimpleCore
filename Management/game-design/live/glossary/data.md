---
based-on: 53af9956f851
---
# Bảng thuật ngữ

Nơi duy nhất định nghĩa các từ dùng chung trong thiết kế; node khác nhắc từ bằng `glossary#<id>` thay vì định nghĩa lại.

## Ý cốt lõi

### run | AI 8 | Lượt: một lần chơi, từ lúc vào trận tới lúc trận kết thúc
Khác trận: trận là phần chơi trên dải chiến trường bên trong lượt. Màn bắt đầu và màn chọn đội nằm ngoài lượt.

### paused | AI 8 | Tạm dừng: trận đứng yên hoàn toàn
Khác lúc thở: lúc thở trận vẫn chạy.

### leave-game | AI 7 | Rời khỏi game: game không còn ở trên màn hình
Ví dụ chuyển ứng dụng, khoá máy, có cuộc gọi.

### battlefield | AI 8 | Dải chiến trường: dải đất chạy ngang qua thành, nơi hero, quái và thành đứng

### remembered-team | AI 7 | Đội được nhớ: đội đã vào trận ở lượt gần nhất
Khác đội của lượt đang chơi: đội được nhớ là đội cũ, dùng để điền sẵn màn chọn đội.

### fighting-style | AI 7 | Cách đánh: nhóm (cận chiến hoặc bắn xa) cộng kiểu đòn của một hero
Khác chỉ số: hai hero cùng cách đánh vẫn có thể khác máu, sức đánh, tầm đánh.

### range | AI 8 | Tầm đánh: khoảng cách xa nhất tới mục tiêu mà vẫn đánh được
Với thành còn gọi là tầm bắn.

### base-stats | AI 8 | Chỉ số gốc: chỉ số của một loại quái trước khi được làm mạnh lên theo đợt

### down | AI 8 | Gục: hero hết máu và tạm rời chiến trường
Khác bị hạ: gục là của hero và hero sẽ quay lại; bị hạ là của quái.

### defeated | AI 8 | Bị hạ: quái hết máu và rời trận hẳn
Khác gục của hero.

### reach-castle | AI 8 | Tới thành: thành nằm trong tầm đánh của quái
Khác chạm thành: quái bắn xa tới thành khi còn đứng ở xa.

### target | AI 8 | Mục tiêu: quái mà một hero hoặc thành đang nhắm để đánh
Dùng chung cho hero và thành.

### castle-skill | AI 8 | Kĩ năng thành: đòn đặc biệt mà người chơi bấm để thành dùng
Khác đòn tự bắn: đòn tự bắn là đòn thường thành tự đánh, không cần bấm.

### castle-zone | AI 7 | Vùng quanh thành: một khoảng ở cả hai bên, tính từ thành ra

### wave | AI 9 | Đợt: một danh sách quái ra trong một khoảng của trận

### active-wave | AI 8 | Đợt đang đánh: đợt có quái đang ra hoặc còn sống
Khác lúc thở: trong lúc thở không có đợt đang đánh.

### wave-clear | AI 9 | Dọn sạch đợt: quái của đợt đã ra hết và không còn con nào còn sống

### wave-gap | AI 8 | Lúc thở: khoảng từ lúc dọn sạch một đợt tới lúc đợt sau bắt đầu ra quái
Khác tạm dừng: lúc thở trận vẫn chạy.

### score | AI 9 | Điểm: số đợt đã dọn sạch trong lượt
Khác số đợt đang đánh mà HUD hiện.

### level | AI 8 | Cấp: mức hiện tại của một mục nâng cấp
Cấp hero là cấp của mục nâng cấp của hero đó.

### owner-feature | AI 7 | Feature sở hữu: feature duy nhất đọc và ghi một số liệu, hoặc sở hữu một popup

### break-wave | AI 7 | Đợt gãy: đợt mà quái mạnh hơn đội hero và thành tới mức thành bị phá

### counter | AI 6 | Hợp với loại quái: hero hạ một loại quái nhanh hơn, hoặc chịu được nó lâu hơn so với hero khác

### mono-team | AI 6 | Đội lệch: đội mà mọi hero cùng một cách đánh

### scene | AI 8 | Scene: một khung lớn của game, được tải và huỷ riêng
Khác màn: đổi màn không đổi scene.

### screen | AI 8 | Màn: một màn hình người chơi thấy bên trong một scene
Ví dụ màn bắt đầu, màn chọn đội, màn trận.

### participant | AI 8 | Feature tham gia: feature có mặt trong danh sách của một scene

### caller | AI 7 | Feature gọi: feature sở hữu một popup và nhờ dịch vụ popup hiện nó

### popup-id | AI 8 | Id popup: tên của một popup, dạng Owner/Name
Owner là feature gọi, Name là tên popup trong feature đó. Ví dụ PopupLose/Main.

### backdrop | AI 8 | Nền che: tấm phủ toàn màn hình ngay sau một popup
