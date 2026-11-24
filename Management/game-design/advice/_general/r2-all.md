---
advice: r2-all
node: (không gắn node nào)
asked: 2026-11-24 22:50:40
by: gpt (Codex CLI)
---
# Góp ý r2-all

## Câu hỏi

# Vòng 2, câu chung: cả bộ feature có trùng việc, thiếu feature hay ranh giới mơ hồ không

Người nhờ: @GameDesigner, mode Full Auto. Lần hỏi `r2-1` trước đã hỏi từng feature riêng; đây là câu chung cho cả bộ feature vòng 2 (bản đồ feature được gửi kèm).

## Câu hỏi
Nhìn cả bộ feature vòng 2 như một tổng thể:
1. **Trùng việc:** hai feature nào cùng giữ một việc (cùng giữ một dữ liệu, cùng quyết một luật, cùng hiện một thứ)? Nói việc đó nên thuộc feature nào.
2. **Thiếu feature:** vòng lặp chơi của vòng 1 có phần nào chưa feature nào giữ?
3. **Ranh giới mơ hồ:** chỗ nào hai feature chạm nhau mà chưa rõ bên nào làm gì?

Mỗi góp ý ghi rõ nó thuộc feature nào. Chỗ đã rõ thì không viết gì.

## Yêu cầu riêng
- Không đề xuất ngược ba ý người đã chấm lại ở gốc: đội hero cố định chọn trước mỗi lượt; thành tự đánh và có kĩ năng hồi chiêu người chơi bấm; nâng cấp ngay lúc đang đánh.
- Giữ luật chia feature vòng 2: mỗi popup một feature; HUD tách cụm lớn (`feature-hud`, `feature-hud-upgrade`); balance riêng; ba feature hạ tầng mặc định (`feature-scene-flow`, `feature-session-scene`, `feature-popup`). Không gộp feature, không đổi tên key đã chốt.
- Không con số, không dữ liệu theo giai đoạn (bản đầu, MVP, làm trước / để sau).
- Cây đã tới vòng 4 và đã chốt. Chỉ đề xuất điều thật sự là quyết định vòng 2; luật, định nghĩa, con số để nhánh dưới.
- Không lặp góp ý đã có ở các lần hỏi `r2-1` của từng feature.

## Bên hỏi muốn
Danh sách góp ý về cách chia feature, mỗi góp ý một quyết định, ghi feature đích, để đưa vào đúng feature đó.

## Góp ý của GPT

### chu-so-huu-ban-do | thêm | Feature mới feature-battlefield sở hữu bố cục bản đồ trận.
Gốc `#layout` và `#single-map` đã chốt dải đất, vị trí thành và hai mép quái đi vào. Sơ đồ chưa giao các mốc không gian này cho feature nào để `feature-castle`, `feature-hero` và `feature-wave` dùng chung. Góp ý dựa trên sơ đồ đính kèm và chưa được đối chiếu với lịch sử góp ý riêng từng feature.

### chu-so-huu-doi-duoc-nho | thêm | feature-team-select giữ đội được nhớ giữa các lượt làm lựa chọn điền sẵn.
`feature-team-select#team-remember` cần dữ liệu qua lượt, trong khi gốc `#run-reset` và `feature-run#run-record` nói chỉ giữ kỉ lục. Cần sửa hai ý đó để phân biệt tiến trình chơi với lựa chọn điền sẵn trên màn chọn đội. Người chơi vẫn xác nhận đội trước khi vào trận.

### chu-so-huu-thuong-tien | thêm | feature-wallet là nơi duy nhất cấp thưởng tiền khi quái bị hạ hoặc đợt được dọn sạch.
`feature-enemy#enemy-reward`, `feature-wave#wave-clear` và `feature-wallet#earn` đều nhắc việc thưởng. `feature-enemy` xác nhận quái bị hạ, `feature-wave` xác nhận đợt đã dọn sạch, còn `feature-wallet` ghi khoản thưởng tương ứng vào số dư. Như vậy mỗi khoản thưởng có một nơi ghi nhận.

### chu-so-huu-cap-nang | thêm | feature-upgrade giữ cấp nâng cấp đã mua của từng hero và của thành trong lượt.
`feature-hud-upgrade#upgrade-bar-state` lấy cấp từ cùng một nguồn cho mọi nút. `feature-hero#hero-upgrade` và `feature-castle#castle-upgrades` chịu trách nhiệm áp dụng thay đổi sức mạnh lên đối tượng mình quản lý.

### khoi-tao-luot-moi | thêm | feature-run điều phối việc đặt lại trạng thái lượt mới ở từng feature, kể cả khi trận dùng lại scene.
`feature-run#run-reset` yêu cầu bắt đầu sạch khi chơi lại, còn `feature-session-scene#cleanup-scope` chỉ dọn tài nguyên và trạng thái của scene khi rời scene. Mỗi feature đặt lại dữ liệu lượt do mình sở hữu khi `feature-run` bắt đầu lượt mới.

### quyen-chot-luot | thêm | feature-run là nơi xác nhận lượt đã kết thúc khi nhận tín hiệu thành bị phá.
`feature-castle#castle-health` xác nhận thành bị phá, `feature-wave#wave-clear` xác nhận đợt đã dọn sạch, và `feature-popup-lose#lose-score-consistency` hiển thị kết quả đã chốt. Quyền kết thúc lượt thuộc một nơi ngay cả khi thành bị phá sát lúc đợt được dọn sạch.

### yeu-cau-tam-dung | thêm | feature-run tiếp nhận yêu cầu tạm dừng từ nút trên HUD.
`feature-hud#hud-pause` nhận thao tác bấm, `feature-run#run-pause` xác nhận trận đã tạm dừng để `feature-popup-pause#pause-freeze` hiện popup. Cả yêu cầu tạm dừng và lựa chọn tiếp tục ở `feature-popup-pause#pause-run-ownership` đi qua cùng nơi quản lý trạng thái trận.
