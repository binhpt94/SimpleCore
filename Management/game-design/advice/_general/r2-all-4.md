---
advice: r2-all-4
node: (không gắn node nào)
also: feature-save, feature-run, feature-team-select, feature-popup-lose
asked: 2026-11-26 18:13:58
by: gpt (Codex CLI)
---
# Góp ý r2-all-4

## Câu hỏi

# Cả bộ feature sau khi thêm feature-save

Ngày 2026-11-26 người quyết tách việc đọc và ghi dữ liệu xuống máy ra khỏi `feature-run`, thành feature mới `feature-save` (Lưu dữ liệu). Các lần hỏi chung trước (`r2-all`, `r2-all-2`, `r2-all-3`) có từ trước khi có feature này.

## Câu hỏi
Xét cả bộ feature, nhất là chỗ `feature-save` chạm `feature-run`, `feature-home`, `feature-popup-lose`, `feature-team-select`, `feature-session-scene`:
1. **Trùng việc:** feature nào còn tự đọc hay ghi dữ liệu xuống máy, hay tự giữ một phần việc mà nay `feature-save` giữ? Việc đó nên thuộc bên nào?
2. **Hở chỗ:** dữ liệu nào cần còn sau khi tắt game (kỉ lục, đội đã chọn lần trước, cài đặt...) mà chưa feature nào nhận là chủ? Ai đọc dữ liệu lúc mở game, ai ghi lúc nào (hết lượt, thoát giữa lượt, tắt app)?
3. **Ranh giới mơ hồ:** ai quyết khi nào kỉ lục đổi, ai hiển thị kỉ lục ở Home và ở PopupLose, ai nhớ đội đã chọn? `feature-session-scene` dọn dữ liệu của lượt có đụng dữ liệu đã lưu không?
4. **Tách, gộp, thêm:** có nên tách, gộp hay thêm feature nào quanh việc lưu không?

Mỗi góp ý ghi rõ feature đích (hoặc tên key đề xuất cho feature mới). Chỗ đã ổn thì không viết gì.

## Yêu cầu riêng của người
- Không ghi dữ liệu theo giai đoạn (bản đầu, MVP, làm trước / để sau).

## Bên hỏi muốn
@GameDesigner cần các góp ý cụ thể về ranh giới để đưa vào node, trước khi hỏi riêng `feature-save`.

## Góp ý của GPT

### doc-ki-luc-khi-mo | thêm | feature-run đọc kỉ lục qua feature-save khi mở game, trước khi màn bắt đầu hiện.
`feature-home` cần có giá trị từ `feature-run` ngay khi hiện; việc đọc không thuộc màn hình hay `feature-session-scene`. Xem `feature-save`, `feature-home` và `feature-game-flow`.

### ki-luc-mac-dinh | thêm | feature-run dùng kỉ lục 0 khi feature-save báo chưa có dữ liệu đọc được.
Quy định một giá trị thống nhất cho lần đầu mở game và khi dữ liệu không đọc được, để `feature-home` và `feature-popup-lose` không phải tự chọn. Xem `feature-save`, `feature-home` và `feature-popup-lose`.

### xet-ki-luc-khi-thua | thêm | feature-run chỉ đổi kỉ lục khi chốt thua và điểm lượt lớn hơn kỉ lục hiện tại.
Điểm bằng kỉ lục không được tính là phá kỉ lục; thành vỡ rồi hồi sinh chưa phải lúc xét. Xem `feature-popup-revive` và `feature-popup-lose`.

### ghi-ki-luc-moi | thêm | feature-run yêu cầu feature-save ghi kỉ lục ngay khi chốt được kỉ lục mới.
Ghi tại lúc thay đổi, không chờ đóng app; `feature-save` chịu trách nhiệm ghi xuống máy. Xem `feature-save`.

### bo-luot-khong-xet-ki-luc | thêm | feature-run không xét hay ghi kỉ lục khi người chơi bỏ lượt từ popup tạm dừng.
Quy định rõ đường kết thúc không có popup thua, tránh mỗi feature tự quyết có lưu điểm hay không. Xem `feature-popup-pause` và `feature-save`.

### tat-app-khong-luu-luot | thêm | feature-run không lưu trạng thái lượt khi game bị tắt giữa lượt.
Mở lại bắt đầu từ màn đầu, chỉ khôi phục kỉ lục đã ghi; đội, điểm và trạng thái trận chưa kết thúc không được khôi phục. Xem `feature-save`, `feature-team-select` và `feature-scene-flow`.

### don-scene-khong-xoa-luu | thêm | feature-session-scene dọn trạng thái của scene khi rời scene mà không xoá dữ liệu feature-save đã ghi.
Dọn trận và dọn scene không làm mất kỉ lục đã lưu; `feature-save` phải tiếp tục phục vụ khi scene thay đổi. Xem `feature-save` và `feature-run`.

### bao-loi-ghi | thêm | feature-save trả kết quả thành công hoặc thất bại cho mỗi yêu cầu ghi kỉ lục.
`feature-run` cần phân biệt kỉ lục đang giữ trong phiên với giá trị đã ghi thành công, vì lỗi ghi không được làm game ngừng chơi. Xem `feature-run`.

### giu-ki-luc-khi-ghi-loi | thêm | feature-run giữ kỉ lục mới trong phiên hiện tại khi feature-save báo ghi thất bại.
Màn bắt đầu và popup thua vẫn dùng cùng kỉ lục trong phiên; không khẳng định giá trị đó sẽ còn sau khi mở lại. Xem `feature-save`, `feature-home` và `feature-popup-lose`.

### thu-ghi-lai-ki-luc | thêm | feature-run thử ghi lại kỉ lục chưa lưu thành công khi chốt thua ở lượt tiếp theo.
Kỉ lục mới có cơ hội được lưu lại cả khi lượt sau không phá kỉ lục, không cần chờ sự kiện tắt app. Xem `feature-save`.
