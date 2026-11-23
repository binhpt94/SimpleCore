# Ba bước của một scene và feature tham gia

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một scene có một danh sách tham gia; một danh sách có nhiều feature.
- Một feature có thể tham gia nhiều scene (mỗi scene là một lần tham gia riêng).
- `feature-scene-flow` quyết định khi nào một scene bắt đầu tải và khi nào bị huỷ; dịch vụ này lo các bước bên trong.
- `feature-popup` là một bên tham gia của scene Game: popup sống và chết theo scene.

## Định nghĩa
- `glossary#participant`

## Danh sách
- Ba bước: tải → chạy → huỷ.
- Danh sách tham gia của scene Game gồm các feature chơi (ví dụ `feature-home`, `feature-team-select`, `feature-castle`, `feature-hero`, `feature-wave`, `feature-hud`) và các dịch vụ (`feature-popup`). Danh sách đầy đủ chốt ở vòng 4.
- Danh sách tham gia của scene Loading: phần lo tải và xin chuyển sang Game.

## Luật cơ bản
- Bước sau chỉ bắt đầu khi bước trước đã xong với mọi feature tham gia.
- Tải và chạy theo thứ tự danh sách; huỷ theo thứ tự ngược lại.
- Feature có view hoặc cần tải thì bắt buộc tham gia.
- Không view nào có trước bước chạy hay còn lại sau bước huỷ.

## Đã tự quyết
- **Danh sách cố định cho từng scene.** Dễ đọc scene gồm những gì. Đã loại: tự đăng kí lúc chạy.
- **Chờ tải xong hết mới chạy.** Tránh view hiện ra thiếu hình. Đã loại: feature tải xong trước thì chạy trước.
- **Huỷ ngược thứ tự.** Feature đứng sau thường dựa vào feature đứng trước.

## Chưa rõ
- Thứ tự cụ thể của các feature trong danh sách scene Game: vòng 4.
