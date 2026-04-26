import os
import stat
import subprocess
import tempfile
import textwrap
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]


class ComposeCompatTests(unittest.TestCase):
    def make_stub_bin(self, temp_dir: Path) -> Path:
        bin_dir = temp_dir / "bin"
        bin_dir.mkdir()

        stubs = {
            "docker": """#!/usr/bin/env bash
set -euo pipefail
echo "docker:$*" >> "$COMMAND_LOG"
if [[ "${1:-}" == "compose" ]]; then
  echo "docker: unknown command: docker compose" >&2
  exit 1
fi
exit 0
""",
            "docker-compose": """#!/usr/bin/env bash
set -euo pipefail
echo "docker-compose:$*" >> "$COMMAND_LOG"
if [[ "${1:-}" == "logs" ]]; then
  exit 0
fi
if [[ "${1:-}" == "build" ]]; then
  exit 0
fi
if [[ "${1:-}" == "run" ]]; then
  for arg in "$@"; do
    if [[ "$arg" == "--build" ]]; then
      echo "run --build is unsupported in docker-compose v1" >&2
      exit 99
    fi
  done
  exit 0
fi
exit 0
""",
            "curl": """#!/usr/bin/env bash
printf '200'
exit 0
""",
            "npm": """#!/usr/bin/env bash
set -euo pipefail
echo "npm:$*" >> "$COMMAND_LOG"
exit 0
""",
            "npx": """#!/usr/bin/env bash
set -euo pipefail
echo "npx:$*" >> "$COMMAND_LOG"
exit 0
""",
            "sudo": """#!/usr/bin/env bash
set -euo pipefail
"$@"
""",
        }

        for name, content in stubs.items():
            path = bin_dir / name
            path.write_text(textwrap.dedent(content))
            path.chmod(path.stat().st_mode | stat.S_IEXEC)

        return bin_dir

    def run_script(self, args: list[str], temp_dir: Path) -> subprocess.CompletedProcess[str]:
        command_log = temp_dir / "commands.log"
        artifact_dir = temp_dir / "artifacts"
        bin_dir = self.make_stub_bin(temp_dir)
        env = os.environ.copy()
        env["PATH"] = f"{bin_dir}:{env['PATH']}"
        env["COMMAND_LOG"] = str(command_log)
        env["ARTIFACT_DIR"] = str(artifact_dir)
        env["BACKEND_HEALTH_URL"] = "http://example.invalid/backend"
        env["FRONTEND_HEALTH_URL"] = "http://example.invalid/frontend"
        env["E2E_BASE_URL"] = "http://example.invalid"
        env["CI"] = "false"

        return subprocess.run(
            args,
            cwd=REPO_ROOT,
            env=env,
            capture_output=True,
            text=True,
        )

    def test_e2e_script_falls_back_to_docker_compose_v1_without_run_build(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            temp_dir = Path(temp)
            result = self.run_script(["bash", "scripts/e2e.sh"], temp_dir)

            self.assertEqual(result.returncode, 0, msg=result.stderr)

            command_log = (temp_dir / "commands.log").read_text()
            self.assertIn("docker:compose version", command_log)
            self.assertIn("docker-compose:--profile app up -d --build db api web", command_log)
            self.assertIn("build migrator", command_log)
            self.assertIn("build seeder", command_log)
            self.assertIn("run --rm migrator", command_log)
            self.assertIn("run --rm -e E2E_RESET_ENABLED=true", command_log)
            self.assertNotIn("--build migrator", command_log)
            self.assertNotIn("run --rm --build", command_log)

    def test_verify_backend_uses_compose_fallback(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            temp_dir = Path(temp)
            result = self.run_script(["bash", "scripts/verify.sh", "backend"], temp_dir)

            self.assertEqual(result.returncode, 0, msg=result.stderr)

            command_log = (temp_dir / "commands.log").read_text()
            self.assertIn("docker:compose version", command_log)
            self.assertIn("docker-compose:--profile tests run --rm tests-backend", command_log)


if __name__ == "__main__":
    unittest.main()
