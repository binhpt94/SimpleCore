# Kiểm tra dọn sạch sau bước huỷ

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ một bộ đếm cho mỗi feature tham gia: số view đang có và số việc đang chạy (hẹn giờ, việc chờ, việc lặp mỗi khung hình). Chỉ bật ở bản dev.
- Sau bước huỷ của một scene: đọc bộ đếm của mọi feature tham gia; phải bằng 0. Lớp che của `feature-scene-flow` không tính.
- Còn sót: ghi lỗi nêu tên feature và số còn lại.
- Cho feature khác đọc số đếm bất kì lúc nào trong scene để tự kiểm.
- Ngay sau bước chạy: đọc số view của mọi feature tham gia tạo view ở bước chạy; số nào bằng 0 thì ghi lỗi nêu tên feature đó.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| View còn lại sau bước huỷ | 0 | view | `nothing-survives` của node cha |
| Việc đang chạy còn lại sau bước huỷ | 0 | việc | `nothing-survives` của node cha |
| Hình không tính | lớp che của `feature-scene-flow` | | Chỉ sống trong lúc chuyển |
| Bộ đếm có ở bản phát hành | không | | Chỉ để kiểm ở bản dev |
| Số view tối thiểu ngay sau bước chạy, mỗi feature tạo view ở bước chạy | 1 | view | `running-views-nonzero`: báo 0 là quên báo view |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Kiểm sau bước huỷ | Không bắt buộc (chỉ một lần chuyển, Loading → Game) | Đếm view và việc đang chạy | Như PROTOTYPE |
| Bộ đếm theo feature tham gia | Đếm view (đã làm, chưa dùng để kiểm) | Đếm view và việc đang chạy | Như PROTOTYPE |
| Kiểm view lớn hơn 0 sau bước chạy | Không (các feature chưa báo view, TrackView để PROTOTYPE) | Có | Có |

## Trường hợp biên
- **Feature lỗi ở bước huỷ**: vẫn huỷ tiếp các feature còn lại (luật xử lí lỗi ở nhánh ba bước của dịch vụ); số đếm của feature lỗi vẫn được đọc và ghi lỗi nếu còn sót.
- **Lớp che của chuyển scene còn hiện lúc đếm**: không tính.
- **Feature không có view hay việc nào**: số đếm 0, không ghi gì.
- **Feature chỉ có view về sau** (quái khi đợt thả, popup thua khi mở): không vào kiểm ngay sau bước chạy; view của nó vẫn được đếm và kiểm sau bước huỷ.
- **Feature không có view** (ví hay dịch vụ chỉ có luật, `feature-scene-flow` trong scene Game): không vào kiểm view lớn hơn 0.

## Đã tự quyết
- **Kiểm bằng số đếm, không bằng mắt.** Đếm view và việc đang chạy thì kiểm được. Đã loại: "chơi lại vẫn mượt" (không kiểm được).
- **Thu hẹp node về đúng việc của dịch vụ (2026-11-25, người chọn hướng A).** Bản trước có cả kiểm chơi lại nhiều lượt, popup sót ở đầu lượt, chỉ lưu kỉ lục và các trường hợp biên của lượt. Đó là luật của `feature-run` (đặt lại lượt trong scene, không qua vòng đời scene), dịch vụ popup và các feature trận. Các ý `replay-no-growth`, `popup-cleared-on-reset`, `only-record-saved` và các trường hợp biên đó chuyển sang node kiểm tra chơi lại mới dưới nhánh bắt đầu lượt của `feature-run`. Đã loại: giữ ở đây (sổ yêu cầu giao cho SessionScene việc nó không có code).
- **Thêm bộ đếm theo feature và lỗi nêu tên.** Làm rõ "cách đếm" mà bản trước chỉ ghi ở mục Làm gì; node kiểm tra chơi lại của `feature-run` dùng lại số đếm này.

- **Thêm chốt chặn view lớn hơn 0 lúc trận chạy (2026-11-25, người duyệt đề xuất session-usage-tracking theo hướng A, qua @Manager).** Ý `running-views-nonzero`. Nếu một feature quên báo view thì bộ đếm của nó luôn 0, nên kiểm sau bước huỷ báo "sạch" sai; kiểm ngay sau bước chạy bắt được lỗi đó và nêu đúng tên. Chỉ kiểm feature tạo view ở bước chạy, vì quái hay popup chưa có view lúc đó. Cần bộ đếm view từ IDEA (trước ghi "không bắt buộc"). Đã loại: kiểm ở một lúc bất kì giữa trận (lúc thở không có quái thì báo lỗi sai).

- **Kiểm view lớn hơn 0 sau bước chạy dời sang PROTOTYPE (2026-11-25, người quyết qua @Manager).** Ý `running-views-nonzero` đã là việc PROTOTYPE trong sổ, và việc báo view (TrackView) của các feature người đã hoãn tới PROTOTYPE; bảng giai đoạn trước ghi "Có" từ IDEA nên lệch. Sửa dòng đó thành "Không" ở IDEA. Bộ đếm view ở IDEA vẫn giữ vì đã làm xong.

## Chưa rõ
- Feature nào tạo view ở bước chạy: mỗi feature tự khai cho dịch vụ (phiên code cùng @CodeLead); bảng danh sách tham gia ở `feature-session-scene/session-steps/participant-order` không ghi.
