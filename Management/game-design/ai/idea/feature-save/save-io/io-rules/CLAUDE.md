# Luật đọc ghi bản ghi lưu

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Cất một bản ghi duy nhất trên máy: phiên bản dữ liệu và kỉ lục.
- Đọc: trả "có kỉ lục" kèm số, hoặc "chưa có" (theo `rules-read-cases`).
- Ghi: nhận một số nguyên, thay cả bản ghi, trả "thành công" hoặc "thất bại".
- Không có giao diện, không có cheat riêng: cheat kỉ lục nằm ở `feature-home/cheat` và `feature-popup-lose/cheat`. Cheat nào cần đổi kỉ lục trên máy thì cũng đi qua `feature-run` rồi tới đây, không ghi thẳng.

## Thông số
Bảng `Params` (`params`) trong `.excel/Params.csv`: phiên bản dữ liệu, kiểu số của kỉ lục, số bản ghi, thời gian một lần đọc ghi, số lần đọc. File `io-rules.xlsx` chưa dựng được vì máy thiếu thư viện openpyxl (guard chặn cài); có openpyxl thì chạy `gd_excel.py sync`.

## Thông số theo giai đoạn
Cột `idea`, `prototype`, `full` của bảng `Params`. Mọi thông số giữ nguyên từ IDEA: chỉ có một số nguyên, không có gì để thêm theo giai đoạn.

## Trường hợp biên
- **Lần đầu mở game:** không có bản ghi → chưa có → `feature-run` dùng 0. Chưa ghi gì cho tới khi có kỉ lục mới.
- **Phá kỉ lục với điểm 1 ở lượt đầu:** ghi phiên bản 1, kỉ lục 1.
- **Tắt game giữa lúc ghi:** mở lại đọc được kỉ lục cũ hoặc mới, không ra chưa có.
- **Bản ghi phiên bản 2 (bản game mới hơn từng chạy):** đọc chưa có, ghi thất bại; lượt chơi vẫn chơi với kỉ lục trong bộ nhớ, bản ghi phiên bản 2 còn nguyên.
- **Bản ghi phiên bản 0 hoặc âm, hay không phải số:** hỏng → chưa có; không xoá; lần ghi thành công sau thay nó.
- **Kỉ lục cất là chữ hoặc số lẻ (ví dụ 3.5):** hỏng → chưa có.
- **Máy hết chỗ hay không cho ghi:** thất bại, không hiện gì; bản dev ghi cảnh báo.
- **Hai lần ghi liền nhau:** làm theo thứ tự, bản còn lại là lần sau cùng.

## Đã tự quyết
- **Ghi xong trong lần gọi.** Bên gọi không phải chờ, nên `record-save-now` của `feature-run/run-score/score-rules` (ghi trước khi popup thua hiện) giữ được đơn giản.
- **Không xét số âm ở đây.** Luật giá trị của lượt chơi (`save-no-rules`).
- **Cách ghi trọn vẹn để phiên code chọn**, miễn kiểm được `rules-write-whole`. Ví dụ: ghi bản mới đủ rồi mới thay bản cũ.

## Chưa rõ
- Không có điểm chặn.
