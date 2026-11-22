# Danh sách tham gia và xử lí lỗi từng bước

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Scene Loading: danh sách gồm `feature-scene-flow` (view màn Loading, `feature-scene-flow/scene-list/boot-loading`).
- Scene Game: danh sách theo thứ tự dưới đây. Tải và chạy từ 1 tới 14; huỷ từ 14 về 1.

| # | Feature | Vì sao đứng ở đây |
|---|---|---|
| 1 | `feature-popup` | Dịch vụ; mọi feature sau có thể mở popup |
| 2 | `feature-run` | Giữ luồng lượt chơi, kỉ lục; các feature trận nghe "lượt mới bắt đầu" từ nó |
| 3 | `feature-wallet` | Tiền; castle, enemy, upgrade dùng |
| 4 | `feature-castle` | Thành; hero sinh ra từ thành, quái nhắm vào thành |
| 5 | `feature-hero` | Hero |
| 6 | `feature-enemy` | Quái |
| 7 | `feature-wave` | Đợt quái; cần enemy |
| 8 | `feature-upgrade` | Nâng cấp; cần wallet, hero, castle |
| 9 | `feature-home` | Màn bắt đầu |
| 10 | `feature-team-select` | Màn chọn đội |
| 11 | `feature-hud` | HUD trận |
| 12 | `feature-hud-upgrade` | Thanh nâng cấp; cần upgrade, wallet |
| 13 | `feature-popup-lose` | Popup thua; cần popup, run |
| 14 | `feature-popup-pause` | Popup tạm dừng; cần popup, run |

- Bước tải: lần lượt từng feature, feature sau bắt đầu tải khi feature trước xong. Báo tiến độ sau mỗi feature (thanh tiến độ của màn Loading).
- Bước chạy: lần lượt từng feature tạo view; cả bước xong trong một khung hình.
- Bước huỷ: lần lượt ngược lại; mỗi feature huỷ view và bỏ thứ đã tải.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số feature tham gia scene Loading | 1 | feature | Chỉ có màn Loading |
| Số feature tham gia scene Game | 14 | feature | Mọi feature có view hoặc cần tải, trừ `feature-scene-flow`, `feature-session-scene`, `feature-balance` |
| Thời gian tải tối đa một feature | 10 (dự kiến) | giây | Đủ rộng cho máy yếu; quá mức này gần như chắc là lỗi |
| Thời gian bước chạy cả scene | 1 | khung hình | Không tải gì thêm ở bước chạy |
| Thời gian bước huỷ cả scene | không giới hạn, không chờ tải | — | Huỷ chỉ dọn, không chờ mạng hay file |
| Cách tải | Lần lượt | — | Dễ biết feature nào lỗi, tiến độ rõ |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Cách tải các feature | Lần lượt | Lần lượt | Lần lượt (xét tải song song nếu quá 3 giây) |
| Thời gian tải tối đa một feature | 10 giây (dự kiến) | 10 giây | 10 giây |
| Số feature scene Game | 14 | 14 (thêm nếu có feature mới) | Theo danh sách feature lúc đó |

## Trường hợp biên
- **Feature thứ k tải lỗi**: không tải feature k+1 trở đi; huỷ feature k (phần đã tải được) rồi k-1 về 1; báo lỗi nêu tên feature k.
- **Feature thứ k chạy lỗi**: không chạy k+1 trở đi; huỷ từ 14 về 1 (mọi feature đều đã tải); báo lỗi nêu tên feature k.
- **Feature lỗi ở bước huỷ**: ghi lỗi, huỷ tiếp feature đứng trước nó; bước huỷ luôn đi hết danh sách.
- **Yêu cầu huỷ đến khi scene đang tải**: chờ feature đang tải xong (hoặc quá 10 giây), rồi huỷ các feature đã tải theo thứ tự ngược.
- **Một feature có tên trong danh sách hai lần**: lỗi ngay trước bước tải, nêu tên feature trùng; scene không tải.
- **Danh sách rỗng**: scene vẫn đi qua ba bước, không có view nào (chỉ xảy ra khi cấu hình sai; ghi cảnh báo).
- **Người chơi rời game lúc đang tải**: tải tiếp khi quay lại; thời gian rời game không tính vào 10 giây.

## Đã tự quyết
- **Tải lần lượt, không song song.** Đơn giản, biết rõ feature nào lỗi, thanh tiến độ thật. Đã loại: tải song song (nhanh hơn nhưng khó lần lỗi).
- **Popup đứng đầu danh sách scene Game.** Feature nào cũng có thể cần mở popup khi chạy; popup huỷ sau cùng nên không popup nào còn chủ mà mất dịch vụ.
- **Run đứng trước các feature trận.** Các feature trận nghe "lượt mới bắt đầu" từ `feature-run/run-start`.
- **Balance không tham gia.** Số liệu nằm trong cấu hình của từng feature (`feature-balance/balance-config`), không có view hay việc tải riêng.
- **Lỗi tải, chạy thì dừng; lỗi huỷ thì đi tiếp.** Tải thiếu là chơi sai; huỷ dở là rò rỉ.

## Chưa rõ
- Thứ tự giữa `feature-hero` và `feature-enemy` không phụ thuộc nhau; giữ như bảng, phiên code báo nếu cần đổi.
- Thêm feature mới vào game thì phải thêm vào bảng này.
