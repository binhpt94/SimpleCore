# Chữ kết quả trên popup thua

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Khi `feature-run` nhờ mở popup thua (qua `feature-popup`): đọc điểm, kỉ lục, cờ kỉ lục mới từ `feature-run/run-score/score-rules`.
- Điền 3 dòng chữ; cờ bật thì thêm dòng "Kỉ lục mới!" kèm hiệu ứng nảy lặp.
- Nút bấm của popup ở `feature-popup-lose/lose-actions/actions-spec`.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số popup thua mỗi lượt | tối đa 1 | popup | Chỉ khi thua, không khi bỏ lượt |
| Dòng luôn hiện | 3 (tiêu đề, điểm, kỉ lục) | dòng | `lose-result` |
| Dòng có điều kiện | 1 ("Kỉ lục mới!") | dòng | Chỉ khi cờ bật |
| Chữ tiêu đề | "Thành đã bị phá" | | Nói vì sao thua |
| Chữ điểm | "Trụ được N đợt" | | Điểm = số đợt dọn sạch |
| Chữ kỉ lục | "Kỉ lục: M đợt" | | Cùng cách viết với màn bắt đầu |
| Hiệu ứng "Kỉ lục mới!" | phóng 1.0 → 1.15 → 1.0, chu kì 1 giây, lặp tới khi popup đóng (dự kiến) | giây | Anim bằng scale |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Tiêu đề, điểm, kỉ lục | Có, chữ đơn giản | Có | Có |
| Dòng "Kỉ lục mới!" | Có, trên dải nền nổi bật, không cần hiệu ứng | Có, kèm nảy lặp | Có, kèm nảy lặp và hiệu ứng pháo giấy |
| Popup hiện ra | Hiện ngay | Hiện ngay | Phóng từ nhỏ ra đủ cỡ trong 0.2 giây (dự kiến) |

## Trường hợp biên
- **Điểm 0, kỉ lục 0:** "Trụ được 0 đợt", "Kỉ lục: 0 đợt", không có dòng kỉ lục mới.
- **Phá kỉ lục:** N = M, có dòng kỉ lục mới.
- **Bằng kỉ lục cũ:** N = M nhưng không có dòng kỉ lục mới.
- **Ghi kỉ lục xuống máy thất bại:** popup vẫn hiện đúng kỉ lục trong lúc game mở, không báo lỗi.

## Đã tự quyết
- **Chữ "Trụ được N đợt" thay cho "Điểm: N".** Người chơi hiểu ngay điểm là số đợt. Đã loại: "Điểm: N" (phải giải thích thêm).
- **Không câu động viên khi không phá kỉ lục.** Theo node cha.
- **Một frame `main.frame` đủ 4 dòng chữ (2026-11-24, gộp hai frame).** Luật mới: mỗi frame vòng 4 là một ảnh claude-concept. Frame "không phá kỉ lục" chỉ thiếu dòng "Kỉ lục mới!" so với frame "phá kỉ lục", nên là ảnh thừa; giữ một frame có cả dòng đó (ghi "chỉ khi phá kỉ lục"), số mẫu 16/16. Hiệu ứng nảy ghi ở bảng Thông số, không ghi trên frame. Đã loại: giữ hai frame (hai ảnh gần trùng nhau).
- **Dòng "Kỉ lục mới!" có dải nền riêng (2026-11-24).** Vòng 3 đòi lượt phá kỉ lục "trông vui hơn hẳn"; chữ trơn không làm được điều đó và ảnh claude-concept cũng không thể hiện (chữ do tool in). Thêm hình `new-record-bg` (round-rect 400x70, cùng chỗ với dòng chữ) để @ClaudeImage vẽ thành một phần riêng; dải nảy cùng dòng chữ. Đã loại: chỉ đổi màu chữ (không vẽ được phần, nhìn không khác bao nhiêu); pháo giấy (để FULL).
- **Frame vẽ cả popup (2026-11-23).** Frame có nền che toàn màn (cờ `overlap` cho khung popup vì cố ý nằm trên nền che) và hai nút lấy số từ `feature-popup-lose/lose-actions/actions-spec` (`btn-y`, `replay-x`, `replay-w`, `home-x`, `home-w`). Node này sở hữu vị trí các dòng chữ (`text-x`, `text-w`, `title-y`, `title-h`, `row-h`, `score-y`, `record-y`, `newrec-*`); actions-spec `use` lại. Số mẫu chỉ để nhìn bố cục và gen ảnh concept, không phải thông số. Đã loại: mỗi node chỉ vẽ nửa của mình kèm hộp giữ chỗ (không thấy được popup thật, không dùng để gen ảnh được).
- **Dòng "Kỉ lục mới!" nằm ở khoảng trống giữa kỉ lục và hai nút** (y 340–410 trong khung, nút bắt đầu ở 540). Không có dòng này thì khoảng trống để trống; bố cục hai trạng thái giữ nguyên, nút không nhảy chỗ.

## Chưa rõ
- Không có điểm chặn.
