---
based-on: c266fb040742
---
# Bảng thuật ngữ

Nơi duy nhất định nghĩa các từ dùng chung trong thiết kế; node khác nhắc từ bằng `glossary#<id>` thay vì định nghĩa lại.

## Ý cốt lõi

### run | AI 8 | Lượt: từ lúc vào trận tới lúc trận kết thúc
Màn bắt đầu và màn chọn đội nằm ngoài lượt. Một lượt có đúng một trận.

### battle | AI 8 | Trận: phần chơi trên dải chiến trường, có HUD và thanh nâng cấp

### run-end | AI 8 | Kết thúc lượt: lúc trận dừng hẳn và điểm của lượt được chốt

### playing | AI 8 | Đang đánh: trận đang chạy, người chơi mua nâng cấp và bấm kĩ năng được

### paused | AI 8 | Tạm dừng: trận đứng yên hoàn toàn, popup tạm dừng đang mở
Khác lúc thở: lúc thở trận vẫn chạy, chỉ không có quái mới ra.

### leave-game | AI 7 | Rời khỏi game: game không còn ở trên màn hình
Ví dụ chuyển ứng dụng, khoá máy, có cuộc gọi.

### quit-run | AI 8 | Bỏ lượt: người chơi tự kết thúc lượt đang chơi

### battlefield | AI 8 | Dải chiến trường: dải đất chạy ngang qua thành ở giữa màn hình, nơi hero, quái và thành đứng

### side | AI 8 | Bên: nửa dải chiến trường giữa một mép màn hình (trái hoặc phải) và thành

### slot | AI 7 | Chỗ: một ô trong đội, chứa được một hero

### run-team | AI 8 | Đội của lượt: các hero người chơi đã chọn ở màn chọn đội ngay trước trận

### remembered-team | AI 7 | Đội được nhớ: đội đã vào trận gần nhất trong lần mở game này

### fighting-style | AI 7 | Cách đánh: nhóm (cận chiến hoặc bắn xa) cộng kiểu đòn của một hero
Cách đánh quyết hero đứng chắn ở trước hay đánh từ phía sau. Mỗi hero một cách đánh, không đổi trong trận.

### melee | AI 8 | Cận chiến: tầm đánh ngắn, phải đi sát mục tiêu mới đánh được
Dùng cho cả hero và quái.

### ranged | AI 8 | Bắn xa: tầm đánh dài, đánh được từ xa

### hp | AI 9 | Máu: sức chịu đòn; máu tối đa là mức đầy, máu hiện tại là phần còn lại

### attack | AI 9 | Sức đánh: lượng máu một đòn trừ của mục tiêu

### attack-speed | AI 8 | Tốc độ đánh: đánh nhanh hay chậm; cao hơn thì khoảng cách giữa hai đòn ngắn hơn

### range | AI 8 | Tầm đánh: khoảng cách xa nhất tới mục tiêu mà vẫn đánh được
Với thành, tầm đánh (còn gọi tầm bắn) tính từ thành ra mỗi bên.

### base-stats | AI 8 | Chỉ số gốc: chỉ số của một loại quái trước khi làm mạnh lên theo đợt

### down | AI 8 | Gục: hero máu về 0, tạm rời chiến trường
Gục không làm thua và không mất cấp.

### respawn-wait | AI 7 | Thời gian chờ: khoảng từ lúc hero gục tới lúc hồi sinh

### defeated | AI 8 | Bị hạ: quái máu về 0 và rời trận hẳn

### reach-castle | AI 8 | Tới thành: thành nằm trong tầm đánh của quái

### target | AI 8 | Mục tiêu: quái còn sống mà một hero hoặc thành đang nhắm để đánh

### nearest | AI 7 | Gần nhất: khoảng cách trên dải chiến trường nhỏ nhất, không phân biệt bên trái hay bên phải

### auto-shot | AI 8 | Đòn tự bắn: đòn thường của thành, tự bắn theo tốc độ đánh, không cần thao tác

### castle-skill | AI 8 | Kĩ năng thành: đòn đặc biệt người chơi bấm để thành dùng, có hồi chiêu
Khác đòn tự bắn.

### cooldown | AI 9 | Hồi chiêu: thời gian chờ từ lúc dùng một kĩ năng tới lúc dùng lại được

### castle-zone | AI 7 | Vùng quanh thành: một khoảng ở cả hai bên tính từ thành

### killer | AI 7 | Bên hạ: thứ làm quái bị hạ (hero, đòn tự bắn của thành, kĩ năng thành)

### wave | AI 9 | Đợt: một danh sách quái ra trong một khoảng của trận

### active-wave | AI 8 | Đợt đang đánh: đợt có quái đang ra hoặc còn sống; mỗi lúc chỉ có một

### unlocked-type | AI 7 | Loại đã mở: loại quái được phép có trong đợt từ một số đợt trở đi

### spawn-interval | AI 7 | Nhịp thả: khoảng cách giữa hai lần ra quái liên tiếp

### wave-clear | AI 9 | Dọn sạch đợt: quái của đợt đã ra hết và không còn con nào còn sống

### wave-gap | AI 8 | Lúc thở: khoảng từ lúc dọn sạch một đợt tới lúc đợt sau bắt đầu ra quái
Trận vẫn chạy, chỉ không có quái mới. Khác tạm dừng.

### score | AI 9 | Điểm: số đợt đã dọn sạch trong lượt
Khác số đợt đang đánh mà HUD hiện.

### record | AI 8 | Kỉ lục: điểm cao nhất của mọi lượt từng chơi trên máy này

### new-record | AI 7 | Phá kỉ lục: điểm vừa chốt lớn hơn hẳn kỉ lục trước lượt đó
Bằng kỉ lục không tính.

### upgrade-item | AI 8 | Mục nâng cấp: một thứ mua được nhiều lần, mỗi lần làm một hero hoặc một mặt của thành mạnh hơn

### level | AI 8 | Cấp: số lần một mục nâng cấp đã được mua trong lượt, tính từ cấp thấp nhất

### hero-level | AI 8 | Cấp hero: cấp của mục nâng cấp của một hero

### next-price | AI 8 | Giá cấp sau: số tiền phải trả để một mục lên thêm một cấp từ cấp hiện tại

### can-buy | AI 8 | Mua được: đang đánh và số dư lớn hơn hoặc bằng giá cấp sau

### upgrade-value | AI 6 | Lời của một lần nâng: sức mạnh thêm vào so với giá phải trả

### wave-income | AI 7 | Thu của một đợt: tổng tiền hạ quái và thưởng dọn đợt trong đợt đó

### number | AI 7 | Số liệu: một giá trị chỉnh được ảnh hưởng tới độ khó, thu chi hay sức của hero, thành, quái

### owner-feature | AI 7 | Feature sở hữu: feature duy nhất đọc và ghi một số liệu, hoặc sở hữu một popup

### enemy-power | AI 6 | Sức quái của đợt: tổng sức gây hại và sức chịu đòn của mọi quái trong đợt

### team-power | AI 6 | Sức đội: tổng sức đánh và sức chịu đòn của hero và thành ở một thời điểm

### break-wave | AI 7 | Đợt gãy: đợt mà sức quái vượt sức đội tới mức thành bị phá

### pressure | AI 6 | Áp lực: sức quái của đợt chia cho sức đội; càng gần đợt gãy càng lớn

### hero-power | AI 6 | Sức tổng của hero: giá trị chung của năm chỉ số, dùng để so các hero ở cùng cấp

### counter | AI 6 | Hợp với loại quái: hero hạ loại quái đó nhanh hơn, hoặc chịu được nó lâu hơn so với hero khác

### mono-team | AI 6 | Đội lệch: đội mà mọi hero cùng một cách đánh

### scene | AI 8 | Scene: một khung lớn của game; chuyển scene là huỷ khung cũ và tải khung mới

### screen | AI 8 | Màn: một màn hình người chơi thấy bên trong một scene; đổi màn không chuyển scene

### scene-loading | AI 8 | Scene Loading: scene đầu tiên khi mở game, lo tải những thứ scene Game cần

### scene-game | AI 8 | Scene Game: scene chơi, chứa mọi màn người chơi thao tác

### participant | AI 8 | Feature tham gia: feature có mặt trong danh sách của một scene, nhận đủ ba bước tải, chạy, huỷ

### load-step | AI 8 | Tải: bước chuẩn bị thứ cần dùng (hình, prefab, cấu hình), chưa hiện gì

### run-step | AI 8 | Chạy: bước tạo view và bắt đầu hoạt động

### dispose-step | AI 8 | Huỷ: bước huỷ view, bỏ thứ đã tải, dừng mọi hoạt động trong scene

### view | AI 8 | View: thứ người chơi thấy hoặc bấm được trên màn hình

### clean | AI 7 | Dọn sạch scene: sau khi huỷ, scene không còn view, không còn việc đang chạy, không còn trạng thái của trận

### clean-start | AI 7 | Bắt đầu sạch: lượt mới không mang theo trạng thái của lượt trước

### popup | AI 8 | Popup: một bảng hiện đè lên màn đang chơi, có nút để người chơi chọn

### caller | AI 7 | Feature gọi: feature sở hữu popup và nhờ dịch vụ popup hiện nó

### popup-id | AI 8 | Id popup: tên dạng Owner/Name, Owner là feature gọi, Name là tên popup trong feature đó
Ví dụ PopupLose/Main.

### popup-layer | AI 8 | Lớp popup: tầng hiển thị cao nhất của scene, chỉ dành cho popup

### backdrop | AI 8 | Nền che: tấm phủ toàn màn hình ngay sau một popup, làm tối và chặn bấm phía dưới

### top-popup | AI 7 | Popup trên cùng: popup mở gần nhất còn đang mở; chỉ nó nhận bấm
