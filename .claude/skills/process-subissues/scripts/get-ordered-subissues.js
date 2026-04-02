#!/usr/bin/env node
'use strict';

/**
 * get-ordered-subissues.js
 * Usage: node get-ordered-subissues.js <owner> <repo> <issue_number>
 *
 * GitHubのissueからsub-issuesを取得し、blockedBy関係を考慮した
 * トポロジカルソート順で出力します。
 */

const { spawnSync } = require('child_process');

const [, , owner, repo, issueNumberStr] = process.argv;
const issueNumber = parseInt(issueNumberStr, 10);

if (!owner || !repo || !issueNumber) {
  console.error('Usage: node get-ordered-subissues.js <owner> <repo> <issue_number>');
  process.exit(1);
}

// GraphQLクエリ（shell quoting問題を避けるためspawnSyncで渡す）
const query = `
query($owner: String!, $repo: String!, $number: Int!) {
  repository(owner: $owner, name: $repo) {
    issue(number: $number) {
      number
      title
      state
      subIssues(first: 50) {
        nodes {
          number
          title
          state
          id
          blockedBy(first: 20) {
            nodes { number title state }
          }
          blocking(first: 20) {
            nodes { number title state }
          }
        }
      }
    }
  }
}
`.trim();

// gh api graphql をspawnSyncで呼び出し（引数配列なのでshell escaping不要）
const ghResult = spawnSync(
  'gh',
  [
    'api', 'graphql',
    '-f', `query=${query}`,
    '-f', `owner=${owner}`,
    '-f', `repo=${repo}`,
    '-F', `number=${issueNumber}`,
  ],
  { encoding: 'utf8' }
);

if (ghResult.status !== 0) {
  console.error('GitHub API error:', ghResult.stderr || ghResult.error?.message);
  process.exit(1);
}

let data;
try {
  data = JSON.parse(ghResult.stdout);
} catch (e) {
  console.error('Failed to parse GitHub API response:', e.message);
  console.error('Raw output:', ghResult.stdout.slice(0, 500));
  process.exit(1);
}

if (data.errors) {
  console.error('GraphQL errors:', JSON.stringify(data.errors, null, 2));
  process.exit(1);
}

const parentIssue = data.data?.repository?.issue;
if (!parentIssue) {
  console.error(`Issue #${issueNumber} not found in ${owner}/${repo}`);
  process.exit(1);
}

const subIssues = parentIssue.subIssues?.nodes || [];

if (subIssues.length === 0) {
  console.log(JSON.stringify({
    parentNumber: parentIssue.number,
    parentTitle: parentIssue.title,
    ordered: [],
    hasCycle: false,
    total: 0,
    open: 0,
    closed: 0,
    message: 'Sub-issuesが見つかりません',
  }, null, 2));
  process.exit(0);
}

// sub-issueの番号セット（外部issueによるブロックは無視）
const subIssueNumbers = new Set(subIssues.map(i => i.number));
const issueMap = Object.fromEntries(subIssues.map(i => [i.number, i]));

// 依存グラフ構築
// graph[A] = [B, C] → Aが完了するとB, Cがunblockされる
// inDegree[X] = Xをブロックしているsub-issue数
const inDegree = {};
const graph = {};
for (const { number } of subIssues) {
  inDegree[number] = 0;
  graph[number] = [];
}

for (const issue of subIssues) {
  const blockers = (issue.blockedBy?.nodes || []).filter(b => subIssueNumbers.has(b.number));
  for (const blocker of blockers) {
    graph[blocker.number].push(issue.number);
    inDegree[issue.number]++;
  }
}

// Kahnのアルゴリズムでトポロジカルソート
const queue = subIssues
  .filter(i => inDegree[i.number] === 0)
  .map(i => i.number)
  .sort((a, b) => a - b);

const ordered = [];
const visited = new Set();

while (queue.length > 0) {
  queue.sort((a, b) => a - b); // 同優先度は番号順（決定的な順序）
  const current = queue.shift();
  if (visited.has(current)) continue;
  visited.add(current);

  const issue = issueMap[current];
  const blockedByInScope = (issue.blockedBy?.nodes || [])
    .filter(b => subIssueNumbers.has(b.number))
    .map(b => ({ number: b.number, title: b.title, state: b.state }));
  const blockingInScope = (issue.blocking?.nodes || [])
    .filter(b => subIssueNumbers.has(b.number))
    .map(b => ({ number: b.number, title: b.title, state: b.state }));

  ordered.push({
    number: issue.number,
    title: issue.title,
    state: issue.state,
    blockedBy: blockedByInScope,
    blocking: blockingInScope,
    canStart: blockedByInScope.every(b => b.state === 'CLOSED'),
  });

  for (const next of graph[current]) {
    inDegree[next]--;
    if (inDegree[next] === 0) queue.push(next);
  }
}

// 循環依存があれば残りを追加
const hasCycle = visited.size < subIssueNumbers.size;
if (hasCycle) {
  for (const num of subIssueNumbers) {
    if (!visited.has(num)) {
      const issue = issueMap[num];
      ordered.push({
        number: issue.number,
        title: issue.title,
        state: issue.state,
        blockedBy: (issue.blockedBy?.nodes || [])
          .filter(b => subIssueNumbers.has(b.number))
          .map(b => ({ number: b.number, title: b.title, state: b.state })),
        blocking: (issue.blocking?.nodes || [])
          .filter(b => subIssueNumbers.has(b.number))
          .map(b => ({ number: b.number, title: b.title, state: b.state })),
        canStart: false,
        cycleWarning: true,
      });
    }
  }
}

const result = {
  parentNumber: parentIssue.number,
  parentTitle: parentIssue.title,
  ordered,
  hasCycle,
  total: subIssues.length,
  open: subIssues.filter(i => i.state === 'OPEN').length,
  closed: subIssues.filter(i => i.state === 'CLOSED').length,
};

console.log(JSON.stringify(result, null, 2));
