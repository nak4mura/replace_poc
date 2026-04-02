let d = '';
process.stdin.on('data', c => d += c);
process.stdin.on('end', () => {
  const j = JSON.parse(d);
  const f = (j.tool_input && (j.tool_input.file_path || j.tool_input.path)) || '';
  const cwd = process.cwd();
  const absF = f ? require('path').resolve(f) : '';
  const safe = !f || absF.toLowerCase().startsWith(cwd.toLowerCase());
  console.log(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision: safe ? 'allow' : 'ask',
      permissionDecisionReason: 'File outside working directory: ' + absF + ' (cwd: ' + cwd + ')'
    }
  }));
});
