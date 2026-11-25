---
based-on: 6181a7c21bf0
by: step-after-combat=gpt@r2-1#chot-giao-chien, spawn-after-results=gpt@r2-1#thu-tu-tha-quai, keep-drawn-spawn=gpt@r2-1#giu-lan-boc-khi-cho-tha, init-on-run=gpt@r2-1#khoi-tao-theo-luot, first-step-on-start=gpt@r2-1#moc-buoc-dau, seed-record-owner=gpt@r2-1#chu-so-huu-ban-ghi-seed, clock-drives-gameplay=gpt@r2-1#dong-ho-khi-tran-chay, continue-after-revive=gpt@r2-1#tiep-nhip-sau-hoi-sinh, clear-on-dispose=gpt@r2-1#don-trang-thai-nhip, tester-fast-run=gpt@r2-1#dieu-kien-chay-lai, revive-result-queued=gpt@r2-1#phat-lai-ket-qua-hoi-sinh, seeded-rng=gpt@r2-1#seed-dev-dung-config
---
# Nhịp và ngẫu nhiên của trận

Hạ tầng cho logic gameplay của trận: một nhịp chung thay cho thời gian khung hình, và một nguồn số ngẫu nhiên tất định theo seed. Cùng seed và cùng thao tác thì trận ra cùng kết quả, nên tester giả lập và chạy nhanh trận được. Người yêu cầu (2026-11-25).

## Ý cốt lõi

### clock-drives-gameplay | AI 7 | Logic gameplay lấy thời gian từ đồng hồ gameplay của nhịp chung; đồng hồ này chỉ cộng các bước mà lượt cho trận chạy
Hero, quái, thành, đợt quái, kĩ năng, hồi sinh, giao chiến không tự đọc thời gian khung hình. Số bước chung vẫn tiến lúc tạm dừng để xếp thao tác; lịch thả quái, lúc thở, hồi chiêu đứng yên suốt lúc dừng. Phần chỉ để nhìn không bắt buộc.

### update-or-tick | AI 7 | Một config chung chọn logic gameplay chạy theo Update của khung hình hay theo TickUpdate cố định
Mọi logic gameplay chạy được ở cả hai cách; đổi cách chạy chỉ cần đổi config, không sửa code của feature. Người yêu cầu.

### seeded-rng | AI 7 | Số ngẫu nhiên của gameplay tất định theo seed; mỗi lượt bản dev dùng đúng seed trong config
Hiện có ở `feature-wave` (xáo thứ tự quái, bốc bên ra). Không feature gameplay nào tự gọi bộ ngẫu nhiên riêng. Giữ seed config thì lặp lại được một lượt ngay trong cùng lần mở game.

### rng-per-feature | AI 7 | Mỗi feature lấy số ngẫu nhiên từ một dòng riêng, cùng sinh ra từ seed của trận
Một feature thêm hay bớt một lần bốc thì số ngẫu nhiên của feature khác không lệch theo.

### tester-fast-run | AI 7 | Cùng đội, config, seed, chuỗi thao tác và độ dài từng bước thì chạy lại ra cùng kết quả, kể cả khi tua nhanh
Thao tác giữ số bước và thứ tự trong bước. Ở cách theo khung hình, bản dev phát lại độ dài từng bước đã ghi, vì hai lần chạy có thể chia cùng một khoảng thời gian thành các bước khác nhau. Tua nhanh chỉ có ở bản dev. Người yêu cầu.

### clock-no-rules | AI 7 | Feature này chỉ cấp nhịp và số ngẫu nhiên, không giữ luật gameplay nào
Ai làm gì trong một nhịp vẫn do feature sở hữu quyết; tạm dừng vẫn là việc của `feature-run`, thứ tự trong nhịp giao chiến vẫn ở `feature-combat`.

### step-after-combat | AI 7 | Nhịp chỉ chạy bước kế tiếp sau khi giao chiến đã báo chốt xong bước hiện tại
Khi chạy nhiều bước trong một khung hay tua nhanh, mỗi bước mới đọc máu và kết quả đã chốt của bước trước.

### spawn-after-results | AI 7 | Đợt quái thả quái dựa trên kết quả giao chiến và lượt chơi đã xử lí xong
Đợt quái xét trần quái sống sau khi đã nhận đủ tin bị hạ; thành vỡ thì pha thả dùng trạng thái mới của lượt.

### keep-drawn-spawn | AI 7 | Khi trần quái sống chặn một lần thả, đợt quái giữ nguyên loại và bên ra đã bốc cho lần đó
Số bước chờ trống chỗ không đổi con sắp ra và không làm lệch các lần bốc sau.

### init-on-run | AI 7 | Khi lượt chơi yêu cầu dựng lượt mới, nhịp đặt số bước và thời gian dư về 0, chọn seed và khởi tạo các dòng ngẫu nhiên
`feature-run` quyết khi nào yêu cầu, kể cả khi lượt mới dùng lại scene.

### first-step-on-start | AI 7 | Bước gameplay đầu tiên chỉ chạy sau khi lượt chơi xác nhận lượt đã bắt đầu
Dựng trận và chờ HUD sẵn sàng không tiêu thời gian thả quái hay hồi chiêu.

### seed-record-owner | AI 7 | Nhịp ghi seed của lượt đúng một lần, lúc lượt chơi xác nhận lượt đã bắt đầu
Lần dựng trận hỏng không để lại bản ghi của một lượt chưa chơi.

### continue-after-revive | AI 7 | Thành hồi sinh thì nhịp tiếp tục số bước và vị trí bốc của các dòng ngẫu nhiên trong cùng lượt
Đợt quái chạy tiếp đúng lịch đã lập và dùng đúng số ngẫu nhiên kế tiếp.

### clear-on-dispose | AI 7 | Khi lượt chơi huỷ trận, nhịp dọn hàng chờ thao tác, thời gian dư và các dòng ngẫu nhiên của lượt
Dọn xong trước khi báo huỷ xong; lượt sau trong cùng scene bắt đầu với trạng thái riêng.

### revive-result-queued | AI 6 | Kết quả hồi sinh từ popup đi qua hàng chờ của nhịp để được ghi và phát lại theo số bước
Popup vẫn đóng trước khi báo kết quả. Bấm Xem chưa biết quảng cáo có thành công không, nên bản chạy lại đưa kết quả đã ghi tới lượt chơi ở đúng bước.
