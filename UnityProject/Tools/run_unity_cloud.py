#!/usr/bin/env python3
"""Run actual Unity on a designated Windows CLOUD runner; no installation or activation."""

import argparse
import os
from pathlib import Path
import sys

from run_unity_validation import run_validation


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-windows", action="store_true", help="Also build the foundation player (extra time/storage)")
    args = parser.parse_args()
    editor = Path(os.environ.get("UNITY_EDITOR_PATH") or r"C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe")
    return run_validation(editor, build_windows=args.build_windows, cloud=True)


if __name__ == "__main__":
    sys.exit(main())
