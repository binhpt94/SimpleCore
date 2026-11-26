# Luật chuyển màn của lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
IDEA (chỉ có màn trận; feature này tự đổi màn):
- Mở game và chơi lại: dựng trận mới với đội mặc định, báo lượt mới (`feature-run/run-start`).
- Khi thành bị phá: dừng trận ngay, chốt điểm (`feature-run/run-score`), chờ 1 giây, nhờ `feature-popup` hiện popup thua.

PROTOTYPE trở đi (bảng đường đi giữa ba màn chính thuộc `feature-game-flow`, nhánh đường đi của nó; feature này không hiện màn bắt đầu hay màn chọn đội, không giữ trạng thái màn chính):
- **Vào trận:** điều hướng màn giao đội và xin dựng trận. Feature này dựng trận mới, báo lượt mới, rồi báo điều hướng màn "dựng xong" (hoặc "lỗi"); điều hướng màn mới hiện màn trận.
- **Trong trận:** feature này giữ mọi bước trong trận: thành bị phá, chờ 1 giây, popup thua.
- **Rời trận:** nhận lựa chọn từ `feature-popup-lose` (chơi lại, về màn bắt đầu). Feature này chốt điểm nếu chưa chốt, đóng popup đang mở, huỷ mọi thứ của trận, rồi chuyển đúng lựa chọn đó cho điều hướng màn. Màn đích do điều hướng màn chọn và hiện.
- Các báo nút của `feature-home` (Chơi) và `feature-team-select` (Vào trận, Quay lại) đi thẳng tới điều hướng màn, không qua feature này.

Các tin cần có giữa các feature ở PROTOTYPE (tên và hình thức contract do @CodeLead đặt):
| Từ | Tới | Tin |
|---|---|---|
| `feature-scene-flow` | `feature-game-flow` | scene Game sẵn sàng |
| `feature-home` | `feature-game-flow` | người chơi bấm Chơi |
| `feature-team-select` | `feature-game-flow` | bấm Vào trận kèm đội đã chọn; bấm Quay lại |
| `feature-game-flow` | `feature-run` | xin dựng trận với đội này |
| `feature-run` | `feature-game-flow` | trận đã dựng xong; dựng trận lỗi |
| `feature-popup-lose` | `feature-run` | lựa chọn rời trận (chơi lại, về màn bắt đầu) |
| `feature-run` | `feature-game-flow` | đã dọn trận xong, kèm lựa chọn rời trận |

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số scene | 2 (Loading, Game) | scene | Theo `feature-scene-flow/scene-list` |
| Màn đầu tiên trong scene Game | Trận, đội mặc định | | `boot-to-home`; từ PROTOTYPE là màn bắt đầu (bảng giai đoạn) |
| Độ trễ trước popup thua | 1 (dự kiến) | giây | Đủ thấy thành bị phá, không làm chậm chơi lại |
| Số lần bấm có tác dụng trong lúc đổi màn | 1 | lần | Tránh mở trận hai lần |
| Thời gian đổi màn | 0 (đổi ngay) | giây | Mức đơn giản nhất; hiệu ứng ở bảng giai đoạn |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Màn sau Loading | Vào thẳng trận với đội mặc định (`feature-team-select/team-memory/memory-rules`) | Màn bắt đầu | Màn bắt đầu |
| Chơi lại ở popup thua | Vào thẳng trận mới với đội mặc định | Sang màn chọn đội | Sang màn chọn đội |
| Về màn bắt đầu ở popup thua | Không có (chưa có màn bắt đầu) | Có | Có || Độ trễ trước popup thua | 1 giây | 1 giây | 1 giây || Hiệu ứng đổi màn | Không | Không | Mờ dần ra rồi vào, 0.3 giây mỗi chiều (dự kiến) |
| Ai chọn và hiện màn đích (bắt đầu, chọn đội, trận) | Feature này (chỉ có màn trận) | `feature-game-flow`; feature này chỉ dựng trận, dọn trận và chuyển lựa chọn rời trận | Như PROTOTYPE |
| Trạng thái màn chính (bắt đầu, chọn đội, trận) | Không cần (luôn là trận) | Giữ ở `feature-game-flow`, không giữ ở đây | Như PROTOTYPE |

## Trường hợp biên
- **Thành bị phá cùng khung hình với lúc dọn sạch đợt:** xử lí thành bị phá trước; đợt đó không được tính vào điểm.
- **Rời game đúng lúc thành vừa bị phá (trong 1 giây chờ):** thời gian ở ngoài không tính vào 1 giây; quay lại thì popup thua hiện.
- **Bấm "Chơi lại" và "Về màn bắt đầu" gần như cùng lúc:** chỉ lần bấm đầu tính (`transition-lock`).
- **Đang ở màn bắt đầu hoặc màn chọn đội mà rời game:** không có gì dừng; quay lại vẫn ở màn đó.
- **Nút quay lại của máy (Android):** ở màn bắt đầu, màn chọn đội, trong trận đang đánh, popup thua và trong 1 giây chờ: không làm gì (trong trận: tự quyết 2026-11-26, vì không còn tạm dừng; đã loại: thoát game, hỏi rời trận).

## Đã tự quyết
- **Mọi màn trong scene Game, không tách scene trận.** Theo `feature-scene-flow/scene-list` và ý `flow-scenes` của node cha (đã sửa cho khớp, 2026-11-23). Lượt mới luôn sạch vì trận được dựng mới và huỷ hết mỗi lần (`fresh-battle`). Đã loại: tải lại scene Game mỗi lượt (chậm, trái `no-return-loading`).
- **Mở game vào thẳng trận (2026-11-25, sửa lệch, theo @Manager).** Ý `boot-to-home` và hàng "Màn đầu tiên" bản trước ghi màn bắt đầu, trái với bảng giai đoạn của chính node này ("Màn sau Loading: vào thẳng trận"). Ý cốt lõi nêu giá trị IDEA; giữ `id` cũ dù tên không còn khớp. Đã loại: giữ màn bắt đầu (IDEA không có màn đó).
- **Chờ 1 giây trước popup thua.** Popup hiện ngay thì người chơi không hiểu vì sao thua. Đã loại: 3 giây (chậm chơi lại).
- **Bỏ qua báo dọn sạch tới sau khi chốt điểm.** Điểm chỉ chốt một lần.

- **Từ PROTOTYPE, điều hướng màn giữ bảng đường đi; lượt chơi chỉ dựng, dọn trận và lo các bước trong trận (2026-11-26, @Manager chuyển báo chặn của GameFlow).** Theo quyết định đã chốt: người chấm `flow-buttons-send-choice` 2 và các ý `run-switches-screens`, `team-handoff`, `run-routes` của lượt chơi bị loại ở vòng 2 ("điều hướng màn là nơi duy nhất chọn màn đích"; rời trận đi qua lượt chơi để dọn trận trước, ý `leave-choice-after-dispose`). Bảng giai đoạn của `feature-game-flow/flow-routes/routes-values` đã ghi điều hướng màn đổi màn từ PROTOTYPE; node này sửa mục Làm gì và bảng giai đoạn cho khớp, không đổi ý nào (các ý mô tả đường đi của người chơi, đúng ở cả hai giai đoạn). Đã loại: lượt chơi giữ việc đổi màn ở PROTOTYPE (trái các quyết định trên, hai feature cùng giữ bảng đường đi). Phiên Run bỏ các bước đổi sang màn bắt đầu, màn chọn đội ở PROTOTYPE; phiên GameFlow giữ chúng.
- **Rà lại theo `flow-scenes` mới của node cha (2026-11-23).** Các ý đã khớp sẵn; chỉ ghi `based-on` mới và xoá ghi chú mâu thuẫn.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ popup hồi sinh, popup tạm dừng, bỏ lượt và nút quay lại = tạm dừng; thành bị phá là chờ 1 giây rồi hiện popup thua.

## Chưa rõ
- Không có điểm chặn.
