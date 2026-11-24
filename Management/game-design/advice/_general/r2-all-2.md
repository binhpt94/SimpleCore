---
advice: r2-all-2
node: (không gắn node nào)
asked: 2026-11-24 23:03:35
by: gpt (Codex CLI)
---
# Góp ý r2-all-2

## Câu hỏi

# Vòng 2, câu chung (lần 2): cách chia feature của cả bộ

Người nhờ: @GameDesigner, mode Full Auto. Hỏi lại câu chung của vòng 2. Lần này không có rào nào về cách chia feature: được đề xuất feature mới, gộp hay tách feature, đổi phạm vi của feature đã có.

## Câu hỏi
Nhìn cả bộ feature vòng 2 (bản đồ feature được gửi kèm) như một tổng thể:
1. **Trùng việc:** hai feature nào cùng giữ một việc? Việc đó nên thuộc feature nào?
2. **Thiếu feature:** phần nào của game chưa feature nào giữ?
3. **Ranh giới mơ hồ:** chỗ nào hai feature chạm nhau mà chưa rõ bên nào làm gì?
4. **Gộp hay tách:** feature nào nên gộp với feature khác, feature nào nên tách thành nhiều feature?

Mỗi góp ý ghi rõ feature đích (hoặc tên key đề xuất cho feature mới). Chỗ đã ổn thì không viết gì.

## Yêu cầu riêng của người
- Không ghi dữ liệu theo giai đoạn (bản đầu, MVP, làm trước / để sau).

## Bên hỏi muốn
Danh sách góp ý về cách chia feature, mỗi góp ý một quyết định, kèm feature đích.

## Góp ý của GPT

### phan-xu-sat-thuong | thêm | Feature mới: feature-combat chốt thứ tự xử lý sát thương khi nhiều đòn trúng trong cùng một nhịp.
Nếu hero và quái cùng tung đòn kết liễu, kết quả phải nhất quán giữa các lần chạy. Các bên chọn mục tiêu và gửi đòn trúng, rồi cập nhật máu theo kết quả xử lý. Các node liên quan nằm ở `ai/idea/feature-hero`, `ai/idea/feature-enemy` và `ai/idea/feature-castle`.

### moc-chot-diem-luot | thêm | feature-run tính vào điểm cả đợt được dọn sạch trong cùng nhịp thành bị phá.
Đòn cuối của hero có thể hạ con quái cuối đúng lúc thành sập. `ai/idea/feature-wave` xác nhận đợt đã sạch, `ai/idea/feature-castle` báo thành bị phá và `ai/idea/feature-run` đợi xử lý xong các sự kiện của nhịp đó rồi chốt điểm.

### tach-dieu-huong-man | thêm | Tách: feature-game-flow sở hữu điều hướng giữa các màn trong scene Game.
Màn bắt đầu và màn chọn đội xuất hiện trước khi có lượt, còn lựa chọn sau popup thua đến khi lượt đã kết thúc. Đổi đầu mối chuyển màn ở `ai/idea/feature-home`, `ai/idea/feature-team-select`, `ai/idea/feature-run` và `ai/idea/feature-popup`. `ai/idea/feature-scene-flow` bàn giao khi scene Game sẵn sàng.

### gop-giao-dien-tran | thêm | Gộp: feature-hud chứa cả thanh nâng cấp và sở hữu bố cục giao diện trận.
Nút kĩ năng, nút tạm dừng và thanh mua cùng tranh chỗ ở mép màn hình ngang. Gộp `ai/idea/feature-hud-upgrade` vào `ai/idea/feature-hud` để một nơi bố trí vùng bấm và giữ dải chiến trường thoáng. `ai/idea/feature-upgrade` cung cấp trạng thái các mục và xử lý yêu cầu mua.

### kiem-tra-doi-hinh | thêm | feature-team-select xác nhận đội hợp lệ trước khi gửi yêu cầu vào trận.
`ai/idea/feature-team-select` dùng danh sách hero từ `ai/idea/feature-hero` để kiểm quân số và các ràng buộc chọn đội. Khi người chơi xác nhận một đội hợp lệ, `ai/idea/feature-run` nhận đội đã chốt để dựng trận.

### sinh-doi-dau-tran | thêm | feature-hero tạo cả đội khi trận bắt đầu tại điểm xuất phát do feature-castle cung cấp.
`ai/idea/feature-run` bàn giao đội đã chốt khi dựng trận. `ai/idea/feature-castle` cung cấp vị trí, còn `ai/idea/feature-hero` tạo đúng các hero trong đội và dùng cùng điểm đó khi hồi sinh.

### tinh-so-tien-thuong | thêm | feature-wallet tính tiền thưởng theo loại quái bị hạ hoặc số thứ tự đợt vừa dọn.
`ai/idea/feature-enemy` báo loại quái bị hạ, `ai/idea/feature-wave` báo số thứ tự đợt đã dọn và `ai/idea/feature-wallet` tra mức thưởng do `ai/idea/feature-balance` cân chỉnh. Như vậy mỗi khoản thưởng có một nơi xác định số tiền.

### mua-nang-cap-tron-ven | thêm | feature-upgrade xử lý mỗi lần mua như một giao dịch gồm trừ đúng giá và tăng đúng một cấp.
Hai lần chạm liên tiếp có thể cùng thấy một giá trên HUD. `ai/idea/feature-upgrade` xử lý lần lượt theo cấp hiện tại và chỉ hoàn tất lần mua khi `ai/idea/feature-wallet` xác nhận đã trừ đủ giá của lần đó.

### tam-dung-khi-ra-nen | thêm | feature-run tự tạm dừng trận đang chạy khi game chuyển xuống nền.
Người chơi có thể rời game giữa lúc thành đang bị đánh. Khi mở lại, `ai/idea/feature-popup-pause` cho người chơi chủ động tiếp tục hoặc bỏ lượt. Các feature trong trận dùng trạng thái tạm dừng từ `ai/idea/feature-run`.
