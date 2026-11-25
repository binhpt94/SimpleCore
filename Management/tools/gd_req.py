#!/usr/bin/env python3
"""The requirement book of one code folder: every requirement the fixed design (live) gives it, and how far the
feature session has got with each (see reqbook.py for where the list comes from and how a change is caught).

    python Management/tools/gd_req.py <F>                                     the book (any chat that reads the design)
    python Management/tools/gd_req.py status                                  one line per feature (Leaders)
    python Management/tools/gd_req.py <F> doing <node>#<id> "<ghi chú>"       the feature's chat: started, what it is at
    python Management/tools/gd_req.py <F> done <node>#<id> <Lớp.Hàm>[,...]    the feature's chat: the tests that show it
    python Management/tools/gd_req.py <F> done <node>#<id> play "<cách thấy>" the feature's chat: seen only when playing
    python Management/tools/gd_req.py <F> verify <node>#<id>[,...]            @CodeLead, after running the tests
    python Management/tools/gd_req.py <F> reject <node>#<id> "<lỗi>"          @CodeLead: back to the feature

<node> is the node's path in live or its last key when that is unique (wallet-rules#start-50). guard.py lets only the
chat of <F> write doing and done, and only @CodeLead verify and reject; the human waives a requirement on the page.
A leaf's phase is marked done (gd_progress.py, the page) only when every requirement of it is verified or waived.
"""
import sys
from pathlib import Path

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import gamedesign as gd  # noqa: E402
import reqbook as rb  # noqa: E402

USAGE = __doc__.split("\n\n")[1]


def run(root, argv):
    """(exit code, text to print)."""
    if len(argv) == 2 and argv[1] == "status":
        return 0, gd.emit(root, "req", "all", rb.status_all(root))
    if len(argv) < 2 or not gd.CODE_NAME_RX.match(argv[1]):
        return 2, "Cách dùng:\n" + USAGE
    feature = argv[1]
    if len(argv) == 2:
        return 0, gd.emit(root, "req", feature, rb.render(rb.view(root, feature)))
    action, rest = argv[2], argv[3:]
    if action == "doing" and len(rest) == 2:
        lines = rb.mark(root, feature, rest[0], "doing", feature, note=rest[1])
    elif action == "done" and len(rest) == 3 and rest[1] == "play":
        lines = rb.mark(root, feature, rest[0], "done", feature, play=rest[2])
    elif action == "done" and len(rest) == 2:
        lines = rb.mark(root, feature, rest[0], "done", feature, tests=rest[1].split(","))
    elif action == "verify" and len(rest) == 1:
        lines = rb.mark(root, feature, rest[0], "verify", "@CodeLead")
    elif action == "reject" and len(rest) == 2:
        lines = rb.mark(root, feature, rest[0], "reject", "@CodeLead", note=rest[1])
    else:
        return 2, "Cách dùng:\n" + USAGE
    v = rb.view(root, feature)
    return 0, "\n".join(lines + [f"Sổ {feature}: {rb.counts_line(v)}"])


def main(argv):
    root = HERE.parent.parent
    if len(argv) >= 3 and argv[-2] == "--root":      # test-only override
        root, argv = Path(argv[-1]), argv[:-2]
    try:
        code, text = run(root, argv)
    except (ValueError, OSError) as ex:
        print(f"Không ghi: {ex}", file=sys.stderr)
        return 1
    print(text, file=sys.stdout if code == 0 else sys.stderr)
    return code


if __name__ == "__main__":
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")
    sys.exit(main(sys.argv))
