using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClubHouse;
using ClubHouse.Models;

namespace GitHub2Clubhouse
{
    internal class ClubHouseStoryImporter
    {
        private readonly ClubHouseClient _client;
        private IReadOnlyCollection<User> Users;
        private Project Project;
        private IDictionary<string, string> _userMapping;
        private Collection<Story> _stories = new Collection<Story>();
        private List<Epic> _epics = new List<Epic>();
        private bool _fetchedEpics = false;

        public ClubHouseStoryImporter(string apiKey) : this(apiKey, null)
        { }

        public ClubHouseStoryImporter(string apiKey, IDictionary<string, string> userMapping)
        {
            _client = new ClubHouseClient(apiKey);
            _userMapping = userMapping;
        }

        public async Task<IReadOnlyCollection<User>> PopulateUsers()
        {
            Users = await _client.Users.List();
            return Users;
        }

        public async Task<Project> PopulateProject(string name)
        {
            Project = (await _client.Projects.List()).FirstOrDefault(x => x.Name == name);
            if (Project == null)
            {
                throw new ArgumentException($"Couldn't find a Clubhouse project with the name {name}");
            }
            return Project;
        }

        public async Task<(Story story, int epicId)> ConvertToStory(Octokit.Issue issue, GitHubIssueListProvider ghProvider)
        {
            var story = new Story
            {
                CreatedAt = issue.CreatedAt.DateTime,
                Name = issue.Title,
                Description = issue.Body,
                ProjectId = Project.Id,
            };

            story.RequestedById = GitHubUser2ClubHouseUser(issue.User);
            story.OwnerIds = issue.Assignees.Select(GitHubUser2ClubHouseUser).ToList();

            var clubHouseEpicId = await GetOrCreateClubhouseEpic(issue);
            if (clubHouseEpicId > 0)
            {
                story.EpicId = clubHouseEpicId;
            }

            if (issue.Comments > 0)
            {
                foreach (var comment in await ghProvider.GetIssueComments(issue.Number))
                {
                    story.Comments.Add(new Comment
                    {
                        AuthorId = GitHubUser2ClubHouseUser(comment.User),
                        CreatedAt = comment.CreatedAt.DateTime,
                        Text = comment.Body
                    });
                }
            }
            _stories.Add(story);
            return (story, clubHouseEpicId);
        }

        private async Task<int> GetOrCreateClubhouseEpic(Octokit.Issue issue)
        {
            if (issue.Milestone == null)
            {
                return 0;
            }
            if (!_fetchedEpics)
            {
                _epics = (await _client.Epics.List()).ToList();
            }

            var directHit = _epics.FirstOrDefault(x => x.Name.Equals(issue.Milestone.Title));
            if (directHit != null)
            {
                return directHit.Id;
            }

            //else, we don't have an epic with this details....
            var newEpic = new Epic
            {
                Name = issue.Milestone.Title,
                CreatedAt = issue.Milestone.CreatedAt.DateTime,
                //deadline
                Description = issue.Milestone.Description,

            };
            if (issue.Milestone.DueOn.HasValue)
            {
                newEpic.Deadline = issue.Milestone.DueOn.Value.DateTime;
            }
            await _client.Epics.Create(newEpic);
            return newEpic.Id;

        }

        public async Task BatchLoadStories()
        {
            await _client.Stories.Create(_stories);
        }

        private string GitHubUser2ClubHouseUser(Octokit.User githubUser)
        {
            return GitHubUser2ClubHouseUser(githubUser.Login);
        }

        private string GitHubUser2ClubHouseUser(string githubUser)
        {
            var directHit = Users.SingleOrDefault(x => x.Username.Equals(githubUser, StringComparison.OrdinalIgnoreCase));
            if (directHit != null) return directHit.Id;

            if (_userMapping != null && _userMapping.TryGetValue(githubUser, out string foundUsername))
            {
                var foundUser = Users.SingleOrDefault(x => x.Username.Equals(foundUsername, StringComparison.OrdinalIgnoreCase));
                if (foundUser != null) return foundUser.Id;
            }

            //No hits...
            return string.Empty;
        }
    }
}
