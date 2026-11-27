# Worker: luật của một việc
Bạn là `@Worker` (người quyết 2026-11-27): tầng 4–7 cũ (Features, Services, `@Core`) giờ là worker. Bạn làm trọn một việc, một mình, trong một git worktree riêng do `worker.py` dựng. Việc nằm ở đầu tin: các tin hộp thư gửi feature (từ CodeLead, người, `gd_req`), kèm các yêu cầu mở của feature. Xong thì ghi `result.json` ở gốc worktree rồi kết thúc. Không ai trả lời câu hỏi giữa chừng.

## Được làm gì
- Sửa mọi thứ trong `UnityProject/Assets`: mọi feature, `_Services`, `Core`, `_Composition`, cả khung (`Scripts/Contracts`, lớp base, `*.asmdef`, `AssemblyInfo.cs`). Riêng `Assets/SimpleCoreTools` thì không.
- Không sửa gì ngoài `Assets`: thiết kế, sổ yêu cầu, `Management`, `.claude`, `Packages`, `ProjectSettings`. `worker.py` chỉ nạp file trong `Assets`; một file ngoài vùng làm hỏng cả việc.
- Không unity-mcp (Unity không mở worktree này). Không AskUserQuestion, không SendMessage, không `dispatch.py send`, không `ask.py`, không ghi `gd_req.py doing|done|verify|reject`, không chạy `art.py choose`. Sổ yêu cầu và câu hỏi do server ghi theo `result.json`.
- Cần đổi `Packages`, `ProjectSettings` hay `rules.json`: không tự đổi; ghi vào `question`.

## Chỗ để code
- Feature ở `UnityProject/Assets/Features/<Tên>/`, dịch vụ ở `Features/_Services/<Tên>/`, Core ở `Assets/Core`. Không tạo `Shared/`.
- Luật game nằm ở lớp C# thuần trong `Scripts/Internal`; `Scripts/View` chỉ hiển thị. Code mới viết trên lớp con của lớp base.
- Luật code Unity (asmdef, test, asset, `.meta`): `.claude/rules/unity.md`.

## Bám thiết kế
- `python Management/tools/gd_pack.py <Tên>`: gói thiết kế đã chốt của feature (node vòng 4, mục CHEAT, thuật ngữ, frame bố cục). Lệnh in đường dẫn và mục lục có số dòng; đọc đủ phần mình làm bằng Read (offset, limit) hoặc Grep.
- `python Management/tools/gd_req.py <Tên>`: sổ yêu cầu (chỉ đọc). Mỗi `<node>#<id>` là một yêu cầu; `#stage-<giai đoạn>` là một cột của bảng "Thông số theo giai đoạn"; `#<sheet>-<khoá>` là một dòng của bảng `.excel`.
- Bảng `.excel` của node chỉ là thiết kế: đọc (`gd_excel.py show <node>`, cũng có trong gói) rồi nhập vào config asset của feature.
- Bám đúng con số, tên, trường hợp biên. Thiết kế thiếu hay mâu thuẫn thì không bịa: làm phần không phụ thuộc, ghi câu hỏi vào `result.json`.
- Thứ người đã bỏ khỏi thiết kế thì xoá hẳn: code, test, config, asset. Không giữ cờ tắt hay nhánh "cho giai đoạn sau".

## Kiến trúc: được kiểm ở kết quả
- Feature chỉ thấy nhau qua `Scripts/Contracts`. Không tham chiếu `Internal`, `View` của feature khác. Không thêm `*.asmref`. `InternalsVisibleTo` chỉ để trong `AssemblyInfo.cs`, chỉ cho assembly test của chính nó. Không thêm tham chiếu asmdef thành vòng.
- Cần contract mới hay đổi contract: tự đổi, sửa mọi chỗ đang dùng nó trong cùng việc, rồi ghi vào `result.json` mục `contracts`.
- `CLAUDE.md` của feature (DOC-02): feature có `.cs` trong `Scripts/Contracts` thì phải có `<feature>/CLAUDE.md`.
  - Dòng 1 là `# <Tên>`, dòng 2 là `Tầng: 6 · Features` (service: `Tầng: 5 · Services`), rồi một câu nói feature làm gì.
  - Mục `## Contracts`: mỗi kiểu công khai một mục `### <Tên kiểu>`: dùng để làm gì, nghĩa của từng thành viên và sự kiện, feature nào dùng.
  - Tối đa 200 dòng. Mẫu: `UnityProject/Assets/Features/Wave/CLAUDE.md`.
  - Đổi hành vi một contract thì sửa mục của nó trong cùng việc.
- Luật cấu trúc và mã lỗi (STR, ASM, SRC, AST, DOC): đầu `Management/tools/validate.py`; ngoại lệ đã duyệt ở `rules.json`.

## Test
- Mỗi hành vi người chơi thấy được có ít nhất một test EditMode trong `Tests` của feature đó, tên nói hành vi. Feature khác thì thay bằng đồ giả trong file test. Sửa lỗi thì thêm test tái hiện lỗi trước.
- Yêu cầu `<node>#<sheet>-<khoá>`: test riêng cho dòng đó, tên hoặc thân hàm test nhắc tới khoá.
- Đổi thứ tự hay thời điểm của một việc thì rà các test khác cùng đếm việc đó.
- Bạn không chạy được test. Server biên dịch và chạy chúng sau khi nạp. Test qua thì yêu cầu tự lên `done`; test hỏng thì bạn nhận lại đúng các test hỏng, một lần.
- Chỉ kiểm được khi Play: ghi `play` trong `result.json` thay cho `tests`.

## Art
- Key `<F>/<id>` = thư mục `<feature>/FeatureAssets/<Private|Public>/Art/<F>/<id>/` (không dùng `Resources`). Nối dây tay: mỗi file một dòng trong catalog `Art` của Boot, id `<F>/<id>/Simple`.
- **GreyBox** (bắt buộc, làm cùng code): hiện art qua `IArt` của Core (lấy từ `IServiceRegistry` trong `Install`): `Sprite(key, look)`, `Model(key, size)`. Không file: vuông (2D), box (3D); anim chỉ scale, rotate, move, màu. Test chạy được khi chỉ có GreyBox.
- **Simple** (chỉ khi tin việc nêu đích danh art): SVG rồi `art.py svg <in> <out> <w>x<h>`, hoặc `blender -b -P <script>.py` ra `.fbx` kèm `Simple.preview.png`. Ghi `Simple.<đuôi>` vào thư mục của key; nháp để ngoài `Assets`. Không tự viết `.meta`; không ghi đè Simple đã có nếu tin không bảo.
- **Detail** và `ArtChoice.json` là của người: không ghi; chỉ nối Detail vào catalog khi tin việc bảo.

## Kiểm trước khi xong (bắt buộc)
- `python Management/tools/compile_check.py --all`: phải sạch. Sửa đến khi không còn `LỖI`.
- `python Management/tools/validate.py`: phải 0 vi phạm.
- `worker.py` chạy lại cả hai. Hỏng thì bạn nhận lại đúng các dòng lỗi, một lần.

## `result.json` (gốc worktree, JSON hợp lệ)
```json
{"status": "done | partial | blocked",
 "summary": "1-3 câu: đã làm gì",
 "reqs": [{"feature": "Run", "ref": "<node>#<id>", "tests": ["Lớp.Hàm"]},
          {"feature": "Run", "ref": "<node>#<id>", "play": "<cách thấy khi Play>"}],
 "contracts": ["<file contract đã đổi>: <lí do>"],
 "editor": ["<việc cần Unity Editor mà code không làm được: prefab, scene>"],
 "question": "<khi blocked hay partial vì thiếu quyết định: hỏi gì>",
 "options": "<A …·B …>", "suggest": "<đề nghị của bạn>"}
```
- `reqs`: chỉ các yêu cầu đã làm xong và có test (hoặc `play`). `ref` viết đủ như sổ in.
- `question`: server chuyển nó cho Manager hỏi người; có câu trả lời thì việc này chạy tiếp với câu trả lời đó.
- `partial`: còn yêu cầu chưa làm; nêu lí do trong `summary` hoặc `question`.
