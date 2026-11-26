# Save
Tầng: 6 · Features

Cất một bản ghi duy nhất xuống máy (phiên bản dữ liệu 1 và một số nguyên kỉ lục) và trả lại đúng số đó sau khi tắt mở game; không quyết giá trị, không có giao diện.

## Contracts

### ISavedRecord
- Dùng để: đọc và ghi kỉ lục đã lưu trên máy. Dùng được ở mọi scene. Feature dùng: Run (chỉ Run).
- `bool TryRead(out int record)`: xong ngay trong lần gọi. `true` khi bản ghi trên máy có phiên bản 1 và kỉ lục là số nguyên 32 bit (kể cả âm). `false` (chưa có, `record` = 0, không dùng) khi chưa từng ghi, bản ghi hỏng (thiếu dòng, phiên bản 0, âm hay không phải số, kỉ lục là chữ hay số lẻ) hoặc phiên bản lớn hơn 1. Đọc không bao giờ xoá hay sửa bản ghi.
- `bool Write(int record)`: xong ngay trong lần gọi, thay cả bản ghi bằng phiên bản 1 và số này. `true` khi đã nằm đủ trên máy. `false` khi máy từ chối ghi, hoặc trên máy là bản ghi phiên bản lớn hơn 1 (bản ghi đó giữ nguyên). Thất bại không ném lỗi, không hiện gì cho người chơi; bản dev ghi một dòng cảnh báo `[Save] write failed, <lí do>` vào log. Các lần ghi làm theo thứ tự gọi; bản còn lại là lần sau cùng. Bản ghi hỏng chỉ bị thay khi có lần ghi thành công.

## Bên trong
- `SavedRecord` (Internal): luật đọc ghi của `feature-save/save-io/io-rules`. Dạng bản ghi: hai dòng văn bản, `1` rồi kỉ lục.
- `FileRecordStore`: cổng thiết bị, một file `record.save` trong `Application.persistentDataPath`. Ghi đủ vào `record.save.tmp` rồi mới thay file chính, nên tắt game giữa lúc ghi vẫn còn bản cũ hoặc bản mới.
- `SaveInstaller.asset` trong `FeatureAssets/Public`: đăng ký `ISavedRecord`. Không phụ thuộc feature nào, không tham gia phiên.

Test: assembly `Game.Save.Tests` (EditMode); đồ giả máy hết chỗ `FakeStore` trong `SavedRecordTests.cs`.
