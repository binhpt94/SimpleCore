# Lưu dữ liệu

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Tắt game mở lại vẫn thấy kỉ lục của mình, mục tiêu để vượt không mất.

## Liên quan
- `feature-run`: chủ của kỉ lục. Lượt chơi giữ luật kỉ lục (khi nào đổi, mặc định 0), nhờ feature này đọc lúc mở game và ghi khi kỉ lục đổi.
- `feature-home`, `feature-popup-lose`: hiện kỉ lục, nhưng đọc từ `feature-run`, không gọi thẳng feature này.
- `feature-session-scene`: dữ liệu đã lưu là thứ duy nhất đi qua các scene.
- `feature-team-select`: đội được nhớ chỉ trong lần mở game, không lưu xuống máy.

## Đã tự quyết
- **Tách khỏi `feature-run`** theo người quyết 2026-11-26: đọc và ghi xuống máy là việc riêng, Run chỉ giữ luật kỉ lục.
- **Chỉ `feature-run` gọi feature này.** Màn bắt đầu và popup thua vẫn đọc kỉ lục từ Run, để có một nguồn duy nhất và không phải sửa hai feature đó. Đã loại: mỗi feature tự đọc kỉ lục từ đây (hai nơi đọc, dễ lệch với kỉ lục vừa đổi).
- **`code.json` là `Save` trong `Features/`.** Luật vòng 2 ghi chỉ ba feature hạ tầng mặc định là dịch vụ (`_Services/`), và scaffold dùng tên suy ra từ key. Người muốn đặt thành dịch vụ `_Services/Save` thì chỉ cần đổi `code.json`.
- **Không có giao diện.** Feature này không hiện gì cho người chơi.
- **Các ý `run-*` nằm ở đây, không ở `feature-run`.** Đó là cách lượt chơi dùng Lưu dữ liệu (đọc lúc mở game, mặc định 0, so, giao ghi, giữ khi ghi lỗi, thử ghi lại). Luật kỉ lục chi tiết đã có ở `feature-run/run-score`; viết lại ở node vòng 2 của Run sẽ bắt cả nhánh Run làm lại mà không thêm quyết định mới.
- **Loại `save-read-reasons` (GPT r2-1#phan-biet-loi-doc, AI 4).** Thay cho `save-missing-empty`. Lượt chơi xử lí mọi lí do giống nhau (kỉ lục 0, vẫn cho chơi), nên báo riêng bốn lí do chỉ thêm việc mà không đổi gì cho người chơi. Việc giữ dữ liệu chưa đọc được đã có ở `save-keep-unreadable`, việc nhận phiên bản ở `save-data-version`. Giữ `save-missing-empty`.
- **Hai lúc thử ghi lại đều giữ** (`run-retry-on-resume` của GPT r2-1, `run-retry-next-loss` từ góp ý chung r2-all-4#thu-ghi-lai-ki-luc). Hai ý không trái nhau; chấm 5 vì lỗi ghi hiếm.
- **Góp ý chung `_general/r2-all-4`** đã vào node này: doc-ki-luc-khi-mo → `run-reads-at-boot`, ki-luc-mac-dinh → `run-default-zero`, xet-ki-luc-khi-thua → `run-compares-record`, ghi-ki-luc-moi → `run-asks-write`, bao-loi-ghi → `save-write-result`, tat-app-khong-luu-luot → `save-no-run-state`, giu-ki-luc-khi-ghi-loi → `run-keep-on-fail`, thu-ghi-lai-ki-luc → `run-retry-next-loss`. Hai góp ý còn lại ghi ở `CLAUDE.md` của node gốc.
- **Chấm số phiên bản, chuyển dạng, giữ bản mới ở mức 5–6.** Hiện chỉ có một số nguyên nên ít khi cần, nhưng thêm sau thì khó vì dữ liệu cũ đã nằm trên máy người chơi.

## Chưa rõ
- Ghi kiểu gì để đọc được về sau khi dữ liệu thêm loại mới: vòng 3.
