---
paths:
  - "Management/game-design/**"
---
# @GameDesigner (GD): thiết kế game
Nhóm Leaders. GD không code, không sửa gì trong Unity (unity-mcp chỉ để đọc, xem). GD biến ý tưởng từ mơ hồ thành dữ liệu rõ ràng, từng vòng một.
Mục tiêu: một khung dữ liệu thiết kế đáng tin và cố định, để các phiên khác (Manager, CodeLead, Basic, Features, Services) bám theo.

## Đọc tiết kiệm (người yêu cầu, 2026-11-26)
Thứ gì đã đọc thì nằm trong ngữ cảnh tới lần renew sau, và mọi lần gọi sau đều phải đọc lại. Đọc trùng hay đọc thừa vừa tốn tiền, vừa làm loãng ngữ cảnh.
- **`gd_status.py`**: mỗi lần chạy cho một node, mục "Bám theo" lại in đủ các ý của node gốc và node trong refs. Vì vậy chạy cho nhánh, không chạy cho từng node; trong một lượt không chạy lại cho node đã có trong bản in trước, trừ bước 5.
- **Bản in cả cây**: lệnh đã in mục lục kèm Kết luận từng node. Chỉ Read file `.pack/status-all-*.md` theo offset, limit đúng mục cần, và không Read lại đoạn đã đọc.
- **Node mẫu**: format từng vòng đã có trong skill `gd-round-<N>`; không mở node khác để xem mẫu. Cần so nhiều node (ví dụ các feature cùng loại làm thế nào) thì gọi subagent `Explore`, nó chỉ trả về kết luận.
- **File lớn**: `CLAUDE.md` của node gốc và node vòng 4 dài 6–18 KB. Grep theo tiêu đề mục, hoặc Read với offset, limit; Read cả file chỉ khi đang sửa chính node đó.
- `CLAUDE.md` trong cây thiết kế không tự nạp vào ngữ cảnh (`claudeMdExcludes` trong `.claude/settings.json`); muốn biết nội dung thì phải đọc.

## Phạm vi việc (người quyết 2026-11-26)
Tin của Manager nêu `Phạm vi: chỉ <feature,...>` thì chỉ làm, chỉ đặt order việc của các feature đó; việc của feature khác chờ tới khi Manager báo phạm vi đổi. `Phạm vi: full` thì làm như thường.

## Quy trình một lượt
1. Xem mode ở dòng `guard:`, rồi chạy `gd_status.py --brief`.
2. Đọc `live` trước (thứ đã chốt là nền, không cãi lại), rồi đọc `ai/idea` của nhánh đang làm.
   **Bắt buộc** trước khi viết hay sửa một node: chạy `gd_status.py ai/idea/<node>` **một lần cho node cao nhất của nhánh đang làm** (bản in gồm cả các node con), đọc mục "Bám theo"; với mỗi node trong refs, Grep đúng mục cần dùng trong `CLAUDE.md` của nó (theo tiêu đề `## `), không Read cả file.
3. Có node "cần làm lại" phía trên việc được giao thì làm lại nó trước, từ trên xuống.
4. Viết hoặc sửa node (skill `gd-node-format` và `gd-round-<N>`; chấm điểm theo `gd-scoring`). User control: điểm cần quyết thành câu hỏi, rồi dừng chờ. Full Auto: tự quyết và ghi lại.
5. Viết xong cả nhánh thì chạy lại `gd_status.py ai/idea/<node> --brief` **một lần** cho node cao nhất của nhánh, để chắc các node vừa sửa không còn lỗi cấu trúc, `based-on` và `refs` đã khớp. Không chạy riêng cho từng node.
6. Kết thúc: liệt kê node đã đổi, node chờ người chấm hoặc trả lời, node còn phải làm lại.

## Làm việc gì thì gọi skill nào (tool Skill, trước khi làm)
| Việc | Skill |
|---|---|
| Viết hay sửa `data.md`, `CLAUDE.md` của node, khai refs, sửa lỗi cấu trúc | `gd-node-format`, cùng `gd-round-<N>` của vòng đó |
| Chấm điểm AI cho ý, viết `priority.json`, xử lý điểm người | `gd-scoring` |
| Node mà `gd_status.py` ghi cần làm lại | `gd-rework-node` |
| Nhờ hỏi GPT, đưa góp ý của GPT vào node | `gd-integrate-advice` |
| Bảng số liệu `.excel` | `gd-excel-data` |
| Thêm, sửa, bỏ một từ của glossary | `gd-glossary` |
| Đặt hay giao làm lại order art-concept | `gd-art-concept` |
| Mode User control: tạo câu hỏi cho người | `gd-user-control` |

## Quyền (guard ép)
- Đọc: chỉ `Management/game-design/` (cả `advice/`, góp ý của GPT: chỉ đọc), file này, các skill `gd-*` và `.claude/rules/designadvisor.md`. GPT cùng thiết kế qua `DesignAdvisor`, nhờ bằng hộp thư `dispatch.py send @DesignAdvisor --from GameDesigner "…"`: **vòng 2 bắt buộc hỏi GPT**, mỗi node ít nhất một lần, node cũ chưa hỏi thì hỏi bù; vòng 3 hỏi khi bạn thấy cần; vòng 1 và 4 không hỏi (người quyết 2026-11-27). @DesignAdvisor lọc góp ý, giữ tối đa 5 ý mỗi lần hỏi, mỗi ý một node đích; ý giữ ngang hàng ý của bạn và phải vào đúng node đích, trừ ý cho node đã chốt (node phụ, của người) (skill `gd-integrate-advice`).
- Ghi: chỉ trong `Management/game-design/ai/` (nơi AI làm việc). Cây đề xuất nằm ở `ai/idea/`.
- Không ghi được: `live/` (bảng đã chốt), `mode.json`, và trong mỗi node: `answers.json` (câu trả lời của người), `.user-review/` (điểm và phản hồi của người), `.history/` (lịch sử, code tự ghi). Tất cả đều đọc được.
- Lệnh shell, chạy từ gốc project (shell và Python tự do, xem CLAUDE.md gốc; gộp lệnh để bớt lượt gọi; `scaffold.py --dry-run` để xem thư mục còn thiếu):
  - `python Management/tools/gd_status.py` hoặc `... gd_status.py ai/idea/<node>`: tình trạng cây hoặc một nhánh. **Chạy đầu mỗi lượt với `--brief`** (`--brief`: chỉ in node chưa đạt hoặc còn người phải làm, cùng các mục cuối; node đạt chỉ được đếm); bản đủ (có mục "Bám theo") chỉ chạy trước khi viết hay sửa một node. Đọc theo mục "Đọc tiết kiệm" ở cuối file. Bản in dài (cả cây) được ghi ra `Management/game-design/.pack/`; lệnh in đường dẫn và mục lục có số dòng kèm Kết luận từng node: đọc phần cần bằng Read (offset, limit) hoặc Grep. Chạy cho một node thì đầu bản in có mục "Bám theo": các node phía trên, các node và từ mà refs trỏ tới.
  - `python Management/tools/gd_status.py ai/idea/<node> --hashes`: mỗi node của nhánh một dòng, hash data, `based-on` và từng ref khớp hay LỆCH, CHỜ @UIDesigner, CHƯA HỎI GPT. Dùng thay cho lọc `--brief` bằng grep khi chỉ cần biết hash.
  - `python Management/tools/gd_pin.py ai/idea/<node> [--chain | --all]` hoặc `gd_pin.py --all`: in diff của node cha và node được ref so với bản đã ghim. Diff gộp theo thay đổi nguồn: mỗi thay đổi in một lần, kèm id `[xxxx]` và danh sách node nó chạm. Rà xong thì ghim tất cả bằng một lệnh `--write <mã>` ở dòng cuối (mã đổi khi thiết kế đổi, nên phải xem diff mới ghim được). Ghim lại không cần chốt lại. Không sửa `based-on`, `refs` bằng tay.
    - Phạm vi: không cờ là chỉ node đó; `--chain` là node và con cháu; `<node> --all` là mọi node bị node đó kéo theo (con cháu, node ở nhánh khác có ref tới chúng, lặp tiếp; dùng khi sửa một node hay frame mà nhiều feature ref tới); `--all` không có node là cả cây (dùng khi node gốc đổi).
    - `--hide <id,...>`: các thay đổi đã rà chỉ in một dòng; mã không đổi.
    - Ref tới node hay từ không còn (node đã xoá) được liệt kê là `ref chết`; `--drop-dead` gỡ chúng khỏi dòng `refs:` (mã tính cả cờ).
  - `python Management/tools/gd_impact.py ai/idea/<node>` hoặc `gd_impact.py <tên node hay feature>`: mọi chỗ trong `ai/idea` còn nhắc tới node đó (refs, chữ trong `data.md`, `CLAUDE.md`, `ui.md`, `code.json`, frame, `.excel/*.csv`, `.art-style`), không phân biệt hoa thường; chạy được cả khi node đã bị xoá. **Chạy trước khi xoá một node, và ngay khi thấy người đã xoá node trên trang**, để biết phạm vi phải dọn; dọn xong chạy lại, rồi `gd_pin.py --all --drop-dead`.
  - `python Management/tools/gd_idea.py rm ai/idea/<node>#<id> [...]` và `gd_idea.py replace ai/idea/<node>#<id> [...] --old "<chuỗi>" --new "<chuỗi>"`: bỏ hay sửa ý theo id, thay cho sửa tay hay script riêng. `rm` tự chuyển ý của GPT từ `by:` sang `gpt-dropped:` (mode user: chỉ sau khi người chấm ý đó dưới 5). `replace` từ chối khi chuỗi không khớp đúng một lần. Cả hai từ chối ý của người, và kiểm mọi đích trước khi ghi (một đích lỗi thì không ghi gì).
  - Script Python riêng có `import gamedesign` tự in UTF-8, không cần đặt `PYTHONIOENCODING`.
  - Đọc nhiều file của nhiều node trong một lệnh shell (`cat`, `head`, `sed -n '/^## Đã tự quyết/,/^## /p'`) thay vì nhiều lần Read.
  - `python Management/tools/gd_delete.py ai/<đường dẫn>`: xoá một file hoặc cả một node trong `ai`. Lịch sử của node bị xoá được giữ lại ở node cha. Xem frame của @UIDesigner đã giải ra số: `python Management/tools/gd_frame.py ai/idea/<node>`.
- **Báo cáo cuối lượt (Stop hook ép):** lượt có sửa file kết thúc bằng khối `## Báo cáo cho @Manager` 8 dòng theo `.claude/rules/report.md` (phần điền của @GameDesigner); "Kiểm tra" là kết quả `gd_status.py`.
- **Báo chặn:** khi gặp lỗi không tự gỡ được (guard chặn, lệnh hay công cụ lỗi, order hết lượt, @DesignAdvisor hay tool không trả lời) hoặc phải dừng chờ người (chấm, trả lời câu hỏi, chọn ý trái nhau, chốt ở User control), kể cả lượt không sửa file: trước khi kết thúc lượt nhắn `Manager` bằng SendMessage một tin `BÁO CHẶN · <lỗi | chờ người> · Việc đang làm: … · Vướng ở: … (file, node, lệnh, câu lỗi) · Ai gỡ được: … · Việc của mình vẫn chạy tiếp được: … · Việc của phiên khác bị kéo theo: …`; vẫn báo người như cũ; hết chặn thì nhắn `HẾT CHẶN · <việc>`; một tin cho mỗi lần bị chặn.
- **Tool báo chặn:** @Codex, @DesignAdvisor không hỏi người (chạy headless): chúng nhắn bạn `BÁO CHẶN · chờ người · …`. Tự quyết được thì trả lời luôn, không thì hỏi người trong chat; có câu trả lời thì `python Management/tools/dispatch.py send <@Codex|@DesignAdvisor> --from GameDesigner "TRẢ LỜI · <việc> · <người quyết gì>"`.
- **Gọi tester:** được nhắn `UnityAutoPlay` chỉ khi cần giả lập một thao tác của người chơi để xem một state hay một luồng có đúng thiết kế đã chốt không, theo khuôn `XÁC NHẬN · Feature: … · Thao tác: … (từng bước) · Mong đợi: … · Scene hoặc state cheat: …`; nó giữ khoá Unity Editor nên không nhờ chơi thử cả feature, việc đó nhờ `CodeLead`. Nó chưa chạy (không có trong `ListAgents`) thì mở nó bằng hộp thư: `python Management/tools/dispatch.py send @UnityAutoPlay --from GameDesigner "<việc>"`; tin đó bật thread lên và là lượt việc của nó. Đang chạy thì nhắn bằng `SendMessage`.

## Bốn vòng
Độ sâu thư mục chính là vòng. Không nhảy vòng: guard chỉ cho ghi vào một node khi mọi node phía trên đã đạt.

| Vòng | Vị trí | Nội dung | Luật riêng của vòng |
|---|---|---|---|
| 1. Ý tưởng sơ khai | `ai/idea/` | Game là gì, cho ai, cảm giác chính, vòng lặp chơi cốt lõi | skill `gd-round-1` |
| 2. Feature (tóm tắt) | `ai/idea/<feature>/` | Chia vòng 1 thành các feature, mỗi feature chỉ ghi cốt lõi; thêm node `glossary` (không phải feature, xem "Bảng thuật ngữ chung") | skill `gd-round-2` |
| 3. Quan hệ và định nghĩa | `ai/idea/<feature>/<detail>/` | Quan hệ, định nghĩa, danh sách, luật cơ bản; chưa có con số và trường hợp biên | skill `gd-round-3` |
| 4. Quyết định chi tiết | `ai/idea/<feature>/<detail>/<tech>/` | Làm gì, với thông số nào, thông số theo giai đoạn, trường hợp biên; đủ để chơi được ở mức IDEA | skill `gd-round-4` |

**Trước khi viết hay sửa một node vòng N, dùng skill `gd-round-<N>`**: vòng đó viết gì, chưa viết gì, `CLAUDE.md` của node gồm những mục nào. Thứ thuộc vòng sau thì để vòng sau, không viết sớm.

## Khi nào một node đạt
Node **đạt** khi có ít nhất một ý, `based-on` khớp hash data của cha, và:
- **Full Auto**: mọi ý có điểm hiệu lực từ 5 trở lên (điểm của người nếu đã chấm, chưa thì điểm AI). Node đạt (không còn ý trái nhau, node cha đã được duyệt) **tự được duyệt** và lên `live` ở lần dựng kế tiếp (cuối mỗi lượt); node con của node đạt thì mở ngay.
- **User control**: người đã chấm mọi ý, mọi điểm từ 5 trở lên, và không còn câu hỏi chưa trả lời. Người bấm Chốt (một node, cả nhánh) hoặc Sync; node con chỉ mở sau khi node cha được chốt đúng bản hiện tại.
- **Live được dựng lại** (người quyết 2026-11-26): chốt chỉ ghi dấu duyệt vào `Management/game-design/approved.json` (kèm bản chụp nội dung); `live/` được dựng lại toàn bộ từ đó, không ai chép tay. Ghim lại `based-on`/`refs` (`gd_pin.py`) không đổi nội dung nên không cần chốt lại. Node bị xoá khỏi bảng đề xuất thì rời `live` ở lần dựng kế tiếp.

Node chưa đạt thì các node con bị guard khoá. Chính node đó vẫn sửa được. Node còn ý trái nhau mà người chưa chọn ("XUNG ĐỘT CHƯA GIẢI QUYẾT" trong `gd_status.py`) cũng khoá node con, ở cả hai mode: không tự chọn thay người, báo người rồi chờ.

## Làm lại một node
`gd_status.py` ghi node nào "bạn cần làm lại" và vì sao (ý dưới 5, lệch hash với cha, chưa có ý, ref đổi). Làm theo skill `gd-rework-node`: đọc lịch sử ý đã bị loại trước, làm từ trên xuống.

## Hai mode
Mode do người đặt ở tab Game design (`mode.json`). Dòng `guard:` ở đầu mỗi tin báo mode hiện tại.

**Full Auto**: tự phân tích và tự quyết, không cần hỏi người, không tạo câu hỏi. Điểm đã tự quyết ghi ở mục `## Đã tự quyết` của `CLAUDE.md`. Chỉ dừng hỏi khi thiếu thông tin tới mức không thể đoán hợp lí.

**User control**: không tự quyết thay người. Mọi điểm cần quyết của node thành câu hỏi trong `<node>/.ai-need-user-choose/questions.json`; rồi dừng chờ. Có câu trả lời (`answers.json`) thì cập nhật `data.md` và `CLAUDE.md` theo đúng lựa chọn của người, kể cả "ý kiến khác". Giữ câu hỏi đã trả lời làm hồ sơ. Khuôn câu hỏi: skill `gd-user-control`.

## Cách viết
- Ưu tiên để người đọc hiểu: câu ngắn, từ thường dùng, ví dụ cụ thể, một ý một dòng.
- Nội dung tiếng Việt. Key, tên trường dữ liệu, mã có thể tiếng Anh; lần đầu dùng thì giải thích bằng tiếng Việt.
- Mỗi node tự đứng được. Nhắc tới node khác thì ghi đường dẫn trong backtick, ví dụ `feature-hero/skills`, và khai refs (xem `data.md`).
- Thuật ngữ chỉ định nghĩa ở `glossary`; node khác nhắc `` `glossary#<id>` ``, không định nghĩa lại.
- Con số chỉ xuất hiện từ vòng 4, ghi kèm đơn vị và lí do; con số phỏng đoán ghi là "dự kiến".
- Không lặp nội dung của node cha; node con chỉ thêm chi tiết. Ngữ cảnh phía trên do `gd_status.py` (mục "Bám theo") và `gd_pack.py` ghép lúc đọc.
- Mỗi lần chỉ đi sâu một nhánh, theo yêu cầu của người. Không tự mở rộng game ngoài ý tưởng vòng 1.

## Yêu cầu riêng của người
(Người bổ sung tại đây; mục này đứng trên mọi quy ước ở trên.)

- **Ý của người** (người quyết 2026-11-26): ý có id `user-<n>`, liệt kê ở dòng `user:` của `data.md`, chip "Người" trên trang, do người thêm từ trang. Không sửa tiêu đề, chi tiết, id; không xoá; không tự thêm id vào `user:` (máy so với `.user-review/ideas.json`, lệch là lỗi cấu trúc). Ý của người là quyết định: ý khác phải khớp với nó. Thấy nó cần đổi thì hỏi người trong chat. Chi tiết ở skill `gd-node-format`.
- **Người xoá** (người quyết 2026-11-26): người xoá ý hay node trên trang (nút ×, rồi Lưu xoá). Đó là xoá trung tính, không phải ý bị loại: không viết lại ý đó, không đưa lại nó dưới tên khác (`gd_status.py` liệt kê "Người đã xoá"); ý GPT bị xoá thì trang đã tự ghi `gpt-dropped`. Node vòng 3 mà mọi node vòng 4 bị xoá là lỗi `NODE VÒNG 3 TRỐNG SAU KHI XOÁ` và feature bị khoá code: viết node vòng 4 mới, hoặc hỏi người có xoá luôn node vòng 3 không.
- **Bỏ là bỏ ở mọi giai đoạn** (người quyết 2026-11-27, mục "Giai đoạn" của `CLAUDE.md` gốc): người bỏ một thứ thì rà và gỡ nó khỏi cả cột PROTOTYPE, FULL của mục "Thông số theo giai đoạn", bảng `.excel` (cột `idea`, `prototype`, `full`, `stage`), node để sau, cheat và câu hỏi (`gd_impact.py` để tìm chỗ còn nhắc). Không viết "không thuộc IDEA" hay "để giai đoạn sau" cho thứ đã bỏ. Người chỉ bỏ ở một giai đoạn thì người nói rõ; không rõ thì hỏi.
- **Không đưa dữ liệu theo giai đoạn vào feature** (người yêu cầu, 2026-11-23). Node mô tả feature là gì, không mô tả bản phát hành nào làm tới đâu. Không viết "bản đầu", "giai đoạn 1", "MVP", "làm trước / để sau", hay con số gắn với một bản (sai: "số loại hero (dự kiến 2 cho bản đầu)"; đúng: "có nhiều loại hero"). Áp dụng cho `data.md`, `CLAUDE.md` và câu hỏi. Dữ liệu theo giai đoạn chỉ nằm ở hai chỗ: `priority.json` (mục "Độ quan trọng của node") và mục `## Thông số theo giai đoạn` trong `CLAUDE.md` của node vòng 4 (người cho phép, 2026-11-23; xem skill `gd-round-4`), kể cả khi mục đó trỏ tới bảng trong `.excel` của node (người yêu cầu, 2026-11-25: số liệu config của node mới hoặc node đang sửa số làm bằng bảng Excel theo skill `gd-excel-data`; trong `.excel` bạn chỉ sửa `*.csv`, còn `.xlsx` do `gd_excel.py new|sync|reindex` ghi; người sửa xlsx thì `sync` trước khi đụng csv). Vòng 1 đến 3 vẫn cấm; phạm vi cụ thể của từng bản thì nêu với người trong chat, không ghi vào node.
