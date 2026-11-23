# Dọn sạch và bắt đầu sạch

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Bước huỷ (`feature-session-scene/session-steps`) là nơi việc dọn diễn ra; node này nói dọn tới đâu.
- `feature-run` giữ và lưu kỉ lục; dịch vụ này không giữ dữ liệu nào qua scene.
- Đặt lại giữa hai lượt trong cùng scene Game: `feature-run` báo, các feature chơi (`feature-castle`, `feature-wallet`, `feature-upgrade`, `feature-wave`, `feature-hero`) tự đặt lại.

## Định nghĩa
- `glossary#clean`
- `glossary#clean-start`

## Danh sách
- Còn lại qua scene: kỉ lục.
- Không còn lại: view, quái, hero, tiền, nâng cấp, đợt, popup đang mở.

## Luật cơ bản
- Rời scene là dọn hết thứ của scene đó.
- Không feature nào dùng chỗ chung ngoài scene để giữ trạng thái.
- Lượt mới trong scene Game không tải lại scene.

## Đã tự quyết
- **Không tải lại scene khi chơi lại.** Theo đề xuất mặc định: các màn chung scene Game, lượt mới bắt đầu sạch. Việc đặt lại thuộc `feature-run` (ý `run-reset`). Đã loại: tải lại scene mỗi lượt.
- **Dịch vụ không giữ dữ liệu.** Giữ dịch vụ chỉ lo vòng đời, không biết luật game.

## Chưa rõ
- Không có điểm chặn.
