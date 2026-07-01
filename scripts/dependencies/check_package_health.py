#!/usr/bin/env python3

from __future__ import annotations

import argparse
import os
import pathlib
import subprocess
import sys
from typing import Sequence


VULNERABLE_HEADING = "has the following vulnerable packages"
OUTDATED_HEADING = "has the following updates to its packages"


def repository_root() -> pathlib.Path:
    return pathlib.Path(__file__).resolve().parents[2]


def run_dotnet_list(solution: str, mode: str) -> subprocess.CompletedProcess[str]:
    env = os.environ.copy()
    env.setdefault("DOTNET_CLI_UI_LANGUAGE", "en-US")

    return subprocess.run(
        [
            "dotnet",
            "list",
            solution,
            "package",
            f"--{mode}",
            "--include-transitive",
            "--format",
            "console",
            "--no-restore",
        ],
        cwd=repository_root(),
        env=env,
        text=True,
        capture_output=True,
    )


def emit_output(result: subprocess.CompletedProcess[str]) -> None:
    if result.stdout:
        sys.stdout.write(result.stdout)
        if not result.stdout.endswith("\n"):
            sys.stdout.write("\n")

    if result.stderr:
        sys.stderr.write(result.stderr)
        if not result.stderr.endswith("\n"):
            sys.stderr.write("\n")


def check_mode(solution: str, mode: str, heading: str) -> tuple[bool, int]:
    result = run_dotnet_list(solution, mode)
    emit_output(result)

    found = heading in result.stdout
    if result.returncode != 0 and not found:
        return False, result.returncode

    return found, 0


def parse_args(argv: Sequence[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Check package health for a solution.")
    parser.add_argument(
        "--solution",
        default="ModularityKit.Mutator.slnx",
        help="Path to the solution file to inspect.",
    )
    return parser.parse_args(argv)


def main(argv: Sequence[str]) -> int:
    args = parse_args(argv)

    vulnerable_found, vulnerable_error = check_mode(args.solution, "vulnerable", VULNERABLE_HEADING)
    if vulnerable_error:
        return vulnerable_error

    outdated_found, outdated_error = check_mode(args.solution, "outdated", OUTDATED_HEADING)
    if outdated_error:
        return outdated_error

    if vulnerable_found:
        print(
            "Vulnerable packages were reported above. Update the affected package reference and rerun the check.",
            file=sys.stderr,
        )
        return 1

    if outdated_found:
        print(
            "Outdated packages were reported above. Update the affected package references when you are ready.",
            file=sys.stderr,
        )

    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
