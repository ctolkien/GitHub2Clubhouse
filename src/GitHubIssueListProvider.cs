using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace GitHub2Clubhouse
{
    internal class GitHubIssueListProvider
    {
        protected string ProductHeaderValue = "GitHub2ClubHouse";
        private readonly GitHubClient _client;
        internal IReadOnlyList<Issue> Issues;
        internal IReadOnlyList<IssueComment> IssueComments;

        private readonly string _org;
        private readonly string _repo;

        public GitHubIssueListProvider(string personalAccessToken, string org, string repo)
        {
            _client = new GitHubClient(new ProductHeaderValue(ProductHeaderValue));
            _client.Credentials = new Credentials(personalAccessToken);
            _org = org;
            _repo = repo;
        }

        public async Task<IReadOnlyList<Issue>> GetIssues()
        {
            Issues = await _client.Issue.GetAllForRepository(_org, _repo, new RepositoryIssueRequest
            {
                Filter = IssueFilter.All,
                State = ItemStateFilter.Open
            });

            return Issues;
        }
        public async Task<IReadOnlyList<IssueComment>> GetIssueComments()
        {
            IssueComments = await _client.Issue.Comment.GetAllForRepository(_org, _repo);

            return IssueComments;
        }

        public async Task<IReadOnlyList<IssueComment>> GetIssueComments(int issueId)
        {
            return await _client.Issue.Comment.GetAllForIssue(_org, _repo, issueId);
        }

        public async Task ConfigureWebHook(string chHookUrl)
        {
            var allHooks = await _client.Repository.Hooks.GetAll(_org, _repo);
            if (allHooks.Any(x => x.Url != chHookUrl))
            {
                var config = new Dictionary<string, string>
            {
                { "url", chHookUrl },
                { "content_type", WebHookContentType.Json.ToString() },
                { "secret", "" },
                { "insecure_ssl", "false" }
            };

                var webhook = new NewRepositoryHook("web", config)
                {
                    Events = new List<string> { "*" }
                };

                await _client.Repository.Hooks.Create(_org, _repo, webhook);
            }
        }
    }
}
