---
based-on: ca84fa282d14
group: infra
refs: feature-run/run-score@691534d6dde7
by: run-compares-record=gpt@r2-1#run-so-ky-luc, run-asks-write=gpt@r2-1#run-ghi-ky-luc, run-reads-at-boot=gpt@r2-1#run-doc-khoi-dong, run-default-zero=gpt@r2-1#mac-dinh-ky-luc, save-keep-unreadable=gpt@r2-1#loi-doc-khong-ghi-de, save-atomic-write=gpt@r2-1#ghi-khong-tron-ven, save-write-result=gpt@r2-1#ket-qua-ghi, run-retry-on-resume=gpt@r2-1#thu-lai-ghi, save-write-order=gpt@r2-1#thu-tu-ghi, save-data-version=gpt@r2-1#phien-ban-du-lieu, save-migrate-old=gpt@r2-1#chuyen-doi-du-lieu, save-keep-newer=gpt@r2-1#giu-ban-khong-ho-tro, save-io-node=gpt@r2-1#node-xu-ly-luu
gpt-dropped: r2-1#phan-biet-loi-doc=save-read-reasons
---
# Lưu dữ liệu

Feature đọc và ghi dữ liệu xuống máy cho các feature khác, để dữ liệu còn sau khi tắt game. Feature này chỉ cất và trả lại; luật của từng loại dữ liệu thuộc feature sở hữu nó.

## Ý cốt lõi

### save-role | AI 8 | Lưu dữ liệu là nơi duy nhất trong game đọc và ghi dữ liệu xuống máy
Feature khác muốn giữ gì qua lần tắt game thì nhờ feature này, không tự ghi. Đã loại: mỗi feature tự ghi phần của mình (dễ trùng chỗ, khó kiểm).

### save-no-rules | AI 8 | Lưu dữ liệu không quyết giá trị; feature sở hữu dữ liệu giữ luật của nó
Ví dụ: `feature-run` quyết khi nào kỉ lục đổi; feature này chỉ cất số được giao và trả lại đúng số đó.

### save-record-only | AI 7 | Kỉ lục điểm là dữ liệu duy nhất game lưu xuống máy
Theo ý gốc "mỗi lượt bắt đầu lại từ đầu, chỉ giữ kỉ lục". Đội được nhớ, tiền, nâng cấp không lưu.

### save-no-run-state | AI 7 | Tắt game giữa lượt thì không lưu gì của lượt đó; mở lại chỉ còn kỉ lục đã ghi
Đội, điểm đang đếm và trạng thái trận không được khôi phục.

### save-survive-restart | AI 8 | Dữ liệu đã ghi còn nguyên sau khi tắt rồi mở lại game

### save-missing-empty | AI 7 | Chưa từng ghi, hoặc dữ liệu đọc không được, thì báo là chưa có; bên gọi tự chọn giá trị mặc định
Không chặn người chơi vì lỗi lưu.

### save-any-scene | AI 7 | Đọc và ghi được ở mọi scene, không phụ thuộc scene nào đang mở

### run-reads-at-boot | AI 8 | `feature-run` nhờ Lưu dữ liệu đọc kỉ lục lúc mở game, trước khi màn bắt đầu hiện
Màn bắt đầu không hiện nhầm số mặc định rồi mới đổi sang kỉ lục đã lưu.

### run-default-zero | AI 7 | `feature-run` dùng kỉ lục 0 khi Lưu dữ liệu báo chưa có
Giá trị mặc định là của bên sở hữu kỉ lục, theo `save-no-rules`.

### run-compares-record | AI 7 | `feature-run` so điểm với kỉ lục khi lượt chốt và chỉ đổi kỉ lục khi điểm lớn hơn
Bằng kỉ lục thì không phải kỉ lục mới.

### run-asks-write | AI 8 | Kỉ lục đổi thì `feature-run` giao Lưu dữ liệu ghi ngay, không chờ rời popup thua

### save-write-result | AI 6 | Mỗi lần ghi, Lưu dữ liệu báo lại thành công hay thất bại

### run-keep-on-fail | AI 7 | Ghi thất bại thì `feature-run` vẫn giữ kỉ lục mới trong lần mở game này
Màn bắt đầu và popup thua vẫn thấy cùng một kỉ lục; không hứa số đó còn sau khi mở lại.

### run-retry-on-resume | AI 5 | Kỉ lục chưa ghi được thì `feature-run` thử ghi lại khi game trở lại màn hình

### run-retry-next-loss | AI 5 | Kỉ lục chưa ghi được thì `feature-run` thử ghi lại ở lần chốt lượt kế tiếp
Kể cả khi lượt sau không phá kỉ lục.

### save-write-order | AI 6 | Các lần ghi được làm theo đúng thứ tự nhận
Lần ghi cũ không được xong sau và đè mất kỉ lục mới hơn.

### save-atomic-write | AI 7 | Ghi bị ngắt giữa chừng thì lần đọc sau trả về bản ghi đầy đủ gần nhất
Một lần mất điện không làm mất kỉ lục cũ.

### save-keep-unreadable | AI 6 | Đọc không được thì Lưu dữ liệu giữ nguyên dữ liệu đang có, không tự ghi đè bằng giá trị mặc định

### save-data-version | AI 6 | Dữ liệu lưu mang số phiên bản, Lưu dữ liệu xem số đó trước khi đọc nội dung

### save-migrate-old | AI 5 | Dữ liệu của phiên bản cũ được chuyển sang dạng mới mà vẫn giữ kỉ lục

### save-keep-newer | AI 5 | Dữ liệu của phiên bản mới hơn bản game đang chạy thì không bị ghi đè
Mở bằng bản game cũ không được phá dữ liệu của bản mới.

### save-io-node | AI 7 | Đọc, ghi và khôi phục dữ liệu lưu là một node con riêng
Gom kết quả đọc ghi, ghi bị ngắt và phiên bản vào một chỗ, tách khỏi luật kỉ lục của `feature-run`.
