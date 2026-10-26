#!/usr/bin/env python3
"""Copy raw FBX exports into the Unity project (T02). Plain python3, no Blender needed.

    python3 Source/Blender/scripts/sync_to_unity.py          # copy new/changed files
    python3 Source/Blender/scripts/sync_to_unity.py --dry    # show what would happen

Source/Blender/exports/<Prefix>_*.fbx  ->  Game/Assets/Art/Models/<Category>/
    Plant_*  -> Plants     Char_* -> Characters     anything else -> Misc
Only files whose content changed are rewritten, so Unity does not reimport needlessly. Never deletes anything.
"""
import filecmp
import os
import shutil
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
SRC = os.path.join(ROOT, 'Source', 'Blender', 'exports')
DST = os.path.join(ROOT, 'Game', 'Assets', 'Art', 'Models')
CATEGORY = {'Plant': 'Plants', 'Char': 'Characters'}


def main(dry=False):
    if not os.path.isdir(SRC):
        print('no exports dir: ' + SRC)
        return 1
    n = 0
    for f in sorted(os.listdir(SRC)):
        if not f.lower().endswith('.fbx'):
            continue
        cat = CATEGORY.get(f.split('_', 1)[0], 'Misc')
        d = os.path.join(DST, cat)
        dst = os.path.join(d, f)
        src = os.path.join(SRC, f)
        if os.path.exists(dst) and filecmp.cmp(src, dst, shallow=False):
            print('unchanged  ' + os.path.relpath(dst, ROOT))
            continue
        print(('would copy ' if dry else 'copy       ') + os.path.relpath(dst, ROOT))
        if not dry:
            os.makedirs(d, exist_ok=True)
            shutil.copyfile(src, dst)
        n += 1
    print('%d file(s) %s' % (n, 'would change' if dry else 'synced'))
    return 0


if __name__ == '__main__':
    sys.exit(main('--dry' in sys.argv))
