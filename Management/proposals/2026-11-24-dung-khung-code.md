# Dựng khung code cho các feature đã chốt

Người giao (2026-11-24): bắt đầu dựng khung code. Người làm: @CodeLead, theo mục "Dựng khung cho một feature" trong `.claude/rules/codelead.md`.

## Hiện trạng
- `scaffold.py --dry-run`: cả 19 thư mục đã có (Core, _Composition, 2 service, 15 feature). Không cần tạo thêm.
- `Management/game-design/live` có thiết kế các feature dưới đây. Feature nào chưa tới vòng 4 thì guard khoá khung; gặp khoá thì bỏ qua feature đó và báo @Manager.

## Thứ tự đề xuất
Đi từ thứ được dùng nhiều nhất tới thứ dùng nó, để Contracts phía dưới có trước khi phía trên cần.

1. **Core**: rà interface `I*.cs` (IServiceRegistry, ISessionScene...) có đủ cho các bước sau.
2. **Service**: `_Services/SessionScene`, `_Services/Popup`, rồi `SceneFlow`.
3. **Dữ liệu và luật nền**: `Balance`, `Wallet`.
4. **Đơn vị chiến trường**: `Enemy`, `Hero`, `Castle`.
5. **Trận**: `Wave`, `Upgrade`, `Run`.
6. **Màn hình**: `Hud`, `HudUpgrade`, `PopupPause`, `PopupLose`, `TeamSelect`, `Home`.
7. **Nối dây**: `_Composition` (Boot variant, catalog), làm sau khi các feature có installer.

## Mỗi feature
1. `python Management/tools/gd_pack.py <Tên>` để đọc thiết kế đã chốt.
2. Contracts chỉ cho thứ feature khác đang cần, ghi rõ tên feature cần.
3. asmdef, `AssemblyInfo.cs`, lớp base `internal abstract` trong `Internal/Base` và `View/Base`.
4. `validate.py --feature <Tên>`, biên dịch, báo cáo, rồi nhắn phiên feature rằng khung đã sẵn.

## Điểm cần người quyết
- Thứ tự trên do @Manager đề xuất từ tên feature. @CodeLead đọc `gd_pack` thấy phụ thuộc khác thì đổi thứ tự và ghi lý do vào báo cáo.
