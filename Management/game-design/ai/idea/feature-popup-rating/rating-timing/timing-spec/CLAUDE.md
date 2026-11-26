# Lúc mời: thời điểm, cờ đã mời và trường hợp biên
Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Nghe tín hiệu "đã chốt kết quả thua" của `feature-run` (tín hiệu popup thua dùng để mở, ý `lose-opens-after-result` của `feature-popup`).
- Đếm 1.5 giây theo thời gian thật, không tính lúc game ở ngoài màn hình.
- Hết giờ: đọc cờ "kỉ lục mới" (`feature-run/run-score/score-rules#record-read-anytime`) và cờ "đã mời". Cờ kỉ lục bật và chưa mời thì xin dịch vụ mở `PopupRating/Main` (node `rating-invite/invite-spec`).
- Popup mời đã hiện thì bật cờ "đã mời".
- Trong lúc đếm, `feature-run` bắt đầu huỷ trận (người chơi đã chọn trên popup thua) thì dừng đếm, không mời.

## Thông số
- Bảng `TimingParams` (`params`) trong `.excel/`: `invite_delay` thời gian từ lúc trận dừng tới lúc xin mở, `max_invites_per_launch` số lần mời tối đa mỗi lần mở game.
- Độ trễ 1 giây trước popup thua là số của `feature-run/run-flow/flow-rules`, không chép vào bảng; `invite_delay` phải lớn hơn số đó.

## Thông số theo giai đoạn
- Cột `idea`, `prototype`, `full` của bảng `TimingParams`; mọi dòng giữ như IDEA.

## Trường hợp biên
- **Điểm 0:** cờ "kỉ lục mới" không bao giờ bật (`score-rules#record-update`), nên không mời.
- **Lượt đầu tiên sau khi cài game, điểm lớn hơn 0:** kỉ lục cũ là 0, cờ bật, nên được mời. Đây là cách chính để người chơi mới thấy lời mời.
- **Người chơi chạm nút popup thua trong 0.5 giây trước khi lời mời hiện:** huỷ lần mời; lượt sau phá kỉ lục vẫn được mời.
- **Rời game trong lúc đếm:** dừng đếm, quay lại thì đếm tiếp; popup thua vẫn hiện như luật của `feature-run`.
- **Ghi kỉ lục xuống máy thất bại:** cờ "kỉ lục mới" vẫn bật trong bộ nhớ (`score-rules#record-keep-on-fail`), nên vẫn mời.
- **Dịch vụ từ chối mở:** không mời, cờ "đã mời" giữ tắt, ghi cảnh báo.
- **Scene Game bị huỷ khi lời mời đang mở:** dịch vụ huỷ popup (`dispose-silent`); cờ "đã mời" đã bật nên không mời lại trong lần mở game đó.
- **Hai popup chồng:** popup thua và popup mời là 2, dưới mức tối đa 3 của dịch vụ.

## Chưa rõ
- Không còn.

## Đã tự quyết
- **Đếm từ tín hiệu chốt thua, không chờ tín hiệu "popup thua đã hiện".** `feature-popup-lose` không phát tín hiệu đó, và sửa nó nằm ngoài phạm vi. 0.5 giây dư sau 1 giây của `feature-run` giữ cho lời mời luôn nằm trên popup thua. Đã loại: mở cùng lúc với popup thua (dễ lẫn thứ tự chồng).
- **0.5 giây (dự kiến):** đủ để thấy dòng "Kỉ lục mới!", chưa đủ lâu để người chơi đã kịp chạm nút. Đã loại: 2 giây (dễ bị chạm Chơi lại trước).
