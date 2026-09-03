# legends-legacy

## Equipment and Forge documentation

- [Design and implementation requirements](docs/design/equipment-specification.md)
- [Implementation status and verification](docs/design/equipment-implementation-status.md)
- [Post-Alpha cleanup and database startup](docs/design/equipment-post-alpha-cleanup.md)
- [Historical implementation review](docs/design/equipment-implementation-review.md)
- [Current naming and storage contracts](docs/engineering/equipment-naming-and-compatibility.md)
- [Meran / Tier 2 implementation and transition](docs/design/equipment-region-two-progression.md)
- [Meran PvE balance and Forge pacing](docs/design/equipment-meran-pve-balance-report.md)
- [Equipment reference builds](docs/content-balancing/equipment-reference-builds.md)

Equipment and Forge are the current player equipment path. The five equipment capabilities default to enabled. Crafting, gathering, queued tempering and Alpha compatibility paths have been removed. Shenic / Tier 1 and Meran / Tier 2 now have ordinary drops, dungeon targets, recovery and Forge pricing. The status document tracks balance and player acceptance; the cleanup document explains the generated migrations, including removal of obsolete saved quest progress. The API applies pending migrations on startup.

## Backend Requirements:

- Visual Studio (Recommended)
- .NET 10 SDK
- MSSQL Express
- Azure Data Studio (Recommended tool for managing Database)

## Frontend Requirements:

- Node.js (Latest LTS version recommended)
- Angular CLI (npm install -g @angular/cli)
- VSCode (Recommended)

## Guide

### Install Dependencies

Ensure all required software is installed before proceeding.

### Backend Setup

Open the backend solution found at /legends-legacy/LL/LegendsLegacy.sln project in Visual Studio.

#### Install DOTNET and Apply migrations:

Run the following to install dotnet.<br>
This can be done from your Package Manager Console, found in VS.<br>
`dotnet tool install --global dotnet-ef`

Then run the following to apply the latest migrations and set up the database<br>
`dotnet ef database update`

Verify database connection using Azure Data Studio (Recommended) or SQL Server Management Studio.

### Frontend Setup

Navigate to the frontend directory at /legends-legacy/LL/src/Presentation/ll/

Open VSCode from this path.<br>
Open a new terminal directly in VSCode

#### Install dependencies:

`npm install`

#### Start the development server:

`ng serve -o`
