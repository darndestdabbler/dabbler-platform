using System.Diagnostics;

namespace AcmeCoffeeOnline.Web.Tests;

public sealed class WorkflowAssetSmokeTests
{
    [Fact]
    public void AiRouterConfig_LoadsNestedPromptTemplatesAsync()
    {
        var repoRoot = GetRepoRoot();
        var tempScriptPath = Path.GetTempFileName() + ".py";
        File.WriteAllText(
            tempScriptPath,
            """
from pathlib import Path

config_text = Path('ai-router/config.py').read_text(encoding='utf-8')
router_config = Path('ai-router/router-config.yaml').read_text(encoding='utf-8')

assert "config_dir / \"prompt-templates\"" in config_text
assert "config_dir.parent / \"prompt-templates\"" in config_text
assert "system_prompt_file: prompt-templates/google-system.md" in router_config
assert "prompt_template_file: prompt-templates/verification.md" in router_config

required_files = [
    Path('ai-router/prompt-templates/google-system.md'),
    Path('ai-router/prompt-templates/anthropic-system.md'),
    Path('ai-router/prompt-templates/task-types/documentation.md'),
    Path('ai-router/prompt-templates/verification.md'),
    Path('ai-router/utils/load_env.py'),
]

for required in required_files:
    assert required.exists(), f"missing workflow asset: {required}"

print('workflow-assets-ok')
""");

        var command = @"
$ErrorActionPreference = 'Stop'
$env:GEMINI_API_KEY = 'test-gemini'
$env:ANTHROPIC_API_KEY = 'test-anthropic'
if (Get-Command py -ErrorAction SilentlyContinue) {
  & py -3 '" + tempScriptPath + @"'
} elseif (Get-Command python -ErrorAction SilentlyContinue) {
  & python '" + tempScriptPath + @"'
} else {
  throw 'Python interpreter not found in PATH.'
}
";

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -Command \"{command.Replace("\"", "\\\"")}\"",
                    WorkingDirectory = repoRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Assert.True(process.ExitCode == 0, $"workflow asset smoke check failed.{Environment.NewLine}STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}");
            Assert.Contains("workflow-assets-ok", stdout);
        }
        finally
        {
            File.Delete(tempScriptPath);
        }
    }

    private static string GetRepoRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }
}
