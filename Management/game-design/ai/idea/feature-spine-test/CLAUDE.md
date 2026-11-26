# Nhân vật thử Spine

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Không có gì cho lối chơi. Người làm game xem được một nhân vật Spine chạy animation giữa sprite 2D của trận: có hiện đúng không, đổi animation có giật không, lặp và chạy một lần có đúng không, cỡ, thứ tự vẽ, quay mặt có sai không, trước khi quyết có dùng Spine cho hero, quái hay không.

## Liên quan
- `feature-battlefield`: nhân vật đứng trên dải đất, theo camera và thứ tự vẽ của nền chiến trường.
- `feature-hero`, `feature-enemy`, `feature-castle`: chỉ để đặt cạnh khi so cỡ và thứ tự vẽ; nhân vật không đọc, không gửi gì cho chúng.
- `feature-hud`, `feature-popup`: HUD và popup vẽ trên nhân vật như trên mọi thứ của trận.
- `feature-button-test`, `feature-cube-test`: hai feature thử khác cùng nằm trên màn trận; chỗ đứng và nút của Spine không chồng lên chúng.
- Không feature nào gọi tới feature này, và nó không gọi feature nào.

## Đã tự quyết
- **Feature riêng**, không gộp vào `feature-cube-test` hay `feature-hero`: người yêu cầu một feature thử Spine riêng (2026-11-26); bỏ được mà không đụng gì khác.
- **Đặt trên chiến trường của màn trận**, như `feature-cube-test`: chỉ ở đó mới so được với hero, quái, HUD, popup. Đã loại: màn riêng.
- **Một nhân vật**, không phải nhiều: các điều cần kiểm đều thấy được trên một nhân vật bằng cách đổi animation và quay mặt. Đã loại: nhiều nhân vật mỗi con một animation (không thấy được chuyển giữa hai animation).
- **Người thử tự đổi animation.** Đã loại: tự xoay vòng theo thời gian (khó canh đúng lúc chụp, không thử được đổi giữa chừng).
- **Nhân vật mẫu có sẵn.** Studio (@ClaudeImage, @Codex, @Blender) không ra được file xương Spine. Đã loại: đặt vẽ nhân vật Spine theo style của game (chưa có công cụ).
- **Không chặn ai, không bị đánh.** Nếu nhân vật tương tác với trận thì nó đổi lối chơi, sai mục đích thử.
- `code.json`: `SpineTest`, theo luật đặt tên của vòng 2.
- **Góp ý GPT `r2-core` (2026-11-26), 19 góp ý**: 17 thành ý (2 làm rõ `spine-on-battle`, `spine-no-effect`; 15 ý mới). Ý về điều cần kiểm cụ thể (`overlap-order`, `material-check`, `narrow-camera`, `flip-anchor`, `mid-mix-switch`, `perf-cost`, `art-fit`, `size-vs-hero`, `pause-freeze`, `touch-lock`) được giữ ở đây theo luật đưa góp ý vào node, và đi xuống `spine-checks` thành điều kiểm chấm được. Con số GPT nêu (1,5u, x=+9u, 0,2 giây, 60 giây) bỏ khỏi vòng 2, đã nằm hoặc sẽ nằm ở `spine-spec`.
- **Dừng theo trận (`pause-freeze`), thay cho chạy theo thời gian thực**: Spine dùng cho hero, quái thì phải đứng khi trận dừng, nên phải thử điều này. Tín hiệu dừng đi qua nơi lắp màn, nên vẫn không gọi thẳng feature khác (`spine-no-effect`).
- **Loại `control-zone`** (`r2-core#o-dieu-khien-thu`, AI 3): đặt cụm nút ra ô riêng ngoài dải chiến trường trái với bố cục người đã chấm 9 (`spine-spec/main-bold.frame`, cụm nút ngay trên đầu nhân vật, để mắt không phải rời nhân vật khi bấm). Phần "không đè HUD và các feature thử khác" đã kiểm ở frame.
- **Loại `check-procedure`** (`r2-core#node-cach-kiem`, AI 4): một node quy trình riêng trùng với `pass-rule`, `result-sheet`, với `spine-checks` (mỗi điều kiểm một ý) và với đợt chơi thử của tester (`Tester/`, ghi ảnh, kết quả, lỗi). Thêm node chỉ để chép lại các thứ đó.
- `perf-cost` (AI 6): máy yếu nhất được hỗ trợ và ngân sách FPS chưa có trong thiết kế; giữ ý, ghi ở Chưa rõ.

## Chưa rõ
- Máy yếu nhất được hỗ trợ và ngân sách FPS (cho `perf-cost`): chưa có node nào chốt; người quyết, hoặc để tới giai đoạn có đo hiệu năng.
- Cài runtime spine-unity vào `Packages` và giấy phép Spine là việc của người và @CodeLead; thiết kế không chờ việc đó.
- Nhân vật mẫu cụ thể và tên các animation của nó chốt ở vòng 4; cần đúng bộ mẫu đi kèm bản runtime được cài.
- Khi nào bỏ feature này khỏi game: người quyết sau khi xem kết quả thử.
