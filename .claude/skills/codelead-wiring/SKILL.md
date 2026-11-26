---
name: codelead-wiring
description: Chỉ phiên FEATURE=@CodeLead. Nối dây trong Assets/_Composition - thêm installer của feature đã kiểm qua vào Boot variant của scene dùng nó, thêm id asset công khai vào catalog, scene; validate _Composition, biên dịch, BootTests, Play, lưu scene và prefab; test ở _Composition/Tests cho yêu cầu nối dây. Dùng khi một feature vừa được kiểm qua, hoặc khi sửa Boot, catalog, scene.
---
# @CodeLead: nối dây
Luật chung: `.claude/rules/codelead.md`; thứ tự cài và luật installer ở `.claude/rules/unity.md`.

## Nối dây (`Assets/_Composition`)
- Vùng này chỉ nối dây: `Boot.cs`, các variant của `Boot.prefab` (danh sách installer theo thứ tự cài, catalog asset id → asset), scene. Không chứa luật game.
- Feature báo xong và đã kiểm qua: thêm installer của nó vào Boot variant của scene dùng nó, thêm id asset công khai vào catalog. Thứ tự cài và luật installer ở `.claude/rules/unity.md`.
- Sửa xong: `validate.py --feature _Composition`, biên dịch, chạy `BootTests`, Play; lưu scene và prefab đã mở trước khi hết lượt.
- Đây là code @CodeLead tự viết và tự kiểm: báo cáo ghi rõ đã đổi gì trong `_Composition` để người soi.
- **Test ở `_Composition/Tests`** (của bạn) được sổ nhận cho yêu cầu nối dây của dịch vụ: phiên sở hữu trích `BootTests.<Hàm>` khi ghi `done`; giữ tên test ổn định, đổi tên thì sổ báo "mất test".
