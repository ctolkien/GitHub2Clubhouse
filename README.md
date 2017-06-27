# GitHub2Clubhouse
Cross platform tool to migrate issues and wireup Clubhouse.io

## What's it do?

* Converts all your open issues from a specified GitHub repository to Clubhouse stories. Including comments.
* Creates Clubhouse Epics to align with any GitHub milestones
* Preserves item creation dates 
* Maps GitHub users to their corresponding ClubHouse users where appropriate. In the case where the usernames do not align, there is a usermapping.txt file which can be used for manual mapping.
* Can also automate creating the Clubhouse webhook in the GitHub repository.

![Sample image](https://raw.githubusercontent.com/ctolkien/GitHub2Clubhouse/master/image.png)

## Requirements

* [.NET Core](https://www.microsoft.com/net/download/core)

## Running

* Clone the repository
* `dotnet restore`
* `dotnet run -p src/GitHub2Clubhouse.csproj`

