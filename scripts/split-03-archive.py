# Maintainer only: rebuild docs/live/03_ARCHIVE.md from a *full* legacy 03 (line 41+).
# After the repo switched to a slim `03_kovetkezo_lepesek.md`, running this blindly will destroy the archive.
import pathlib
import sys

root = pathlib.Path(__file__).resolve().parents[1]
src = root / "docs" / "live" / "03_kovetkezo_lepesek.md"
dst = root / "docs" / "live" / "03_ARCHIVE.md"

HEADER = """# Archív: 03 — részletes iterációs napló

> **Takarékos használat:** alapból ne húzd be ezt a fájlt a Cursor chatbe. A rövid, aktuális útmutató: [`03_kovetkezo_lepesek.md`](03_kovetkezo_lepesek.md). Ide csak akkor térj vissza, ha konkrét **régi iteráció** vagy **upgrade / Batch** checklist részlete kell.

---

"""


def main() -> None:
    text = src.read_text(encoding="utf-8")
    if "## Takarékos dokumentációs mód" in "\n".join(text.splitlines()[:80]):
        print("Refusing: 03 is already slim. Edit 03_ARCHIVE.md directly or restore a full 03 backup first.", file=sys.stderr)
        sys.exit(1)
    lines = text.splitlines(keepends=True)
    body = "".join(lines[40:]) if len(lines) > 40 else ""
    dst.write_text(HEADER + body, encoding="utf-8", newline="\n")
    print(f"Wrote {dst} ({len(body)} chars body, {len(lines) - 40} lines)")


if __name__ == "__main__":
    main()
