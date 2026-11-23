# Danh sách scene và đường đi

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Game có hai scene; mỗi lần chạy game đi qua Loading đúng một lần rồi ở lại Game.
- Một scene có nhiều màn (scene Game: màn bắt đầu, chọn đội, trận, popup). Một màn chỉ thuộc một scene.
- Vòng đời từng scene (tải, chạy, huỷ) do `feature-session-scene` lo; feature này chỉ quyết định đi từ scene nào sang scene nào.
- Thứ tự các màn trong scene Game do `feature-run` lo, không phải feature này.

## Định nghĩa
- `glossary#scene`
- `glossary#screen`
- `glossary#scene-loading`
- `glossary#scene-game`

## Danh sách
- Scene Loading.
- Scene Game: màn bắt đầu (`feature-home`), màn chọn đội (`feature-team-select`), trận, popup thua (`feature-popup-lose`), popup tạm dừng (`feature-popup-pause`).

## Luật cơ bản
- Mở game: vào Loading, tải xong tự sang Game.
- Vào Game thì màn đầu tiên là màn bắt đầu (`feature-home`).
- Đổi giữa các màn trong Game không đi qua feature này.
- Không quay lại Loading khi đang chơi.

## Đã tự quyết
- **Mọi màn chơi chung scene Game** (theo đề xuất mặc định). Lí do: lượt chơi ngắn, chơi lại phải nhanh; scene Game có ít màn. Đã loại: mỗi màn một scene; trận ở scene riêng.
- **Loading không có nút bấm.** Màn bắt đầu đã có nút chơi. Đã loại: "chạm để chơi" ở Loading.
- **Không quay lại Loading.** Không có lí do tải lại từ đầu trong một lần chạy game.

## Chưa rõ
- Loading hiện gì cho người chơi (tên game, thanh tiến độ): vòng 4.
