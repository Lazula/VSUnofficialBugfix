#!/usr/bin/python3

# This is a utility script for if there are
# existing language entries that need to be
# applied somewhere else to avoid having to
# copy things over manually.
#
# Respects any existing strings/translations

import json
import sys

from pathlib import Path

# existing key name -> new key name
MAPPINGS = {
    "item-candle": "block-candle"
}

def read_languages(lang_dir: str):
    langsfile = lang_dir / "languages.json"
    for line in open(langsfile, encoding="utf-8"):
        if "code:" in line and "li" not in line:
            yield line.split('"')[1]

def main():
    if len(sys.argv) < 2:
        print(f"Usage: {sys.argv[0]} <assets folder>")
        return

    lang_dir = Path(sys.argv[1]) / "game" / "lang"

    for langcode in read_languages(lang_dir):
        print(lang_dir / (langcode + ".json"))
        changed = False
        lang_entries = {}
        for lang_entry in open(lang_dir / (langcode + ".json"), encoding="utf-8"):
            if '"' not in lang_entry:
                continue
            key = lang_entry.split('"')[1]
            value = lang_entry.split('"')[3]
            if key in MAPPINGS:
                lang_entries[key] = value

        our_langfile = f"VSUnofficialBugfix/assets/unofficial-bugfix/lang/{langcode}.json"
        try:
            our_lang_entries = json.load(open(our_langfile, encoding="utf-8"))
        except FileNotFoundError:
            our_lang_entries = {}

        for (old_key, new_key) in MAPPINGS.items():
            new_key = f"game:{new_key}"
            if new_key in our_lang_entries:
                continue

            changed = True
            if old_key not in lang_entries:
                continue

            our_lang_entries[new_key] = lang_entries[old_key]
        
        if changed:
            json.dump(our_lang_entries, open(our_langfile, mode="w", encoding="utf-8"), ensure_ascii=False, indent=4)

if __name__ == "__main__":
    main()
