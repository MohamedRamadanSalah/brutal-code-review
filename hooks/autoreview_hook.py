"""Auto-trigger the brutal-review skill after Claude finishes writing code.

Two modes, wired in ~/.claude/settings.json:
  mark  (PostToolUse on Write|Edit): remember edited source files for this session.
  stop  (Stop): if source files were edited, block the stop once and ask Claude
        to run brutal-review on them, then clear the list.

Set BRUTAL_REVIEW_AUTO=0 to switch it off.
"""
import json
import os
import sys
from pathlib import Path

SOURCE_EXTS = {
    ".cs", ".cshtml", ".razor", ".dart", ".ts", ".tsx", ".js", ".jsx", ".mjs",
    ".py", ".go", ".java", ".kt", ".swift", ".sql", ".rs", ".php", ".rb", ".vue",
}
STATE_DIR = Path.home() / ".claude" / "brutal-review" / "state"
CLAUDE_HOME = (Path.home() / ".claude").resolve()


def pending_file(session_id: str) -> Path:
    safe = "".join(c for c in session_id if c.isalnum() or c in "-_") or "unknown"
    return STATE_DIR / f"{safe}.pending"


def mark(data: dict) -> None:
    path = (data.get("tool_input") or {}).get("file_path") or ""
    if not path or Path(path).suffix.lower() not in SOURCE_EXTS:
        return
    try:
        if Path(path).resolve().is_relative_to(CLAUDE_HOME):
            return  # skip Claude's own config, skills and hooks
    except OSError:
        pass
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    pf = pending_file(data.get("session_id", ""))
    existing = set(pf.read_text(encoding="utf-8").splitlines()) if pf.exists() else set()
    if path not in existing:
        with pf.open("a", encoding="utf-8") as f:
            f.write(path + "\n")


def stop(data: dict) -> None:
    if data.get("stop_hook_active"):
        return  # already continuing because of a Stop hook; never loop
    pf = pending_file(data.get("session_id", ""))
    if not pf.exists():
        return
    files = [line for line in pf.read_text(encoding="utf-8").splitlines() if line.strip()]
    pf.unlink(missing_ok=True)
    if not files:
        return
    listing = "\n".join(f"- {f}" for f in files)
    reason = (
        "AUTO SELF-REVIEW: you edited source code this session. Before finishing, "
        "load the `brutal-review` skill and run it in self-review mode on your own "
        "changes to these files:\n"
        f"{listing}\n"
        "Research first, prove every finding, keep it proportionate to the size of the "
        "change, record ledger entries with Author: Claude, and offer fixes rather than "
        "applying them silently."
    )
    print(json.dumps({"decision": "block", "reason": reason}))


def main() -> None:
    if os.environ.get("BRUTAL_REVIEW_AUTO", "1") == "0" or len(sys.argv) < 2:
        return
    try:
        data = json.loads(sys.stdin.read() or "{}")
    except json.JSONDecodeError:
        return
    {"mark": mark, "stop": stop}.get(sys.argv[1], lambda _: None)(data)


if __name__ == "__main__":
    main()
