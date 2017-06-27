using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GitHub2Clubhouse
{
    class Program
    {
        static void Main(string[] args)
        {
            //Here, we collect all the information from the user.
            Console.Write("Enter your GitHub organisation name: ");
            var githubOrg = Console.ReadLine().Trim();

            Console.Write("Enter your GitHub repository name: ");
            var githubRepo = Console.ReadLine().Trim();

            Console.WriteLine("Enter your GitHub Personal Access Token: ");
            var githubPersonalAccessToken = Console.ReadLine().Trim();

            Console.WriteLine("Enter your Clubhouse API Key: ");
            var clubhouseKey = Console.ReadLine().Trim();

            Console.WriteLine("Enter the name of the Clubhouse Project you'd like to import to: ");
            var clubhouseProjectName = Console.ReadLine().Trim();

            Console.WriteLine("If you'd like to configure the Clubhouse webhook for this repository, enter the URL provided by Clubhouse here (otherwise just press enter): ");
            var clubhouseWebhookUrl = Console.ReadLine().Trim();

            var userMapping = new Dictionary<string, string>();
            if (File.Exists("usermapping.txt"))
            {
                Console.WriteLine("Found usermapping.txt...");
                foreach (var line in File.ReadAllLines("usermapping.txt"))
                {
                    var splitValues = line.Split(',');
                    userMapping.Add(splitValues[0], splitValues[1]);
                    Console.WriteLine($"Applying mapping from GitHub User: {splitValues[0]} to Clubhouse User: {splitValues[1]}");
                }
            }
            else
            {
                Console.WriteLine("We could not find a usermapping.txt file - we'll make our best guess to line users up by their username. To configure this, see usermapping.sample.txt");
            }

            Task.Run(async () =>
            {
                await Bootstrapper(
                    githubPersonalAccessToken,
                    githubOrg,
                    githubRepo,
                    clubhouseKey,
                    clubhouseProjectName,
                    userMapping,
                    clubhouseWebhookUrl);
            }).GetAwaiter().GetResult();
        }

        private static async Task Bootstrapper(
            string githubPersonalAccessToken,
            string githubOrg,
            string githubRepo,
            string clubhouseKey,
            string clubhouseProjectName,
            IDictionary<string, string> userMapping,
            string clubhouseWebhookUrl)
        {
            var clubhouse = new ClubHouseStoryImporter(clubhouseKey, userMapping);
            var project = await clubhouse.PopulateProject(clubhouseProjectName);
            Console.WriteLine($"Found Clubhouse Project: {project.Name}");
            var users = await clubhouse.PopulateUsers();
            Console.WriteLine($"Found Clubhouse Users: {users.Count}");
            Console.WriteLine("Fetching Issues...");
            var githubProvider = new GitHubIssueListProvider(githubPersonalAccessToken, githubOrg, githubRepo);

            if (!string.IsNullOrEmpty(clubhouseWebhookUrl))
            {
                await githubProvider.ConfigureWebHook(clubhouseWebhookUrl);
            }

            var issues = await githubProvider.GetIssues();
            Console.WriteLine($"Found {issues.Count} issues for repository {githubRepo}");
            Console.WriteLine("Converting to Clubhouse Stories...");
            var epicCounter = 0;
            for (int i = 0; i < issues.Count; i++)
            {
                DrawTextProgressBar(i + 1, issues.Count);
                var creation = await clubhouse.ConvertToStory(issues[i], githubProvider);
                if (creation.epicId > 0)
                {
                    epicCounter++;
                }
            }
            Console.WriteLine("");
            if (epicCounter > 0)
            {
                Console.WriteLine($"We've created or aligned {epicCounter} Epics to match GitHub milestones");
            }
            Console.WriteLine("All set, are you sure you'd like to proceed? [Y/N] ");
            Console.WriteLine("");

            var proceed = Console.ReadKey();
            if (proceed.Key == ConsoleKey.Y)
            {
                Console.WriteLine("Bulk Importing Stories...");
                await clubhouse.BatchLoadStories();
            }
            else
            {
                Console.WriteLine("Exiting...");
            }
        }

        private static void DrawTextProgressBar(int progress, int total)
        {
            //draw empty progress bar
            Console.CursorLeft = 0;
            Console.Write("["); //start
            Console.CursorLeft = 32;
            Console.Write("]"); //end
            Console.CursorLeft = 1;
            float onechunk = 30.0f / total;

            //draw filled part
            int position = 1;
            for (int i = 0; i < onechunk * progress; i++)
            {
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw unfilled part
            for (int i = position; i <= 31; i++)
            {
                Console.BackgroundColor = ConsoleColor.Green;
                Console.CursorLeft = position++;
                Console.Write(" ");
            }

            //draw totals
            Console.CursorLeft = 35;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write(progress.ToString() + " of " + total.ToString() + "    "); //blanks at the end remove any excess
        }
    }
}